// Author = Dheeraj Ram
// Roll Number = 142301015

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Networking.Packets;

namespace Networking.Queues;

/// <summary>
/// Priority-based sending queue with anti-starvation scheduling.
/// </summary>
public class SendingQueue : Queue
{
    public const int DefaultMediumWeightRatio = 3;
    public const int DefaultMaxHighBurst = 4;
    public const int DefaultMaxStarvationLimit = 10;

    private readonly ConcurrentQueue<Packet> _highQueue;
    private readonly ConcurrentQueue<Packet> _mediumQueue;
    private readonly ConcurrentQueue<Packet> _lowQueue;

    private readonly object _dequeueLock;

    private readonly int _mediumWeightRatio;
    private readonly int _maxHighBurst;
    private readonly int _maxStarvationLimit;

    private int _consecutiveHigherPriorityCount;
    private int _highBurstCount;
    private int _mediumServedCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="SendingQueue"/> class.
    /// </summary>
    /// <param name="mediumWeightRatio">Number of medium-priority packets served before yielding.</param>
    /// <param name="maxHighBurst">Maximum consecutive high-priority packets allowed before yielding.</param>
    /// <param name="maxStarvationLimit">Maximum consecutive higher-priority dispatches before forcing low-priority dispatch.</param>
    public SendingQueue(int mediumWeightRatio = DefaultMediumWeightRatio,
                        int maxHighBurst = DefaultMaxHighBurst,
                        int maxStarvationLimit = DefaultMaxStarvationLimit)
    {
        _highQueue = new ConcurrentQueue<Packet>();
        _mediumQueue = new ConcurrentQueue<Packet>();
        _lowQueue = new ConcurrentQueue<Packet>();

        _dequeueLock = new object();

        _mediumWeightRatio = mediumWeightRatio;
        _maxHighBurst = maxHighBurst;
        _maxStarvationLimit = maxStarvationLimit;

        _consecutiveHigherPriorityCount = 0;
        _highBurstCount = 0;
        _mediumServedCount = 0;
    }

    /// <summary>
    /// Normalizes an integer priority value to a <see cref="Priority"/> enum value.
    /// </summary>
    /// <param name="priority">The raw priority integer.</param>
    /// <returns>The normalized <see cref="Priority"/>.</returns>
    public static Priority NormalizePriority(int priority)
    {
        if (priority >= (int)Priority.High)
        {
            return Priority.High;
        }

        if (priority <= (int)Priority.Low)
        {
            return Priority.Low;
        }

        return Priority.Medium;
    }

    private static readonly Dictionary<int, string> s_moduleIdToNameMap = new()
    {
        { 1, "Controller" },
        { 5, "FileSynchroniser" },
        { 6, "Chat" },
        { 7, "Whiteboard" },
        { 8, "ScreenShare" },
        { 13, "Networking" }
    };

    private static string GetModuleName(int moduleId)
    {
        return s_moduleIdToNameMap.TryGetValue(moduleId, out string? name) ? $"{name} ({moduleId})" : $"Module-{moduleId}";
    }

    private string GetQueueSnapshot()
    {
        return $"Queues: [H:{_highQueue.Count}, M:{_mediumQueue.Count}, L:{_lowQueue.Count}, Total:{Size()}] | Metrics: [Burst:{_highBurstCount}/{_maxHighBurst}, MedServed:{_mediumServedCount}/{_mediumWeightRatio}, StarveCount:{_consecutiveHigherPriorityCount}/{_maxStarvationLimit}]";
    }

    /// <summary>
    /// Enqueues a packet into the appropriate priority sub-queue.
    /// </summary>
    /// <param name="packet">The packet to enqueue.</param>
    public override void Enqueue(Packet packet)
    {
        if (packet == null)
        {
            Trace.TraceWarning($"[Networking] SendingQueue.Enqueue: Cannot enqueue null packet | {GetQueueSnapshot()}");
            return;
        }

        Priority priority = NormalizePriority(packet.Priority);

        switch (priority)
        {
            case Priority.High:
                _highQueue.Enqueue(packet);
                break;
            case Priority.Medium:
                _mediumQueue.Enqueue(packet);
                break;
            case Priority.Low:
                _lowQueue.Enqueue(packet);
                break;
        }

        int payloadBytes = packet.SerializedData?.Length ?? 0;
        Trace.TraceInformation(
            $"[Networking] SendingQueue.Enqueue: Priority: {priority} | Module: {GetModuleName(packet.Module)} | Destination: {packet.Destination} | PayloadSize: {payloadBytes}B | {GetQueueSnapshot()}");

        SignalItemAdded();
    }

