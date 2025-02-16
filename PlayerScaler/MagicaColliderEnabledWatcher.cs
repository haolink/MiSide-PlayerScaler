using System;
using UnityEngine;
using HarmonyLib;
using MagicaCloth;
using Il2CppInterop;
using Il2CppInterop.Runtime;

[HarmonyPatch(typeof(Behaviour))]
class MagicaColliderEnabledWatcher
{
    /// <summary>
    /// Overrules the Setter.
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// 
    [HarmonyPatch(typeof(Behaviour), "set_enabled")]
    [HarmonyPrefix]
    private static bool OverruleMagicaIfRequired(Behaviour __instance, ref bool value)
    {
        // Suppress or modify the value being set        
        if (PluginConfiguration.ConfigJSON.Configuration.AllowColliderToggle && __instance.GetIl2CppType().IsAssignableFrom(Il2CppType.From(typeof(MagicaCapsuleCollider))))
        {
            MagicaCapsuleCollider cc = new MagicaCapsuleCollider(__instance.Pointer);
            if (cc.IsColliderFromAMita())
            {
                bool pluginSetting = FixedSizeScaler.CollidersEnabled;
                bool input = value;
                value &= pluginSetting;
                Debug.Log($"Is Mita: PS-{pluginSetting} GS-{input} ==> {value}");
            }

        }

        return true; // Allow original setter
    }
}

