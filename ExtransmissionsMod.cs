using Quintessential;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;
using MonoMod;
using Quintessential.Serialization;
using YamlDotNet.Core;
using MonoMod.Utils;
using System.Runtime.CompilerServices;

namespace Extransmissions;

using PartType = class_139;
using PartTypes = class_191;
using Permissions = enum_149;
using AtomTypes = class_175;
using Texture = class_256;
using PartInfo = class_236;
using VanillaAtoms = Brimstone.API.VanillaAtoms;
using BF = System.Reflection.BindingFlags;
using MI = System.Reflection.MethodInfo;


using MHS = ExtransmissionsMod.MoleculeHookState; // <- I'm lazy
//dotnet build;rm Extransmissions.dll;cp bin/Debug/net4.5.2/Extransmissions.dll ./
public class ExtransmissionsMod : QuintessentialMod {
  public List<Func<Sim,bool>> shouldSuppressOutputs = new();
  public AtomType RandomAtom;
  internal static Texture origInputBase;
  internal static Texture origInputRing;
  internal static Texture origInputGloss;
  internal static Texture origInputBond;
  internal static Texture origOutputBase;
  internal static Texture origOutputRing;
  internal static Texture origOutputGloss;
  internal static Texture origOutputBond;

  internal enum MoleculeHookState {
    OFF, PRE_SOLVE_DRAW_INPUTS, DRAW_OUTPUTS
  }
  internal static MoleculeHookState molHookState = MoleculeHookState.OFF;
  internal static SolutionEditorBase molHookStateSeb = null;
  internal static readonly Random random = new();

  internal static ConditionalWeakTable<SolutionEditorBase, ExtraRules> extraRulesTable = new();
  internal static ExtraRules maybeLastExtraRulesCreatedBySolutionInit = null;
  internal static Dictionary<Molecule, int> inputMolMap = new();
  internal static Dictionary<Molecule, int> outputMolMap = new();

  public override void Load() {
  }
 
  internal void AcceptExtraOutputs(List<Part> list, Part item9, Sim s) { 
    Solution method_1817() {
      return s.field_3818.method_502(); 
    }
    bool method_1845(HexIndex a, HexRotation b, Molecule c, Molecule d) {
      return (bool)typeof(Sim).GetMethod("method_1845", BF.NonPublic | BF.Static)
      .Invoke(s, new object[] { a, b, c, d });
    }
    bool method_1833(Molecule a, List<Part> b) {
      return (bool)typeof(Sim).GetMethod("method_1833", BF.NonPublic | BF.Instance)
      .Invoke(s, new object[] { a, b });
    }
    void method_1856(Sound param_5408) {
      typeof(Sim).GetMethod("method_1856", BF.NonPublic | BF.Instance)
      .Invoke(s, new object[] { param_5408 });
    }
    int method_1846(HexIndex param_5385, Molecule param_5386, Molecule param_5387) {
      return (int)typeof(Sim).GetMethod("method_1846", BF.NonPublic | BF.Static)
      .Invoke(s, new object[] { param_5385, param_5386, param_5387 });
    }
    //
    var seb = s.field_3818;
    var ER =
      extraRulesTable.GetValue(seb,
      (s) => new ExtraRules(maybeLastExtraRulesCreatedBySolutionInit, ExtraRules.SebToPuzzle(s)));
    
    if(s is null) {return ;}

    Molecule maybeOrigOutputMol = item9.method_1185(method_1817());
    if (outputMolMap.ContainsKey(maybeOrigOutputMol)
        && ER.outputMolRules.TryGetValue(outputMolMap[maybeOrigOutputMol], out var maybeOR)
        && maybeOR is not null
        && maybeOR.acceptedMolecules is not null) {
      foreach (var acceptedMolModel in maybeOR.acceptedMolecules()) {
        Molecule m = acceptedMolModel.Molecule.FromModel();
        {
          PartSimState partSimState5 = s.field_3821[item9];
          HexIndex hexIndex9 = item9.method_1161();
          HexRotation hexRotation = item9.method_1163();
          Molecule origTRUE = item9.method_1185(method_1817()); // <- replace?
          Molecule orig = m;
          if (item9.method_1159().field_1553) {
            Maybe<Molecule> origShift = s.method_1848(hexIndex9 + orig.method_1100().Keys.First().Rotated(hexRotation));
            if (!shouldSuppressOutputs.Any(e => e(s)) 
                && origShift.method_1085() 
                && !method_1833(origShift.method_1087(), list) 
                && method_1845(hexIndex9, hexRotation, orig, origShift.method_1087())) {
              s.field_3823.Remove(origShift.method_1087());
              partSimState5.field_2730 = Math.Min(partSimState5.field_2730 + 1, item9.method_1169());
              partSimState5.field_2743 = true;
              method_1856(class_238.field_1991.field_1868); 
            } 
          } 
        }
      }
    } 
  }

