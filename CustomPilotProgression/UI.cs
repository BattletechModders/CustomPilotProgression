using BattleTech;
using BattleTech.UI;
using BattleTech.UI.Tooltips;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Localize;

namespace CustomPilotProgression {
  public class UICustomProgression: MonoBehaviour, IPointerClickHandler, IEventSystemHandler, IPointerEnterHandler, IPointerExitHandler {
    public PilotWeaponsProgression pilot_exp { get; set; } = null;
    public SGBarracksRosterSlot parent { get; set; } = null;
    public Image background;
    public bool hover = false;
    public void OnPointerClick(PointerEventData eventData) {
      Log.M?.TWL(0,$"UICustomProgression.OnPointerClick",true);
      pilot_exp.changed = false;
      background.color = color0;
      try {
        SGBarracksMWLevelingPanel.PooledInstantine().SetPilot(parent.Pilot.pilotDef);
      }catch(Exception e) {
        Log.M?.TWL(0,e.ToString());
        UIManager.logger.LogException(e);
      }
    }
    public void Init(SGBarracksRosterSlot p, PilotWeaponsProgression pe) {
      if(parent != p) {
        this.parent = p;
        this.background = this.parent.ExpertiseTooltip.gameObject.GetComponent<Image>();
        background.color = color0;
      }
      this.pilot_exp = pe;
    }
    private float t = 0f;
    private float delta = 1f;
    private static Color color0 = new Color(0f, 0f, 0f, 0f);
    private static Color color1 = new Color(1f, 0f, 0f, 1f);
    public void Update() {
      if(pilot_exp == null) { return; }
      if(parent == null) { return; }
      if(background == null) { return; }
      if(Input.GetMouseButtonDown(0) && this.hover) {
        this.OnPointerClick(null);
      }
      if(pilot_exp.changed == false) { return; }
      background.color = Color.Lerp(color0, color1, t);
      t += Time.deltaTime * delta;
      if(t > 1f) { t = 1f; delta = -1f; }
      if(t < 0f) { t = 0f; delta = 1f; }
    }
    public void OnPointerEnter(PointerEventData eventData) {
      Log.M?.TWL(0, $"UICustomProgression.OnPointerEnter", true);
      hover = true;
    }
    public void OnPointerExit(PointerEventData eventData) {
      Log.M?.TWL(0, $"UICustomProgression.OnPointerExit", true);
      hover = false;
    }
  }
  [HarmonyPatch(typeof(SGBarracksRosterSlot))]
  [HarmonyPatch("Refresh")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class SGBarracksRosterSlot_Refresh {
    public class PilotLevelTuple {
      public PilotWeaponLevelingDef leveling;
      public WeaponProgressionItem exp;
      public PilotLevelTuple(PilotWeaponLevelingDef l, WeaponProgressionItem e) {
        this.leveling = l;
        this.exp = e;
      }
    }
    public static void Postfix(SGBarracksRosterSlot __instance) {
      try {
        if(__instance.pilot == null) { return; }
        Log.M?.TWL(0, $"SGBarracksRosterSlot.Refresh {__instance.pilot.pilotDef.Description.Id}");
        var pilot_exp = __instance.pilot.GetProgression();
        if(pilot_exp == null) { return; }
        List<PilotLevelTuple> header_levelings = new List<PilotLevelTuple>();
        foreach(var exp in pilot_exp.progression) {
          if(PilotWeaponLevelingDef.dataManager.TryGetValue(exp.Key, out var levelingDef) == false) { continue;  }
          if(string.IsNullOrEmpty(levelingDef.HeaderChar) == false) {
            header_levelings.Add(new PilotLevelTuple(levelingDef, exp.Value));
          }
        }
        header_levelings.Sort((a, b) => { return a.leveling.HeaderChar.CompareTo(b.leveling.HeaderChar); });
        string header = string.Empty;
        string description = string.Empty;
        foreach(var hlevel in header_levelings) {
          var levelDef = hlevel.leveling.GetLevel(hlevel.exp, out var next_level, out var max_level);
          if(levelDef == null) { continue; }
          if(string.IsNullOrEmpty(description) == false) { description += "\n"; }
          description += $"<color={hlevel.leveling.Color}><b>{hlevel.leveling.Description.UIName}:</b></color>";
          float exp = Mathf.Round(hlevel.exp.experience);
          float cap = Mathf.Round(hlevel.exp.experience_cap);
          if(next_level != null) { cap = next_level.experience; }else
          if(max_level != null) { cap = max_level.experience; };
          if(exp > cap) { exp = cap; }
          description += $"\n - __/CPP.EXPERIENCE/__:<color={levelDef.Color}>{exp}/{cap}</color>";
          description += $"\n - __/CPP.LEVEL/__:<color={levelDef.Color}>{(string.IsNullOrEmpty(levelDef.LevelDef.Description.UIName)?levelDef.HeaderChar:levelDef.LevelDef.Description.UIName)}</color>";
          if(max_level != null) {
            description += $"/<color={max_level.Color}>{(string.IsNullOrEmpty(max_level.LevelDef.Description.UIName) ? max_level.HeaderChar : max_level.LevelDef.Description.UIName)}</color>";
          } else {
            description += $"/-";
          }
          description += $"\n - __/CPP.ABILITIES/__:";
          if(levelDef.LevelDef.AbilityDefs == null) { levelDef.LevelDef.ForceRefreshAbilityDefs(); }
          foreach(var ability in levelDef.LevelDef.AbilityDefs) {
//            if(UnityGameInstance.BattleTechGame.DataManager.AbilityDefs.TryGet(abilityName, out var ability)) {
              description += $"\n    - {new Localize.Text(ability.Description.Name).ToString()}";

//            }
          }
          if(string.IsNullOrEmpty(levelDef.HeaderChar)) { continue; }
          if(string.IsNullOrEmpty(header) == false) { header += " "; }
          header += $"<color={hlevel.leveling.Color}>{hlevel.leveling.HeaderChar.Substring(0,1)}</color>";
          header += $"<color={levelDef.Color}>{levelDef.HeaderChar.Substring(0, 1)}</color>";
        }
        BaseDescriptionDef tooltip = new BaseDescriptionDef(__instance.pilot.pilotDef.Description.Id, "__/CPP.WEAPON_LEVELS/__", description, string.Empty);
        __instance.ExpertiseTooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(tooltip));
        __instance.expertise.SetText(header);
        UICustomProgression ui = __instance.ExpertiseTooltip.gameObject.GetComponent<UICustomProgression>();
        if(ui == null) { ui = __instance.ExpertiseTooltip.gameObject.AddComponent<UICustomProgression>(); }
        ui.Init(__instance, pilot_exp);
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
      }
    }
  }

}