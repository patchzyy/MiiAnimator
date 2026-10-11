# Peek animations

For WheelWizard's `MiiPeeker`: a Mii peeking over the top edge of a card, with everything below the edge hidden behind
it (the profile page's license picker). Same geometry and camera as the profile window clips: the edge is the sill at
y = 88 with the wall at z = 58, and the view's bottom edge sits on it (`MiiImageVariants.PeekLive`, about 100 px tall).

Asleep clips start and end with the chin resting on the edge, eyes closed, arms out of sight. Awake clips start and end
up to the shoulders with both hands on the edge. `wake_up` and `doze_off` link the two; WheelWizard plays them as the
`Arrival` of its `PeekAwake` / `PeekAsleep` performances when the peeker is hovered and left.

| Folder | Clip | Motion |
| --- | --- | --- |
| `asleep` | `Peek_asleep_breathing.miianim` | Slow breaths with a soft snore and drifting Z's (8 s loop). |
| `asleep/slip` | `Peek_asleep_slip.miianim` | Slowly slides down until only the eyes show, startles and pulls back up (8 s). A rare extra between the asleep loops, not one of them. |
| `wake_up` | `Peek_wake_up.miianim` | Startles awake, pops up and grabs the edge with both hands (0.9 s, quick for hovering). |
| `awake` | `Peek_awake_look_around.miianim` | Looks left, right and back at you with a blink and a smile (6 s loop). |
| `awake` | `Peek_awake_happy.miianim` | Sways, drums the fingers on the edge, a little bounce with a big smile (5 s loop). |
| `doze_off` | `Peek_doze_off.miianim` | Heavy blinks, lets go of the edge and sinks back down onto it (1.8 s). |

Source: `PeekAnimations`, checked by `PeekAnimationTests` (hands clear of the head, body and card; head above the edge
with room for the nametag at a 100 x 96 view; baselines line up). `WritePeekAnimations` exports them.
