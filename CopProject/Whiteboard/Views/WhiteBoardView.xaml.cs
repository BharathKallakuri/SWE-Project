// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Whiteboard.ViewModels;

namespace Whiteboard.Views;

public sealed partial class WhiteBoardView : UserControl
{
    private readonly WhiteBoardViewModel _viewModel;

    public WhiteBoardView(WhiteBoardViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();

        _viewModel = viewModel;

        ToolbarInfo toolbarInfo = new(new SolidColorBrush(Colors.Black), 3, new SolidColorBrush(Colors.Red));
        _viewModel.InitializeViewModel(WhiteboardCanvas, toolbarInfo);
    }
}
