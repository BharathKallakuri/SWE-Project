<!-- Author: Simeon Kandan Sonar -->

# Cloud Networking

This document explains what I have done in Cloud Networking and how I deployed it.

## 1. Deployment

- Used **Microsoft Azure App Services** for hosting.
- Used **Azure for Students** subscription, which restricts available regions. Chose **South Central India**.
- Enabled WebSockets.
- No authentication or authorization implemented.
- Deployed two App Services (F Tier and B Tier).
- Added the `lan-all` launch profile to expose the HTTP server over the local network for testing.

## 2. Main Modules

### 2.1 Cloud (Shared Library)

Created `Cloud.dll` as the shared library. Kept submodules like `Cloud.Networking` and `Cloud.Persistence` physically separated through folders and logically through namespaces.

### 2.2 Cloud.Networking

Implemented the client-side messaging layer.

- **IMessenger:** Created a common interface for connection, sending, receiving and syncing messages.
- **SignalRMessenger:** Implemented SignalR communication with retry logic, automatic reconnection and message syncing.
- **MessengerFactory:** Created a single entry point for messenger creation.
- **DiagnosticMessenger:** Added logging without swallowing exceptions.
- **SilentMessenger:** Added a decorator that swallows exceptions.
- **Message:** Created a shared immutable message record.
- **Contract:** Defined common client-server method names.
- **ConnectionState:** Created our own connection states instead of exposing SignalR types.

Also added `IHubConnection` and `ITracer` abstractions to make testing easier.

### 2.3 Cloud.Networking.Server

Implemented the backend using ASP.NET Core and SignalR.

- **Program:** Configured dependency injection, middleware and endpoints.
- **MessageHub:** Handles message validation, storage, broadcasting and retrieving missed messages.
- **InMemoryMessageStore:** Implemented FIFO message storage with a 10,000-message limit. Tried to make it thread-safe.
- **MonitorHubFilter:** Tracks SignalR connections and method execution.
- **ServerMonitor:** Tracks connected clients and incoming messages.
- **Dashboard:** Added a live dashboard showing connected clients, recent messages and server-side broadcasting.

**Endpoints:**
- `/hub` – SignalR communication.
- `/` – Dashboard.
- `/health` – Health check.

### 2.4 Cloud.UI

Implemented the WinUI notification interface.

- **NotificationServiceView:** Created the notification page.
- **NotificationServiceViewModel:** Handles UI state, notifications, server selection and synchronization.
- **NotificationService:** Acts as the bridge between the UI and networking layer.
- **Commands:** Implemented Connect, Disconnect, Send, Sync and Clear.
- **Models:** Added `NotificationItem` and `ServerPreset`.

**Connection behaviour:**
- Hardcoded two server URLs as presets, with the more powerful server selected by default.
- On the first connection, connects to the selected server and automatically syncs missed messages.
- On reconnection, automatically syncs messages missed during disconnection.
- Manual syncing is also available.

### 2.5 UX Integration

Integrated `Cloud.UI` into the main SecureMeet application.

- Hosts `NotificationServiceView` on the Cloud page.
- Creates the view model using `MessengerFactory.GetMessenger`.
- Disposes the view model when the page is reset.

## 3. Testing Utilities

### 3.1 Cloud.Networking.ClientConsole

Created a console application to test the actual server without opening the UI. Used it to test connections, sending, receiving and reconnection.

### 3.2 Cloud.Networking.UIHarness

Created a standalone WinUI application to test `Cloud.UI` against the actual server without launching the entire SecureMeet application.