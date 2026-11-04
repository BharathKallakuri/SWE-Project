// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.UI.Xaml;
using Whiteboard.Client;
using Whiteboard.Test.Mocks.Networking;
using Whiteboard.ViewModels;
using Whiteboard.Views;

namespace Whiteboard.WinUI3Harness;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // 1. Create client from network/controller
        IWhiteboardClient client = WhiteboardFactory.CreateClientWhiteboard(
            new MockCommunicatorFactory().CreateClientCommunicator(),
            new(IPAddress.Loopback, 40000),
            "Username"
        );

        var viewModel = new WhiteBoardViewModel(client);

        var view = new WhiteBoardView(viewModel);

        this.Content = view;
    }
}
