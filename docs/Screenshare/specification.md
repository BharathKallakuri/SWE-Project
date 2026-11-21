# ScreenShare Specification

## Overview

ScreenShare lets anyone in a meeting share their screen, and lets everyone else watch one or more shared screens at the same time. Shared screens show up as small tiles in a grid. Clicking a tile opens it in a bigger view.

To keep network use low, we don't send the whole screen every time. The screen is split into small square tiles, and only the tiles that changed since the last frame are sent. The viewer keeps the last picture and paints the new tiles on top of it.

## Features

- **Tile based change detection:** Each captured frame is split into 64×64 pixel tiles. Every tile is compared with the same tile in the last frame we sent. Only the tiles that changed are sent.
- **Tile hashes:** Every changed tile carries a 64-bit hash (FNV-1a) of its pixels along with its position and image data.
- **Full frame fallback:** If 50% or more of the screen changed, we send the whole frame instead of lots of separate tiles. Even when less than half changed, if the changed tiles add up to more than 16 KB we also encode the whole frame and send whichever of the two is smaller (this helps with things like scrolling a page).
- **Holding back tiny changes:** If only 1% of the screen or less changed (for example a blinking cursor or a hover highlight), we don't send it right away. The change is kept and sent together with later changes, or after 12 frames (about half a second), whichever comes first. So small changes are delayed a little but never lost.
- **Pulse for a still screen:** If nothing changed, a small "pulse" packet with no image is sent instead. Pulses also get a frame number, so the viewer can tell a still screen apart from a lost packet.
- **Frame numbers:** Every packet has a frame number and a capture time (in UTC). The viewer uses these to ignore old or out of order packets.
- **Stitching on the viewer side:** The viewer keeps the last full picture for each stream and paints changed tiles onto it. A set of tiles is applied  together or not at all, so a broken tile cant leave the picture half updated.
- **Two streams per sharer:** Each shared screen is sent as two streams:
  - a **full stream** (up to 480p, up to 30 FPS, 64×64 tiles) for the zoomed view, and
  - a **small grid stream** (180p, 5 FPS, 16×16 tiles) for the grid tiles.

  Viewers get the small stream while the tile is small, and switch to the full stream when they zoom in. The tile keeps showing the old picture until the first frame of the new stream arrives.
- **Only fetching what is visible:** Tiles that scroll out of view are unsubscribed, so their frames stop coming. If nobody is watching one of a sharer's streams, the sharer stops capturing and sending it.
- **Image formats:** Changed tiles are sent as PNG. Full frames are sent as JPEG (quality 75) by default, which is roughly a quarter of the PNG size. Setting the quality to 0 sends full frames as PNG instead.
- **Handling slow viewers:** The sharer can only have 8 frames out without an acknowledgement (ack). If no ack comes back within 2 seconds, it assumes frames were lost and starts again with a full frame. A viewer that falls more than 5 frames behind stops getting changed tiles, and gets one full frame once it catches up. A viewer that doesn't ack anything for 10 seconds is dropped (this catches clients that crashed).
- **Screen size changes:** If the sharer's screen size changes, a full frame is sent. Captures taller than the max height are scaled down and keep their aspect ratio.
- **Grid UI:** Shared screens are shown as tiles in a scrollable WinUI grid. Clicking a tile opens it in a zoomed view, with a "Back to grid" button. The zoomed view shows the current FPS. A toggle button starts and stops your own sharing.
- **Smooth UI:** If the UI falls behind, only the newest frame for each stream is drawn and older ones are skipped, so frames dont pile up.

## Architecture & Workflow

### 1. Capture and processing (sharer)

1. The screen is captured with GDI at a fixed rate (30 FPS max) and scaled down to the max height (480p by default). One capture feeds both the full and the small stream.
2. The new frame is compared with the last frame that was sent tile by tile.
3. Based on how much changed, the result is one of: nothing changed (pulse), a small change (held back for now), some tiles changed (delta), or most of the screen changed (full frame).
4. The result is turned into a numbered data packet and handed to the networking module.
5. The very first frame of a stream is always a full frame, since the viewer has nothing to build on yet.

### 2. Relay through the host

