# Extransmissions

This is a mod adds additional ways to transmit molecules in and out of a puzzle, or extransmissions, if you will. 

**WARNING**: Extransmissions is a fairly invasive mod. Though I've tried to not make it more incompatible than necessary, it's quite likely some mods may not work correctly with Extransmissions, in particular any mods that change the way inputs/outputs work. This may cause issues only when Extransmissions mechanics are utilized, or at any point at all. If you're experiencing weird behavior please do try to remove Extransmissions first and see if that was the issue!

As an example, another mod of mine (Extransmutations)'s Ichor mechanic required explicit compat to work properly, and even then there are some quirks remaining when Ichor and custom inputs/outputs interact.

To use Extransmissions, you must attach a CustomPermissions to your puzzle with the pattern `extransmissions::rule::<rule name>::<data>` where `<data>` is basically another yaml file embedded as a string. Yes, this is somewhat cursed.

`dumbpuzzleexample.puzzle.yaml` is a terrible puzzle that nonetheless uses several Extransmissions rules and features. You can look at it to see how it's done, and reference the rules found on `ExtraRules.cs` for more information.

You can also just ask me, I don't mind. I get that it's all fairly byzantine.

Some extra input/output 'rules' currently included in the mod are: 

#### RandomInput Rule
Randomly outputs one of a list of potential molecules. The RNG is seeded and consistent every run, and the input's graphics cycle through the potential options.

#### MultiOutput Rule
Allows one output to accept multiple inputs. 

Additionally, it also allows an output to accept *all* molecules that fit, without making progress towards solving the puzzle or downright causing the solution to fail. This can be used to prevent 'output conditionals' in which you sort molecules by placing them over an input and seeing if they get accepted or not.

#### Additional Rules
Additional rules allowing more complex relationships between inputs and outputs are possible, but not currently included.