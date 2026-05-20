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
  private static Dictionary<string, RuleKind> ruleKinds = new() {
    {"RandomInputRule",new RandomInputRule()},
  };

  int hash = 0;
  public Random rng = new();
  public Dictionary<int, InputRule> inputMolRules = new();
  public Dictionary<int, OutputRule> outputMolRules = new();

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
  /// <summary> 
  /// Alter ExtraRules according to customPermissionString if it has the prefix
  /// extransmissions::rule:: , ignore otherwise, error if somehow invalid. 
  /// </summary>
  /// <param name="customPermissionString">An entry in puzzle.CustomPermissions, unprocessed</param>
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
          && rk.TryRead(withoutPrefix)) {
        rk.ApplyRule(this);
        //if (rk is RandomInputRule rir) { Log($"{rir.DebugStr()}"); }
      }
      else {
        Log($"No rules matched input: {withoutPrefix}");
        throw new ArgumentException($"No rules matched input");
      }
    }
  }

  internal ExtraRules(int hash) {
    this.hash = hash;
    SimReset();
  }

  public class IOTextures {
    public Texture? newTextureBase;
    public Texture? newTextureRing;
    public Texture? newTextureGloss;
    public Texture? newTextureBond;
  }
  public class RuleCtx {
    public Part? p;
    public SolutionEditorBase? seb;
    public Sim? sim;
    public Random rng;
    internal RuleCtx(Random rng, SolutionEditorBase? seb = null, Part? part = null, Sim? sim = null) {
      this.p = part;
      this.seb = seb;
      this.rng = rng;
      this.sim = sim;
    }
  }
  public class InputRule {
    public IOTextures? newTextures;
    public Func<RuleCtx, List<PuzzleModel.PuzzleIoM>>? displayMolecules;
    /// <summary>
    /// Called very many times (unfortunately) to generate an output molecule.
    /// </summary>
    public Func<RuleCtx, OriginalMolecule, Molecule>? chooseSpawnMolecule;
    public Action? onSimReset;
  }
  public class OutputRule {
    public IOTextures? newTextures;
    public Action? onSimReset;
  }
  public interface RuleKind {
    public bool TryRead(string data);
    public void ApplyRule(ExtraRules er);
  }
  [Serializable]
  public class RandomInputRule : RuleKind {
    public int InputMol = -1;
    public List<PuzzleModel.PuzzleIoM>? RandomBag = null;
 
    private List<Molecule> currentBag = new();
    public bool TryRead(string data) {
      var rir = YamlHelper.Deserializer.Deserialize<RandomInputRule>(data);
      if (rir is null) { return false; }
      if (rir.InputMol == -1) { return false; }
      if (rir.RandomBag is null || rir.RandomBag.Count <= 0) { return false; }
      InputMol = rir.InputMol;
      RandomBag = rir.RandomBag;
      MaybeResetBag();
      return true;
    }
    public void ApplyRule(ExtraRules er) {
      er.inputMolRules.Add(InputMol, new InputRule() {
        newTextures = BLUE_INPUT,
        displayMolecules = (_) => RandomBag!,
        chooseSpawnMolecule = (ctx, _orig) => {  
          int maxExcl = currentBag.Count; 
          int choose = ctx.rng.Next(0, maxExcl); 
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
    private void MaybeResetBag() {
      if (RandomBag == null) { return; }
      if (currentBag.Count > 0) { return; }
      currentBag = new();
      foreach (var item in RandomBag) {
        currentBag.Add(item.Molecule.FromModel());
      }
    }
  }
}
#nullable disable