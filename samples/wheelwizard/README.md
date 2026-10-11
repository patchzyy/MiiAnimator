# WheelWizard Mii animations

The clips here are authored for the camera and purpose of each current WheelWizard animation folder. Each section has its own guide with clip names, durations and playback recommendations.

| Folder | Framing | Guide |
| --- | --- | --- |
| `editor` | Facial edits have fixed-root upper-body variants; body/general edits can use the full body. | [Editor clips](editor/README.md) |
| `friends` | Head and shoulders, facing slightly to screen right. | [Friends clips](friends/README.md) |
| `leaderboard` | Three full-body Miis on a shared podium, coordinated on one timeline. | [Leaderboard scenes and podium](leaderboard/README.md) |
| `library` | Tight square head portrait, with restrained shoulder/arm movement. | [My Miis clips](library/README.md) |
| `profile` | Full-body profile actions and existing window-card animations. | [Profile clips](profile/README.md) |
| `peek` | Head over the top edge of a card (WheelWizard's MiiPeeker): asleep on the edge, awake while hovered. | [Peek clips](peek/README.md) |
| `rooms` | Head-only gestures for very small circular lobby portraits. | [Room clips](rooms/README.md) |
| `shared` | Reusable full-body entrances, exits, idles and reactions. | [Shared clips](shared/README.md) |

## Using the bundle

Choose clips by folder and filename. Loop ambient idles; play action reactions once, then return to the appropriate idle. The per-folder guides describe the baseline pose and transition behavior. Preserve the Mii's own face during most idle frames.

The [twenty editor idle loops](editor/idle/README.md) include ten standing and ten close-camera choices. They all keep the selected face unchanged and return to the same neutral pose at the loop boundary.

The [editor interaction suite](editor/interactions/README.md) includes three playful front-click reactions and four backward flinches mapped to the head, body and each leg.

`inventory.json` lists every exported clip, duration, actor roles, event frames and particle count. The assets are `.miianim` files supported by this repository's current core. Files that contain extra actors or particles need the matching playback support in WheelWizard's copy of the core and renderer; the local WheelWizard source still uses a single-Mii rendering path. The leaderboard guide specifies its GLB placement, role binding and correction for Miis of different sizes.

Particle images travel inside each file (the `SHAP` chunk), so players draw them without knowing any shapes. The editor's purple frustration marks, for example, are the built-in squiggle image, set upright and narrow on the effect.

The authoring code lives in `tests/MiiAnim.Core.Tests/Authoring`. Each folder's test class has a writer method that recreates only its new clips. `WheelWizardSampleWriter.WriteRelative` exports directly into this layout. Existing hand-edited animation files are preserved separately from the new writer catalogs.

Validation is headless: pose and camera bounds, planted-foot/contact checks, loop/return poses, format round trips and GLB structure. No computer-use tools or desktop app launches are required.
