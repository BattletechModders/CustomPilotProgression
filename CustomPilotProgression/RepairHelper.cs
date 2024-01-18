using BattleTech.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CustomPilotProgression {
  [HarmonyPatch(typeof(SimGameInterruptManager), "QueuePauseNotification")]
  [HarmonyPatch(MethodType.Normal)]
  //[HarmonyPatch(new Type[] { typeof(string), typeof(string) })]
  public static class GenericPopupBuilder_Constructor {
    public static void Prefix(SimGameInterruptManager __instance, string title, string message, Sprite image, string audioEvent, Action primaryAction = null, string primaryButtonText = "Continue", Action secondaryAction = null, string secondaryButtonText = null) {
      try {
        Log.M?.TWL(0, $"SimGameInterruptManager.QueuePauseNotification {title}");
        Log.M?.WL(0, Environment.StackTrace);
      } catch(Exception e) {
        Log.M?.TWL(0, e.ToString(), true);
        UIManager.logger.LogException(e);
      }
    }
  }
}