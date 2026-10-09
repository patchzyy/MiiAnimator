# Friends cards

These clips are authored for the tightly cropped head and upper shoulders in `FriendsListItem`.
Every clip keeps the root position and hidden legs fixed. There are no walks, jumps, waves outside
the crop, or particles. Chest motion is deliberately small; the face, chin, and eye timing carry
the action.

The source `FriendsSideProfile` image uses character rotation `(350, 15, 355)` and camera rotation
`(12, 0, 0)`. The animation baseline includes a **+15 degree root yaw**, a head pitch of -8 degrees,
and head roll of -4 degrees. With the source front at +Z and screen right at +X, positive yaw turns
the nose slightly toward the text on the right. Retain the front camera and its 12 degree pitch;
do not apply the static image variant's character rotation again on top of these tracks.

All six files start and end at the same right-facing baseline with the Mii's own normal expression.
The four idle files can loop or be interchanged at their boundaries. Play the presence-change clips
once, then switch to an idle for the new state. The `.miianim` format stores a loop length rather
than a loop flag, so the caller chooses looping or one-shot playback.

| Folder | File | Timing | Motion |
| --- | --- | --- | --- |
| online | `Friends_online_idle_attentive.miianim` | 6 s loop | Light breaths, one small acknowledgement toward the text, two blinks. |
| online | `Friends_online_idle_amused.miianim` | 7 s loop | Restrained two-beat chuckle with a tiny shoulder echo and a cheek tilt. |
| online | `Friends_became_online.miianim` | 2.6 s once | Notices the connection, brightens, then gives a small friendly double nod. |
| offline | `Friends_offline_idle_restful.miianim` | 7 s loop | Slower exhale and chin settling with soft, longer blinks. |
| offline | `Friends_offline_idle_thoughtful.miianim` | 8 s loop | Quiet glance slightly farther right and down, then a slow return. |
| offline | `Friends_became_offline.miianim` | 3.2 s once | Held closing nod, exhale, and extended blink before settling. |

Presence markers are `online` at frame 24 and `offline` at frame 70. Both one-shot clips emit `done`
at their final frame. Idle clips have no action markers.

Geometry checks cover both body rigs, fixed lower-body transforms, actual rendered head direction,
head landmark displacement through the pitched front camera, readable head rotation, quiet loop
seams, and arm clearance. The crop check allows at most 14 render units of landmark movement from
baseline across short, average, and tall body scales. It checks relative movement, not a pixel-exact
UI crop; hair, hats, and custom camera sizing still need their usual padding in the player.

The authoring source is `tests/MiiAnim.Core.Tests/Authoring/FriendsAnimations.cs`; the
`WriteFriendsAnimations` test writes the files into these folders.

## Presence: asleep and awake

`FriendsPresenceAnimations` adds the clips WheelWizard plays on the cards. Offline friends sleep; online friends idle
awake and now and then wave at you. Same card baseline as above, except the asleep baseline: head dropped (+5° pitch)
and tilted (+9° roll), chest slumped (+4°), eyes closed.

| Folder | File | Timing | Motion |
| --- | --- | --- | --- |
| online | `Friends_online_idle_curious.miianim` | 7 s loop | Turns to look at you with a little smile and tilt, then back to the card. |
| online/wave | `Friends_wave_hello.miianim` | 2.6 s once | Turns to you, raises the hand beside the chin and waves from the elbow three times. |
| online/wave | `Friends_wave_excited.miianim` | 2.3 s once | Quicker, bouncier wave with a wink and a few sparkles. |
| online/wave | `Friends_wave_shy.miianim` | 2.9 s once | Small, slow, bashful wave with the head tilted. |
| asleep | `Friends_asleep_breathing.miianim` | 8 s loop | Two deep breaths with a soft snore, Z's drifting up. |
| asleep | `Friends_asleep_nodding.miianim` | 9 s loop | The head sinks lower and lower, bobs back up with a start, dozes on. |
| asleep | `Friends_asleep_bubble.miianim` | 10 s loop | A snore bubble swells from the nose and pops with a snort, then Z's. |
| doze_off | `Friends_doze_off.miianim` | 2.8 s once | Awake → asleep: heavy blinks, a nod that catches itself, sinks into sleep. |
| wake_up | `Friends_wake_up.miianim` | 2 s once | Asleep → awake: stirs, startles awake, blinks and brightens. |

Waves swing the forearm from a still elbow (`AnimBuilder.ArmWorldElbow`), so they read as waves even though the hand
is a ball. The card is cropped tightly, so the hand comes up beside the chin and the Z's rise in the gap between the
head and the card text (`WheelWizardShot.FriendsCard` checks both in card pixels).
