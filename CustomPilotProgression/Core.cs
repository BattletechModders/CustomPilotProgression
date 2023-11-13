using BattleTech;
using HarmonyLib;
using HBS.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CustomPilotProgression {
  public static class Core {
    public class WeaponProgressionGenerator {
      public string id { get; set; } = string.Empty;
      public HashSet<string> systemTags { get; set; } = new HashSet<string>();
      public HashSet<string> pilotTags { get; set; } = new HashSet<string>();
      [JsonIgnore]
      private TagSet f_systemTags { get; set; } = new TagSet();
      [JsonIgnore]
      private TagSet f_pilotTags { get; set; } = new TagSet();
      [JsonIgnore]
      public TagSet SystemTags {
        get {
          if(f_systemTags == null) {
            f_systemTags = new TagSet(systemTags);
          }
          return f_systemTags;
        }
      }
      [JsonIgnore]
      public TagSet PilotTags {
        get {
          if(f_pilotTags == null) {
            f_pilotTags = new TagSet(pilotTags);
          }
          return f_pilotTags;
        }
      }
      public HashSet<string> levelings { get; set; } = new HashSet<string>();
      public float experience_min { get; set; } = 0f;
      public float experience_max { get; set; } = 0f;
      public float experience_cap_min { get; set; } = 0f;
      public float experience_cap_max { get; set; } = 0f;
    }
    public class Settings {
      public bool debugLog { get; set; } = true;
      public string ChangedChar = "^";
      public string ChangedCharColor = "yellow";
      public string ChangedColor = "#C1FF33FF";
      [JsonIgnore]
      private Color? f_ChangedColor = new UnityEngine.Color?();
      [JsonIgnore]
      public Color changedColor {
        get {
          if(f_ChangedColor.HasValue == false) {
            if(UnityEngine.ColorUtility.TryParseHtmlString(this.ChangedColor, out var tmpcol)) {
              f_ChangedColor = tmpcol;
            } else {
              f_ChangedColor = UnityEngine.Color.magenta;
            }
          }
          return f_ChangedColor.Value;
        }
      }
      public string LevelingBackColor = "#3366FF";
      [JsonIgnore]
      private Color? f_LevelingBackColor = new UnityEngine.Color?();
      [JsonIgnore]
      public Color levelingBackColor {
        get {
          if(f_LevelingBackColor.HasValue == false) {
            if(UnityEngine.ColorUtility.TryParseHtmlString(this.LevelingBackColor, out var tmpcol)) {
              f_LevelingBackColor = tmpcol;
            } else {
              f_LevelingBackColor = UnityEngine.Color.magenta;
            }
          }
          return f_LevelingBackColor.Value;
        }
      }
      public string LevelNotSelectedBackColor = "#808080";
      [JsonIgnore]
      private Color? f_LevelNotSelectedBackColor = new UnityEngine.Color?();
      [JsonIgnore]
      public Color levelNotSelectedBackColor {
        get {
          if(f_LevelNotSelectedBackColor.HasValue == false) {
            if(UnityEngine.ColorUtility.TryParseHtmlString(this.LevelNotSelectedBackColor, out var tmpcol)) {
              f_LevelNotSelectedBackColor = tmpcol;
            } else {
              f_LevelNotSelectedBackColor = UnityEngine.Color.magenta;
            }
          }
          return f_LevelNotSelectedBackColor.Value;
        }
      }
      public string LevelSelectedBackColor = "#008000";
      [JsonIgnore]
      private Color? f_LevelSelectedBackColor = new UnityEngine.Color?();
      [JsonIgnore]
      public Color levelSelectedBackColor {
        get {
          if(f_LevelSelectedBackColor.HasValue == false) {
            if(UnityEngine.ColorUtility.TryParseHtmlString(this.LevelSelectedBackColor, out var tmpcol)) {
              f_LevelSelectedBackColor = tmpcol;
            } else {
              f_LevelSelectedBackColor = UnityEngine.Color.magenta;
            }
          }
          return f_LevelSelectedBackColor.Value;
        }
      }
      public WeaponProgressionGenerator defaultGenerator { get; set; } = new WeaponProgressionGenerator();
      public List<WeaponProgressionGenerator> pilotGenerators { get; set; } = new List<WeaponProgressionGenerator>();
      public WeaponProgressionGenerator GetGenerator(string leveling, TagSet pilotTags, TagSet systemTags) {
        var generator = Core.settings.defaultGenerator;
        foreach(var gen in Core.settings.pilotGenerators) {
          if((gen.PilotTags.IsEmpty == false)&&(pilotTags != null)) {
            if(pilotTags.ContainsAll(gen.PilotTags) == false) { continue; }
          }
          if((gen.SystemTags.IsEmpty == false)&&(systemTags != null)) {
            if(systemTags.ContainsAll(gen.SystemTags) == false) { continue; }
          }
          if(gen.levelings.Count != 0) {
            if(gen.levelings.Contains(leveling) == false) { continue; }
          }
          generator = gen;
          break;
        }
        return generator;
      }
    }
    public static Settings settings = new Settings();
    public static void FinishedLoading(List<string> loadOrder, Dictionary<string, Dictionary<string, VersionManifestEntry>> customResources) {
      Log.M?.TWL(0, "FinishedLoading", true);
      try {
        CustomPrewarm.Core.RegisterSerializator("CustomPilotProgression_Weapons", BattleTechResourceType.PilotDef, PilotsWeaponsProgression.GetLoadedProgression);
        CustAmmoCategories.ToHitModifiersHelper.registerModifier2("CustomPilotProgression", "PROGRESSION", false, false, PilotsWeaponsProgressionHelper.GetModifier, PilotsWeaponsProgressionHelper.GetModifierName);
        foreach(var customResource in customResources) {
          Log.M?.TWL(0, "customResource:" + customResource.Key);
          if(customResource.Key == "PilotWeaponLevelingDef") {
            foreach(var entry in customResource.Value) {
              try {
                Log.M?.WL(1, entry.Value.FilePath);
                PilotWeaponLevelingDef def = PilotWeaponLevelingDef.FromJSON(File.ReadAllText(entry.Value.FilePath));
                Log.M?.WL(1, "id:" + def.Description.Id);
                def.Register();
              } catch(Exception e) {
                Log.M?.TWL(0, e.ToString(), true);
              }
            }
          } else if(customResource.Key == "WeaponProgressionGenerator") {
            foreach(var entry in customResource.Value) {
              try {
                Log.M?.WL(1, entry.Value.FilePath);
                WeaponProgressionGenerator def = JsonConvert.DeserializeObject<WeaponProgressionGenerator>(File.ReadAllText(entry.Value.FilePath));
                Log.M?.WL(1, "id:" + def.id);
                Core.settings.pilotGenerators.Add(def);
              } catch(Exception e) {
                Log.M?.TWL(0, e.ToString(), true);
              }
            }
          }
          //} else if(customResource.Key == "PilotWeaponLevelDef") {
          //  foreach(var entry in customResource.Value) {
          //    try {
          //      Log.M?.WL(1, entry.Value.FilePath);
          //      PilotWeaponLevelDef def = PilotWeaponLevelDef.FromJSON(File.ReadAllText(entry.Value.FilePath));
          //      Log.M?.WL(1, "id:" + def.Description.Id);
          //      def.Register();
          //    } catch(Exception e) {
          //      Log.M?.TWL(0, e.ToString(), true);
          //    }
          //  }
          //}
        }
        //Log.M?.WL(0, JsonConvert.SerializeObject(PilotWeaponLevelingDef.dataManager, Formatting.Indented));
        //Log.M?.WL(0, JsonConvert.SerializeObject(PilotWeaponLevelDef.dataManager, Formatting.Indented));
        StringBuilder names = new StringBuilder();
        foreach(var levelingDef in PilotWeaponLevelingDef.dataManager) {
          names.Append(levelingDef.Key);
        }
        PilotWeaponLevelingDef.VERSION_HASH = names.ToString().GetHashCode();
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
      }
    }
    public static void Init(string directory, string settingsJson) {
      Log.BaseDirectory = directory;
      Log.InitLog();
      Core.settings = JsonConvert.DeserializeObject<Core.Settings>(settingsJson);
      Log.M?.TWL(0, "Initing... " + directory + " version: " + Assembly.GetExecutingAssembly().GetName().Version, true);
      try {
        var harmony = new Harmony("ru.kmission.custompilotprogression");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
      }
    }
  }
}
