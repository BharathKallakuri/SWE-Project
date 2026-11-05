// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.UI.Xaml.Input;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the current state of the view model, which is dependent on the currently chosen tool
/// </summary>
public interface IViewModelState
{
    /// <summary>
    /// Called every time the pointer is pressed on the canvas
    /// </summary>
    void PointerPressed(object sender, PointerRoutedEventArgs e);

    /// <summary>
    /// Called every time the captured pointer is moved on the canvas
    /// </summary>
    void PointerMoved(object sender, PointerRoutedEventArgs e);

    /// <summary>
    /// Called every time the captured pointer is released on the canvas
    /// </summary>
    void PointerReleased(object sender, PointerRoutedEventArgs e);

    /// <summary>
    /// Called every time the captured pointer is lost
    /// </summary>
    void PointerCaptureLost(object sender, PointerRoutedEventArgs e);
}
