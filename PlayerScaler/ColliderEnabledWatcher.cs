using System;
using UnityEngine;
using HarmonyLib;
using System.Reflection;
using BepInEx.Configuration;

[HarmonyPatch(typeof(UnityEngine.Collider))]
public static class ColliderEnabledWatcher
{ 
    /// <summary>
    /// Hooks into native code - Harmony annotations would have hooked the wrapper method.
    /// </summary>
    public static void HookNativeUnityCollider()
    {
        var harmony = new Harmony("org.miside.plugins.playerscale.collider_enable_watch");
        harmony.PatchAll(typeof(ColliderEnabledWatcher));
    }

    /// <summary>
    /// Overrules the Setter.
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// 
#pragma warning disable IDE0051 // Is called via Harmony patcher - weirdly enough, hooking the property does not work and seems to hook into the wrapper instead.
    [HarmonyPatch("set_enabled")]
    [HarmonyPrefix]
    private static bool OverruleIfRequired(Collider __instance, ref bool value)
#pragma warning restore IDE0051
    {
        // Suppress or modify the value being set
        if (__instance is CapsuleCollider && PluginConfiguration.ConfigJSON.Configuration.AllowColliderToggle)
        {
            CapsuleCollider cc = ((CapsuleCollider)__instance);

            if (cc.IsColliderFromAMita())
            {
                bool pluginSetting = FixedSizeScaler.CollidersEnabled;
                bool input = value;
                value &= pluginSetting;
                //Debug.Log($"Attempt to set {input} - plugin master state is {pluginSetting} --> {value}");
            } 
            else
            {
                Debug.Log($"{__instance.name}: no state");
            }

        }        

        return true; // Allow original setter
    }
}

