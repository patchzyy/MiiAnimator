# Shared success reactions

These one-shots run at 60 fps and finish in neutral standing with the normal face. Return to idle on `done`.

| File | Duration | Motion | Useful events |
| --- | --- | --- | --- |
| `Success_fist_pump.miianim` | 2.20 s | Two compact victory pumps, proud posture, a cheeky wink, and shirt-tinted stars. | `celebrate` 34, `done` 132 |
| `Success_happy_hop.miianim` | 2.70 s | A knee bend, happy tucked hop, soft landing, and open-handed celebration. | `jump` 22, `landed` 54, `celebrate` 80, `done` 162 |

The fist pump keeps both shoes planted for small confirmations. The happy hop is the more exuberant option: its legs switch from IK to FK at take-off and return to flat grounded IK at frame 54. Included stars and landing dust finish before `done`. Neither should loop.
