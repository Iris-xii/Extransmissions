# Extransmissions

**WARNING**: Extransmissions is a fairly invasive mod. Though I've tried to not make it more incompatible than necessary, it's quite likely some mods may not work correctly with Extransmissions, in particular any mods that change the way inputs/outputs work. This may cause issues only when Extransmissions mechanics are utilized, or at any point at all. If you're experiencing weird behavior please do try to remove Extransmissions first and see if that was the issue!

As an example, another mod of mine (Extransmutations)'s Ichor mechanic required explicit compat to work properly, and even then there are some quirks remaining when Ichor and custom inputs/outputs interact.

To use Extransmissions, you must attach a CustomPermissions to your puzzle with the pattern `extransmissions::rule::<rule name>::<data>` where `<data>` is basically another yaml file embedded as a string. Yes, this is somewhat cursed.

`dumbpuzzleexample.puzzle.yaml` is a terrible puzzle that nonetheless uses several Extransmissions rules and features. You can look at it to see how it's done, and reference the rules found on `ExtraRules.cs` for more information.

You can also just ask me, I don't mind. I get that it's all fairly byzantine.