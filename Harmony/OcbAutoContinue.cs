using HarmonyLib;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections;

public class OcbAutoContinue : IModApi
{

    static bool isStarted = false;

    // Entry class for patching
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
        if (wasChecked)
        {
            // Re-use cached result
            return isAutoStart;
        }
        else if (IsShiftPressed)
        {
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
    // [HarmonyPatch(typeof(XUiC_SteamLogin))]
    // [HarmonyPatch("updateState")]
    // public class SphereII_SteamLoginAutoLogin
    // {
    //     private static void Postfix(XUiC_SteamLogin __instance)
    //     {
    //         if (!IsAutoStart()) return;
    //         Log.Warning("OCB Auto Continue is activated. Game is going into Offline mode.");
    //         Log.Warning("Please make sure Steam is started before starting the game.");
    //         Log.Warning("Multi-player disabled as steam was not detected!");
    //         var method = __instance.GetType().GetMethod("BtnOffline_OnPressed", BindingFlags.NonPublic | BindingFlags.Instance);
    //         method?.Invoke(__instance, new object[] { null, null });
    //     }
    // }

    [HarmonyPatch(typeof(XUiC_MainMenuButtons))]
    [HarmonyPatch("OnOpen")]
    public class OCB_Main_Menu_AutoClick
    {
        private static void Postfix(XUiC_MainMenuButtons __instance)
        {
            if (!IsAutoStart()) return;
            if (isStarted) __instance.btnQuit_OnPressed(null, 0);
            else __instance.btnContinueGame_OnPressed(null, 0);
        }
    }

    [HarmonyPatch(typeof(XUiC_NewContinueGame))]
    [HarmonyPatch("OnOpen")]
    public class OCB_XUIC_NewContinueGame
    {

        private static IEnumerator ContinueGame(XUiC_NewContinueGame __instance)
        {
            yield return new WaitForSeconds(0.05f);
            __instance.BtnStart_OnPressed(null, 0);
            isStarted = true;
        }

        private static void Postfix(XUiC_NewContinueGame __instance)
        {
            if (!IsAutoStart()) return;
            GameManager.Instance.StartCoroutine(
                ContinueGame(__instance));
        }
    }

}