    /// <summary>
    /// Dequeues the next packet according to priority and fairness policies.
    /// </summary>
    /// <returns>The dequeued packet, or null if the queue is empty.</returns>
    public override Packet? Dequeue()
    {
        lock (_dequeueLock)
        {
            if (_consecutiveHigherPriorityCount >= _maxStarvationLimit &&
                _lowQueue.TryDequeue(out Packet? starvedPacket))
            {
                int priorStarveCount = _consecutiveHigherPriorityCount;
                _consecutiveHigherPriorityCount = 0;
                _mediumServedCount = 0;
                int payloadBytes = starvedPacket.SerializedData?.Length ?? 0;
                Trace.TraceInformation(
                    $"[Networking] SendingQueue.Dequeue: Priority: Low | Reason: Anti-starvation threshold reached ({priorStarveCount} >= {_maxStarvationLimit}) | Module: {GetModuleName(starvedPacket.Module)} | Destination: {starvedPacket.Destination} | PayloadSize: {payloadBytes}B | {GetQueueSnapshot()}");
                return starvedPacket;
            }

            bool allowHigh = _highBurstCount < _maxHighBurst || (_mediumQueue.IsEmpty && _lowQueue.IsEmpty);
            if (allowHigh && _highQueue.TryDequeue(out Packet? highPacket))
            {
                string reason = _highBurstCount < _maxHighBurst
                    ? $"High burst ({_highBurstCount + 1}/{_maxHighBurst})"
                    : "High beyond burst limit (Medium and Low queues empty)";

                _highBurstCount++;
                if (!_lowQueue.IsEmpty)
                {
                    _consecutiveHigherPriorityCount++;
                }

                int payloadBytes = highPacket.SerializedData?.Length ?? 0;
                Trace.TraceInformation(
                    $"[Networking] SendingQueue.Dequeue: Priority: High | Reason: {reason} | Module: {GetModuleName(highPacket.Module)} | Destination: {highPacket.Destination} | PayloadSize: {payloadBytes}B | {GetQueueSnapshot()}");
                return highPacket;
            }

            bool allowMedium = _mediumServedCount < _mediumWeightRatio || _lowQueue.IsEmpty;
            if (allowMedium && _mediumQueue.TryDequeue(out Packet? mediumPacket))
            {
                string reason = _mediumServedCount < _mediumWeightRatio
                    ? $"Medium served ({_mediumServedCount + 1}/{_mediumWeightRatio})"
                    : "Medium beyond ratio (Low queue empty)";
                if (_highBurstCount >= _maxHighBurst)
                {
                    reason += $" (yielded after {_highBurstCount} High burst)";
                }

                _mediumServedCount++;
                _highBurstCount = 0;
                if (!_lowQueue.IsEmpty)
                {
                    _consecutiveHigherPriorityCount++;
                }

                int payloadBytes = mediumPacket.SerializedData?.Length ?? 0;
                Trace.TraceInformation(
                    $"[Networking] SendingQueue.Dequeue: Priority: Medium | Reason: {reason} | Module: {GetModuleName(mediumPacket.Module)} | Destination: {mediumPacket.Destination} | PayloadSize: {payloadBytes}B | {GetQueueSnapshot()}");
                return mediumPacket;
            }

            if (_lowQueue.TryDequeue(out Packet? lowPacket))
            {
                string reason = (_highQueue.IsEmpty && _mediumQueue.IsEmpty)
                    ? "Higher queues empty"
                    : "Higher queues yielded/exhausted";

                _mediumServedCount = 0;
                _highBurstCount = 0;
                _consecutiveHigherPriorityCount = 0;

                int payloadBytes = lowPacket.SerializedData?.Length ?? 0;
                Trace.TraceInformation(
                    $"[Networking] SendingQueue.Dequeue: Priority: Low | Reason: {reason} | Module: {GetModuleName(lowPacket.Module)} | Destination: {lowPacket.Destination} | PayloadSize: {payloadBytes}B | {GetQueueSnapshot()}");
                return lowPacket;
            }

            Trace.TraceWarning($"[Networking] SendingQueue.Dequeue: Queue is empty | {GetQueueSnapshot()}");
            return null;
        }
    }

