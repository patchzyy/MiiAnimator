# Room lobby portraits

These clips are for the very small circular Mii heads in the room list and player list. WheelWizard currently uses a 50 × 50 portrait there. They animate only Head rotation and Expression; the body stays in its rest pose. There are no translations, scale changes, squash, limbs, particles or extra Miis.

Use the existing head portrait camera and crop, facing forward with the head centred in the circle. Keep the camera fixed while a clip plays. Leave enough space around the hair for a 7° head turn and a 2.5° tilt. Preview at the actual icon size; a full-body camera will hide these small gestures. Use each player's own Mii, including its own eyes and mouth.

| File | Duration at 60 fps | Playback | Gesture / use |
| --- | --- | --- | --- |
| `Lobby_idle_blink.miianim` | 8 s | Loop | Default quiet idle: brief blinks and a tiny relaxed settle. |
| `Lobby_idle_glance_left.miianim` | 10 s | Loop | One gentle left glance, followed by a long neutral pause. |
| `Lobby_idle_glance_right.miianim` | 10 s | Loop | Mirrored glance for the opposite side or another player. |
| `Lobby_idle_listen.miianim` | 9 s | Loop | Small listening tilt, a calm hold, then a slow return. |
| `Lobby_host_nod.miianim` | 2.8 s | Once | A single modest host acknowledgement, keeping the Mii's own face. |
| `Lobby_greeting.miianim` | 3.2 s | Once | Small turn and two soft greeting nods with a brief smile. |
| `Lobby_ready_nod.miianim` | 2.6 s | Once | Gentle confirmation when a player becomes ready. |
| `Lobby_state_notice.miianim` | 3 s | Once | A brief attentive lift and glance when room state changes. |

Start with `Lobby_idle_blink`. Mix the four idle variants across players and use different starting phases so a full lobby does not blink in unison. Idle loops contain long pauses and no event markers. Their first and last frames match, with 12 neutral frames on each side of the seam.

Play reactions once on the relevant change, then return to an idle at frame 0. They also start and finish neutral, with a `done` event at the last frame. Optional gesture markers are `nod` (host, frame 44), `hello` (greeting, frame 48), `ready` (ready nod, frame 44), and `noticed` (state notice, frame 28). Avoid triggering a reaction on every polling refresh.

The Mii's normal face is visible for at least 80% of every clip, and for more than 96% of each idle loop. Blinks last 8 frames (about 0.13 s), smiles last less than half a second, and the state notice changes the eyes briefly without opening the mouth. Pitch stays within 5°, yaw within 7°, and roll within 2.5° to keep the face readable in its little circle.

The editable authors and export catalog are in `tests/MiiAnim.Core.Tests/Authoring/RoomsAnimations.cs`. `RoomsAnimationTests.WriteRoomsAnimations` regenerates the `.miianim` files in this folder.
