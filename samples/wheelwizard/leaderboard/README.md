# Leaderboard podium scenes

Three different ensemble loops, each containing **all three Miis**. Select one complete scene for the podium; each folder gives a different placement the comic lead. Load the app's actual first, second and third players into the same roles for every scene.

| Folder | Clip | Length | Interaction |
| --- | --- | --- | --- |
| `first_place` | `First_place_high_fives.miianim` | 24 seconds | Gold offers silver a high five, rests, then bends the offered hand lower for bronze's low five. The third Mii watches and reacts during each exchange. |
| `second_place` | `Second_place_conductor.miianim` | 26 seconds | Silver cheekily conducts gold's bow and bronze's bigger bow. After a pause, both neighbours turn the joke around and make silver bow while they applaud. |
| `third_place` | `Third_place_copycat.miianim` | 28 seconds | Bronze sends a tiny shoulder dance across gold and silver. After a pause, bronze stops early and catches the other two still copying the pose. |

The loops start with six seconds of quiet breathing and staggered blinks. Each has a separate quiet break between its two bits and at least six seconds of calm at the end. No actor disappears, no footsteps are needed, and both feet stay on the assigned step. Palm contact is intentional; the actors' heads and torsos remain separated.

## Actor binding and podium layout

| Animation actor | App Mii | Binding | Canonical standing origin | Top surface |
| --- | --- | --- | --- | --- |
| 0 | First place | Main animation `Tracks` | `(0, 70, 0)` | `Y = 70` |
| 1 | Second place | Extra actor named `second` | `(-132, 46, 0)` | `Y = 46` |
| 2 | Third place | Extra actor named `third` | `(132, 28, 0)` | `Y = 28` |

`scene.json` in each placement folder carries the filename, role bindings, placements, loop timing, events, scale information, and any palm contact cues. Actor 0 remains gold in **every** scene, including the scenes focused on silver or bronze. Render `animation.ForActor(actorIndex)` for each player on the same frame; playing the files individually as single-Mii clips loses the coordinated exchange.

The authored motion moves the root two units below its standing origin, gently breathing down another 1.2 units. Foot IK targets still sit at each step's exact height. This gives the knees reach slack while preserving sole contact. The gold high-five scene briefly leans the two involved roots twelve units toward one another without moving their planted feet. There is no global root squash that would accidentally scale their step elevations.

## Bundled model and coordinates

`podium.glb` is a self-contained glTF 2.0 binary mesh: embedded vertex/index buffers, flat normals, triangle primitives, five PBR materials, and raised mesh numerals. Gold, silver, bronze, the dark base and ivory digits all use plain material factors. There are no textures, fonts, remote files, skins, lights, or renderer-specific extensions to load. It can be loaded independently of the animator.

The model uses the rig's **canonical** units, Y up, front +Z. Its origin is the middle of the base at ground level. The base extends from Y=0 to Y=10, and the step tops are exactly 70/46/28. Each step is 132 units wide and 96 deep with two-unit bevels; the flat standing area is 128 by 92. Empty glTF nodes `first_anchor`, `second_anchor` and `third_anchor` mark the table's standing origins. They are reference anchors, not animated parents. Draw the model once with no additional translation or rotation.

For the core's shared rendered stage, transform the GLB by:

```csharp
var defaultScale = MiiStage.DefaultBodyScale; // build=64, height=64
var podiumToStage = Matrix4x4.CreateScale(0.7f * defaultScale);
// Equivalent scale vector: (0.58275, 0.6195, 0.58275).
```

The 0.7 conversion alone gives WheelWizard's unscaled render units. The additional default body scale gives this core's shared stage, where root X/Z travel already uses the average Mii's scale. Use the same camera transform for the prop, all three bodies, heads and any effects. `podium_preview.png` is a headless geometry preview with the standing anchors annotated.

## Players with different build or height

The ready previews and exact palm contacts are calibrated for build=64, height=64. Male and female meshes share the skeleton; the headless checks evaluate foot contact and clearances with both bodies.

For each actual player's body scale `actorScale = MiiBodyModel.CalculateBodyScale(build, height)`, apply this additional **stage-space Y translation** to its body and FFL head after `MiiStage.BodyToStage` / `HeadToStage`:

```csharp
float stageOffsetY = 0.7f * podiumTop *
    (MiiStage.DefaultBodyScale.Y - actorScale.Y);
```

This keeps the feet at the model's fixed stage-space top, rather than letting a shorter player's step elevation shrink with its body. Root X/Z travel already receives `MiiStage.TravelCorrection`; do not add the platform X translation a second time.

During the high-five root leans, a non-default build can introduce a small foot drift through that X travel correction. To keep each foot exactly planted at runtime, add the following to that foot's **canonical TargetX** offset before evaluating the rig:

```csharp
float targetCorrectionX = (rootMoveX - canonicalPlacementX) *
    (actorScale.X - MiiStage.DefaultBodyScale.X) / actorScale.X;
```

The other two scenes have constant root X and need no such correction. Rest-foot spacing still follows the player's own build.

Height correction does not retarget the authored FK arm rotations. The high-five sidecar supplies the exact contact cues:

| Frame | Actors / wrist bones | Canonical palm point |
| --- | --- | --- |
| 432 | 0 `HandR`, 1 `HandL` | `(-66, 166, 8)` |
| 792 | 0 `HandL`, 2 `HandR` | `(66, 150, 8)` |

For a default player, these points match for the brief contact hold through frame+6. Convert a cue to stage with `canonicalPoint * 0.7f * MiiStage.DefaultBodyScale` to obtain a starting target for runtime hand IK. Players with significantly different proportions may need an adapted target and a closer lean so both arm chains can reach without stretching. The two non-contact scenes can use their authored FK gestures directly. Exact touching across arbitrary body sizes is an integration responsibility, rather than a guarantee of the unmodified file.

## WheelWizard integration status

This bundle targets **AnimatorProgram's current multi-actor `.miianim` format**. The inspected WheelWizard checkout's `Features/MiiAnimation/Core` does not yet contain `AnimActor`, and its `MiiRealtimeView` evaluates a single animation without `ActorCount` / `ForActor` playback. Synchronize its core format/evaluator and render every bound actor plus the podium before deploying these ensemble clips. This repository supplies the assets, authoring source, checked placement rules and sidecars; it does not change WheelWizard's application wiring.

## Reproduction

The source for the three loops is `tests/MiiAnim.Core.Tests/Authoring/LeaderboardAnimations.cs`. The `LeaderboardAnimationTests.WriteLeaderboardSamples` test writes all three `.miianim` files and their `scene.json` sidecars. The other leaderboard tests check serialization, all-frame sole contact, platform bounds, both body meshes, loop closure, quiet gaps, head/body clearance, palm approach/contact/release, and the GLB structure, materials, winding and normals.

To regenerate only the prop and its geometry preview headlessly, run `python samples/wheelwizard/leaderboard/generate_podium.py`. The GLB generator uses the Python standard library; regenerating the optional PNG uses Pillow. Neither is needed for playback.

## Entrances

`enter/first|second|third` hold one single-Mii clip per place, all 4.2 s long, to be started together before a scene:
bronze drops onto its step first, then silver, then gold lands with a bigger thump and throws its arms up in a burst
of confetti while the other two turn and clap. Each starts invisible (`Visible` track) high enough above the stage
for the shortest Miis, falls with FK legs, plants its IK feet on touchdown (with a puff of dust on the step), and ends
exactly on its actor's first frame of the scenes (which all start the same), so a scene takes over without a seam.

Source: `PodiumEntranceAnimations`.
