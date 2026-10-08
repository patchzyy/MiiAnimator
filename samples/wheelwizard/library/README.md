# My Miis library portraits

These clips are for the square `MiiListTile` portraits. They use the head and face for the readable part of each gesture. Upper arms open by at most seven degrees below the neck; forearms and hands remain at rest. Root, chest, hips, legs, body size, and position do not animate.

The framing target is WheelWizard's front-facing `BodyType.face` camera with zero camera/character rotation and zoom 1: 15 degree field of view, target height `4.805 / 0.14`, camera distance `57.553 / 0.14`, translated by the rest head position. The crop checks also account for the 90 square tile's `ImageOnlyMargin="-4,-9,-4,0"`. The head outline is the same typical face and medium-hair proxy used by `ClipCheck`; unusually tall hairstyles depend on the app's portrait framing.

| File | Duration | Gesture |
| --- | --- | --- |
| `favorite/Favorite_grateful_nod.miianim` | 1.50 s | Surprised delight, then two small appreciative nods. |
| `favorite/Favorite_delighted_wink.miianim` | 1.30 s | A pleased tilt, one cheeky wink, and a tiny shoulder opening. |
| `favorite/Favorite_shy_smile.miianim` | 1.70 s | A shy downward glance, brief closed eyes, then a warm nod. |
| `favorite/Unfavorite_soft_nod.miianim` | 1.20 s | Calm acknowledgement with a small nod and blink. |
| `favorite/Unfavorite_curious_blink.miianim` | 1.40 s | Brief questioning side glance and blink, then neutral. |
| `hover/Hover_enter_bright.miianim` | 0.60 s | Friendly little nod into the bright smiling anchor. |
| `hover/Hover_idle_bright.miianim` | 4.00 s | Quiet head sway, a small glance, and one blink. Loops. |
| `hover/Hover_leave_bright.miianim` | 0.50 s | Relaxes from the bright anchor back to neutral. |
| `hover/Hover_enter_curious.miianim` | 0.60 s | Questioning tilt into the curious smiling anchor. |
| `hover/Hover_idle_curious.miianim` | 4.00 s | Glances across the tile, blinks, and returns to the same tilt. Loops. |
| `hover/Hover_leave_curious.miianim` | 0.50 s | Straightens the curious tilt with a tiny nod. |

All clips are 60 fps and use the tile's current Mii. They have no embedded author Mii, additional actors, or particles.

## Reactions

Play a favorite or unfavorite clip once after the star state changes. Each begins and ends in the rest pose with the Mii's own neutral face. Favorite clips include a `favorite_ack` marker at the expressive accent; unfavorite clips use `unfavorite_ack`. These are reaction markers, not commands to change the favorite state. `done` occurs at the final frame.

## Hover transitions and loops

Choose one matching set per tile: `enter_bright -> idle_bright -> leave_bright`, or `enter_curious -> idle_curious -> leave_curious`. Enter plays once and emits `hover_ready` at the last frame. Its ending pose exactly matches the idle's first and last frames and the leave clip's first frame. Both idles loop with identical rotations and expression at the seam, an eased start, and a short held ending. They have no events that would repeatedly signal completion.

Bright anchor: head pitch -1.5, yaw 0, roll -2 degrees, upper-arm opening 2 degrees, smile. Curious anchor: head pitch 1, yaw -3, roll 3 degrees, the same arm opening, smile. Leave plays once and emits `done` after returning to rest/neutral.

If the pointer exits during an idle or enter gesture, blend the current pose into the matching leave clip over about 6 frames rather than snapping to its anchor. If a favorite reaction interrupts hover, blend from the current pose to the reaction's neutral start, then return through the matching hover enter if the tile is still hovered. The animation files define the endpoint poses; the application owns interruption blending and one-shot playback.

Authoring is in `tests/MiiAnim.Core.Tests/Authoring/LibraryAnimations.cs`; `LibraryAnimationTests.WriteLibraryAnimations` writes the files. The accompanying checks cover both body models, typical and extreme height/build portrait framing, arm/head/torso separation, stationary lower body, matching hover endpoints and seams, and lossless file/event round trips.
