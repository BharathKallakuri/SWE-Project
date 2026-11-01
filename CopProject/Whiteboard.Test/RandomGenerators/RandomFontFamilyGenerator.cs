// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Media;

namespace Whiteboard.Test.RandomGenerators;

/// <summary>
/// Generates a random <see cref="FontFamily"/>
/// </summary>
[ExcludeFromCodeCoverage]
public class RandomFontFamilyGenerator : BaseGenerator
{
    /// <summary>
    /// Random font families for testing
    /// </summary>
    private static readonly List<FontFamily> s_random_font_families =
        [new FontFamily("Segoe UI"), new FontFamily("Arial"), new FontFamily("Consolas"), new FontFamily("Calibri ")];

    public static FontFamily Generate()
    {
        int randomFamily = Rng.Next(s_random_font_families.Count);
        FontFamily randomColor = s_random_font_families[randomFamily];

        return randomColor;
    }
}
