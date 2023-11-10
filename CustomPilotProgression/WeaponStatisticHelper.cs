using BattleTech;
using CustAmmoCategories;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CustomPilotProgression {
  public static class ProgressionHelper {
    private static Dictionary<string, PilotWeaponLevelingDef> weaponsLevelingCache = new Dictionary<string, PilotWeaponLevelingDef>();
    public static PilotWeaponLevelingDef GetLevelingDef(this Weapon weapon) {
      if(weapon == null) { return null; }
      if(weapon.weaponDef == null) { return null; }
      if(weapon.weaponDef.Description == null) { return null; }
      if(weaponsLevelingCache.TryGetValue(weapon.weaponDef.Description.Id, out var result)) {
        return result;
      }
      string leveling_type = string.Empty;
      foreach(var tag in weapon.weaponDef.ComponentTags) {
        if(tag.StartsWith(PilotWeaponLevelingDef.LEVELING_WEAPON_CATEGORY_TAG_PREFIX) == false) { continue; }
        leveling_type = "pilotweaponlevelingdef_" + tag.Substring(PilotWeaponLevelingDef.LEVELING_WEAPON_CATEGORY_TAG_PREFIX.Length);
        if(PilotWeaponLevelingDef.dataManager.TryGetValue(leveling_type, out result)) {
          weaponsLevelingCache[weapon.weaponDef.Description.Id] = result;
          return result;
        }
      }
      leveling_type = "pilotweaponlevelingdef_" + weapon.weaponDef.WeaponCategoryValue.Name;
      if(PilotWeaponLevelingDef.dataManager.TryGetValue(leveling_type, out result)) {
        weaponsLevelingCache[weapon.weaponDef.Description.Id] = result;
        return result;
      }
      weaponsLevelingCache[weapon.weaponDef.Description.Id] = null;
      return null;
    }
  }
  [HarmonyPatch(typeof(AdvWeaponHitInfo))]
  [HarmonyPatch("FlushInfo")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(AttackDirector.AttackSequence) })]
  public static class AdvWeaponHitInfo_FlushInfo {
    public static void ProcessAttack(AdvWeaponHitInfo advInfo, Dictionary<PilotWeaponLevelingDef, float> consolidated) {
      try {
        PilotWeaponLevelingDef levelingDef = advInfo.weapon.GetLevelingDef();
        if(levelingDef == null) { return; }
        var pilot_exp = advInfo.weapon.parent.GetPilot().GetProgression();
        if(pilot_exp == null) { return; }
        var weapon_exp = pilot_exp.GetProgression(levelingDef);
        if(float.IsNaN(weapon_exp.experience_pending)) { weapon_exp.experience_pending = weapon_exp.experience; }
        var levelDef = levelingDef.GetLevel(weapon_exp.experience_pending);
        if(levelDef == null) { return; }
        var progression = levelDef.GetProgressionByChance(advInfo.hitChance);
        float success_hits = 0f;
        float hits = 0f;
        foreach(var hit in advInfo.hits) {
          if(hit.isAOE) { continue; }
          hits += 1f;
          if(hit.isHit) { success_hits += 1f; }
        }
        if(hits < 0.01f) { return; }
        float exp_mod = success_hits / hits;
        float exp = exp_mod * (progression.success_exp - progression.fail_exp) + progression.fail_exp;
        weapon_exp.experience_pending += exp;
        if(consolidated.ContainsKey(levelingDef) == false) { consolidated[levelingDef] = 0f; }
        consolidated[levelingDef] += exp;
        if(weapon_exp.experience_pending > weapon_exp.experience_cap) { weapon_exp.experience_pending = weapon_exp.experience_cap; }
        Log.M?.WL(1, $"pilot:{advInfo.weapon.parent.GetPilot().Description.Id} progression:{levelingDef.Description.Id} level:{levelDef.levelDef} exp:{weapon_exp.experience_pending}");
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        AttackDirector.attackLogger.LogException(e);
      }
    }
    public static void Prefix(AttackDirector.AttackSequence sequence) {
      try {
        Log.M?.TWL(0, "AdvWeaponHitInfo.FlushInfo");
        if(sequence == null) { return; }
        WeaponHitInfo?[][] weaponHitInfo = sequence.weaponHitInfo;
        Dictionary<PilotWeaponLevelingDef, float> consolidated = new Dictionary<PilotWeaponLevelingDef, float>();
        for(int groupIndex = 0; groupIndex < weaponHitInfo.Length; ++groupIndex) {
          for(int weaponIndex = 0; weaponIndex < weaponHitInfo[groupIndex].Length; ++weaponIndex) {
            if(weaponHitInfo[groupIndex][weaponIndex].HasValue == false) {
              continue;
            }
            AdvWeaponHitInfo advInfo = weaponHitInfo[groupIndex][weaponIndex].Value.advInfo();
            AdvWeaponHitInfo_FlushInfo.ProcessAttack(advInfo, consolidated);
          }
        }
        foreach(var floatie in consolidated) {
          sequence.attacker.Combat.MessageCenter.PublishMessage(new FloatieMessage(sequence.attacker.GUID, sequence.attacker.GUID,$"{floatie.Key.Description.UIName}:{(floatie.Value>0f?"+":"-")}{Mathf.Round(floatie.Value)}__/CPP.EXP/__",FloatieMessage.MessageNature.Buff));
        }
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        AttackDirector.attackLogger.LogException(e);
      }
    }
  }
}