# Shared Mii animations

Reusable full-body motions for any WheelWizard screen. All clips use 60 fps, adapt to the playing Mii, and include their expressions and any particles. The original `Appear`, `Idle_breathing`, and `Idle_weight_shift` files are preserved alongside 15 new variations.

| Action | Clips | Playback |
| --- | --- | --- |
| [Appear](appear/README.md) | Original launch; balloon arrival | Play once, then idle |
| [Disappear](disappear/README.md) | Goodbye cloud; balloon departure | Play once, remove after `done` |
| [Error](error/README.md) | Puzzled shrug; startled recovery | Play once, then idle |
| [Hover](hover/README.md) | Perk up; little wave | Play once on pointer entry |
| [Idle](idle/README.md) | Breathing; weight shift; curious look; tiny groove | Loop |
| [Loading](loading/README.md) | Foot tap; pondering | Loop until the operation completes |
| [Selected](selected/README.md) | Presentation; affirmative nod | Play once, then idle |
| [Success](success/README.md) | Fist pump; happy hop | Play once, then idle |

Standing motions keep grounded IK. Flight and hop variations intentionally use FK in the air and return to IK at touchdown. New idles and loading loops have a quiet neutral pause on both sides of the seam. New one-shots finish in neutral with a `done` event; disappearances finish hidden. Action guides give durations, event frames, and the intended character of each variation.

The new files are authored in `SharedAnimations.All` and exported by `SharedAnimationWriter.WriteSharedAnimations` in the core test project. The catalog only includes additions, so regeneration preserves the three original samples.
