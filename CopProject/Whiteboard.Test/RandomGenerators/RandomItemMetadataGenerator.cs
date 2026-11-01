// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Whiteboard.Network.Dtos;

namespace Whiteboard.Test.RandomGenerators;

/// <summary>
/// Generates a random <see cref="ItemDto"/>
/// </summary>
[ExcludeFromCodeCoverage]
public class RandomItemMetadataGenerator
{
    /// <summary>
    /// Random number generator
    /// </summary>
    private static readonly Random s_random = new();

    /// <summary>
    /// Allowed characters in the username
    /// </summary>
    private static readonly string s_allowed_chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-";

    /// <summary>
    /// Generate a random <see cref="ItemDto"/>
    /// </summary>
    /// <returns>A random <see cref="ItemDto"/></returns>
    public static ItemMetadata Generate()
    {
        // 1. Generate a GUID
        Guid id = Guid.NewGuid();

        // 2. Generate a random z-index
        double zIndex = s_random.NextDouble() * 100;

        // 3. Generate a random string for creator
        int stringLength = s_random.Next(34);
        string createdBy = s_random.GetString(s_allowed_chars, stringLength);

        // 4. get current time
        DateTime currentTime = DateTime.Now;
        DateTime createdDateTime = currentTime;
        DateTime modifiedDateTime = currentTime;

        // 5. Generate a rando orientation
        double orientation = s_random.NextDouble() * 360;

        // 6. Generate item metadata
        ItemMetadata metaData = new(id, zIndex, createdBy, createdDateTime, modifiedDateTime, orientation);
        return metaData;
    }
}
