using HarmonyLib;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;

public class OcbAutoContinue : IModApi
{

    // Entry class for A20 patching
    public void InitMod(Mod mod)
    {
        Log.Out("Loading Patch: " + GetType().ToString());
        var harmony = new Harmony(GetType().ToString());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    static bool IsAltPressed => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
    static bool IsShiftPressed => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

    static bool isAutoStart = false;
    static bool wasChecked = false;

    static bool IsAutoStart()
    {
        if (wasChecked) {
            // Re-use cached result
            return isAutoStart;
        }
        else if(IsShiftPressed) {
            wasChecked = true;
            isAutoStart = false;
        }
        else
        {
            foreach (string arg in Environment.GetCommandLineArgs())
                if (arg.EqualsCaseInsensitive("-autostart"))
                    isAutoStart = true;
        }
        return isAutoStart;
    }

    // If steam is not detected, we want the game to go into Offline mode.
    // This can happen even if Steam is running, but at a different permission level.
    [HarmonyPatch(typeof(XUiC_SteamLogin))]
    [HarmonyPatch("updateState")]
    public class SphereII_SteamLoginAutoLogin
    {
        private static void Postfix(XUiC_SteamLogin __instance)
        {
            if (!IsAutoStart()) return;
            Log.Warning("OCB Auto Continue is activated. Game is going into Offline mode.");
            Log.Warning("Please make sure Steam is started before starting the game.");
            Log.Warning("Multi-player disabled as steam was not detected!");
            var method = __instance.GetType().GetMethod("BtnOffline_OnPressed", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(__instance, new object[] { null, null });
        }
    }

    [HarmonyPatch(typeof(XUiC_MainMenu))]
    [HarmonyPatch("OnOpen")]
    public class SphereII_Main_Menu_AutoClick
    {
        private static void Postfix(XUiC_MainMenu __instance)
        {
            if (!IsAutoStart()) return;
            if (GamePrefs.GetInt(EnumGamePrefs.AutopilotMode) == 0)
            {
                GamePrefs.Set(EnumGamePrefs.AutopilotMode, 1);
                var method = __instance.GetType().GetMethod("btnContinueGame_OnPressed", BindingFlags.NonPublic | BindingFlags.Instance);
                method?.Invoke(__instance, new object[] { null, null });
            }
            else if (GamePrefs.GetInt(EnumGamePrefs.AutopilotMode) == 1)
            {
                var method = __instance.GetType().GetMethod("btnQuit_OnPressed", BindingFlags.NonPublic | BindingFlags.Instance);
                method?.Invoke(__instance, new object[] { null, null });
            }
        }
    }

    [HarmonyPatch(typeof(XUiC_NewContinueGame))]
    [HarmonyPatch("OnOpen")]
    public class SphereII_XUIC_NewContinueGame
    {
        private static void Postfix(XUiC_NewContinueGame __instance)
        {
            if (!IsAutoStart()) return;
            var method = __instance.GetType().GetMethod("BtnStart_OnPressed", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(__instance, new object[] { null, null });
        }
    }

}
