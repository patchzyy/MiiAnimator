# Shared disappearance

These are one-shots at 60 fps. Start from neutral standing, play once, then remove the Mii after `done`. They stay hidden after `vanished`, including the final neutral pose. Do not loop them.

| File | Duration | Motion | Useful events |
| --- | --- | --- | --- |
| `Disappear_goodbye_poof.miianim` | 2.20 s | A warm wave, small squash, and shirt-coloured cloud. | `goodbye` 24, `vanished` 96, `done` 132 |
| `Disappear_balloon_away.miianim` | 2.60 s | A goodbye wave followed by an airy upward departure with bubbles. | `goodbye` 24, `lift_off` 64, `vanished` 132, `done` 156 |

The cloud departure keeps grounded IK throughout its visible portion. The balloon departure switches to FK at lift-off so its legs rise with the body. Particles finish before `done`; neither clip needs extra actors or a supplied Mii.
