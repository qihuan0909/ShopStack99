using System;
using System.Reflection;
using Duckov.Economy;
using UnityEngine;
using ShopMasterExtremesModConfig;
using TMPro;
using UnityEngine.UI;
using Duckov.Economy.UI;

namespace ShopMasterExtreme
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private static bool showAllItems = false;
        private bool updateReady = false;
        private bool ModConfigReday = false;
        private bool waitingForKey = false;
        private object harmonyInstance;
        private Type harmonyType;
        private Type harmonyMethodType;
        private static string configPath;
        private static KeyCode keyCode;

        private bool patched = false;
        private static bool showUI = false;
        private static int restockAmount = 99;

        private static readonly System.Collections.Generic.SortedDictionary<string, object> langMenuOptions =
        new System.Collections.Generic.SortedDictionary<string, object>
        {
            { "0. Automatic / 自动判断", "Auto" },
            { "1. Simplified Chinese / 简体中文", "Chinese" },
            { "2. English", "English" },
            { "3. Japanese / 日本語", "Japanese" },
            { "4. Korean / 한국어", "Korean" },
            { "5. German / Deutsch", "German" },
            { "6. French / Français", "French" },
            { "7. Russian / Русский", "Russian" },
            { "8. Spanish / Español", "Spanish" },
            { "9. Portuguese / Português", "Portuguese" }
        };

        private void Update()
        {
            TryPatch();
            TrySetConfig();

            if (Input.GetKeyDown(keyCode))
            {
                showUI = !showUI;
                string status = showUI ? "On" : "Off";
                Loger.Log(string.Format(Localization.Lang["Log_PanelStatus"], status));
            }
        }

        private void OnDisable()
        {
            if (ModConfigAPI.IsAvailable())
            {
                ModConfigAPI.SafeRemoveOnOptionsChangedDelegate(OnOptionChanged);
                Loger.Log(Localization.Lang["Log_ConfigRemoved"]);

                ModConfigAPI.SafeSave("ShopMasterExtreme", "RestockAmount", restockAmount);
                Loger.Log(string.Format(Localization.Lang["Log_ConfigSaved"], restockAmount));
            }

            TryUnpatch();
            Localization.TryUnloadLocallization();

            updateReady = false;
            ModConfigReday = false;
            patched = false;
            showUI = false;
        }

        private void Start()
        {
            Localization.TryLoadLocalization();

            string dllDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            configPath = System.IO.Path.Combine(dllDir, "Config.ini");

            if (System.IO.File.Exists(configPath))
            {
                try
                {
                    string[] lines = System.IO.File.ReadAllLines(configPath);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("ToggleKey=", StringComparison.OrdinalIgnoreCase))
                        {
                            string keyName = line.Substring("ToggleKey=".Length).Trim();
                            if (Enum.TryParse(keyName, out KeyCode parsed))
                                keyCode = parsed;
                            else
                                keyCode = KeyCode.Home;
                        }
                        else if (line.StartsWith("Loger.ShowLog=", StringComparison.OrdinalIgnoreCase))
                        {
                            bool.TryParse(line.Substring("Loger.ShowLog=".Length).Trim(), out Loger.ShowLog);
                        }
                        else if (line.StartsWith("ShowAllItems=", StringComparison.OrdinalIgnoreCase))
                        {
                            bool.TryParse(line.Substring("ShowAllItems=".Length).Trim(), out showAllItems);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Loger.LogError(string.Format(Localization.Lang["Log_ConfigReadError"], ex));
                    keyCode = KeyCode.Home;
                }
            }
            else
            {
                keyCode = KeyCode.Home;
                SaveConfig();
            }

            Loger.Log(string.Format(Localization.Lang["Log_CurrentKeyInfo"], keyCode));
        }

        private static void SaveConfig()
        {
            try
            {
                string content = $"# ShopMasterExtreme Configuration\n" +
                                 $"ToggleKey={keyCode}\n" +
                                 $"Loger.ShowLog={Loger.ShowLog}\n" +
                                 $"ShowAllItems={showAllItems}\n";
                System.IO.File.WriteAllText(configPath, content);
                Loger.Log(string.Format(Localization.Lang["Log_ConfigWriteInfo"], keyCode, Loger.ShowLog, showAllItems));
            }
            catch (Exception ex)
            {
                Loger.LogError(string.Format(Localization.Lang["Log_ConfigWriteError"],ex));
            }
        }

        private void TrySetConfig()
        {
            if (ModConfigAPI.IsAvailable())
            {
                if (ModConfigReday)
                    return;

                ModConfigAPI.Initialize();
                Loger.Log(string.Format(Localization.Lang["Log_RegisterConfig"]));

                ModConfigAPI.SafeAddInputWithSlider(
                    "ShopMasterExtreme",
                    "RestockAmount",
                    "Shop Stack Amount | 商店库存数量",
                    typeof(int),
                    restockAmount,
                    new Vector2(1, 999)
                );

                ModConfigAPI.SafeAddDropdownList(
                    "ShopMasterExtreme",
                    "Language",
                    "Mod Language | 插件语言设置 (Requires Restart | 需重启)",
                    langMenuOptions,
                    typeof(string),
                    "Auto"
                );

                ModConfigAPI.SafeAddOnOptionsChangedDelegate(OnOptionChanged);

                restockAmount = ModConfigAPI.SafeLoad("ShopMasterExtreme", "RestockAmount", 99);
                Localization.ManualLanguage = ModConfigAPI.SafeLoad("ShopMasterExtreme", "Language", "Auto");
                Loger.Log(string.Format(Localization.Lang["Log_RestockAmount"], restockAmount));
                ModConfigReday = true;
            }
            else
            {
                Loger.LogWarning(string.Format(Localization.Lang["Log_NoModConfig"]));
                ModConfigReday = true;
            }
        }

        private void TryPatch()
        {
            if (updateReady)
                return;

            try
            {
                harmonyType = Type.GetType("HarmonyLib.Harmony, 0Harmony");
                harmonyMethodType = Type.GetType("HarmonyLib.HarmonyMethod, 0Harmony");
                if (harmonyType == null || harmonyMethodType == null)
                {
                    Loger.LogError(string.Format(Localization.Lang["Log_HarmonyError"]));
                    return;
                }

                harmonyInstance = Activator.CreateInstance(harmonyType, "com.hgxy.ShopMasterExtreme");

                var target = typeof(StockShop).GetMethod(
                    "DoRefreshStock",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);

                if (target == null)
                {
                    Loger.LogError(string.Format(Localization.Lang["Log_TargetNotFound"]));
                    return;
                }

                var postfix = typeof(ModBehaviour).GetMethod(nameof(AfterRefresh),
                    BindingFlags.Public | BindingFlags.Static);

                var postfixHM = Activator.CreateInstance(harmonyMethodType, postfix);

                var patch = harmonyType.GetMethod("Patch", new Type[]
                {
                    typeof(MethodBase),
                    harmonyMethodType,
                    harmonyMethodType,
                    harmonyMethodType,
                    harmonyMethodType
                });

                patch.Invoke(harmonyInstance, new object[] { target, null, postfixHM, null, null });

                BulkPurchase.ApplyPatches(harmonyInstance, harmonyType, harmonyMethodType);

                Loger.Log(string.Format(Localization.Lang["Log_StartComplete"]));
                patched = true;
                updateReady = true;
            }
            catch (Exception ex)
            {
                Loger.LogError(string.Format(Localization.Lang["Log_PatchError"], ex));
                patched = false;
            }
        }

        private void TryUnpatch()
        {
            try
            {
                if (harmonyInstance != null)
                {
                    harmonyType.GetMethod("UnpatchAll", new[] { typeof(string) })
                        .Invoke(harmonyInstance, new object[] { "com.hgxy.ShopMasterExtreme" });
                    Loger.Log(string.Format(Localization.Lang["Log_UnpatchSuccess"]));
                }
                GameObject.Destroy(BulkPurchase.myInputField.gameObject);
                patched = false;
                updateReady = false;
            }
            catch (Exception ex)
            {
                Loger.LogError(string.Format(Localization.Lang["Log_UnpatchError"], ex)); ;
            }
        }

        private void OnOptionChanged(string key)
        {
            if (key == $"{"ShopMasterExtreme"}_RestockAmount")
            {
                restockAmount = ModConfigAPI.SafeLoad("ShopMasterExtreme", "RestockAmount", 99);
                Loger.Log(string.Format(Localization.Lang["Log_RestockUpdated"], restockAmount));
                ForceRefreshAllShops();
            }
            else if (key == "ShopMasterExtreme_Language")
            {
                string newLang = ModConfigAPI.SafeLoad("ShopMasterExtreme", "Language", "Auto");

                if (newLang != Localization.ManualLanguage)
                {
                    Localization.ManualLanguage = newLang;
                    Loger.Log(string.Format(Localization.Lang["Log_ChangingLanguage"], newLang));

                    TryUnpatch();
                    Localization.TryUnloadLocallization();

                    Localization.TryLoadLocalization();
                    TryPatch();

                    Loger.Log(Localization.Lang["Log_StartComplete"]);
                }
            }
        }

        private void ForceRefreshAllShops()
        {
            try
            {
                var allShops = GameObject.FindObjectsOfType<StockShop>();
                int count = 0;
                foreach (var shop in allShops)
                {
                    var method = typeof(StockShop).GetMethod("DoRefreshStock",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    method?.Invoke(shop, null);
                    count++;
                }
                Loger.Log(string.Format(Localization.Lang["Log_ForceRefreshSuccess"], count));
            }
            catch (Exception ex)
            {
                Loger.LogError(string.Format(Localization.Lang["Log_ForceRefreshError"], ex));
            }
        }

        private void OnGUI()
        {
            if (!showUI) return;

            GUI.BeginGroup(new Rect(20, 20, 280, 380), string.Format(Localization.Lang["Mod_ControlPanelTitle"]), GUI.skin.window);

            Loger.ShowLog = GUI.Toggle(new Rect(10, 25, 230, 25), Loger.ShowLog, string.Format(Localization.Lang["Mod_EnableLog"]));
            showAllItems = GUI.Toggle(new Rect(10, 55, 230, 25), showAllItems, string.Format(Localization.Lang["Mod_ShowAllItems"]));

            if (GUI.Button(new Rect(10, 95, 230, 30), patched ? string.Format(Localization.Lang["Mod_Reboot"]) : string.Format(Localization.Lang["Mod_EnablePatch"])))
            {
                if (patched)
                    TryUnpatch();
                else
                    TryPatch();
            }

            if (GUI.Button(new Rect(10, 135, 230, 30), string.Format(Localization.Lang["Mod_ForceRefresh"])))
            {
                ForceRefreshAllShops();
            }

            GUI.Label(new Rect(10, 175, 230, 25), string.Format(Localization.Lang["Mod_CurrentKey"], keyCode));
            if (GUI.Button(new Rect(10, 205, 230, 30), string.Format(Localization.Lang["Mod_ChangeKey"])))
            {
                waitingForKey = true;
                Loger.Log(string.Format(Localization.Lang["Log_WaitKeyPrompt"]));
            }

            if (waitingForKey)
            {
                GUI.Label(new Rect(10, 245, 230, 25), string.Format(Localization.Lang["Mod_WaitingKey"]));
                Event e = Event.current;
                if (e.isKey)
                {
                    keyCode = e.keyCode;
                    waitingForKey = false;
                    Loger.Log(string.Format(Localization.Lang["Log_NewKeyBind"], keyCode));
                    SaveConfig();
                }
            }

            if (GUI.Button(new Rect(10, 285, 230, 30), string.Format(Localization.Lang["Mod_SaveConfig"])))
            {
                SaveConfig();
            }

            GUI.EndGroup();
        }

        public static void AfterRefresh(StockShop __instance)
        {
            int amount = ModConfigAPI.IsAvailable()
            ? ModConfigAPI.SafeLoad("ShopMasterExtreme", "RestockAmount", restockAmount)
            : restockAmount;

            foreach (var e in __instance.entries)
            {
                if (showAllItems)
                {
                    e.Show = showAllItems;
                }
                e.CurrentStock = amount;
            }

            Loger.Log(string.Format(Localization.Lang["Log_ShopRestocked"], __instance.MerchantID, amount, showAllItems));
        }
    }
}
