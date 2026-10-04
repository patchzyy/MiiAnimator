# Mii Animator

A standalone animation tool for Miis. Pose a Mii with FK/IK limbs and squash & stretch, animate it on a
clean timeline (dope sheet + curve editor), and export a compact `.miianim` file that WheelWizard can play.

## Projects

- `src/MiiAnim.Core` — the `.miianim` format, Mii rig (skinned 3DS body), curves, IK and pose evaluation.
  WheelWizard contains a copy of this library so playback is identical in both apps.
- `src/MiiAnimator` — the Avalonia + OpenGL editor.
- `tests/MiiAnim.Core.Tests` — rig, IK, curve and file format tests.
- `samples` — the built-in templates exported as `.miianim` files.

## Running

```
dotnet run --project src/MiiAnimator
```

The head is rendered from `FFLResHigh.dat`. The animator uses the copy WheelWizard downloads
(`%APPDATA%\CT-MKWII\MiiRendering\FFLResHigh.dat`) or one picked via File → Locate FFLResHigh.dat.

Press F1 in the app for controls and shortcuts.

## Credits

The skinned 3DS Mii body models come from [ariankordi/FFL-Testing](https://github.com/ariankordi/FFL-Testing).
