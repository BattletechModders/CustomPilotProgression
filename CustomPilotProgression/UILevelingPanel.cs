using BattleTech;
using BattleTech.UI;
using BattleTech.UI.Tooltips;
using HBS;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static BattleTech.UI.GenericPopup;

namespace CustomPilotProgression {
  public class UILevel {
    public LanceMechEquipmentListItem level;
    public Dictionary<AbilityDef,LanceMechEquipmentListItem> abilities = new Dictionary<AbilityDef, LanceMechEquipmentListItem>();
    public UILevel(LanceMechEquipmentListItem l) {
      this.level = l;
      this.abilities = new Dictionary<AbilityDef, LanceMechEquipmentListItem>();
    }
  }
  public class SGBarracksMWLevelingPanel : MonoBehaviour {
    public GenericPopup popup = null;
    public SGBarracksServicePanel panel = null;
    public GameObject panelGO = null;
    public PilotDef Pilot = null;
    public List<RectTransform> levelings = new List<RectTransform>();
    public List<RectTransform> levels = new List<RectTransform>();
    public List<RectTransform> abilities = new List<RectTransform>();
    public Dictionary<PilotWeaponLevel, UILevel> levels_ui = new Dictionary<PilotWeaponLevel, UILevel>();
    public bool ui_inited = true;
    public void SetPilot(PilotDef p) {
      this.Pilot = p;
      ui_inited = true;
      Log.M?.TWL(0,$"SGBarracksMWLevelingPanel.SetPilot {p.Description.Id}");
      foreach(var rect in levels) { rect.gameObject.SetActive(false); };
      foreach(var rect in abilities) { rect.gameObject.SetActive(false); };
      var prog = p.GetProgression();
      if(prog == null) {
        Log.M?.WL(1, $"no progression");
        return;
      }
      foreach(var levelingDef in PilotWeaponLevelingDef.dataManager) {
        float cap = 0f;
        PilotWeaponLevel cur_level = null;
        var exp = prog.GetProgression(levelingDef.Value);
        if(exp != null) {
          cap = exp.experience_cap;
          cur_level = levelingDef.Value.GetLevel(exp.experience);
        }
        Log.M?.WL(1, $"{levelingDef.Key} cap:{cap}");

        foreach(var level in levelingDef.Value.levels) {
          if(this.levels_ui.TryGetValue(level, out var ui)) {
            Log.M?.WL(2, $"level:{level.HeaderChar} exp:{level.experience}");
            ui.level.gameObject.SetActive(cap > level.experience);
            foreach(var abl in ui.abilities) { abl.Value.gameObject.SetActive(cap > level.experience); };
            if(cur_level == level) {
              ui.level.itemText.SetText($"<color={level.Color}>->{(string.IsNullOrEmpty(level.LevelDef.Description.UIName) ? level.HeaderChar : level.LevelDef.Description.UIName)}</color>");
              ui.level.backgroundColor.SetUIColor(UIColor.Custom);
              ui.level.backgroundColor.OverrideWithColor(Core.settings.levelSelectedBackColor);
              foreach(var abl in ui.abilities) {
                abl.Value.itemText.SetText($"<color={level.Color}>->{abl.Key.Description.Name}</color>");
                abl.Value.backgroundColor.SetUIColor(UIColor.Custom);
                abl.Value.backgroundColor.OverrideWithColor(Core.settings.levelSelectedBackColor);
              }
            } else {
              ui.level.itemText.SetText($"<color={level.Color}>{(string.IsNullOrEmpty(level.LevelDef.Description.UIName) ? level.HeaderChar : level.LevelDef.Description.UIName)}</color>");
              ui.level.backgroundColor.SetUIColor(UIColor.Custom);
              ui.level.backgroundColor.OverrideWithColor(Core.settings.levelNotSelectedBackColor);
              foreach(var abl in ui.abilities) {
                abl.Value.itemText.SetText($"<color={level.Color}>{abl.Key.Description.Name}</color>");
                abl.Value.backgroundColor.SetUIColor(UIColor.Custom);
                abl.Value.backgroundColor.OverrideWithColor(Core.settings.levelNotSelectedBackColor);
              }
            }
          }
        }
      }
      ui_inited = false;
    }
    public void Update() {
      if(ui_inited) { return; }
      if(panel == null) { return; }
      RectTransform content = panel.biographyScrollbar.content.gameObject.GetComponent<RectTransform>();
      var sizeDelta = content.sizeDelta;
      foreach(var leveling in levelings) {
        sizeDelta = leveling.sizeDelta;
        sizeDelta.x = content.sizeDelta.x;
        leveling.sizeDelta = sizeDelta;
      }
      foreach(var level in levels) {
        sizeDelta = level.sizeDelta;
        sizeDelta.x = content.sizeDelta.x * 0.9f;
        level.sizeDelta = sizeDelta;
      }
      foreach(var ability in abilities) {
        sizeDelta = ability.sizeDelta;
        sizeDelta.x = content.sizeDelta.x * 0.6f;
        ability.sizeDelta = sizeDelta;
      }
      ui_inited = true;
    }
    public void OnClose() {
      if(this.popup == null) {
        var popups = LazySingletonBehavior<UIManager>.Instance.popupNode.gameObject.GetComponentsInChildren<GenericPopup>(true);
        foreach(var tmp in popups) {
          if(tmp.PrefabName == "uixPrfPanl_SIM_mwLeveling-Widget") { GameObject.Destroy(tmp.gameObject); }
        }
      } else {
        popup.Visible = false;
      }
    }
    public static SGBarracksMWLevelingPanel PooledInstantine() {
      SGBarracksMWLevelingPanel instance = LazySingletonBehavior<UIManager>.Instance.popupNode.gameObject.GetComponentInChildren<SGBarracksMWLevelingPanel>(true);
      if(instance != null) {
        if(instance.popup != null) { 
          instance.popup.Visible = true;
          return instance;
        } else {
          GameObject.Destroy(instance.gameObject);
        }
      }
      Log.M?.TWL(0,$"SGBarracksMWLevelingPanel.PooledInstantine");
      GenericPopup genericPopup = LazySingletonBehavior<UIManager>.Instance.CreatePopupModule<GenericPopup>();
      genericPopup.SetPrefabName("uixPrfPanl_SIM_mwLeveling-Widget");
      genericPopup.gameObject.name = "uixPrfPanl_SIM_mwLeveling-Widget";
      genericPopup.Title = "Leveling";
      genericPopup.TextContent = "Leveling";
      instance = genericPopup.gameObject.AddComponent<SGBarracksMWLevelingPanel>();
      instance.popup = genericPopup;
      GenericPopupButtonSettings popupButtonSettings = new GenericPopupButtonSettings() {
        Content = "Close",
        OnPress = instance.OnClose,
        CloseOnPress = false,
        HotKey = null
      };
      genericPopup.AddButton(popupButtonSettings);
      genericPopup.SetEscapeHandler(EscapeHandler.Cancel);
      genericPopup.ForceRefreshImmediate();
      var representation = genericPopup.representation;
      var occlusionLayer = genericPopup.occlusionLayer;
      var contentText = genericPopup.occlusionLayer;
      SGBarracksMWDetailPanel details = LazySingletonBehavior<UIManager>.Instance.CreatePopupModule<SGBarracksMWDetailPanel>();
      SGBarracksServicePanel panel = details.gameObject.GetComponentInChildren<SGBarracksServicePanel>(true);
      instance.panelGO = GameObject.Instantiate(panel.gameObject);
      details.Pool();
      instance.panel = instance.panelGO.GetComponent<SGBarracksServicePanel>();
      instance.panelGO.transform.SetParent(instance.popup._contentText.transform.parent);
      instance.panelGO.transform.localScale = Vector3.one;
      instance.panelGO.SetActive(true);
      ContentSizeFitter fitter = instance.popup._contentText.transform.parent.gameObject.GetComponent<ContentSizeFitter>();
      if(fitter != null) { fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; }
      instance.popup._contentText.gameObject.SetActive(false);
      instance.panelGO.transform.SetSiblingIndex(instance.popup._contentText.transform.GetSiblingIndex()+1);
      var childs = instance.panel.biographyScrollbar.content.gameObject.GetComponentsInChildren<Transform>();
      foreach(var child in childs) {
        if(child.transform.parent != instance.panel.biographyScrollbar.content.transform) { continue; }
        child.gameObject.SetActive(false);
      }
      VerticalLayoutGroup contentLayout = instance.panel.biographyScrollbar.content.gameObject.GetComponent<VerticalLayoutGroup>();
      if(contentLayout != null) {
        contentLayout.spacing = 3f;
        contentLayout.childAlignment = TextAnchor.MiddleRight;
      }
      foreach(var levelingDef in PilotWeaponLevelingDef.dataManager) {
        GameObject levelingGO = LazySingletonBehavior<UIManager>.Instance.dataManager.PooledInstantiate("uixPrfPanl_LC_MechLoadoutItem", BattleTechResourceType.UIModulePrefabs);
        levelingGO.SetActive(true);
        levelingGO.transform.SetParent(instance.panel.biographyScrollbar.content.transform);
        levelingGO.transform.localScale = Vector3.one;
        LanceMechEquipmentListItem levelingItem = levelingGO.GetComponent<LanceMechEquipmentListItem>();
        instance.levelings.Add(levelingGO.GetComponent<RectTransform>());
        levelingItem.itemText.SetText($"<color={levelingDef.Value.Color}>{levelingDef.Value.Description.UIName}</color>");
        levelingItem.backgroundColor.SetUIColor(UIColor.Custom);
        levelingItem.backgroundColor.OverrideWithColor(Core.settings.levelingBackColor);
        BaseDescriptionDef leveling_tooltip = new BaseDescriptionDef(levelingDef.Value.Description);
        levelingItem.EquipmentTooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(leveling_tooltip));
        foreach(var level in levelingDef.Value.levels) {
          GameObject levelGO = LazySingletonBehavior<UIManager>.Instance.dataManager.PooledInstantiate("uixPrfPanl_LC_MechLoadoutItem", BattleTechResourceType.UIModulePrefabs);
          levelGO.SetActive(false);
          levelGO.transform.SetParent(instance.panel.biographyScrollbar.content.transform);
          levelGO.transform.localScale = Vector3.one;
          LanceMechEquipmentListItem levelItem = levelGO.GetComponent<LanceMechEquipmentListItem>();
          instance.levels.Add(levelGO.GetComponent<RectTransform>());
          BaseDescriptionDef level_tooltip = new BaseDescriptionDef(level.LevelDef.Description.Id, level.LevelDef.Description.UIName, level.LevelDef.Description.Details, level.LevelDef.Description.Icon);
          levelItem.itemText.SetText($"<color={level.Color}>{(string.IsNullOrEmpty(level.LevelDef.Description.UIName)? level.HeaderChar: level.LevelDef.Description.UIName)}</color>");
          levelItem.backgroundColor.SetUIColor(UIColor.Custom);
          levelItem.backgroundColor.OverrideWithColor(Core.settings.levelNotSelectedBackColor);
          levelItem.EquipmentTooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(level_tooltip));
          instance.levels_ui[level] = new UILevel(levelItem);
          foreach(var ability in level.LevelDef.AbilityDefs) {
            GameObject abilityGO = LazySingletonBehavior<UIManager>.Instance.dataManager.PooledInstantiate("uixPrfPanl_LC_MechLoadoutItem", BattleTechResourceType.UIModulePrefabs);
            abilityGO.SetActive(false);
            abilityGO.transform.SetParent(instance.panel.biographyScrollbar.content.transform);
            abilityGO.transform.localScale = Vector3.one;
            LanceMechEquipmentListItem abilityItem = abilityGO.GetComponent<LanceMechEquipmentListItem>();
            instance.abilities.Add(abilityGO.GetComponent<RectTransform>());
            abilityItem.itemText.SetText($"<color={level.Color}>{ability.Description.Name}</color>");
            abilityItem.backgroundColor.SetUIColor(UIColor.Custom);
            abilityItem.backgroundColor.OverrideWithColor(Core.settings.levelNotSelectedBackColor);
            abilityItem.EquipmentTooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(ability.Description));
            instance.levels_ui[level].abilities[ability] = abilityItem;
          }
        }
      }
      instance.ui_inited = false;
      return instance;
    }
  }
}