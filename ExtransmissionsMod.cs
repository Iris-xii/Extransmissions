using Quintessential;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;
using Quintessential.Serialization;
using YamlDotNet.Core;

namespace Extransmissions;

using PartType = class_139;
using PartTypes = class_191;
using Permissions = enum_149;
using AtomTypes = class_175;
using Texture = class_256;
using PartInfo = class_236;
using VanillaAtoms = Brimstone.API.VanillaAtoms;
using BF = System.Reflection.BindingFlags;


using MHS = ExtransmissionsMod.MoleculeHookState; // <- I'm lazy
//dotnet build;rm Extransmissions.dll;cp bin/Debug/net4.5.2/Extransmissions.dll ./
public class ExtransmissionsMod : QuintessentialMod {
  public static Texture origInputBase;
  public static Texture origInputRing;
  public static Texture origInputGloss;
  public static Texture origInputBond;
  public static Texture origOutputBase;
  public static Texture origOutputRing;
  public static Texture origOutputGloss;
  public static Texture origOutputBond;

  public enum MoleculeHookState {
    OFF, PRE_SOLVE_DRAW_INPUTS, DRAW_OUTPUTS
  }
  public static MoleculeHookState molHookState = MoleculeHookState.OFF;
  public static SolutionEditorBase molHookStateSeb = null;
  public static readonly Random random = new();

  public static ExtraRules maybeActiveER = null;
  public static Dictionary<Molecule, int> inputMolMap = new();
  public static Dictionary<Molecule, int> outputMolMap = new();

  public override void Load() {
  }

  public delegate void origEditorMethod925(Molecule param_4569, Vector2 param_4570, HexIndex param_4571, float param_4572, float param_4573, float param_4574, float param_4575, bool param_4576, SolutionEditorBase param_4577);
  public static void OnEditorMethod925(
      origEditorMethod925 orig,
      Molecule molecule,
      Vector2 param_4570,
      HexIndex param_4571,
      float param_4572,
      float param_4573,
      float param_4574,
      float param_4575,
      bool param_4576,
      SolutionEditorBase param_4577) {
    var simTime = molHookStateSeb is not null ? molHookStateSeb.method_510() : 0.0f;
    var gTime = molHookStateSeb is not null ? molHookStateSeb.method_509() : 0.0f;

    if (molHookState == MHS.PRE_SOLVE_DRAW_INPUTS
        && inputMolMap.ContainsKey(molecule)
        && maybeActiveER is not null
        && maybeActiveER.inputMolRules.TryGetValue(inputMolMap[molecule], out var maybeIR)
        && maybeIR is not null
        && maybeIR.displayMolecules is not null) {
      var displayMolecules = maybeIR.displayMolecules(
        new ExtraRules.RuleCtx(rng: maybeActiveER.rng, part: null, seb: molHookStateSeb));
      float molCount = (float)displayMolecules.Count;

      Molecule chosenMol =
        displayMolecules[(int)Math.Floor(gTime % molCount)].Molecule.FromModel();

      orig(chosenMol, param_4570, param_4571, param_4572, param_4573, param_4574, param_4575, param_4576, param_4577);
      return;
    }
    //if (molHookState != MHS.OFF) {
    //  //Logger.Log($"[extransmissions] {System.Environment.StackTrace}");  
    //  Molecule origMolecule = param_4569;
    //  Molecule changed = new();
    //  foreach (var atom in origMolecule.method_1100()) { // atoms
    //    AtomType target = atom.Value.field_2275;
    //    if (atom.Value.field_2275 == VanillaAtoms.water || atom.Value.field_2275 == VanillaAtoms.fire) {
    //      target = (random.Next() > (Int32.MaxValue / 2)) ? VanillaAtoms.fire : VanillaAtoms.water;
    //    }
    //    changed.method_1105(new(target), atom.Key);
    //  }
    //  foreach (var orig277 in origMolecule.method_1101()) {
    //    var new277 = orig277.method_754();
    //    changed.method_1112(new277.field_2186, new277.field_2187, new277.field_2188, new());
    //  }
    //  orig((gTime % 2.0) > 1.0 ? param_4569 : changed, param_4570, param_4571, param_4572, param_4573, param_4574, param_4575, param_4576, param_4577);
    //  return;
    //}
    orig(molecule, param_4570, param_4571, param_4572, param_4573, param_4574, param_4575, param_4576, param_4577);
    return;
  }

