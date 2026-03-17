using ShopMasterExtreme.Configs;
using ShopMasterExtreme.Functions;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ShopMasterExtreme.Miscellaneous
{
    internal static class GUI
    {
        private static bool waitingForKey = false;
        internal static bool showUI = false;

        internal static void DrawGUI()
        {
            if (!showUI) return;

            UnityEngine.GUI.BeginGroup(new Rect(20, 20, 280, 380), string.Format(Localization.Lang["Mod_ControlPanelTitle"]), UnityEngine.GUI.skin.window);

            Loger.ShowLog = UnityEngine.GUI.Toggle(new Rect(10, 25, 230, 25), Loger.ShowLog, string.Format(Localization.Lang["Mod_EnableLog"]));
            ShopStackManager.showAllItems = UnityEngine.GUI.Toggle(new Rect(10, 55, 230, 25), ShopStackManager.showAllItems, string.Format(Localization.Lang["Mod_ShowAllItems"]));

            if (UnityEngine.GUI.Button(new Rect(10, 95, 230, 30), Miscellaneous.Harmony.patched ? string.Format(Localization.Lang["Mod_Reboot"]) : string.Format(Localization.Lang["Mod_EnablePatch"])))
            {
                if (Miscellaneous.Harmony.patched)
                    Miscellaneous.Harmony.TryUnpatch();
                else
                    Miscellaneous.Harmony.TryPatch();
            }

            if (UnityEngine.GUI.Button(new Rect(10, 135, 230, 30), string.Format(Localization.Lang["Mod_ForceRefresh"])))
            {
                ShopStackManager.ForceRefreshAllShops();
            }

            UnityEngine.GUI.Label(new Rect(10, 175, 230, 25), string.Format(Localization.Lang["Mod_CurrentKey"], Config.keyCode));
            if (UnityEngine.GUI.Button(new Rect(10, 205, 230, 30), string.Format(Localization.Lang["Mod_ChangeKey"])))
            {
                waitingForKey = true;
                Loger.Log(string.Format(Localization.Lang["Log_WaitKeyPrompt"]));
            }

            if (waitingForKey)
            {
                UnityEngine.GUI.Label(new Rect(10, 245, 230, 25), string.Format(Localization.Lang["Mod_WaitingKey"]));
                Event e = Event.current;
                if (e.isKey)
                {
                    Config.keyCode = e.keyCode;
                    waitingForKey = false;
                    Loger.Log(string.Format(Localization.Lang["Log_NewKeyBind"], Config.keyCode));
                    Config.SaveConfig();
                }
            }

            if (UnityEngine.GUI.Button(new Rect(10, 285, 230, 30), string.Format(Localization.Lang["Mod_SaveConfig"])))
            {
                Config.SaveConfig();
            }

            UnityEngine.GUI.EndGroup();
        }

        internal static void MonitorGUIToggle()
        {
            if (Input.GetKeyDown(Config.keyCode))
            {
                showUI = !showUI;
                string status = showUI ? "On" : "Off";
                Loger.Log(string.Format(Localization.Lang["Log_PanelStatus"], status));
            }
        }
    }
}
