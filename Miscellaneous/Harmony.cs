using ShopMasterExtreme.Functions;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Rendering;
using UnityEngine;

namespace ShopMasterExtreme.Miscellaneous
{
    internal static class Harmony
    {
        internal static bool updateReady = false;
        internal static bool patched = false;
        private static object harmonyInstance;
        private static Type harmonyType;
        private static Type harmonyMethodType;

        internal static void TryPatch()
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

                ShopStackManager.ApplyPatches(harmonyInstance, harmonyType, harmonyMethodType);
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

        internal static void TryUnpatch()
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
    }
}
