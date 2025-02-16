using System;
using UnityEngine;
using HarmonyLib;

[HarmonyPatch(typeof(Collider))]
public static class ColliderEnabledWatcher
{ 
    /// <summary>
    /// Hooks into native code - Harmony annotations would have hooked the wrapper method.
    /// </summary>
    public static void HookNativeUnityCollider(bool hookCollider = true)
    {        
        if ((PluginConfiguration.ConfigJSON.Configuration.AllowColliderToggle && hookCollider) || PluginConfiguration.ConfigJSON.Configuration.IncludePlayerSpeed)
        {
            var harmony = new Harmony("org.miside.plugins.playerscale.collider_enable_watch");

            if (PluginConfiguration.ConfigJSON.Configuration.AllowColliderToggle && hookCollider)
            {
                harmony.PatchAll(typeof(ColliderEnabledWatcher));
            }

            //harmony.PatchAll(typeof(MagicaColliderEnabledWatcher));

            if (PluginConfiguration.ConfigJSON.Configuration.IncludePlayerSpeed)
            {
                harmony.PatchAll(typeof(PlayerSpeedModder));
            }            
        }               
    }

    /// <summary>
    /// Overrules the Setter.
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// 
    [HarmonyPatch("set_enabled")]
    [HarmonyPrefix]
    private static bool OverruleIfRequired(Collider __instance, ref bool value)
    {
        // Suppress or modify the value being set
        
        if (PluginConfiguration.ConfigJSON.Configuration.AllowColliderToggle)
        {
            Debug.Log($"Setting {value} on {__instance.gameObject.name}");
            if (__instance.IsColliderFromAMita())
            {
                bool pluginSetting = FixedSizeScaler.CollidersEnabled;
                bool gameInput = value;
                if ( BaseScaler.ScalerWritesCollider )
                {
                    gameInput = __instance.GetGameDefault();
                }
                else
                {
                    __instance.SetGameDefault(gameInput);
                }
                
                value = gameInput & pluginSetting;
                Debug.Log($"Is Mita: PS-{pluginSetting} GS-{gameInput} ==> {value}");
            }             

        }        

        return true; // Allow original setter
    }    
}

