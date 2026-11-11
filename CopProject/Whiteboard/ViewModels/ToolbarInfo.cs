/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using System.Diagnostics.CodeAnalysis;
using Windows.UI;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the currently selected tooltip options
/// </summary>
/// <param name="BorderColor">Brush for borders</param>
/// <param name="BorderWidth">Stroke thickness</param>
/// <param name="FillColor">Brush for fill</param>
[ExcludeFromCodeCoverage]
public record ToolbarInfo(
    Color BorderColor,
    double BorderWidth,
    Color FillColor
);
