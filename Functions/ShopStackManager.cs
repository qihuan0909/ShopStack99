using Duckov.Economy;
using ShopMasterExtreme;
using System;
using System.Collections.Generic;
using System.Text;
using ShopMasterExtremesModConfig;
using static ShopMasterExtreme.ModBehaviour;
using System.Reflection;
using UnityEngine;

namespace ShopMasterExtreme.Functions
{
    internal class ShopStackManager
    {
        internal static int restockAmount = 99;
        internal static bool showAllItems = false;

        internal static void ApplyPatches(object harmonyInstance, Type harmonyType, Type harmonyMethodType)
        {
            var target = typeof(StockShop).GetMethod(
                    "DoRefreshStock",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);

            if (target == null)
            {
                Loger.LogError(string.Format(Localization.Lang["Log_TargetNotFound"]));
                return;
            }

            var postfix = typeof(ShopStackManager).GetMethod(nameof(AfterRefresh),
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

            var postfixHM = Activator.CreateInstance(harmonyMethodType, new object[] { postfix });

            var patch = harmonyType.GetMethod("Patch", new Type[]
            {
                    typeof(MethodBase),
                    harmonyMethodType,
                    harmonyMethodType,
                    harmonyMethodType,
                    harmonyMethodType
            });

            patch.Invoke(harmonyInstance, new object[] { target, null, postfixHM, null, null });
        }

        internal static void AfterRefresh(StockShop __instance)
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

        internal static void ForceRefreshAllShops()
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
    }
}
