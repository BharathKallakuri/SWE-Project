// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.UI.Xaml.Media;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the currently selected tooltip options
/// </summary>
/// <param name="BorderBrush">Brush for borders</param>
/// <param name="StrokeThickness">Stroke thickness</param>
/// <param name="FillBrush">Brush for fill</param>
public record ToolbarInfo(
    Brush BorderBrush,
    double StrokeThickness,
    Brush FillBrush
);