  public static void OnSebMethod2000(On.SolutionEditorBase.orig_method_2000 orig,
      class_236 param_5573, Vector2 param_5574, Molecule molecule, bool param_5575) {

    bool altTexture = random.Next() > (Int32.MaxValue / 2);
    ExtraRules.IOTextures maybeAltTextureInput = null;
    ExtraRules.IOTextures maybeAltTextureOutput = null;
    if (maybeActiveER is not null) {
      bool isInput = inputMolMap.ContainsKey(molecule);
      bool isOutput = outputMolMap.ContainsKey(molecule);
      if (isInput) {
        int idx = inputMolMap[molecule];
        if (maybeActiveER.inputMolRules.ContainsKey(idx)) {
          maybeAltTextureInput = maybeActiveER.inputMolRules[idx].newTextures;
        }
      }
      if (isOutput) {
        int idx = outputMolMap[molecule];
        if (maybeActiveER.outputMolRules.ContainsKey(idx)) {
          maybeAltTextureOutput = maybeActiveER.outputMolRules[idx].newTextures;
        }
      }
    }
    bool textureChangedAtAll = maybeAltTextureInput is not null
      || maybeAltTextureOutput is not null;

    if (textureChangedAtAll) {
      origInputBase = class_238.field_1989.field_90.field_176;
      origInputRing = class_238.field_1989.field_90.field_181;
      origInputGloss = class_238.field_1989.field_90.field_179;
      origInputBond = class_238.field_1989.field_90.field_177;
      origOutputBase = class_238.field_1989.field_90.field_188;
      origOutputRing = class_238.field_1989.field_90.field_191;
      origOutputGloss = class_238.field_1989.field_90.field_190;
      origOutputBond = class_238.field_1989.field_90.field_189;

      if (maybeAltTextureInput is not null) {
        class_238.field_1989.field_90.field_176 = maybeAltTextureInput.newTextureBase;
        class_238.field_1989.field_90.field_181 = maybeAltTextureInput.newTextureRing;
        class_238.field_1989.field_90.field_179 = maybeAltTextureInput.newTextureGloss;
        class_238.field_1989.field_90.field_177 = maybeAltTextureInput.newTextureBond;
      }
      if (maybeAltTextureOutput is not null) {
        class_238.field_1989.field_90.field_188 = maybeAltTextureOutput.newTextureBase;
        class_238.field_1989.field_90.field_191 = maybeAltTextureOutput.newTextureRing;
        class_238.field_1989.field_90.field_190 = maybeAltTextureOutput.newTextureGloss;
        class_238.field_1989.field_90.field_189 = maybeAltTextureOutput.newTextureBond;
      }
    }

    orig(param_5573, param_5574, molecule, param_5575);

    if (textureChangedAtAll) {
      class_238.field_1989.field_90.field_176 = origInputBase;
      class_238.field_1989.field_90.field_181 = origInputRing;
      class_238.field_1989.field_90.field_179 = origInputGloss;
      class_238.field_1989.field_90.field_177 = origInputBond;
      class_238.field_1989.field_90.field_188 = origOutputBase;
      class_238.field_1989.field_90.field_191 = origOutputRing;
      class_238.field_1989.field_90.field_190 = origOutputGloss;
      class_238.field_1989.field_90.field_189 = origOutputBond;
    }
  }

