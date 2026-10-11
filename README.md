# Mii Animator

(keep in mind this is **only** made for creation of animation purposes, codebase if **fully** LLM generated )

A standalone animation tool for Miis. Pose a Mii with FK/IK limbs and squash & stretch, animate it on a
clean timeline (dope sheet + curve editor), and export a compact `.miianim` file that WheelWizard can play.

## Projects

- `src/MiiAnim.Core` — the `.miianim` format, Mii rig (skinned 3DS body), curves, IK and pose evaluation.
  Everything an animation shows is in its file (particle images included); players know no presets or shapes.
  WheelWizard contains a copy of this library so playback is identical in both apps; edit it here, never there.
- `src/MiiAnimator` — the Avalonia + OpenGL editor, plus authoring-only helpers: templates, particle presets
  and the built-in particle images (`Assets/ParticleShapes`).
- `tests/MiiAnim.Core.Tests` — rig, IK, curve and file format tests.
- `samples` — the built-in templates exported as `.miianim` files.
- [`samples/wheelwizard`](samples/wheelwizard/README.md) — WheelWizard clips organized by Mii screen and action, with folders for upcoming animations.

## Running

```
dotnet run --project src/MiiAnimator
```

The head is rendered from `FFLResHigh.dat`. The animator uses the copy WheelWizard downloads
(`%APPDATA%\CT-MKWII\MiiRendering\FFLResHigh.dat`) or one picked via File → Locate FFLResHigh.dat.

Press F1 in the app for controls and shortcuts.

## Credits

The skinned 3DS Mii body models come from [ariankordi/FFL-Testing](https://github.com/ariankordi/FFL-Testing).
