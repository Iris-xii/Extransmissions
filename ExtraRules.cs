using Quintessential;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;
using Quintessential.Serialization;

namespace Extransmissions;

using PartType = class_139;
using PartTypes = class_191;
using Permissions = enum_149;
using AtomTypes = class_175;
using Texture = class_256;
using PartInfo = class_236;
using VanillaAtoms = Brimstone.API.VanillaAtoms;
using BF = System.Reflection.BindingFlags;

using OriginalMolecule = Molecule;
using Molecule = Molecule;
using static ExtransmissionsMod;

/// <summary>
/// Extra rules for a puzzle, currently to be loaded in a really hacky
/// way from weird custom permissions. 
/// </summary> 
#nullable enable 
public class ExtraRules {
  public static readonly IOTextures BLUE_INPUT = new() {
    newTextureBase = class_235.method_615("textures/parts/alt_input_base"),
    newTextureRing = class_235.method_615("textures/parts/alt_input_ring"),
    newTextureGloss = class_235.method_615("textures/parts/alt_input_gloss"),
    newTextureBond = class_235.method_615("textures/parts/alt_input_bond"),
  };
  public static readonly IOTextures BLUE_OUTPUT = new() {
    newTextureBase = class_235.method_615("textures/parts/alt_output_base"),
    newTextureRing = class_235.method_615("textures/parts/alt_output_ring"),
    newTextureGloss = class_235.method_615("textures/parts/alt_output_gloss"),
    newTextureBond = class_235.method_615("textures/parts/alt_output_bond"),
  };
  const string PREFIX = "extransmissions::rule::";
  public delegate IRuleApply? RuleKind(string data);
  private static Dictionary<string, RuleKind> ruleKinds = new() {
    {"RandomInputRule",  RandomInputRule.TryRead},
    {"RandomInput",  RandomInputRule.TryRead}, //<- adding 'Rule' is redundant AF but I already have puzzles with the old name
    {"MultiOutput", MultiOutput.TryRead},
    {"IOPair", IOPair.TryRead},
  };

  int hash = 0;
  public Random rng = new();
  public Dictionary<int, InputRule> inputMolRules = new();
  public Dictionary<int, OutputRule> outputMolRules = new();
  List<IRuleApply> ruleApply = new();

  public static void AddRuleKind(string type, RuleKind rk) => ruleKinds.Add(type, rk);



  public void SimReset() {
    rng = new Random(hash);
    foreach (var inputRule in inputMolRules) {
      if (inputRule.Value.onSimReset is not null) { inputRule.Value.onSimReset(); }
    }
    foreach (var outputRule in outputMolRules) {
      if (outputRule.Value.onSimReset is not null) { outputRule.Value.onSimReset(); }
    }
  }

  public void ReadCustomPermissionString(string customPermissionString) {
    if (customPermissionString.StartsWith(PREFIX)) {
      var withoutEtPrefix = customPermissionString.Substring(PREFIX.Length);
      int sepLocation = withoutEtPrefix.IndexOf("::");
      var type = sepLocation >= 0 ? withoutEtPrefix.Substring(0, sepLocation) : "";
      var withoutPrefix = sepLocation >= 0 ? withoutEtPrefix.Substring(sepLocation + "::".Length) : "";
      //Log($"Loading data:\n{withoutPrefix}\n");
      if (type != "") { Log($"Loading type: {type}"); }
      if (type != ""
          && ruleKinds[type] is RuleKind rk
          && rk(withoutPrefix) is IRuleApply apply) {
        ruleApply.Add(apply);
        apply.ApplyRule(this);
        //if (rk is RandomInputRule rir) { Log($"{rir.DebugStr()}"); }
      }
      else {
        Log($"No rules matched input: {withoutPrefix}");
        throw new ArgumentException($"No rules matched input");
      }
    }
  }