  internal delegate void origEditorMethod925(Molecule param_4569, Vector2 param_4570, HexIndex param_4571, float param_4572, float param_4573, float param_4574, float param_4575, bool param_4576, SolutionEditorBase param_4577);
  internal static void OnEditorMethod925(
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
    var ER = param_4577 is not null ?
      extraRulesTable.GetValue(param_4577, (s) => new ExtraRules(maybeLastExtraRulesCreatedBySolutionInit, ExtraRules.SebToPuzzle(s)))
      : maybeLastExtraRulesCreatedBySolutionInit;
    if (molHookState == MHS.PRE_SOLVE_DRAW_INPUTS
        && inputMolMap.ContainsKey(molecule)
        && ER is not null
        && ER.inputMolRules.TryGetValue(inputMolMap[molecule], out var maybeIR)
        && maybeIR is not null
        && maybeIR.displayMolecules is not null) {
      var displayMolecules = maybeIR.displayMolecules();
      float molCount = (float)displayMolecules.Count;
      Molecule chosenMol =
        displayMolecules[(int)Math.Floor(gTime % molCount)].Molecule.FromModel();

      orig(chosenMol, param_4570, param_4571, param_4572, param_4573, param_4574, param_4575, param_4576, param_4577);
      return;
    }
    if (molHookState == MHS.DRAW_OUTPUTS
        && outputMolMap.ContainsKey(molecule)
        && ER is not null
        && ER.outputMolRules.TryGetValue(outputMolMap[molecule], out var maybeOR)
        && maybeOR is not null
        && maybeOR.displayMolecules is not null) {
      var displayMolecules = maybeOR.displayMolecules();
      float molCount = (float)displayMolecules.Count;
      Molecule chosenMol =
        displayMolecules[(int)Math.Floor(gTime % molCount)].Molecule.FromModel();

      orig(chosenMol, param_4570, param_4571, param_4572, param_4573, param_4574, param_4575, param_4576, param_4577);
      return;
    }
    orig(molecule, param_4570, param_4571, param_4572, param_4573, param_4574, param_4575, param_4576, param_4577);
    return;
  }

