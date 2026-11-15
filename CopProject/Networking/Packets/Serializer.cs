// Author = Srikar
// Roll Number = 142301013

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Networking.Packets;

/// <summary>
/// JSON converter for <see cref="IPEndPoint"/> instances.
/// </summary>
public class IPEndPointConverter : JsonConverter<IPEndPoint>
{
    public override IPEndPoint? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? str = reader.GetString();
        return str != null ? IPEndPoint.Parse(str) : null;
    }

    public override void Write(Utf8JsonWriter writer, IPEndPoint value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>
/// JSON-based serializer and deserializer for type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of object to serialize.</typeparam>
public class Serializer<T> : ISerializer<T>
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="Serializer{T}"/> class.
    /// </summary>
    public Serializer()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new IPEndPointConverter());
    }

    /// <summary>
    /// Serializes an object to a UTF-8 encoded JSON byte array.
    /// </summary>
    /// <param name="item">The object to serialize.</param>
    /// <returns>A byte array containing the JSON payload.</returns>
    public byte[] Serialize(T item)
    {
        if (item == null)
        {
            return Array.Empty<byte>();
        }
        return JsonSerializer.SerializeToUtf8Bytes(item, _options);
    }

    /// <summary>
    /// Deserializes a UTF-8 encoded JSON byte array into an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="data">The JSON byte array to deserialize.</param>
    /// <returns>The deserialized object, or default if data is empty or invalid.</returns>
    public T? Deserialize(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return default;
        }
        return JsonSerializer.Deserialize<T>(data, _options);
    }
}