  public Hook puzzleinfoscreen_method_1275;
  public static void OnSolLoad(
      Action<PuzzleInfoScreen, Solution> orig,
      PuzzleInfoScreen self,
      Solution param_5012) {
    Puzzle puzzle = param_5012.method_1934();
    var perms = puzzle.CustomPermissions;
    Log($"Reading solution {param_5012.field_3915} @ {puzzle.field_2766}");
    ExtraRules extraRules = new(puzzle.field_2766.GetHashCode());
    foreach (var item in perms) {
      extraRules.ReadCustomPermissionString(item);
    }

    inputMolMap.Clear();
    outputMolMap.Clear();
    PuzzleInputOutput[] inputs = puzzle.field_2770;
    for (int i = 0; i < inputs.Length; i++) {
      inputMolMap.Add(inputs[i].field_2813, i);
    }
    PuzzleInputOutput[] outputs = puzzle.field_2771;
    for (int i = 0; i < outputs.Length; i++) {
      outputMolMap.Add(outputs[i].field_2813, i);
    }

    maybeActiveER = extraRules;
    orig(self, param_5012);
  }

  //method_1167()
  public static Part moleculeHookPart = null;
  public static Sim moleculeHookSimRef = null; 
  public static HashSet<int> cyclesSeen = new();
  public Molecule MoleculeSpawnHook(Molecule original) {
    int moleculeHookIdxNumber = moleculeHookPart is not null ? moleculeHookPart.method_1167() : -1;
    if (maybeActiveER is not null
        && moleculeHookSimRef is not null
        && moleculeHookPart is not null
        && moleculeHookIdxNumber >= 0
        && maybeActiveER.inputMolRules.TryGetValue(moleculeHookIdxNumber, out var maybeIR)
        && maybeIR is not null
        && maybeIR.chooseSpawnMolecule is not null ) { 
          //seb.method_503()
      SolutionEditorBase seb = moleculeHookSimRef.field_3818;
      var seb_play_status = seb.method_503();
      if(seb_play_status == enum_128.Stopped) {
        maybeActiveER.SimReset();
        cyclesSeen = new();
      }
      if(cyclesSeen.Contains(moleculeHookSimRef.method_1818())) {
        return original;
      } else {
        cyclesSeen.Add(moleculeHookSimRef.method_1818());
      }
      Molecule alternate = maybeIR.chooseSpawnMolecule(
        new ExtraRules.RuleCtx(rng: maybeActiveER.rng,sim: moleculeHookSimRef),
        original
      );
      HexIndex param_ = moleculeHookPart.method_1161();
      HexRotation param_2 = moleculeHookPart.method_1163();
      alternate = alternate.method_1115(param_2).method_1117(param_);
      moleculeHookPart = null;
      return alternate;
    }
    return original;
  }

  public override void LoadPuzzleContent() {
  }


  // editor.method_925 <- RENDER ATOM.
  // Seb.method_2000 <- Draw i/o base
  public Hook seb_method_2000;
  public ILHook h2;
  public Hook h;
  public ILHook moleculeSpawnHook;
  public ILHook preSimHook; 

  private delegate void orig_SolutionEditorBase_method_1994
    (SolutionEditorBase self, Part param_5558, Vector2 param_5559, bool param_5560, bool param_5561);

