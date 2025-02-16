using System;
using UnityEngine;
using HarmonyLib;

[HarmonyPatch(typeof(PlayerMove))]
public static class PlayerSpeedModder
{
    public static float MaxSpeedFactor = 1.0f;

    [HarmonyPatch("FixedUpdate")]
    [HarmonyPrefix]
    private static void PreFixedUpdate(PlayerMove __instance, ref float __state)
    {
        __state = __instance.moveSlow;

        __instance.moveSlow *= Mathf.Pow(1 / MaxSpeedFactor, 1 / 4.0f);
    }

    [HarmonyPatch("FixedUpdate")]
    [HarmonyPostfix]
    private static void PostFixedUpdate(PlayerMove __instance, ref float __state)
    {
        __instance.moveSlow = __state;
    }
}

