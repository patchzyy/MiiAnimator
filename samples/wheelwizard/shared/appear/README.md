# Shared appearance

Both clips are one-shots at 60 fps. Play once, then switch to a shared idle. They finish visible in the neutral standing pose with the Mii's normal face.

| File | Duration | Motion |
| --- | --- | --- |
| `Appear.miianim` | 2.50 s | Original underground launch, airborne spin, landing, and presentation. Preserved as supplied. |
| `Appear_balloon.miianim` | 2.60 s | Bubbly float down, gentle balancing flutter, soft landing, and little hello wave. |

`Appear_balloon` starts hidden, becomes visible at frame 8 (`appeared`), lands at frame 84 (`landed`), waves at frame 108 (`hello`), and fires `done` at frame 156. Airborne legs use FK and return to IK on touchdown. Bubbles and landing dust are stored in the clip and use the player's shirt colour where appropriate.