  internal static void OnSebMethod2000(On.SolutionEditorBase.orig_method_2000 orig,
      class_236 param_5573, Vector2 param_5574, Molecule molecule, bool param_5575) {

    bool altTexture = random.Next() > (Int32.MaxValue / 2);
    ExtraRules.IOTextures maybeAltTextureInput = null;
    ExtraRules.IOTextures maybeAltTextureOutput = null;
    var maybeActiveER = maybeLastExtraRulesCreatedBySolutionInit;
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
  internal static void OnSolLoad(
      Action<PuzzleInfoScreen, Solution> orig,
      PuzzleInfoScreen self,
      Solution param_5012) {
    Puzzle puzzle = param_5012.method_1934();
    Log($"Reading solution {param_5012.field_3915} @ {puzzle.field_2766}");
    ExtraRules extraRules = new(null, puzzle);

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
    maybeLastExtraRulesCreatedBySolutionInit = extraRules;
    orig(self, param_5012);
  }



  //method_1167()
  internal static Part moleculeHookPart = null;
  internal static Sim moleculeHookSimRef = null;
  private static HashSet<CyclesSeen> cyclesSeen = new();
  internal static int simIterCounter = 0;
  internal record struct CyclesSeen {
    internal SolutionEditorBase seb;
    internal int cycle;
  }
  internal Molecule MoleculeSpawnHook(Molecule original) {
    int moleculeHookIdxNumber = moleculeHookPart is not null ? moleculeHookPart.method_1167() : -1;
    if (maybeLastExtraRulesCreatedBySolutionInit is not null
        && moleculeHookPart is not null
        && moleculeHookIdxNumber >= 0
        && maybeLastExtraRulesCreatedBySolutionInit.inputMolRules.TryGetValue(moleculeHookIdxNumber, out var maybeIR)
        && maybeIR is not null
        && maybeIR.chooseSpawnMolecule is not null) {
      //seb.method_503()
      SolutionEditorBase seb = moleculeHookSimRef?.field_3818;
      if (seb is not null) {
        var seb_play_status = seb.method_503();
        if (seb_play_status == enum_128.Stopped) {
          maybeLastExtraRulesCreatedBySolutionInit.SimReset();
          cyclesSeen = new();
        }
        if (cyclesSeen.Contains(new CyclesSeen() { seb = seb, cycle = moleculeHookSimRef?.method_1818() ?? -1 })) {
          return original;
        }
        else {
          cyclesSeen.Add(new CyclesSeen() { seb = seb, cycle = moleculeHookSimRef?.method_1818() ?? -1 });
        }
      }
      Molecule alternate = maybeIR.chooseSpawnMolecule(
        moleculeHookSimRef, seb,
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
    RandomAtom = Brimstone.API.CreateNormalAtom(81, "Extransmissions", "random",
      pathToSymbol: "textures/atoms/extransmissions_random_symbol",
      pathToDiffuse: "textures/atoms/extransmissions_random_diffuse");
    QApi.AddAtomType(RandomAtom);
  }


  // editor.method_925 <- RENDER ATOM.
  // Seb.method_2000 <- Draw i/o base
  public Hook seb_method_2000;
  public ILHook h2;
  public Hook h;
  public ILHook moleculeSpawnHook;
  public ILHook preSimHook;
  public ILHook outputAcceptHook;

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
        //c.EmitDelegate(() => { moleculeHookSimRef = null; });
      }
    );
    preSimHook = new ILHook(
      typeof(Sim).GetMethod("method_1827", BF.Public | BF.Instance),
      ilc => {
        ILCursor c = new(ilc);
        c.Emit(Mono.Cecil.Cil.OpCodes.Ldarg, 0);
        c.EmitDelegate((Sim s) => {
          var cycle = s.method_1818();
          if (cycle == 0) {
            maybeLastExtraRulesCreatedBySolutionInit?.SimReset();
            var seb = s.field_3818;
            extraRulesTable.TryGetValue(seb, out var maybeSimEr);
            maybeSimEr?.SimReset();
          }
        });
      }
    );

    outputAcceptHook = new ILHook(
      typeof(Sim).GetMethod("orig_method_1832", BF.NonPublic | BF.Instance),
      ilc => {
        ILCursor c = new(ilc); 
        c.GotoNext(MoveType.After,
          i => i.MatchCallOrCallvirt(typeof(Sim).GetMethod("method_1848"))); 
        c.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, 0);
        c.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, 88);
        c.Emit(Mono.Cecil.Cil.OpCodes.Ldarg, 0);
        c.EmitDelegate(AcceptExtraOutputs); 
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
    outputAcceptHook = null;
  }

  internal static void Log(string s) => Logger.Log($"[extransmissions] {s}");

}
