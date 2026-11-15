// Author = Srikar
// Roll Number = 142301013

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking.Packets;

/// <summary>
/// Defines generic serialization and deserialization operations.
/// </summary>
/// <typeparam name="T">The type of object to serialize.</typeparam>
public interface ISerializer<T>
{
    /// <summary>
    /// Serializes an object into a byte array.
    /// </summary>
    /// <param name="item">The object to serialize.</param>
    /// <returns>A byte array representing the serialized object.</returns>
    byte[] Serialize(T item);

    /// <summary>
    /// Deserializes a byte array into an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="data">The serialized byte array.</param>
    /// <returns>The deserialized object, or default value if invalid.</returns>
    T? Deserialize(byte[] data);
}