The current Networking module only lets clients talk to the host. So right now all ScreenShare traffic goes through the meeting host:

- The sharer sends its packets to the host.
- The host forwards them as they are to everyone subscribed to that stream. It never decodes the images, so relaying many streams stays cheap.
- The host acks each frame back to the sharer and keeps track of how far behind each viewer is.
- When a viewer joins or loses its picture, the host asks the sharer for a full frame. If several viewers join at once, they share one request.
- The host tells each sharer who is watching each of its streams (the send-to list). If nobody is watching, the sharer pauses that stream.
- The host's own sharing and viewing talk to the relay directly inside the app, since the host can't send to itself over the network.

### 3. Receiving and showing (viewer)

1. The receiver listens for packets on the streams it has subscribed to.
2. A pulse keeps the current picture as it is.
3. Changed tiles are painted onto the stored picture. A full frame replaces it.
4. Every frame is ack, even if it couldn't be used, so the host keeps the pace going.
5. The finished picture is passed to the UI, which draws it in the right tile.

### 4. Meeting integration

When a meeting starts, ScreenShare is created and connected to the view. When the meeting ends, ScreenShare shuts down before the connection is closed, and the view is cleared. If ScreenShare fails to start, the meeting still goes on and the share panel just stays empty.

## Data Structures

### Data packet (`ScreenDataPacket`)

| Field | Type | Description |
| --- | --- | --- |
| StreamId | string | Which stream this is. The small grid stream uses the sharer's id  |
| FrameNumber | long | Frame number, starting from 0 for each stream |
| Timestamp | DateTime (UTC) | When the frame was captured |
| Width, Height | int | Size of the source frame in pixels |
| TileSize | int | Size of a full tile (64 for the full stream, 16 for the grid stream) |
| Type | enum | `FullFrame`, `ChangedTiles` or `Pulse` |
| FullFrameData | byte[] | The whole frame as JPEG or PNG (full frames only) |
| ChangedTiles | list of Tile | The changed tiles (delta packets only) |

### Tile

| Field | Type | Description |
| --- | --- | --- |
| X, Y | int | Top left corner of the tile in pixels |
| Width, Height | int | Tile size. Tiles on the right and bottom edges can be smaller. |
| Hash | ulong | FNV-1a hash of the tile's pixels |
| Data | byte[] | Tile image as PNG |

### Control messages

| Message | Purpose |
| --- | --- |
| Hello | A participant joining asks the host which streams are already running |
| StreamStarted / StreamStopped | Tells everyone a stream started or ended |
| Subscribe / Unsubscribe | A viewer starts or stops watching a stream |
| RequestFullFrame | Asks the sharer to send a full frame next |
| FrameAck | Confirms a frame was received |
| UpdateSendToList | The host tells a sharer who is watching its stream |


## Default settings (`ScreenShareOptions`)

| Setting | Default |
| --- | --- |
| Capture rate | 30 FPS |
| Full stream height | 480p |
| Grid stream height / rate | 180p at 5 FPS |
| Tile size (full / grid) | 64 / 16 pixels |
| Full frame JPEG quality | 75 |
| Max frames without an ack | 8 |
| Ack timeout | 2 seconds |
| Max viewer lag | 5 frames |
| Viewer timeout | 10 seconds |
| Send screen data over UDP | Off (uses TCP for now) |

Screen data goes over TCP for now. Networking sends each UDP message as a single datagram (65,507 bytes max), so full frames and big changes would get dropped. This can be switched on once Networking supports splitting large messages.

## Load Management

All streams currently go through the host, so the host's bandwidth limits how many streams and viewers a meeting can have. A few things keep this load down:

- The host only forwards bytes and never decodes images.
- Grid tiles use the small 5 FPS stream, and only zoomed tiles get the full stream.
- Streams nobody is watching are paused at the sharer.
- Slow viewers are skipped instead of holding everyone else up.

We assume the stream ends when the host leaves the meeting.

The original plan was a peer to peer model, where the host keeps a send-to list for each sharer and the sharer sends directly to its viewers, with the host only stepping in as a relay when needed. The send-to list is already in place, but since Networking clients can't reach each other directly yet, the host does all the relaying for now.