    /// <summary>
    /// Peeks at the next packet that would be dequeued.
    /// </summary>
    /// <returns>The next packet, or null if the queue is empty.</returns>
    public override Packet? Peek()
    {
        lock (_dequeueLock)
        {
            if (_consecutiveHigherPriorityCount >= _maxStarvationLimit &&
                _lowQueue.TryPeek(out Packet? starvedPacket))
            {
                int payloadBytes = starvedPacket.SerializedData?.Length ?? 0;
                Trace.TraceInformation(
                    $"[Networking] SendingQueue.Peek: Priority: Low | Reason: Anti-starvation next ({_consecutiveHigherPriorityCount} >= {_maxStarvationLimit}) | Module: {GetModuleName(starvedPacket.Module)} | Destination: {starvedPacket.Destination} | PayloadSize: {payloadBytes}B | {GetQueueSnapshot()}");
                return starvedPacket;
            }

            bool allowHigh = _highBurstCount < _maxHighBurst || (_mediumQueue.IsEmpty && _lowQueue.IsEmpty);
            if (allowHigh && _highQueue.TryPeek(out Packet? highPacket))
            {
                string reason = _highBurstCount < _maxHighBurst
                    ? $"High burst next ({_highBurstCount + 1}/{_maxHighBurst})"
                    : "High beyond burst next (Medium and Low queues empty)";

                int payloadBytes = highPacket.SerializedData?.Length ?? 0;
                Trace.TraceInformation(
                    $"[Networking] SendingQueue.Peek: Priority: High | Reason: {reason} | Module: {GetModuleName(highPacket.Module)} | Destination: {highPacket.Destination} | PayloadSize: {payloadBytes}B | {GetQueueSnapshot()}");
                return highPacket;
            }

            bool allowMedium = _mediumServedCount < _mediumWeightRatio || _lowQueue.IsEmpty;
            if (allowMedium && _mediumQueue.TryPeek(out Packet? mediumPacket))
            {
                string reason = _mediumServedCount < _mediumWeightRatio
                    ? $"Medium next ({_mediumServedCount + 1}/{_mediumWeightRatio})"
                    : "Medium beyond ratio next (Low queue empty)";

                int payloadBytes = mediumPacket.SerializedData?.Length ?? 0;
                Trace.TraceInformation(
                    $"[Networking] SendingQueue.Peek: Priority: Medium | Reason: {reason} | Module: {GetModuleName(mediumPacket.Module)} | Destination: {mediumPacket.Destination} | PayloadSize: {payloadBytes}B | {GetQueueSnapshot()}");
                return mediumPacket;
            }

            if (_lowQueue.TryPeek(out Packet? lowPacket))
            {
                string reason = (_highQueue.IsEmpty && _mediumQueue.IsEmpty)
                    ? "Higher queues empty"
                    : "Higher queues yielded/exhausted";

                int payloadBytes = lowPacket.SerializedData?.Length ?? 0;
                Trace.TraceInformation(
                    $"[Networking] SendingQueue.Peek: Priority: Low | Reason: {reason} | Module: {GetModuleName(lowPacket.Module)} | Destination: {lowPacket.Destination} | PayloadSize: {payloadBytes}B | {GetQueueSnapshot()}");
                return lowPacket;
            }

            Trace.TraceWarning($"[Networking] SendingQueue.Peek: Queue is empty | {GetQueueSnapshot()}");
            return null;
        }
    }

    /// <summary>
    /// Clears all packets from all priority queues and resets counters.
    /// </summary>
    public override void Clear()
    {
        lock (_dequeueLock)
        {
            Trace.TraceInformation(
                $"[Networking] SendingQueue.Clear: Flushing all sub-queues | {GetQueueSnapshot()}");
            _highQueue.Clear();
            _mediumQueue.Clear();
            _lowQueue.Clear();

            _consecutiveHigherPriorityCount = 0;
            _highBurstCount = 0;
            _mediumServedCount = 0;
            ResetItemAdded();

            Trace.TraceInformation(
                $"[Networking] SendingQueue.Clear: Cleared all sub-queues and reset metrics | {GetQueueSnapshot()}");
        }
    }

    /// <inheritdoc/>
    public override int Size()
    {
        return _highQueue.Count + _mediumQueue.Count + _lowQueue.Count;
    }

    /// <inheritdoc/>
    public override bool IsEmpty()
    {
        return _highQueue.IsEmpty && _mediumQueue.IsEmpty && _lowQueue.IsEmpty;
    }

    public int HighCount => _highQueue.Count;
    public int MediumCount => _mediumQueue.Count;
    public int LowCount => _lowQueue.Count;
}