  internal ExtraRules(ExtraRules er) {
    this.hash = er.hash;
    this.inputMolRules = new();
    this.outputMolRules = new();
    this.ruleApply = new();
    foreach (var item in er.ruleApply) {
      ruleApply.Add(item.Clone());
    }
    foreach (var item in this.ruleApply) {
      item.ApplyRule(this);
    }
    SimReset();
  }
  internal ExtraRules(ExtraRules? er, Puzzle p) {
    if (er is not null) {
      this.hash = er.hash;
      this.inputMolRules = new();
      this.outputMolRules = new();
      this.ruleApply = new();
      foreach (var item in er.ruleApply) {
        ruleApply.Add(item.Clone());
      }
      foreach (var item in this.ruleApply) {
        item.ApplyRule(this);
      }
      SimReset();
    }
    else if (p is not null && p.field_2766 is not null && p.CustomPermissions is not null) {
      int hash = p.field_2766.GetHashCode();
      this.hash = hash;
      var perms = p.CustomPermissions;
      this.inputMolRules = new();
      this.outputMolRules = new();
      this.ruleApply = new();
      foreach (var item in perms) {
        ReadCustomPermissionString(item);
      }
      SimReset();
    }
    else {
      this.hash = 0;
      this.inputMolRules = new();
      this.outputMolRules = new();
      this.ruleApply = new();
      SimReset();
    }
  }
  public static Puzzle SebToPuzzle(SolutionEditorBase seb) => seb.method_502().method_1934();

  public class IOTextures {
    public Texture? newTextureBase;
    public Texture? newTextureRing;
    public Texture? newTextureGloss;
    public Texture? newTextureBond;
  }
  public class InputRule {
    public IOTextures? newTextures;
    public Func<List<PuzzleModel.PuzzleIoM>>? displayMolecules;
    public Func<Sim?, SolutionEditorBase?, OriginalMolecule, Molecule>? chooseSpawnMolecule;
    public Action? onSimReset;
  }
  public class OutputRule {
    public IOTextures? newTextures;
    public Action? onSimReset;
    public Func<List<PuzzleModel.PuzzleIoM>>? displayMolecules;
    public Func<List<PuzzleModel.PuzzleIoM>>? acceptedMolecules;
    public Action? onCorrectMoleculeReceived;
    public Action? onWrongMoleculeReceived;
    public bool sinkAny = false;
    public bool wrongMolCrashesSim = false;
  }
  public interface IRuleApply {
    public void ApplyRule(ExtraRules er);
    /// Should deep clone anything that may be modified, two objects should be independent.
    public IRuleApply Clone();
  }
  [Serializable]
  public class MultiOutput : IRuleApply {
    public int OutputMol = -1;
    public List<PuzzleModel.PuzzleIoM>? Accepts = null;
    public bool SinkAny = false;
    public bool WrongMolCrashesSim = false;

    public static IRuleApply? TryRead(string data) {
      var mo = YamlHelper.Deserializer.Deserialize<MultiOutput>(data);
      if (mo is null) { return null; }
      if (mo.OutputMol == -1) { return null; }
      if (mo.Accepts is null) { return null; }
      return mo;
    }
    public IRuleApply Clone() {
      return new MultiOutput {
        OutputMol = this.OutputMol,
        Accepts = this.Accepts,
        SinkAny = this.SinkAny,
        WrongMolCrashesSim = this.WrongMolCrashesSim,
      };
    }
    public void ApplyRule(ExtraRules er) {
      er.outputMolRules.Add(OutputMol, new OutputRule() {
        newTextures = SinkAny ? BLUE_OUTPUT : null,
        displayMolecules = () => {
          return Accepts!;
        },
        acceptedMolecules = () => {
          return Accepts!;
        },
        sinkAny = this.SinkAny,
        wrongMolCrashesSim = this.WrongMolCrashesSim,
      });
    }
  }
  [Serializable]
  public class RandomInputRule : IRuleApply {
    public int InputMol = -1;
    public List<PuzzleModel.PuzzleIoM>? RandomBag = null;
    public int BagMult = 1;

    List<Molecule> currentBag = new();

