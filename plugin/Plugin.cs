using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace ScritchyScratchyAP
{
    [BepInPlugin("com.yourname.scritchyscratchyap", "Scritchy Scratchy AP", "0.1.0")]
    public class Plugin : BasePlugin
    {
        public static new BepInEx.Logging.ManualLogSource Log;

        // Connection settings, editable in-game via ConnectionGUI (F1).
        public static ConfigEntry<string> ConfigHost;
        public static ConfigEntry<int> ConfigPort;
        public static ConfigEntry<string> ConfigSlotName;
        public static ConfigEntry<string> ConfigPassword;

        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("Scritchy Scratchy AP mod loaded!");

            ConfigHost = Config.Bind("Connection", "Host", "localhost", "Archipelago server host/IP.");
            ConfigPort = Config.Bind("Connection", "Port", 38281, "Archipelago server port.");
            ConfigSlotName = Config.Bind("Connection", "SlotName", "SS", "Your player slot name in the multiworld.");
            ConfigPassword = Config.Bind("Connection", "Password", "", "Server password, if required (leave blank if none).");

            new Harmony("com.yourname.scritchyscratchyap").PatchAll();
            AddComponent<APUpdateManager>();
            AddComponent<ConnectionGUI>();
        }
    }
}