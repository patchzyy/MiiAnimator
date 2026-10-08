# Shared selection reactions

These are one-shots at 60 fps. Each starts and finishes neutral, keeps grounded IK, and restores the normal face. They acknowledge a choice and can hand back to the current idle on `done`.

| File | Duration | Motion | Useful events |
| --- | --- | --- | --- |
| `Selected_ta_da.miianim` | 2.50 s | Small anticipation, proud two-handed presentation, shirt-tinted stars, and a wink. | `selected` 30, `present` 36, `done` 150 |
| `Selected_yes_nod.miianim` | 1.70 s | Two affirmative nods with an open hand and a few stars. | `selected` 22, `done` 102 |

The presentation suits choosing a character or highlighting a result. The nod offers a quicker acknowledgement for ordinary list selections. They do not require a selection marker or any extra actors; their particle effects are included in the files and finish before `done`.