    public IRuleApply Clone() {
      var clone = new RandomInputRule() {
        InputMol = this.InputMol,
        RandomBag = this.RandomBag,
        currentBag = new(),
      };
      clone.MaybeResetBag();
      return clone;
    }
    public static IRuleApply? TryRead(string data) {
      var rir = YamlHelper.Deserializer.Deserialize<RandomInputRule>(data);
      if (rir is null) { return null; }
      if (rir.InputMol == -1) { return null; }
      if (rir.RandomBag is null || rir.RandomBag.Count <= 0) { return null; }
      rir.MaybeResetBag();
      return rir;
    }
    public void ApplyRule(ExtraRules er) {
      er.inputMolRules.Add(InputMol, new InputRule() {
        newTextures = BLUE_INPUT,
        displayMolecules = () => RandomBag!,
        chooseSpawnMolecule = (_, _, _) => {
          int maxExcl = currentBag.Count;
          int choose = er.rng.Next(0, maxExcl);
          Molecule chosen = currentBag[choose];
          currentBag.RemoveAt(choose);
          MaybeResetBag();
          return chosen;
        },
        onSimReset = () => {
          currentBag = new();
          MaybeResetBag();
        },
      });
    }
    void MaybeResetBag() {
      if (RandomBag == null) { return; }
      if (currentBag.Count > 0) { return; }
      currentBag = new();
      foreach (var item in RandomBag) {
        for (int i = 0; i < BagMult; i++) {
          currentBag.Add(item.Molecule.FromModel());
        }
      }
    }
  }


  [Serializable]
  public class IOPair : IRuleApply {
    public int InputMol = -1;
    public int OutputMol = -1;
    public List<PuzzleModel.PuzzleIoM>? RandomInputs = null;
    public List<PuzzleModel.PuzzleIoM>? RandomOutputs = null;


    internal record struct MolPair {
      internal PuzzleModel.MoleculeM i;
      internal PuzzleModel.MoleculeM o;
    }
    internal Queue<MolPair> molPairs = new();


    public IRuleApply Clone() {
      var clone = new IOPair() {
        InputMol = this.InputMol,
        OutputMol = this.OutputMol,
        RandomInputs = this.RandomInputs,
        RandomOutputs = this.RandomOutputs,
        molPairs = new(),
      };
      return clone;
    }

    public static IRuleApply? TryRead(string data) {
      var iop = YamlHelper.Deserializer.Deserialize<IOPair>(data);
      if (iop is null) { return null; }
      if (iop.InputMol == -1) { return null; }
      if (iop.OutputMol == -1) { return null; }
      if (iop.RandomOutputs is null || iop.RandomOutputs.Count <= 0) { return null; }
      if (iop.RandomInputs is null || iop.RandomInputs.Count <= 0) { return null; }
      return iop;
    }

    internal Molecule ChoosePair(ExtraRules er) {
      int choose = er.rng.Next(0, RandomInputs!.Count);
      molPairs.Enqueue(new() {
        i = RandomInputs![choose].Molecule,
        o = RandomOutputs![choose].Molecule,
      });
      //Log($"New pair chosen, current size {molPairs.Count}!  @ {this.GetHashCode()}");
      //if(molPairs.Count > 0) Log($"PEEK! {molPairs.Peek()}");
      //if(molPairs.Count > 0) Log($"PEEKi! {molPairs.Peek().i}");
      //if(molPairs.Count > 0) Log($"PEEKo! {molPairs.Peek().o}");
      return RandomInputs[choose].Molecule.FromModel();
    }

    internal void ResetPairs() {
      //Log("RESET PAIRS");
      molPairs = new();
    }
    internal void AdvancePairs() {
      //Log("DEQUEUE PAIR");
      molPairs.Dequeue();
    }

    public void ApplyRule(ExtraRules er) {
      er.inputMolRules.Add(InputMol, new InputRule() {
        newTextures = BLUE_INPUT,
        displayMolecules = () => RandomInputs!,
        chooseSpawnMolecule = (_sim, _seb, _o) => ChoosePair(er),
        onSimReset = ResetPairs,
      });
      er.outputMolRules.Add(OutputMol, new OutputRule() {
        newTextures = BLUE_OUTPUT,
        displayMolecules = () => molPairs.Count == 0 ? RandomOutputs! : new List<PuzzleModel.PuzzleIoM>() { new() { Molecule = molPairs.Peek().o } },
        acceptedMolecules = () => {
          var accept = new List<PuzzleModel.PuzzleIoM>();
          if (molPairs.Count != 0) { accept.Add(new() { Molecule = molPairs.Peek().o }); }
          //Log(YamlHelper.Serializer.Serialize(accept));
          //Log($"COUNT : {molPairs.Count} @ {this.GetHashCode()}");
          return accept;
        },
        onCorrectMoleculeReceived = AdvancePairs,
        sinkAny = true,
        wrongMolCrashesSim = true,
      });
    }
  }
}
#nullable disable