  public override void PostLoad() {
    seb_method_2000 = new Hook(
      typeof(SolutionEditorBase).GetMethod("method_2000", BF.Public | BF.Static),
      OnSebMethod2000
    );

    puzzleinfoscreen_method_1275 = new Hook(
      typeof(PuzzleInfoScreen).GetMethod("method_1275", BF.NonPublic | BF.Instance),
      OnSolLoad
    );
    h = new Hook(
      typeof(Editor).GetMethod("method_925", BF.Public | BF.Static),
      OnEditorMethod925
    );
    //NOT 2015
    // 1994 <-
    //   | method_1984
    //   | method_1984
    h2 = new ILHook(typeof(SolutionEditorBase).GetMethod("method_1994"
        , BF.NonPublic | BF.Instance),
      ilc => {
        ILCursor c = new(ilc);
        c.Emit(Mono.Cecil.Cil.OpCodes.Ldarg, 0);
        c.EmitDelegate((SolutionEditorBase seb) => { ExtransmissionsMod.molHookStateSeb = seb; });
        c.GotoNext(MoveType.After,
          i => i.MatchCallOrCallvirt(typeof(Editor).GetMethod("method_925"))); //skip 
        c.GotoNext(MoveType.Before,
          i => i.MatchCallOrCallvirt(typeof(Editor).GetMethod("method_925")));
        c.EmitDelegate(() => { ExtransmissionsMod.molHookState = MHS.PRE_SOLVE_DRAW_INPUTS; });
        c.GotoNext(MoveType.After,
          i => i.MatchCallOrCallvirt(typeof(Editor).GetMethod("method_925")));
        c.EmitDelegate(() => { ExtransmissionsMod.molHookState = MHS.OFF; });



        c.GotoNext(MoveType.After,
          i => i.MatchCallOrCallvirt(typeof(Editor).GetMethod("method_925"))); //skip
        c.GotoNext(MoveType.After,
          i => i.MatchCallOrCallvirt(typeof(Editor).GetMethod("method_925"))); //skip
        c.GotoNext(MoveType.After,
          i => i.MatchCallOrCallvirt(typeof(Editor).GetMethod("method_925"))); //skip
        c.GotoNext(MoveType.Before,
          i => i.MatchCallOrCallvirt(typeof(Editor).GetMethod("method_925")));
        c.EmitDelegate(() => { ExtransmissionsMod.molHookState = MHS.DRAW_OUTPUTS; });
        c.GotoNext(MoveType.After,
          i => i.MatchCallOrCallvirt(typeof(Editor).GetMethod("method_925")));
        c.EmitDelegate(() => { ExtransmissionsMod.molHookState = MHS.OFF; });
        c.EmitDelegate(() => { ExtransmissionsMod.molHookStateSeb = null; });
      });

    //method_1167()
    moleculeSpawnHook = new ILHook(
      typeof(Sim).GetMethod("method_1843", BF.NonPublic | BF.Instance),
      ilc => {
        ILCursor c = new(ilc);
        c.Emit(Mono.Cecil.Cil.OpCodes.Ldarg, 0);
        c.EmitDelegate((Sim s) => { moleculeHookSimRef = s; });
        c.GotoNext(MoveType.After,
          i => i.MatchCallOrCallvirt(typeof(IEnumerator<Part>).GetMethod("get_Current")));
        c.Emit(Mono.Cecil.Cil.OpCodes.Dup);
        c.EmitDelegate((Part p) => { moleculeHookPart = p; });
        c.GotoNext(MoveType.After,
          i => i.MatchCallOrCallvirt(typeof(Molecule).GetMethod("method_1115")));
        c.GotoNext(MoveType.After,
          i => i.MatchCallOrCallvirt(typeof(Molecule).GetMethod("method_1117")));
        c.GotoNext(MoveType.After,
          i => i.MatchLdloc(6));
        c.GotoNext(MoveType.After,
          i => i.MatchLdloc(6));
        c.EmitDelegate(MoleculeSpawnHook);
        c.EmitDelegate(() => { moleculeHookSimRef = null; });
      }
    );
    preSimHook = new ILHook(
      typeof(Sim).GetMethod("method_1827", BF.Public | BF.Instance),
      ilc => {
        ILCursor c = new(ilc);
        c.Emit(Mono.Cecil.Cil.OpCodes.Ldarg, 0);
        c.EmitDelegate((Sim s) => {
          var cycle = s.method_1818(); 
          if (cycle == 0 && maybeActiveER is not null) { 
            ExtransmissionsMod.maybeActiveER.SimReset(); 
          }
        });
      }
    ); 
  }

  public override void Unload() {
    puzzleinfoscreen_method_1275 = null;
    seb_method_2000 = null;
    h2 = null;
    h = null;
    moleculeSpawnHook = null;
    preSimHook = null; 
  }

  public static void Log(string s) => Logger.Log($"[extransmissions] {s}");
}
