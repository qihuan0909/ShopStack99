using System;
using System.Reflection;
using Duckov.Economy;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Duckov.Economy.UI;
using ShopMasterExtremesModConfig;
using ShopMasterExtreme.Functions;
using ShopMasterExtreme.Configs;

namespace ShopMasterExtreme
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {

        private void Update()
        {
            Miscellaneous.GUI.MonitorGUIToggle();
        }

        private void OnDisable()
        {
            ModConfig.SaveModConfig();
            ModConfig.UnloadModConfig();
            Miscellaneous.Harmony.TryUnpatch();
            Localization.TryUnloadLocallization();
            Miscellaneous.GUI.showUI = false;
        }

        private void Start()
        {
            Localization.TryLoadLocalization();
            Miscellaneous.Harmony.TryPatch();
            ModConfig.TrySetConfig();
            Config.TryLoadConfig();
        }

        private void OnGUI()
        {
            Miscellaneous.GUI.DrawGUI();
        }
    }
}
