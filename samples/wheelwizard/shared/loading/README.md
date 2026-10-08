# Shared waiting loops

Both clips loop at 60 fps until the operation finishes. They start and end neutral with matching pose and stillness, keep at least one shoe planted, and use no particles. Their quiet seam allows a clean transition to a success or error reaction.

| File | Loop length | Motion |
| --- | --- | --- |
| `Loading_foot_tap.miianim` | 3.00 s | Patient weight shift, a glance down, and four little shoe taps. |
| `Loading_ponder.miianim` | 4.00 s | Weigh two possibilities with alternating open palms and glances. |

`Loading_foot_tap` lifts the right shoe and keys its IK target and angle intentionally. The left shoe stays planted. Optional `tap` events fire on frames 60, 88, 116, and 144. `Loading_ponder` leaves both shoes planted. Neither loop fires `done`; stop looping when the app's operation completes.
