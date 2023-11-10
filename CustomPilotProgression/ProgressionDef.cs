using BattleTech;
using BattleTech.Data;
using BattleTech.Save;
using BattleTech.Save.Test;
using BattleTech.UI;
using HarmonyLib;
using HBS.Collections;
using HBS.Util;
using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CustomPilotProgression {
  //public class PilotWeaponLevelDef: MechComponentDef {
    //public static bool AbilitiesRequested = false;
    //public static Dictionary<string, PilotWeaponLevelDef> dataManager = new Dictionary<string, PilotWeaponLevelDef>();
    //public static PilotWeaponLevelDef empty = new PilotWeaponLevelDef();
    //public List<EffectData> effects { get; set; } = new List<EffectData>();
    //public T GetComponent<T>() => CustomComponents.Database.GetCustom<T>((object)this);
    //public IEnumerable<T> GetComponents<T>() => CustomComponents.Database.GetCustoms<T>((object)this);
    //public List<string> abilityDefNames { get; set; } = new List<string>();
    //public static PilotWeaponLevelDef FromJSON(string json) {
    //  //JObject data = JObject.Parse(json);
    //  //PilotWeaponLevelDef result = data.ToObject<PilotWeaponLevelDef>();
    //  PilotWeaponLevelDef result = new PilotWeaponLevelDef();
    //  JSONSerializationUtility.FromJSON<PilotWeaponLevelDef>(result, json);
    //  return result;
    //}
    //public void Register() {
    //  if(string.IsNullOrEmpty(this.Description.Id)) { return; }
    //  dataManager[this.Description.Id] = this;
    //}
    //public static bool DependenciesLoaded(DataManager dataManager, uint weight) {
    //  if(AbilitiesRequested) { return true; }
    //  HashSet<string> abilities = new HashSet<string>();
    //  foreach(var level in PilotWeaponLevelDef.dataManager) {
    //    foreach(var abilityName in level.Value.abilityDefNames) { abilities.Add(abilityName); }
    //  }
    //  foreach(var abilityName in abilities) {
    //    if(dataManager.AbilityDefs.TryGet(abilityName, out var ability) == false) { return false; }
    //    ability.DataManager = dataManager;
    //    if(ability.DependenciesLoaded(weight) == false) { return false; }
    //  }
    //  AbilitiesRequested = true;
    //  return true;
    //}
    //public static void GatherDependensies(DataManager dataManager, DataManager.DependencyLoadRequest dependencyLoad, uint activeRequestWeight) {
    //  if(AbilitiesRequested) { return; }
    //  HashSet<string> abilities = new HashSet<string>();
    //  foreach(var level in PilotWeaponLevelDef.dataManager) {
    //    foreach(var abilityName in level.Value.abilityDefNames) { abilities.Add(abilityName); }
    //  }
    //  foreach(var abilityName in abilities) {
    //    if(dataManager.AbilityDefs.TryGet(abilityName, out var ability) == false) {
    //      dependencyLoad.RequestResource(BattleTechResourceType.AbilityDef, abilityName);
    //    }
    //    ability.DataManager = dataManager;
    //    if(ability.DependenciesLoaded(activeRequestWeight) == false) {
    //      ability.GatherDependencies(dataManager, dependencyLoad, activeRequestWeight);
    //    }
    //  }
    //  AbilitiesRequested = true;
    //}
  //}
  public class LevelProgression {
    public float chance { get; set; } = 0f;
    public float fail_exp { get; set; } = 0f;
    public float success_exp { get; set; } = 0f;
  }
  public class PilotWeaponLevel {
    public float experience { get; set; } = 0f;
    public List<LevelProgression> progression { get; set; } = new List<LevelProgression>();

    public string Color = "green";
    public string HeaderChar = "0";
    public string levelDef { get; set; } = string.Empty;
    [JsonIgnore]
    private UpgradeDef f_LevelDef = null;
    [JsonIgnore]
    public UpgradeDef LevelDef {
      get {
        if(f_LevelDef == null) {
          if(string.IsNullOrEmpty(this.levelDef)) {
            f_LevelDef = new UpgradeDef();
            f_LevelDef.Description = new DescriptionDef("none_level", new Localize.Text("__/CPP.LEVEL_NONE_NAME/__").ToString(), "__/CPP.LEVEL_NONE_DESCR/__", string.Empty,0,0,false,string.Empty,string.Empty, new Localize.Text("__/CPP.LEVEL_NONE_NAME/__").ToString());
            f_LevelDef.AbilityDefs = new List<AbilityDef>();
            f_LevelDef.statusEffects = new EffectData[0];
          } else if(UnityGameInstance.BattleTechGame.DataManager.UpgradeDefs.TryGet(this.levelDef, out var def)) {
            f_LevelDef = def;
          } else {
            f_LevelDef = new UpgradeDef();
            f_LevelDef.Description = new DescriptionDef("none_level", new Localize.Text("__/CPP.LEVEL_NONE_NAME/__").ToString(), "__/CPP.LEVEL_NONE_DESCR/__", string.Empty, 0, 0, false, string.Empty, string.Empty, new Localize.Text("__/CPP.LEVEL_NONE_NAME/__").ToString());
            f_LevelDef.AbilityDefs = new List<AbilityDef>();
            f_LevelDef.statusEffects = new EffectData[0];
            f_LevelDef.Description.Id = levelDef;
          }
        }
        return f_LevelDef;
      }
    }
    public LevelProgression GetProgressionByChance(float chance) {
      if(this.progression.Count == 0) { return null; }
      if(this.progression.Count == 1) { return this.progression[0]; }
      for(int t = 1; t < this.progression.Count; ++t) {
        if(this.progression[t].chance > chance) { return this.progression[t - 1]; }
      }
      return this.progression[this.progression.Count - 1];
    }
  }
  public class PilotWeaponLevelingDef {
    public static Dictionary<string, PilotWeaponLevelingDef> dataManager = new Dictionary<string, PilotWeaponLevelingDef>();
    public static int VERSION_HASH = 0;
    public DescriptionDef Description { get; private set; } = new DescriptionDef();
    public static readonly string LEVELING_WEAPON_CATEGORY_TAG_PREFIX = "progression_type_";
    public string HeaderChar = string.Empty;
    public string Color = string.Empty;
    public List<PilotWeaponLevel> levels = new List<PilotWeaponLevel>();
    public static bool DependenciesRequested = false;
    public static bool DependenciesLoaded(DataManager dataManager, uint weight) {
      if(DependenciesRequested) { return true; }
      HashSet<string> abilities = new HashSet<string>();
      HashSet<string> levels = new HashSet<string>();
      foreach(var levelings in PilotWeaponLevelingDef.dataManager) {
        foreach(var level in levelings.Value.levels) { if(string.IsNullOrEmpty(level.levelDef) == false) { levels.Add(level.levelDef); }; }
      }
      foreach(var levelDefName in levels) {
        if(dataManager.UpgradeDefs.TryGet(levelDefName, out var levelDef) == false) { return false; }
        foreach(var effect in levelDef.statusEffects) {
          if(effect.effectType == EffectType.ActiveAbility) { if(string.IsNullOrEmpty(effect.activeAbilityEffectData.abilityName) == false) { abilities.Add(effect.activeAbilityEffectData.abilityName); }; }
        }
        if(levelDef.DependenciesLoaded(weight) == false) { return false; }
      }
      foreach(var abilityName in abilities) {
        if(dataManager.AbilityDefs.TryGet(abilityName, out var ability) == false) { return false; }
        ability.DataManager = dataManager;
        if(ability.DependenciesLoaded(weight) == false) { return false; }
      }
      DependenciesRequested = true;
      return true;
    }
    public static void GatherDependensies(DataManager dataManager, DataManager.DependencyLoadRequest dependencyLoad, uint activeRequestWeight) {
      if(DependenciesRequested) { return; }
      HashSet<string> abilities = new HashSet<string>();
      HashSet<string> levels = new HashSet<string>();
      foreach(var levelings in PilotWeaponLevelingDef.dataManager) {
        foreach(var level in levelings.Value.levels) { if(string.IsNullOrEmpty(level.levelDef) == false) { levels.Add(level.levelDef); }; }
      }
      foreach(var levelDefName in levels) {
        if(dataManager.UpgradeDefs.TryGet(levelDefName, out var levelDef) == false) {
          dependencyLoad.RequestResource(BattleTechResourceType.UpgradeDef, levelDefName);
          continue;
        }
        foreach(var effect in levelDef.statusEffects) {
          if(effect.effectType == EffectType.ActiveAbility) { if(string.IsNullOrEmpty(effect.activeAbilityEffectData.abilityName) == false) { abilities.Add(effect.activeAbilityEffectData.abilityName); }; }
        }
        if(levelDef.DependenciesLoaded(activeRequestWeight) == false) {
          levelDef.GatherDependencies(dataManager, dependencyLoad, activeRequestWeight);
        }
      }
      foreach(var abilityName in abilities) {
        if(dataManager.AbilityDefs.TryGet(abilityName, out var ability) == false) {
          dependencyLoad.RequestResource(BattleTechResourceType.AbilityDef, abilityName);
        }
        ability.DataManager = dataManager;
        if(ability.DependenciesLoaded(activeRequestWeight) == false) {
          ability.GatherDependencies(dataManager, dependencyLoad, activeRequestWeight);
        }
      }
      DependenciesRequested = true;
    }

    public static PilotWeaponLevelingDef FromJSON(string json) {
      JObject data = JObject.Parse(json);
      PilotWeaponLevelingDef result = data.ToObject<PilotWeaponLevelingDef>();
      JSONSerializationUtility.FromJSON<DescriptionDef>(result.Description, data["Description"].ToString());
      return result;
    }
    public PilotWeaponLevel GetLevel(float exp) {
      if(this.levels.Count == 0) { return null; };
      if(this.levels.Count == 1) { return this.levels[0]; }
      for(int t = 1;t < this.levels.Count; ++t) {
        if(this.levels[t].experience > exp) { return this.levels[t - 1]; }
      }
      return this.levels[this.levels.Count - 1];
    }
    public PilotWeaponLevel GetLevel(WeaponProgressionItem exp, out PilotWeaponLevel next_level, out PilotWeaponLevel max_level) {
      next_level = null;
      max_level = null;
      PilotWeaponLevel result = null;
      if(this.levels.Count == 0) { return result; };
      result = this.levels[0];
      next_level = null;
      max_level = this.levels[0];
      if(this.levels.Count == 1) { return result; }
      result = null;
      max_level = null;
      for(int t = 1; t < this.levels.Count; ++t) {
        if((this.levels[t].experience > exp.experience) && (result == null)) {
          next_level = this.levels[t];
          result = this.levels[t - 1];
        }
        if((this.levels[t].experience > exp.experience_cap) && (max_level == null)) {
          max_level = this.levels[t - 1];
        }
      }
      if(max_level == null) { max_level = this.levels[this.levels.Count - 1]; }
      if(result != null) { return result; }
      next_level = null;
      result = this.levels[this.levels.Count - 1];
      max_level = this.levels[this.levels.Count - 1];
      return result;
    }
    public void Register() {
      if(string.IsNullOrEmpty(this.Description.Id)) { return; }
      this.levels.Sort((a, b) => { return a.experience.CompareTo(b.experience); });
      foreach(var level in levels) {
        level.progression.Sort((a, b) => { return a.chance.CompareTo(b.chance); });
      }
      dataManager[this.Description.Id] = this;
    }
  }

  [MessagePackObject]
  public class WeaponProgressionItem {
    [Key(1)]
    public string id = string.Empty;
    [Key(2)]
    public float experience = 0f;
    [IgnoreMember]
    public float experience_pending = float.NaN;
    [Key(3)]
    public float experience_cap = 0f;
    [Key(4)]
    public float damage = 0f;
    [Key(5)]
    public float shots = 0f;
    [Key(6)]
    public float hits = 0f;
    [Key(7)]
    public float crits = 0f;
    public override string ToString() {
      return $"exp:{experience} max:{experience_cap} dmg:{damage} shots:{shots} hits:{hits} crits:{crits}";
    }
  }
  [MessagePackObject]
  public class PilotWeaponsProgression {
    [IgnoreMember, JsonIgnore]
    public string id = string.Empty;
    [Key(1)]
    public Dictionary<string, WeaponProgressionItem> progression = new Dictionary<string, WeaponProgressionItem>();
    [Key(2)]
    public bool changed = false;
    [Key(3)]
    public int version_hash = 0;
    public void Merge(PilotWeaponsProgression weaponsProgression) {
      this.changed = weaponsProgression.changed;
      foreach(var prog in weaponsProgression.progression) {
        progression[prog.Key] = prog.Value;
      }
    }
    public WeaponProgressionItem GetProgression(PilotWeaponLevelingDef leveling) {
      if(progression.TryGetValue(leveling.Description.Id, out var result)) { return result; }
      progression[leveling.Description.Id] = new WeaponProgressionItem();
      return progression[leveling.Description.Id];
    }
  }
  public static class PilotsWeaponsProgressionHelper {
    public static string GetModifierName(ToHit instance, AbstractActor attacker, Weapon w, ICombatant target, Vector3 attackPosition, Vector3 targetPosition, LineOfFireLevel lofLevel, MeleeAttackType meleeAttackType, bool isCalledShot, int modifier) {
      PilotWeaponLevelingDef levelingDef = w.GetLevelingDef();
      if(levelingDef == null) { return string.Empty; }
      var pilot_exp = attacker.GetPilot().GetProgression();
      if(pilot_exp == null) { return string.Empty; }
      var weapon_exp = pilot_exp.GetProgression(levelingDef);
      if(float.IsNaN(weapon_exp.experience_pending)) { weapon_exp.experience_pending = weapon_exp.experience; }
      var levelDef = levelingDef.GetLevel(weapon_exp.experience_pending);
      if(levelDef == null) { return string.Empty; }
      return $"{levelingDef.Description.UIName}:{(string.IsNullOrEmpty(levelDef.LevelDef.Description.UIName)?levelDef.HeaderChar:levelDef.LevelDef.Description.UIName)}";
    }
    public static float GetModifier(ToHit instance, AbstractActor attacker, Weapon weapon, ICombatant target, Vector3 attackPosition, Vector3 targetPosition, LineOfFireLevel lofLevel, MeleeAttackType meleeAttackType, bool isCalledShot) {
      return 0f;
    }
    public static PilotWeaponsProgression GetProgression(this Pilot pilot) {
      if(pilot == null) { return null; }
      if(pilot.pilotDef == null) { return null; }
      if(pilot.pilotDef.Description == null) { return null; }
      if(string.IsNullOrEmpty(pilot.pilotDef.Description.Id)) { return null; }
      if(PilotsWeaponsProgression.instance.data.TryGetValue(pilot.pilotDef.Description.Id, out var result)) {
        return result;
      }
      PilotsWeaponsProgression.instance.data[pilot.pilotDef.Description.Id] = new PilotWeaponsProgression();
      return PilotsWeaponsProgression.instance.data[pilot.pilotDef.Description.Id];
    }
    public static PilotWeaponsProgression GetProgression(this PilotDef pilot) {
      if(pilot == null) { return null; }
      if(pilot.Description == null) { return null; }
      if(string.IsNullOrEmpty(pilot.Description.Id)) { return null; }
      if(PilotsWeaponsProgression.instance.data.TryGetValue(pilot.Description.Id, out var result)) {
        return result;
      }
      PilotsWeaponsProgression.instance.data[pilot.Description.Id] = new PilotWeaponsProgression();
      return PilotsWeaponsProgression.instance.data[pilot.Description.Id];
    }
    public static HashSet<AbilityDef> GetProgressionAbilities(this PilotDef pilot) {
      HashSet<AbilityDef> result = new HashSet<AbilityDef>();
      if(pilot.Description == null) { return result; }
      if(string.IsNullOrEmpty(pilot.Description.Id)) { return result; }
      var pilot_exp = pilot.GetProgression();
      if(pilot_exp == null) { return result; }
      foreach(var leveling in pilot_exp.progression) {
        if(PilotWeaponLevelingDef.dataManager.TryGetValue(leveling.Key, out var levelingDef) == false) { continue; }
        var levelDef = levelingDef.GetLevel(leveling.Value.experience);
        levelDef.LevelDef.dataManager = UnityGameInstance.BattleTechGame.DataManager;
        levelDef.LevelDef.ForceRefreshAbilityDefs();
        foreach(var ability in levelDef.LevelDef.AbilityDefs) { result.Add(ability); }
      }
      return result;
    }
    public static HashSet<UpgradeDef> GetProgressionLevels(this PilotDef pilot) {
      HashSet<UpgradeDef> result = new HashSet<UpgradeDef>();
      if(pilot.Description == null) { return result; }
      if(string.IsNullOrEmpty(pilot.Description.Id)) { return result; }
      var pilot_exp = pilot.GetProgression();
      if(pilot_exp == null) { return result; }
      foreach(var leveling in pilot_exp.progression) {
        if(PilotWeaponLevelingDef.dataManager.TryGetValue(leveling.Key, out var levelingDef) == false) { continue; }
        var levelDef = levelingDef.GetLevel(leveling.Value.experience);
        levelDef.LevelDef.dataManager = UnityGameInstance.BattleTechGame.DataManager;
        result.Add(levelDef.LevelDef);
      }
      return result;
    }
    public static List<EffectData> GetProgressionEffects(this PilotDef pilot) {
      List<EffectData> result = new List<EffectData>();
      HashSet<UpgradeDef> levels = pilot.GetProgressionLevels();
      foreach(var level in levels) {
        foreach(var effect in level.statusEffects) {
          result.Add(effect);
        }
      }
      return result;
    }
    public static void Merge(this PilotDef pilotDef, PilotWeaponsProgression progression) {
      if(PilotsWeaponsProgression.instance.data.TryGetValue(pilotDef.Description.Id, out var result) == false) {
        PilotsWeaponsProgression.instance.data.Add(pilotDef.Description.Id, progression);
        return;
      }
      result.Merge(progression);
    }
    public static void UpdateProgression(this PilotDef pilotDef) {
      try {
        if(PilotsWeaponsProgression.instance.data.TryGetValue(pilotDef.Description.Id, out var pilotprogression) == false) {
          pilotprogression = new PilotWeaponsProgression();
        }
        if(pilotprogression.version_hash == PilotWeaponLevelingDef.VERSION_HASH) { return; }
        TagSet systemTags = null;
        if((UnityGameInstance.BattleTechGame.Simulation != null) && (UnityGameInstance.BattleTechGame.Simulation.CurSystem != null)) {
          systemTags = UnityGameInstance.BattleTechGame.Simulation.CurSystem.Tags;
        }
        foreach(var prog_type in PilotWeaponLevelingDef.dataManager) {
          if(pilotprogression.progression.ContainsKey(prog_type.Key)) { continue; }
          var progression = new WeaponProgressionItem();
          var generator = Core.settings.GetGenerator(prog_type.Key, pilotDef.PilotTags, systemTags);
          progression.experience = UnityEngine.Random.Range(generator.experience_min, generator.experience_max);
          progression.experience_pending = progression.experience;
          float cap_min = generator.experience_cap_min;
          float cap_max = generator.experience_cap_max;
          if(cap_min < progression.experience) { cap_min = progression.experience; }
          if(cap_max < progression.experience) { cap_max = progression.experience; }
          progression.experience_cap = UnityEngine.Random.Range(cap_min, cap_max);
          pilotprogression.changed = true;
          Log.M?.WL(1, $"{prog_type.Key} generator:{generator.id} exp:{progression.experience} cap:{progression.experience_cap}");
          pilotprogression.progression[prog_type.Key] = progression;
        }
        PilotsWeaponsProgression.instance.data[pilotDef.Description.Id] = pilotprogression;
        pilotprogression.version_hash = PilotWeaponLevelingDef.VERSION_HASH;
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        SimGameState.logger.LogException(e);
      }
    }
    public static void UpdateProgression(this SimGameState sim, string pilotDefId) {
      try {
        if(sim.DataManager.PilotDefs.TryGet(pilotDefId, out var pilotDef) == false) { return; }
        if(PilotsWeaponsProgression.instance.data.TryGetValue(pilotDef.Description.Id, out var pilotprogression) == false) {
          pilotprogression = new PilotWeaponsProgression();
        }
        if(pilotprogression.version_hash == PilotWeaponLevelingDef.VERSION_HASH) { return; }
        TagSet systemTags = null;
        if((UnityGameInstance.BattleTechGame.Simulation != null) && (UnityGameInstance.BattleTechGame.Simulation.CurSystem != null)) {
          systemTags = UnityGameInstance.BattleTechGame.Simulation.CurSystem.Tags;
        }
        foreach(var prog_type in PilotWeaponLevelingDef.dataManager) {
          if(pilotprogression.progression.ContainsKey(prog_type.Key)) { continue; }
          var progression = new WeaponProgressionItem();
          var generator = Core.settings.GetGenerator(prog_type.Key, pilotDef.PilotTags, systemTags);
          progression.experience = UnityEngine.Random.Range(generator.experience_min, generator.experience_max);
          progression.experience_pending = progression.experience;
          float cap_min = generator.experience_cap_min;
          float cap_max = generator.experience_cap_max;
          if(cap_min < progression.experience) { cap_min = progression.experience; }
          if(cap_max < progression.experience) { cap_max = progression.experience; }
          progression.experience_cap = UnityEngine.Random.Range(cap_min, cap_max);
          pilotprogression.changed = true;
          Log.M?.WL(1, $"{prog_type.Key} generator:{generator.id} exp:{progression.experience} cap:{progression.experience_cap}");
          pilotprogression.progression[prog_type.Key] = progression;
        }
        PilotsWeaponsProgression.instance.data[pilotDef.Description.Id] = pilotprogression;
        pilotprogression.version_hash = PilotWeaponLevelingDef.VERSION_HASH;
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        SimGameState.logger.LogException(e);
      }
    }
  }
  public class PilotsWeaponsProgression {
    public static readonly string STATISTIC_NAME = "PILOTS_WEAPONS_PROGRESSION";
    public Dictionary<string, PilotWeaponsProgression> data = new Dictionary<string, PilotWeaponsProgression>();
    public int version_hash { get; set; } = 0;
    public static PilotWeaponsProgression defaultEntry = new PilotWeaponsProgression();
    public static PilotsWeaponsProgression instance = new PilotsWeaponsProgression();
    public void RosterGenerate(SimGameState __instance) {
      List<Pilot> pilots = new List<Pilot>();
      pilots.AddRange(__instance.PilotRoster);
      pilots.Add(__instance.commander);
      foreach(var pilot in pilots) {
        if(data.TryGetValue(pilot.pilotDef.Description.Id, out var prog) == false) { continue; }
        if(prog.version_hash == PilotWeaponLevelingDef.VERSION_HASH) { continue; }
        foreach(var prog_type in PilotWeaponLevelingDef.dataManager) {
          if(prog.progression.ContainsKey(prog_type.Key)) { continue; }
          var progression = new WeaponProgressionItem();
          var generator = Core.settings.GetGenerator(prog_type.Key, pilot.pilotDef.PilotTags, __instance.CurSystem.Tags);
          progression.experience = UnityEngine.Random.Range(generator.experience_min, generator.experience_max);
          float cap_min = generator.experience_cap_min;
          float cap_max = generator.experience_cap_max;
          if(cap_min < progression.experience) { cap_min = progression.experience; }
          if(cap_max < progression.experience) { cap_max = progression.experience; }
          progression.experience_cap = UnityEngine.Random.Range(cap_min, cap_max);
          prog.version_hash = PilotWeaponLevelingDef.VERSION_HASH;
          prog.changed = true;
          prog.progression[prog_type.Key] = progression;
        }
      }
    }
    public void Generate(SimGameState __instance) {
      Log.M?.TWL(0,$"PilotsWeaponsProgression.Generate cur hash:{this.version_hash} ver hash:{PilotWeaponLevelingDef.VERSION_HASH}");
      if(this.version_hash == PilotWeaponLevelingDef.VERSION_HASH) { return; }
      this.version_hash = PilotWeaponLevelingDef.VERSION_HASH;
      foreach(var pilot in PilotsWeaponsProgression.instance.data) {
        if(__instance.DataManager.PilotDefs.TryGet(pilot.Key, out var pilotdef) == false) { continue; }
        foreach(var prog_type in PilotWeaponLevelingDef.dataManager) {
          if(pilot.Value.progression.ContainsKey(prog_type.Key)) { continue; }
          var progression = new WeaponProgressionItem();
          var generator = Core.settings.GetGenerator(prog_type.Key, pilotdef.PilotTags, __instance.CurSystem.Tags);
          progression.experience = UnityEngine.Random.Range(generator.experience_min, generator.experience_max);
          float cap_min = generator.experience_cap_min;
          float cap_max = generator.experience_cap_max;
          if(cap_min < progression.experience) { cap_min = progression.experience; }
          if(cap_max < progression.experience) { cap_max = progression.experience; }
          progression.experience_cap = UnityEngine.Random.Range(cap_min, cap_max);
          pilot.Value.version_hash = PilotWeaponLevelingDef.VERSION_HASH;
          pilot.Value.changed = true;
          pilot.Value.progression[prog_type.Key] = progression;
        }
      }
    }
    public static PilotWeaponsProgression GetLoadedProgression(string pilotDefId) {
      if(PilotsWeaponsProgression.instance.data.TryGetValue(pilotDefId, out var result)) {
        return result;
      }
      return PilotsWeaponsProgression.defaultEntry;
    }
    public static void MergeWeaponsProgression(PilotWeaponsProgression weaponsProgression) {
      if(PilotsWeaponsProgression.instance.data.TryGetValue(weaponsProgression.id, out var result) == false) {
        PilotsWeaponsProgression.instance.data.Add(weaponsProgression.id, weaponsProgression);
        return;
      }
      result.Merge(weaponsProgression);
    }
    public static void MergeWeaponsProgression(PilotsWeaponsProgression weaponsProgression) {
      foreach(var pilotProgression in weaponsProgression.data) {
        pilotProgression.Value.id = pilotProgression.Key;
        PilotsWeaponsProgression.MergeWeaponsProgression(pilotProgression.Value);
      }
    }
  }

  [HarmonyPatch(typeof(PilotDef))]
  [HarmonyPatch("FromJSON")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(string) })]
  public static class PilotDef_FromJSON {
    public class parceState {
      public string baseJson;
      public PilotWeaponsProgression payload;
      public string errorStr;
    }
    public static void Prefix(ref bool __runOriginal, PilotDef __instance, ref string json, ref parceState __state) {
      PilotWeaponsProgression weaponProgression = null;
      if(__instance.Description != null) {
        weaponProgression = CustomPrewarm.Core.getDeserializedObject(BattleTechResourceType.PilotDef, __instance.Description.Id, "CustomPilotProgression_Weapons") as PilotWeaponsProgression;
        if(weaponProgression != null) {
          __state = new parceState();
          __state.baseJson = json;
          __state.payload = weaponProgression;
          __state.errorStr = string.Empty;
          return;
        }
      }
      JObject defTemp = null;
      __state = new parceState();
      __state.baseJson = json;
      try {
        defTemp = JObject.Parse(json);
      } catch(Exception e) {
        __state.errorStr = e.ToString();
        return;
      }
      try {
        if(defTemp["WeaponsProgression"] != null) {
          weaponProgression = defTemp["WeaponsProgression"].ToObject<PilotWeaponsProgression>();
          defTemp.Remove("WeaponsProgression");
        } else {
          weaponProgression = new PilotWeaponsProgression();
        }
        __state.payload = weaponProgression;
      } catch(Exception e) {
        __state.errorStr = e.ToString();
      }
      json = defTemp.ToString(Formatting.Indented);
    }
    public static void Postfix(PilotDef __instance, string json, ref parceState __state) {
      if(__state == null) { return; }
      if(__instance == null) { Log.M?.TWL(0, "!WARNINIG! PilotDef is null. Very very wrong!", true); return; }
      try {
        if(__state.payload == null) {
          Log.M?.TWL(0, $"PilotDef {__instance.Description.Id} exception");
          Log.M?.WL(0, $"{__state.errorStr}");
          return;
        }
        __state.payload.id = __instance.Description.Id;
        PilotsWeaponsProgression.MergeWeaponsProgression(__state.payload);
      } catch(Exception e) {
        Log.M?.TWL(0,$"PilotDef.FromJSON exception {e.ToString()}");
      }
    }
  }
  [HarmonyPatch(typeof(SimGameState), "Dehydrate")]
  public static class SimGameState_Dehydrate {
    static void Prefix(SimGameState __instance, SerializableReferenceContainer references) {
      Log.M?.TWL(0, "SimGameState.Dehydrate");
      try {
        Log.M?.WL(1, "PilotsWeaponsProgression(" + PilotsWeaponsProgression.instance.data.Count + "):");
        PilotsWeaponsProgression.instance.Generate(__instance);
        PilotsWeaponsProgression.instance.RosterGenerate(__instance);
        Statistic pilotsWeaponsProgression = __instance.CompanyStats.GetStatistic(PilotsWeaponsProgression.STATISTIC_NAME);
        if(pilotsWeaponsProgression == null) {
          __instance.CompanyStats.AddStatistic<string>(PilotsWeaponsProgression.STATISTIC_NAME, JsonConvert.SerializeObject(PilotsWeaponsProgression.instance));
        } else {
          pilotsWeaponsProgression.SetValue<string>(JsonConvert.SerializeObject(PilotsWeaponsProgression.instance));
        }
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        SimGameState.logger.LogException(e);
      }
    }
  }
  [HarmonyPatch(typeof(SimGameState), "Rehydrate")]
  public static class SimGameState_Rehydrate {
    static void Postfix(SimGameState __instance, GameInstanceSave gameInstanceSave) {
      Log.M?.TWL(0, "SimGameState.Rehydrate");
      try {
        Statistic pilotsWeaponsProgression = __instance.CompanyStats.GetStatistic(PilotsWeaponsProgression.STATISTIC_NAME);
        if(pilotsWeaponsProgression != null) {
          PilotsWeaponsProgression loaded = JsonConvert.DeserializeObject<PilotsWeaponsProgression>(pilotsWeaponsProgression.Value<string>());
          PilotsWeaponsProgression.MergeWeaponsProgression(loaded);
        }
        PilotsWeaponsProgression.instance.Generate(__instance);
        PilotsWeaponsProgression.instance.RosterGenerate(__instance);
        Log.M?.WL(1, $"pilots weapon progression ({ PilotsWeaponsProgression.instance.data.Count}):");
        foreach(var pilotProgression in PilotsWeaponsProgression.instance.data) {
          Log.M?.WL(2, $"pilot:{pilotProgression.Key} changed:{pilotProgression.Value.changed}");
          foreach(var weaponProgression in pilotProgression.Value.progression) {
            Log.M?.WL(3, $"weapon:{weaponProgression.Key} {weaponProgression.Value.ToString()}");
            weaponProgression.Value.experience_pending = weaponProgression.Value.experience;
          }
        }
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        SimGameState.logger.LogException(e);
      }
    }
  }
  [HarmonyPatch(typeof(PilotGenerator), "GenerateRandomPilot")]
  public static class PilotGenerator_GenerateRandomPilot {
    static void Postfix(PilotGenerator __instance, ref PilotDef __result) {
      Log.M?.TWL(0, $"PilotGenerator.GenerateRandomPilot {__result.Description.Id} {__result.Description.Callsign}");
      __result.UpdateProgression();
    }
  }
  [HarmonyPatch(typeof(SimGameState), "AddPilotToRoster")]
  [HarmonyPatch(new Type[] { typeof(string) })]
  public static class SimGameState_AddPilotToRoster_0 {
    static  void Postfix(SimGameState __instance, string pilotDefID) {
      Log.M?.TWL(0, $"SimGameState.AddPilotToRoster {pilotDefID}");
      __instance.UpdateProgression(pilotDefID);
    }
  }
  [HarmonyPatch(typeof(SimGameState), "AddPilotToRoster")]
  [HarmonyPatch(new Type[] { typeof(PilotDef), typeof(bool), typeof(bool) })]
  public static class SimGameState_AddPilotToRoster_1 {
    static void Postfix(SimGameState __instance, PilotDef def, bool updatePilotDiscardPile, bool initialHiringDontSpawnMessage) {
      Log.M?.TWL(0, $"SimGameState.AddPilotToRoster {def.Description.Id} {def.Description.Callsign}");
      def.UpdateProgression();
    }
  }
  [HarmonyPatch(typeof(SimGameState), "AddPilotToHiringHall")]
  [HarmonyPatch(new Type[] { typeof(PilotDef), typeof(StarSystem) })]
  public static class SimGameState_AddPilotToHiringHall {
    static void Postfix(SimGameState __instance, PilotDef def, StarSystem system) {
      Log.M?.TWL(0, $"SimGameState.AddPilotToRoster {def.Description.Id} {def.Description.Callsign}");
      def.UpdateProgression();
    }
  }
  [HarmonyPatch(typeof(PilotGenerator), "GeneratePilots")]
  public static class PilotGenerator_GeneratePilots {
    static void Postfix(PilotGenerator __instance, List<PilotDef> roninList) {
      Log.M?.TWL(0, $"PilotGenerator.GenerateRandomPilots");
      try {
        if(roninList == null) { return; }
        if(roninList.Count == 0) { return; }
        foreach(var ronin in roninList) {
          Log.M?.WL(1, $"{ronin.Description.Id} {ronin.Description.Callsign}");
          ronin.UpdateProgression();
        }
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        SimGameState.logger.LogException(e);
      }
    }
  }
  [HarmonyPatch(typeof(Contract), "CompleteContract")]
  public static class Contract_CompleteContract {
    static void Prefix(Contract __instance, MissionResult result, bool isGoodFaithEffort) {
      Log.M?.TWL(0, $"Contract.CompleteContract");
      try {
        if(__instance.State != Contract.ContractState.InProgress) { return; }
        HashSet<string> pilotIds = new HashSet<string>();
        if(UnityGameInstance.BattleTechGame.Simulation != null) {
          foreach(var pilot in UnityGameInstance.BattleTechGame.Simulation.PilotRoster) {
            pilotIds.Add(pilot.pilotDef.Description.Id);
          }
          pilotIds.Add(UnityGameInstance.BattleTechGame.Simulation.commander.pilotDef.Description.Id);
        }
        CombatGameState combat = __instance.BattleTechGame.Combat;
        foreach(var actor in combat.AllActors) {
          if(PilotsWeaponsProgression.instance.data.TryGetValue(actor.GetPilot().pilotDef.Description.Id, out var progression) == false){ continue; }
          if(pilotIds.Contains(actor.GetPilot().pilotDef.Description.Id)) {
            foreach(var progItem in progression.progression) {
              if(float.IsNaN(progItem.Value.experience_pending)) {
                progItem.Value.experience_pending = progItem.Value.experience;
              }
              if(PilotWeaponLevelingDef.dataManager.TryGetValue(progItem.Key, out var levelingDef)) {
                var curLevel = levelingDef.GetLevel(progItem.Value.experience);
                var nextLevel = levelingDef.GetLevel(progItem.Value.experience_pending);
                if(curLevel != nextLevel) { progression.changed = true; }
              }
              progItem.Value.experience = progItem.Value.experience_pending;
            }
          } else {
            foreach(var progItem in progression.progression) {
              progItem.Value.experience_pending = progItem.Value.experience;
            }
          }
        }
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        SimGameState.logger.LogException(e);
      }
    }
  }
  [HarmonyPatch(typeof(SGCharacterCreationWidget), "CreatePilot")]
  public static class SGCharacterCreationWidget_CreatePilot {
    static void Postfix(SGCharacterCreationWidget __instance, ref Pilot __result) {
      Log.M?.TWL(0, $"SGCharacterCreationWidget.CreatePilot {__result.pilotDef.Description.Id}");
      try {
        __result.pilotDef.UpdateProgression();
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        SimGameState.logger.LogException(e);
      }
    }
  }

  [HarmonyPatch(typeof(PilotDef), "DependenciesLoaded")]
  public static class PilotDef_DependenciesLoaded {
    static void Postfix(PilotDef __instance, uint loadWeight, ref bool __result) {
      Log.M?.TWL(0, $"PilotDef.DependenciesLoaded {__instance.Description.Id}");
      try {
        if(__result == false) { return; }
        if(PilotWeaponLevelingDef.DependenciesLoaded(__instance.dataManager, loadWeight) == false) { __result = false; return; }
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        SimGameState.logger.LogException(e);
      }
    }
  }
  [HarmonyPatch(typeof(PilotDef), "GatherDependencies")]
  public static class PilotDef_GatherDependencies {
    static void Postfix(PilotDef __instance, DataManager dataManager, DataManager.DependencyLoadRequest dependencyLoad, uint activeRequestWeight) {
      Log.M?.TWL(0, $"PilotDef.GatherDependencies {__instance.Description.Id}");
      try {
        PilotWeaponLevelingDef.GatherDependensies(dataManager, dependencyLoad, activeRequestWeight);
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        SimGameState.logger.LogException(e);
      }
    }
  }
  [HarmonyPatch(typeof(Pilot), "InitAbilities")]
  [HarmonyPatch(new Type[] { typeof(bool), typeof(bool) })]
  [HarmonyAfter("ca.gnivler.BattleTech.Abilifier")]
  [HarmonyBefore("io.mission.modrepuation")]
  public static class Pilot_InitAbilities_Prefix {
    static void Prefix(Pilot __instance, bool ModifyStats, bool FromSave, ref List<AbilityDef> __state) {
      if(UnityGameInstance.BattleTechGame.Combat == null) { return; }
      Log.M?.TWL(0, $"Pilot.InitAbilities prefix {__instance.Description.Id}");
      try {
        __state = new List<AbilityDef>();
        __state.AddRange(__instance.pilotDef.AbilityDefs);
        var levelAbilities = __instance.pilotDef.GetProgressionAbilities();
        foreach(var abilityDef in levelAbilities) {
          Log.M?.WL(1, $"add:{abilityDef.Description.Id}");
          __instance.pilotDef.AbilityDefs.Add(abilityDef);
        }
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        SimGameState.logger.LogException(e);
      }
    }
    static void Postfix(Pilot __instance, bool ModifyStats, bool FromSave, ref List<AbilityDef> __state) {
      if(UnityGameInstance.BattleTechGame.Combat == null) { return; }
      Log.M?.TWL(0, $"Pilot.InitAbilities postfix {__instance.Description.Id}");
      try {
        __instance.pilotDef.AbilityDefs.Clear();
        __instance.pilotDef.AbilityDefs.AddRange(__state);
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        SimGameState.logger.LogException(e);
      }
    }
  }
  [HarmonyPatch(typeof(Pilot), "ApplyPassiveAbilities")]
  [HarmonyPatch(new Type[] { typeof(int) })]
  public static class Pilot_ApplyPassiveAbilities {
    static void Postfix(Pilot __instance, int stackItemUID) {
      Log.M?.TWL(0, $"Pilot.ApplyPassiveAbilities {__instance.Description.Id}");
      try {
        var levels = __instance.pilotDef.GetProgressionLevels();
        if(__instance.ParentActor == null) {
          foreach(var level in levels) {
            foreach(var effectData in level.statusEffects) {
              if(effectData.effectType == EffectType.StatisticEffect && effectData.statisticData.targetCollection == StatisticEffectData.TargetCollection.Pilot) {
                Variant variant = new Variant(System.Type.GetType(effectData.statisticData.modType));
                variant.SetValue(effectData.statisticData.modValue);
                variant.statName = effectData.statisticData.statName;
                __instance.statCollection.ModifyStatistic(__instance.GUID, stackItemUID, effectData.statisticData.statName, effectData.statisticData.operation, variant);
              }
            }
          }
        } else {
          foreach(var level in levels) {
            foreach(var effectData in level.statusEffects) {
              if((effectData.effectType == EffectType.StatisticEffect) && (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive)) {
                Log.M?.WL(1,$"level:{level.Description.Id} effect:{effectData.Description.Id}");
                __instance.Combat.EffectManager.CreateEffect(effectData, level.Description.Id, stackItemUID, __instance.ParentActor, __instance.ParentActor, new WeaponHitInfo(), 0);
              }
            }
          }
        }
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        SimGameState.logger.LogException(e);
      }
    }
  }
}