# Shared hover reactions

Play these one-shots once when pointer focus enters a Mii. They use grounded IK, start and finish neutral, and return to the normal face. Follow `done` with the current idle instead of repeatedly replaying a hover gesture while the pointer stays still.

| File | Duration at 60 fps | Motion | Useful events |
| --- | --- | --- | --- |
| `Hover_perk_up.miianim` | 1.50 s | A quick attentive lift of the chest, open-hand acknowledgement, and short wink. | `acknowledged` 20, `done` 90 |
| `Hover_little_wave.miianim` | 2.00 s | A relaxed lean and small side wave with two wrist swishes. | `hello` 24, `done` 120 |

Use the perk for compact buttons or fast navigation, and the wave for larger profile or selection tiles. Both work without particles or additional actors.
