using BattleTech;
using BattleTech.Framework;
using BattleTech.Save;
using BattleTech.Save.SaveGameStructure;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using BattleTech.UI.Tooltips;
using CustAmmoCategories;
using CustomUnits;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace CustomPilotProgression {
  public class PilotKillRecord {
    [JsonIgnore]
    public MechDef mech {
      get {
        if(this.unitType != UnitType.Mech) { return null; }
        if(f_mech == null) {
          if(UnityGameInstance.BattleTechGame.DataManager.MechDefs.TryGet(this.unitDefId, out var u)) {
            f_mech = u;
          } else {
            f_mech = new MechDef();
            f_mech.Description = new DescriptionDef();
          }
        }
        return f_mech;
      }
    }
    [JsonIgnore]
    public VehicleDef vehicle {
      get {
        if(this.unitType != UnitType.Vehicle) { return null; }
        if(f_vehicle == null) {
          if(UnityGameInstance.BattleTechGame.DataManager.VehicleDefs.TryGet(this.unitDefId, out var u)) {
            f_vehicle = u;
          } else {
            f_vehicle = new VehicleDef();
            f_vehicle.Description = new DescriptionDef();
          }
        }
        return f_vehicle;
      }
    }
    [JsonIgnore]
    public TurretDef turret {
      get {
        if(this.unitType != UnitType.Turret) { return null; }
        if(f_turret == null) {
          if(UnityGameInstance.BattleTechGame.DataManager.TurretDefs.TryGet(this.unitDefId, out var u)) {
            f_turret = u;
          } else {
            f_turret = new TurretDef();
            f_turret.Description = new DescriptionDef();
          }
        }
        return f_turret;
      }
    }
    [JsonIgnore]
    public BuildingDef buildging {
      get {
        if(this.unitType != UnitType.Building) { return null; }
        if(f_buildging == null) {
          if(UnityGameInstance.BattleTechGame.DataManager.BuildingDefs.TryGet(this.unitDefId, out var u)) {
            f_buildging = u;
          } else {
            f_buildging = new BuildingDef();
            f_buildging.Description = new DescriptionDef();
          }
        }
        return f_buildging;
      }
    }
    [JsonIgnore]
    public PilotDef pilotDef {
      get {
        if(f_pilotDef == null) {
          if(UnityGameInstance.BattleTechGame.DataManager.PilotDefs.TryGet(this.pilotDefId, out var u)) {
            f_pilotDef = u;
          } else {
            f_pilotDef = new PilotDef();
            f_pilotDef.Description = new HumanDescriptionDef();
          }
        }
        return f_pilotDef;
      }
    }
    [JsonIgnore]
    private MechDef f_mech = null;
    [JsonIgnore]
    private VehicleDef f_vehicle = null;
    [JsonIgnore]
    private TurretDef f_turret = null;
    [JsonIgnore]
    private BuildingDef f_buildging = null;
    [JsonIgnore]
    private PilotDef f_pilotDef = null;

    public UnitType unitType { get; set; } = UnitType.UNDEFINED;
    public string pilotDefId { get; set; } = string.Empty;
    public string unitDefId { get; set; } = string.Empty;
    public bool ejected { get; set; } = false;
  }
  public class ContractCombatStat {
    public UnitCombatStatistic statistic = new UnitCombatStatistic();
    public string Name;
  }
  public class PilotCodex {
    public string id { get; set; } = string.Empty;
    public UnitCombatStatistic overallStat = new UnitCombatStatistic();
    public List<ContractCombatStat> contractStat = new List<ContractCombatStat>();
    public List<PilotKillRecord> raw_kills { get; set; } = new List<PilotKillRecord>();
  }
  public class PilotsCodexes {
    public Dictionary<string, PilotCodex> codex = new Dictionary<string, PilotCodex>();
  }
  public class ContractData {
    public string DisplayName = string.Empty;
  }
  public class ContractReference {
    public WeakReference<Contract> contract;
    public string refGUID = string.Empty;
    public ContractData data = null;
    public ContractReference(Contract contract) {
      this.contract = new WeakReference<Contract>(contract);
    }
  }
  public static class PilotsKillsRecordsHelper {
    public static readonly string STATISTIC_NAME = "PILOTS_CODEX";
    public static readonly string CONTRACTS_STATISTIC_NAME = "CONTRACTS_DATA";
    private static PilotsCodexes data = new PilotsCodexes();
    private static Dictionary<int, ContractReference> refContracts = new Dictionary<int, ContractReference>();
    public static void AddCombatStatistic(this PilotDef pilot, UnitCombatStatistic add) {
      if(data.codex.TryGetValue(pilot.Description.Id, out var codex) == false) {
        codex = new PilotCodex();
        data.codex[pilot.Description.Id] = codex;
      }
      codex.overallStat.MergeStatistic(add);
      if(UnityGameInstance.BattleTechGame.Simulation == null) { return; }
      if(UnityGameInstance.BattleTechGame.Simulation.CompletedContract == null) { return; }
      ContractCombatStat contractStat = new ContractCombatStat();
      contractStat.statistic = add.Copy();
      contractStat.Name = UnityGameInstance.BattleTechGame.Simulation.CompletedContract.GetDisplayName();
      codex.contractStat.Add(contractStat);
    }
    public static string GetDisplayName(this Contract contract) {
      if(refContracts.TryGetValue(contract.GetHashCode(), out var reference) == false) { return contract.Name; }
      if(reference.data == null) { return contract.Name; }
      if(string.IsNullOrEmpty(reference.data.DisplayName)) { return contract.Name; }
      return reference.data.DisplayName;
    }
    public static void PreSerialization(GameInstanceSave save) {
      Log.M?.TWL(0, "Save.PreSerialization");
      HashSet<int> to_del = new HashSet<int>();
      Dictionary<string, ContractData> guidsContracts = new Dictionary<string, ContractData>();
      foreach(var cr in refContracts) {
        if(cr.Value.contract.TryGetTarget(out var contract)) {
          if(contract.GUID == null) {
            contract.GUID = "RESTORE_" + Guid.NewGuid().ToString();
          }else if(contract.GUID == string.Empty) {
            contract.GUID = "TO_EMPTY_" + Guid.NewGuid().ToString();
          }
          cr.Value.refGUID = contract.GUID;
          if(cr.Value.data == null) { cr.Value.data = new ContractData(); }
          guidsContracts[contract.GUID] = cr.Value.data;
        } else {
          to_del.Add(cr.Key);
        }
      }
      foreach(int d in to_del) { refContracts.Remove(d); }
      if(save.SimGameSave.simGameState != null) {
        Log.M?.WL(1, "DATA:");
        Log.M?.WL(0, JsonConvert.SerializeObject(guidsContracts,Formatting.Indented));
        Statistic guids = save.SimGameSave.simGameState.CompanyStats.GetStatistic(PilotsKillsRecordsHelper.CONTRACTS_STATISTIC_NAME);
        if(guids == null) {
          UnityGameInstance.BattleTechGame.Simulation.CompanyStats.AddStatistic<string>(PilotsKillsRecordsHelper.CONTRACTS_STATISTIC_NAME, JsonConvert.SerializeObject(guidsContracts));
        } else {
          guids.SetValue<string>(JsonConvert.SerializeObject(guidsContracts));
        }
      }
    }
    public static void PostSerialization(GameInstanceSave save) {
      HashSet<int> to_del = new HashSet<int>();
      foreach(var cr in refContracts) {
        if(cr.Value.contract.TryGetTarget(out var contract)) {
          if(string.IsNullOrEmpty(contract.GUID)) { continue; }
          if(contract.GUID.StartsWith("TO_NULL_")) { contract.GUID = null; }
          if(contract.GUID.StartsWith("TO_EMPTY_")) { contract.GUID = string.Empty; }
        } else {
          to_del.Add(cr.Key);
        }
      }
      foreach(int d in to_del) { refContracts.Remove(d); }
    }
    public static void PostDeserialization(GameInstanceSave save) {
      HashSet<int> to_del = new HashSet<int>();
      Log.M?.TWL(0, $"Save.PostDeserialization");
      foreach(var cr in refContracts) {
        if(cr.Value.contract.TryGetTarget(out var contract)) {
          Log.M?.WL(1, $"{contract.mapName} guid:{contract.GUID}");
          if(string.IsNullOrEmpty(contract.GUID)) { continue; }
          cr.Value.refGUID = contract.GUID;
          if(contract.GUID.StartsWith("TO_NULL_")) { contract.GUID = null; }
          if(contract.GUID.StartsWith("TO_EMPTY_")) { contract.GUID = string.Empty; }
        } else {
          to_del.Add(cr.Key);
        }
      }
      foreach(int d in to_del) { refContracts.Remove(d); }
    }
    public static void ReconnectContracts(SimGameState sim) {
      var stat = sim.CompanyStats.GetStatistic(PilotsKillsRecordsHelper.CONTRACTS_STATISTIC_NAME);
      Log.M?.TWL(0, $"Save.ReconnectContracts");
      if(stat == null) { return; }
      var guids = JsonConvert.DeserializeObject<Dictionary<string, ContractData>>(stat.Value<string>());
      foreach(var cr in refContracts) {
        if(string.IsNullOrEmpty(cr.Value.refGUID)) { continue; }
        if(cr.Value.contract.TryGetTarget(out var contract)) {
          Log.M?.WL(1, $"{contract.mapName} guid:{contract.GUID} refguid:{cr.Value.refGUID}");
          if(guids.TryGetValue(cr.Value.refGUID, out var data)) {
            Log.M?.WL(2, $"data:{data.DisplayName}");
            cr.Value.data = data;
          }
        }
      }
    }
      public static void RegisterReference(this Contract contract) {
      refContracts[contract.GetHashCode()] = new ContractReference(contract);
    }
    public static ContractReference GetReference(this Contract contract) {
      if(refContracts.TryGetValue(contract.GetHashCode(), out var reference)) {
        return reference;
      }
      reference = new ContractReference(contract);
      refContracts[contract.GetHashCode()] = new ContractReference(contract);
      return reference;
    }
    public class CombatKilledRec {
      public AbstractActor target { get; set; } = null;
      public bool ejected { get; set; } = false;
      public CombatKilledRec(AbstractActor u, bool e) { this.target = u; this.ejected = e; }
    }
    private static Dictionary<string, List<CombatKilledRec>> combatKills = new Dictionary<string, List<CombatKilledRec>>();
    public static void AddCombatKill(AbstractActor attacker, AbstractActor target, bool ejected) {
      Pilot pilot = attacker.GetPilot();
      if(pilot == null) { return; }
      if(pilot.Description == null) { return; }
      if(string.IsNullOrEmpty(pilot.Description.Id)) { return; }
      if(combatKills.ContainsKey(pilot.Description.Id) == false) { combatKills[pilot.Description.Id] = new List<CombatKilledRec>(); }
      combatKills[pilot.Description.Id].Add(new CombatKilledRec(target, ejected));
    }
    public static void ClearCombat() {
      combatKills.Clear();
    }
    public static readonly string MECH_STAT_ICON = "!MECH_STAT_ICON!";
    public static readonly string VEHICLE_STAT_ICON = "!VEHICLE_STAT_ICON!";
    public static string GetStatIconByType(this PilotableActorDef unit) {
      if(unit is MechDef mech) {
        string icon_name = MECH_STAT_ICON;
        if(mech.IsSquad()) {
          icon_name = CustomAmmoCategories.Settings.StatisticOnResultScreenBattleArmorSprite;
          if((string.IsNullOrEmpty(icon_name)) || (UIManager.Instance.dataManager.Exists(BattleTechResourceType.Sprite, icon_name) == false)) { icon_name = VEHICLE_STAT_ICON; }
        } else if(mech.IsVehicle()) {
          icon_name = VEHICLE_STAT_ICON;
        }
        return icon_name;
      } else if(unit is VehicleDef vehcile) {
        return VEHICLE_STAT_ICON;
      } else if(unit is TurretDef turret) {
        string icon_name = VEHICLE_STAT_ICON;
        icon_name = CustomAmmoCategories.Settings.StatisticOnResultScreenTurretSprite;
        if((string.IsNullOrEmpty(icon_name)) || (UIManager.Instance.dataManager.Exists(BattleTechResourceType.Sprite, icon_name) == false)) { icon_name = VEHICLE_STAT_ICON; }
        return icon_name;
      } else {
        return VEHICLE_STAT_ICON;
      }
    }
    public static string GetStatIcon(this PilotableActorDef unit) {
      string icon_name = unit.Description.Icon;
      if((string.IsNullOrEmpty(icon_name)) || (UIManager.Instance.dataManager.Exists(BattleTechResourceType.Sprite, icon_name) == false)) { return unit.GetStatIconByType(); }
      return icon_name;
    }
    public static PilotCodex GetCodex(this PilotDef pilot) {
      if(data.codex.TryGetValue(pilot.Description.Id, out var result)) {
        return result;
      }
      result = new PilotCodex();
      result.id = pilot.Description.Id;
      data.codex.Add(result.id, result);
      return result;
    }
    public static void FlushCombatKills(AbstractActor attacker) {
      Pilot pilot = attacker.GetPilot();
      if(pilot == null) { return; }
      if(pilot.Description == null) { return; }
      if(string.IsNullOrEmpty(pilot.Description.Id)) { return; }
      if(combatKills.TryGetValue(pilot.Description.Id, out var kills)) {
        if(PilotsKillsRecordsHelper.data.codex.TryGetValue(pilot.Description.Id, out var codex) == false) {
          codex = new PilotCodex();
          codex.id = pilot.Description.Id;
          PilotsKillsRecordsHelper.data.codex.Add(codex.id, codex);
        }
        foreach(var kill in kills) {
          var kill_rec = new PilotKillRecord();
          kill_rec.pilotDefId = kill.target.GetPilot().Description.Id;
          kill_rec.unitDefId = kill.target.PilotableActorDef.Description.Id;
          kill_rec.ejected = kill.ejected;
          if(kill.target is Mech mech) {
            kill_rec.unitType = UnitType.Mech;
          } else if(kill.target is Vehicle vehicle) {
            kill_rec.unitType = UnitType.Vehicle;
          } else if(kill.target is Turret turret) {
            kill_rec.unitType = UnitType.Turret;
          }
          codex.raw_kills.Add(kill_rec);
        }
        combatKills.Remove(pilot.Description.Id);
      }
    }
    public static string GetCompressed() {
      string raw_json = JsonConvert.SerializeObject(data);
      var raw_json_bytes = Encoding.UTF8.GetBytes(raw_json);
      using(var msi = new MemoryStream(raw_json_bytes))
      using(var mso = new MemoryStream()) {
        using(var gs = new GZipStream(mso, CompressionMode.Compress)) {
          msi.CopyTo(gs);
        }
        return Convert.ToBase64String(mso.ToArray());
      }
    }
    public static void Uncompress(string compressed_str) {
      using(var msi = new MemoryStream(Convert.FromBase64String(compressed_str)))
      using(var mso = new MemoryStream()) {
        using(var gs = new GZipStream(msi, CompressionMode.Decompress)) {
          gs.CopyTo(mso);
        }
        data = JsonConvert.DeserializeObject<PilotsCodexes>(Encoding.UTF8.GetString(mso.ToArray()));
        Log.M?.WL(0, "Uncompress");
        Log.M?.WL(0, JsonConvert.SerializeObject(data,Formatting.Indented));
      }
    }
    public static void Dehydrate(SimGameState sim) {
      Statistic codex = sim.CompanyStats.GetStatistic(PilotsKillsRecordsHelper.STATISTIC_NAME);
      if(codex == null) {
        sim.CompanyStats.AddStatistic<string>(PilotsKillsRecordsHelper.STATISTIC_NAME, GetCompressed());
      } else {
        codex.SetValue<string>(GetCompressed());
      }
    }
    public static void Rehydrate(SimGameState sim) {
      Statistic codex = sim.CompanyStats.GetStatistic(PilotsKillsRecordsHelper.STATISTIC_NAME);
      if(codex != null) {
        Uncompress(codex.Value<string>());
      }
    }
  }
  [HarmonyPatch(typeof(UnitCombatStatisticHelper), "AddKilled")]
  public static class UnitCombatStatisticHelper_AddKilled {
    static void Postfix(AbstractActor attacker, AbstractActor victim, bool ejected) {
      try {
        Log.M?.TWL(0, $"UnitCombatStatisticHelper.AddKilled {attacker.PilotableActorDef.Description.Id} target:{victim.PilotableActorDef.Description.Id} ejected:{ejected}");
        PilotsKillsRecordsHelper.AddCombatKill(attacker, victim, ejected);
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        AbstractActor.logger.LogException(e);
      }
    }
  }
  [HarmonyPatch(typeof(CombatGameState), "OnCombatGameDestroyed")]
  public static class CombatGameState_OnCombatGameDestroyed {
    static void Prefix(CombatGameState __instance) {
      try {
        PilotsKillsRecordsHelper.ClearCombat();
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        AbstractActor.logger.LogException(e);
      }
    }
  }
  public class SGBarracksKilledData : MonoBehaviour {
    public class KilledIcon {
      public Image background;
      public Image foreground;
      public HBSTooltip tooltip;
      public KilledIcon(GameObject iconPrototype) {
        background = iconPrototype.GetComponent<Image>();
        Image[] images = iconPrototype.GetComponentsInChildren<Image>(true);
        foreach(var img in images) {
          if(img.gameObject != iconPrototype) { foreground = img; break; }
        }
        this.tooltip = iconPrototype.GetComponent<HBSTooltip>();
        if(this.tooltip == null) { this.tooltip = iconPrototype.AddComponent<HBSTooltip>(); }
      }
    }
    public SGBarracksServicePanel parent = null;
    public GameObject killedGO = null;
    public GridLayoutGroup killedLayout = null;
    public LocalizableText header = null;
    public List<KilledIcon> icons = new List<KilledIcon>();
    public Image DefaultVehicleSprite = null;
    public Image DefaultMechSprite = null;
    public Pilot pilot = null;

    public GameObject outDamageGO = null;
    public LocalizableText outDamageLabel = null;
    public LocalizableText outDamageStat = null;
    public GameObject attacksGO = null;
    public LocalizableText attacksLabel = null;
    public LocalizableText attacksStat = null;

    public GameObject shotsGO = null;
    public LocalizableText shotsLabel = null;
    public LocalizableText shotsStat = null;
    public GameObject shotsSuccessGO = null;
    public LocalizableText shotsSuccessLabel = null;
    public LocalizableText shotsSuccessStat = null;

    public GameObject criticalsGO = null;
    public LocalizableText criticalsLabel = null;
    public LocalizableText criticalsStat = null;
    public GameObject criticalsSuccessGO = null;
    public LocalizableText criticalsSuccessLabel = null;
    public LocalizableText criticalsSuccessStat = null;

    public GameObject incshotsGO = null;
    public LocalizableText incshotsLabel = null;
    public LocalizableText incshotsStat = null;
    public GameObject incshotsSuccessGO = null;
    public LocalizableText incshotsSuccessLabel = null;
    public LocalizableText incshotsSuccessStat = null;

    public GameObject inccriticalsGO = null;
    public LocalizableText inccriticalsLabel = null;
    public LocalizableText inccriticalsStat = null;
    public GameObject inccriticalsSuccessGO = null;
    public LocalizableText inccriticalsSuccessLabel = null;
    public LocalizableText inccriticalsSuccessStat = null;
    public static SGBarracksKilledData CreateInstance(SGBarracksServicePanel parent) {
      SGBarracksKilledData instance = parent.gameObject.AddComponent<SGBarracksKilledData>();
      GridLayoutGroup layoutSrc = null;
      var layouts = parent.deploymentsLabel.GetComponentsInParent<GridLayoutGroup>(true);
      foreach(var l in layouts) { if(l.gameObject.transform.name == "layout-battlestats") { layoutSrc = l; break; } }
      GameObject headeredLayout = layoutSrc.gameObject.transform.parent.gameObject;
      instance.killedGO = GameObject.Instantiate(headeredLayout);
      instance.killedGO.transform.SetParent(headeredLayout.transform.parent);
      instance.killedGO.transform.SetSiblingIndex(headeredLayout.transform.parent.GetSiblingIndex() + 1);
      instance.killedGO.transform.localScale = Vector3.one;
      instance.killedGO.name = "obj-Killed";
      instance.killedLayout = instance.killedGO.GetComponentInChildren<GridLayoutGroup>();
      while(instance.killedLayout.transform.childCount > 0) {
        GameObject.DestroyImmediate(instance.killedLayout.transform.GetChild(0).gameObject);
      }
      instance.killedLayout.cellSize = new Vector2(40f,40f);
      instance.killedLayout.spacing = new Vector2(5f, 5f);
      //instance.killedLayout.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(-10f, -30f);
      instance.killedLayout.constraintCount = 12;
      instance.header = instance.killedGO.GetComponentInChildren<LocalizableText>();
      instance.header.SetText("Невинно убиенные");
      GameObject mechSpriteGO = UIManager.Instance.dataManager.PooledInstantiate("uixPrfIcon_AA_mechKillStamp", BattleTechResourceType.UIModulePrefabs, parent: instance.killedLayout.transform);
      mechSpriteGO.transform.localScale = Vector3.one;
      mechSpriteGO.SetActive(false);
      Image[] images = mechSpriteGO.GetComponentsInChildren<Image>(true);
      foreach(var img in images) {
        if(img.gameObject != mechSpriteGO) { instance.DefaultMechSprite = img; break; }
      }
      GameObject vehicleSpriteGO = UIManager.Instance.dataManager.PooledInstantiate("uixPrfIcon_AA_vehicleKillStamp", BattleTechResourceType.UIModulePrefabs, parent: instance.killedLayout.transform);
      vehicleSpriteGO.transform.localScale = Vector3.one;
      vehicleSpriteGO.SetActive(false);
      images = vehicleSpriteGO.GetComponentsInChildren<Image>(true);
      foreach(var img in images) {
        if(img.gameObject != vehicleSpriteGO) { instance.DefaultVehicleSprite = img; break; }
      }
      GameObject statSrc = parent.biographyScrollbar.content.gameObject.FindObject<RectTransform>("obj-BattleStats",false).gameObject.FindObject<RectTransform>("battlestat-Deployments", false).gameObject;

      instance.attacksGO = GameObject.Instantiate(statSrc);
      instance.attacksGO.name = "battlestat-Attacks";
      instance.attacksGO.transform.SetParent(statSrc.transform.parent);
      instance.attacksGO.transform.localScale = Vector3.one;
      instance.attacksLabel = instance.attacksGO.FindObject<LocalizableText>("label", false);
      instance.attacksStat = instance.attacksGO.FindObject<LocalizableText>("stat", false);

      instance.shotsGO = GameObject.Instantiate(statSrc);
      instance.shotsGO.name = "battlestat-shots";
      instance.shotsGO.transform.SetParent(statSrc.transform.parent);
      instance.shotsGO.transform.localScale = Vector3.one;
      instance.shotsLabel = instance.shotsGO.FindObject<LocalizableText>("label", false);
      instance.shotsStat = instance.shotsGO.FindObject<LocalizableText>("stat", false);

      instance.criticalsGO = GameObject.Instantiate(statSrc);
      instance.criticalsGO.name = "battlestat-criticals";
      instance.criticalsGO.transform.SetParent(statSrc.transform.parent);
      instance.criticalsGO.transform.localScale = Vector3.one;
      instance.criticalsLabel = instance.criticalsGO.FindObject<LocalizableText>("label", false);
      instance.criticalsStat = instance.criticalsGO.FindObject<LocalizableText>("stat", false);

      instance.outDamageGO = GameObject.Instantiate(statSrc);
      instance.outDamageGO.name = "battlestat-Damage";
      instance.outDamageGO.transform.SetParent(statSrc.transform.parent);
      instance.outDamageGO.transform.localScale = Vector3.one;
      instance.outDamageLabel = instance.outDamageGO.FindObject<LocalizableText>("label", false);
      instance.outDamageStat = instance.outDamageGO.FindObject<LocalizableText>("stat", false);

      instance.shotsSuccessGO = GameObject.Instantiate(statSrc);
      instance.shotsSuccessGO.name = "battlestat-shotssuccess";
      instance.shotsSuccessGO.transform.SetParent(statSrc.transform.parent);
      instance.shotsSuccessGO.transform.localScale = Vector3.one;
      instance.shotsSuccessLabel = instance.shotsSuccessGO.FindObject<LocalizableText>("label", false);
      instance.shotsSuccessStat = instance.shotsSuccessGO.FindObject<LocalizableText>("stat", false);

      instance.criticalsSuccessGO = GameObject.Instantiate(statSrc);
      instance.criticalsSuccessGO.name = "battlestat-critcusuccess";
      instance.criticalsSuccessGO.transform.SetParent(statSrc.transform.parent);
      instance.criticalsSuccessGO.transform.localScale = Vector3.one;
      instance.criticalsSuccessLabel = instance.criticalsSuccessGO.FindObject<LocalizableText>("label", false);
      instance.criticalsSuccessStat = instance.criticalsSuccessGO.FindObject<LocalizableText>("stat", false);

      instance.incshotsGO = GameObject.Instantiate(statSrc);
      instance.incshotsGO.name = "battlestat-incshots";
      instance.incshotsGO.transform.SetParent(statSrc.transform.parent);
      instance.incshotsGO.transform.localScale = Vector3.one;
      instance.incshotsLabel = instance.incshotsGO.FindObject<LocalizableText>("label", false);
      instance.incshotsStat = instance.incshotsGO.FindObject<LocalizableText>("stat", false);

      instance.inccriticalsGO = GameObject.Instantiate(statSrc);
      instance.inccriticalsGO.name = "battlestat-inccrits";
      instance.inccriticalsGO.transform.SetParent(statSrc.transform.parent);
      instance.inccriticalsGO.transform.localScale = Vector3.one;
      instance.inccriticalsLabel = instance.inccriticalsGO.FindObject<LocalizableText>("label", false);
      instance.inccriticalsStat = instance.inccriticalsGO.FindObject<LocalizableText>("stat", false);

      var placeholderGO = GameObject.Instantiate(statSrc);
      placeholderGO.name = "battlestat-placeholder";
      placeholderGO.transform.SetParent(statSrc.transform.parent);
      placeholderGO.transform.localScale = Vector3.one;
      var placeholderLabel = placeholderGO.FindObject<LocalizableText>("label", false);
      var placeholderStat = placeholderGO.FindObject<LocalizableText>("stat", false);

      instance.incshotsSuccessGO = GameObject.Instantiate(statSrc);
      instance.incshotsSuccessGO.name = "battlestat-incshotssuccess";
      instance.incshotsSuccessGO.transform.SetParent(statSrc.transform.parent);
      instance.incshotsSuccessGO.transform.localScale = Vector3.one;
      instance.incshotsSuccessLabel = instance.incshotsSuccessGO.FindObject<LocalizableText>("label", false);
      instance.incshotsSuccessStat = instance.incshotsSuccessGO.FindObject<LocalizableText>("stat", false);

      instance.inccriticalsSuccessGO = GameObject.Instantiate(statSrc);
      instance.inccriticalsSuccessGO.name = "battlestat-incshotssuccess";
      instance.inccriticalsSuccessGO.transform.SetParent(statSrc.transform.parent);
      instance.inccriticalsSuccessGO.transform.localScale = Vector3.one;
      instance.inccriticalsSuccessLabel = instance.inccriticalsSuccessGO.FindObject<LocalizableText>("label", false);
      instance.inccriticalsSuccessStat = instance.inccriticalsSuccessGO.FindObject<LocalizableText>("stat", false);

      instance.attacksLabel.SetText("КОЛ-ВО АТАК");
      instance.shotsLabel.SetText("ИСХОД.ВЫСТРЕЛЫ");
      instance.criticalsLabel.SetText("КОЛ-ВО КРИТ.");

      instance.outDamageLabel.SetText("ИСХ.УРОН");
      instance.shotsSuccessLabel.SetText("ИСХ.ТОЧНОСТЬ");
      instance.criticalsSuccessLabel.SetText("ПРОЦ.КРИТ.");

      instance.incshotsLabel.SetText("ВХОД.ВЫСТРЕЛЫ");
      instance.incshotsSuccessLabel.SetText("ВХОД.ТОЧНОСТЬ");

      instance.inccriticalsLabel.SetText("ВХОД.КРИТ.");
      instance.inccriticalsSuccessLabel.SetText("ПРОЦ.КРИТ.");

      placeholderLabel.SetText("");
      placeholderStat.SetText("");
      return instance;
    }
    public void Refresh() {
      Log.M?.TWL(0,$"SGBarracksKilledData.Refresh {this.pilot.pilotDef.Description.Id}");
      var codex = this.pilot.pilotDef.GetCodex();
      if(codex.overallStat.overallCombatDamage > 1000000) {
        this.outDamageStat.SetText($"{Mathf.Round(codex.overallStat.overallCombatDamage/1000f)}K");
      } else {
        this.outDamageStat.SetText($"{Mathf.Round(codex.overallStat.overallCombatDamage)}");
      }
      if(codex.overallStat.attacksCount > 1000000) {
        this.attacksStat.SetText($"{Mathf.Round(codex.overallStat.attacksCount / 1000f)}K");
      } else {
        this.attacksStat.SetText($"{Mathf.Round(codex.overallStat.attacksCount)}");
      }
      if(codex.overallStat.shootsCount > 1000000) {
        this.shotsStat.SetText($"{Mathf.Round(codex.overallStat.shootsCount / 1000f)}K");
      } else {
        this.shotsStat.SetText($"{Mathf.Round(codex.overallStat.shootsCount)}");
      }
      if(codex.overallStat.criticalHitsCount > 1000000) {
        this.criticalsStat.SetText($"{Mathf.Round(codex.overallStat.criticalHitsCount / 1000f)}K");
      } else {
        this.criticalsStat.SetText($"{Mathf.Round(codex.overallStat.criticalHitsCount)}");
      }
      if(codex.overallStat.shootsCount > 0) {
        this.shotsSuccessStat.SetText($"{Mathf.Round((codex.overallStat.successHitsCount / codex.overallStat.shootsCount)*100f)}%");
      } else {
        this.shotsSuccessStat.SetText($"-");
      }
      if(codex.overallStat.criticalSuccessCount > 0) {
        this.criticalsSuccessStat.SetText($"{Mathf.Round((codex.overallStat.criticalSuccessCount / codex.overallStat.criticalHitsCount) * 100f)}%");
      } else {
        this.criticalsSuccessStat.SetText($"-");
      }
      if(codex.overallStat.incomingCriticalsCount > 1000000) {
        this.inccriticalsStat.SetText($"{Mathf.Round(codex.overallStat.incomingCriticalsCount / 1000f)}K");
      } else {
        this.inccriticalsStat.SetText($"{Mathf.Round(codex.overallStat.incomingCriticalsCount)}");
      }
      if(codex.overallStat.incomingShootsCount > 1000000) {
        this.incshotsStat.SetText($"{Mathf.Round(codex.overallStat.incomingShootsCount / 1000f)}K");
      } else {
        this.incshotsStat.SetText($"{Mathf.Round(codex.overallStat.incomingShootsCount)}");
      }
      if(codex.overallStat.incomingShootsCount > 0) {
        this.incshotsSuccessStat.SetText($"{Mathf.Round((codex.overallStat.incomingHitsCount / codex.overallStat.incomingShootsCount) * 100f)}%");
      } else {
        this.incshotsSuccessStat.SetText($"-");
      }
      if(codex.overallStat.incomingCriticalsCount > 0) {
        this.inccriticalsSuccessStat.SetText($"{Mathf.Round((codex.overallStat.incomingCritSuccessCount / codex.overallStat.incomingCriticalsCount) * 100f)}%");
      } else {
        this.inccriticalsSuccessStat.SetText($"-");
      }
      int killed_displayed = Math.Min(36, codex.raw_kills.Count);
      //int killed_displayed = UnityEngine.Random.RandomRangeInt(9, 31);
      if(killed_displayed > this.icons.Count) {
        while(this.icons.Count < killed_displayed) {
          GameObject iconGO = UIManager.Instance.dataManager.PooledInstantiate("uixPrfIcon_AA_mechKillStamp", BattleTechResourceType.UIModulePrefabs, parent: this.killedLayout.transform);
          iconGO.name = $"killed_item_{this.icons.Count}";
          iconGO.transform.localScale = Vector3.one;
          iconGO.SetActive(false);
          this.icons.Add(new KilledIcon(iconGO));
        }
      }
      string icon_name = string.Empty;
      Log.M?.WL(1, $"raw_kills:{codex.raw_kills.Count} display:{killed_displayed}");
      for(int t = 0; t < this.icons.Count; ++t) {
        if(t >= killed_displayed) { this.icons[t].background.gameObject.SetActive(false); continue; }
        this.icons[t].background.gameObject.SetActive(true);
        if(codex.raw_kills.Count <= t) { continue; }
        if(codex.raw_kills[t].mech != null) {
          this.icons[t].tooltip.SetDefaultStateData(codex.raw_kills[t].mech.GetTooltipStateData());
          icon_name = codex.raw_kills[t].mech.GetStatIcon();
        } else
        if(codex.raw_kills[t].vehicle != null) {
          this.icons[t].tooltip.SetDefaultStateData(new BaseDescriptionDef(codex.raw_kills[t].vehicle.Description.Id, codex.raw_kills[t].vehicle.Description.Name, codex.raw_kills[t].vehicle.Description.Details, codex.raw_kills[t].vehicle.Description.Icon).GetTooltipStateData());
          icon_name = codex.raw_kills[t].vehicle.GetStatIcon();
        } else
        if(codex.raw_kills[t].turret != null) {
          this.icons[t].tooltip.SetDefaultStateData(new BaseDescriptionDef(codex.raw_kills[t].turret.Description.Id, codex.raw_kills[t].turret.Description.Name, codex.raw_kills[t].turret.Description.Details, codex.raw_kills[t].turret.Description.Icon).GetTooltipStateData());
          icon_name = codex.raw_kills[t].turret.GetStatIcon();
        }
        if(icon_name == PilotsKillsRecordsHelper.MECH_STAT_ICON) {
          this.icons[t].foreground.sprite = this.DefaultMechSprite.sprite;
        } else if(icon_name == PilotsKillsRecordsHelper.VEHICLE_STAT_ICON) {
          this.icons[t].foreground.sprite = this.DefaultVehicleSprite.sprite;
        } else if (string.IsNullOrEmpty(icon_name) == false) {
          this.icons[t].foreground.sprite = UIManager.Instance.dataManager.GetObjectOfType<Sprite>(icon_name, BattleTechResourceType.Sprite);
        }
      }
    }
  }

  [HarmonyPatch(typeof(SGBarracksMWDetailPanel), "Initialize")]
  public static class SGBarracksMWDetailPanel_Initialize {
    static void Postfix(SGBarracksMWDetailPanel __instance, SimGameState state) {
      try {
        Log.M?.TWL(0, "SGBarracksMWDetailPanel.Initialize");
        if(__instance.servicePanel.gameObject.GetComponent<SGBarracksKilledData>() == null) {
          SGBarracksKilledData.CreateInstance(__instance.servicePanel);
        }
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        UIManager.logger.LogException(e);
      }
    }
  }
  [HarmonyPatch(typeof(SGBarracksServicePanel), "SetPilot")]
  public static class SGBarracksServicePanel_SetPilot {
    static void Postfix(SGBarracksServicePanel __instance, Pilot p) {
      try {
        Log.M?.TWL(0, "SGBarracksServicePanel.SetPilot");
        var killedWidget = __instance.gameObject.GetComponent<SGBarracksKilledData>();
        if(killedWidget == null) { return; }
        killedWidget.pilot = p;
        killedWidget.Refresh();
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        UIManager.logger.LogException(e);
      }
    }
  }
  [HarmonyPatch(typeof(GameInstanceSave), "PreSerialization")]
  [HarmonyPatch(MethodType.Normal)]
  public static class GameInstanceSave_PreSerialization {
    static void Prefix(GameInstanceSave __instance) {
      try {
        PilotsKillsRecordsHelper.PreSerialization(__instance);
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        UIManager.logger.LogException(e);
      }
    }
  }
  [HarmonyPatch(typeof(GameInstanceSave), "PostSerialization")]
  [HarmonyPatch(MethodType.Normal)]
  public static class GameInstanceSave_PostSerialization {
    static void Postfix(GameInstanceSave __instance) {
      try {
        PilotsKillsRecordsHelper.PostSerialization(__instance);
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        UIManager.logger.LogException(e);
      }
    }
  }
  [HarmonyPatch(typeof(GameInstanceSave), "PostDeserialization")]
  [HarmonyPatch(MethodType.Normal)]
  public static class GameInstanceSave_PostDeserialization {
    static void Postfix(GameInstanceSave __instance) {
      try {
        PilotsKillsRecordsHelper.PostDeserialization(__instance);
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        UIManager.logger.LogException(e);
      }
    }
  }
  [HarmonyPatch(typeof(Contract))]
  [HarmonyPatch(MethodType.Constructor)]
  [HarmonyPatch(new Type[] { typeof(string), typeof(string), typeof(string), typeof(ContractTypeValue), typeof(GameInstance), typeof(ContractOverride), typeof(GameContext), typeof(bool), typeof(int), typeof(int), typeof(int?) })]
  public static class Contract_Constructor0 {
    static void Postfix(Contract __instance) {
      try {
        __instance.RegisterReference();
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        UIManager.logger.LogException(e);
      }
    }
  }
  [HarmonyPatch(typeof(Contract))]
  [HarmonyPatch(MethodType.Constructor)]
  [HarmonyPatch(new Type[] {  })]
  public static class Contract_Constructor1 {
    static void Postfix(Contract __instance) {
      try {
        __instance.RegisterReference();
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        UIManager.logger.LogException(e);
      }
    }
  }

  [HarmonyPatch(typeof(SGContractsListItem), "Init")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyAfter("us.frostraptor.CodeWords")]
  public static class SGContractsListItem_Init {
    static void Postfix(SGContractsListItem __instance, Contract contract) {
      try {
        var r = contract.GetReference();
        if(r.data == null) { r.data = new ContractData(); }
        if(string.IsNullOrEmpty(r.data.DisplayName)) {
          r.data.DisplayName = __instance.contractName.text;
        } else {
          __instance.contractName.SetText(r.data.DisplayName);
        }
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        UIManager.logger.LogException(e);
      }
    }
  }
  [HarmonyPatch(typeof(SimGameState), "ResolveCompleteContract")]
  [HarmonyPatch(MethodType.Normal)]
  public static class SimGameState_ResolveCompleteContract {
    private static Contract savedCompleteContract = null;
    public static Contract SavedCompleteContract(this SimGameState sim) { return savedCompleteContract; }
    static void Prefix(SimGameState __instance) {
      try {
        savedCompleteContract = __instance.CompletedContract;
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        UIManager.logger.LogException(e);
      }
    }
    static void Postfix(SimGameState __instance) {
      try {
        savedCompleteContract = null;
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        UIManager.logger.LogException(e);
      }
    }
  }
  [HarmonyPatch(typeof(SimGameState), "TriggerSaveNow")]
  [HarmonyPatch(MethodType.Normal)]
  public static class SimGameState_TriggerSaveNow {
    static void Prefix(SimGameState __instance, SaveReason reason, SimGameState.TriggerSaveNowOption option) {
      try {
        if(reason == SaveReason.SIM_GAME_COMPLETED_CONTRACT) {
          var contract = __instance.SavedCompleteContract();
          if(contract != null) { __instance.SaveActiveContractName = contract.GetDisplayName(); }
        } else if(reason == SaveReason.SIM_GAME_CONTRACT_ACCEPTED) {
          var contract = !__instance.HasTravelContract ? __instance.SelectedContract : __instance.ActiveTravelContract;
          if(contract != null) { __instance.SaveActiveContractName = contract.GetDisplayName(); }
        }
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        UIManager.logger.LogException(e);
      }
    }
  }
  [HarmonyPatch(typeof(AAR_UnitStatusWidget))]
  [HarmonyPatch("FillInData")]
  [HarmonyPatch(MethodType.Normal)]
  public static class AAR_UnitStatusWidget_FillInData {
    public static void Prefix(AAR_UnitStatusWidget __instance) {
      try {
        Log.M?.TWL(0, $"AAR_UnitStatusWidget.InitData GUID:{__instance.UnitData.mech.GUID}");
        UnitCombatStatistic stat = __instance.UnitData.stat();
        if(stat != null) {
          __instance.UnitData.pilot.pilotDef.AddCombatStatistic(stat);
        }
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        UIManager.logger.LogException(e);
      }
    }
  }

}