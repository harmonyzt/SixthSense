using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SixthSense.Logic;
using UnityEngine;

namespace SixthSense
{
    [BepInPlugin("com.harmonyzt.SixthSense", "SixthSense", "1.0.0")]
    [BepInDependency("me.sol.sain")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;
        private static Harmony _harmony;
        
        public static ConfigEntry<bool> EnableAlert;
        public static ConfigEntry<bool> EnableSound;
        public static ConfigEntry<float> SoundVolume;
        public static ConfigEntry<float> AlertDuration;
        public static ConfigEntry<float> AlertCooldown;

        private void Awake()
        {
            LogSource = Logger;
            
            EnableAlert = Config.Bind("General", "Enable Alert", true, new ConfigDescription("Should the Sixth Sense icon be shown?"));
            EnableSound = Config.Bind("General", "Enable Alert Sound", true, new ConfigDescription("Should the Sixth Sense audio play?"));
            AlertDuration = Config.Bind("General", "Alert Duration", 3f, new ConfigDescription("How long (in seconds) the alert icon stays on screen."));
            AlertCooldown = Config.Bind("General", "Alert Cooldown", 60.0f, new ConfigDescription("Cooldown (in seconds) before the alert can trigger again.", new AcceptableValueRange<float>(3f, 300f)));
            SoundVolume = Config.Bind("Audio", "Sound Volume", 1.0f, new ConfigDescription("Even though sound is tied to EFT UI Sounds, this might be helpful for someone.", new AcceptableValueRange<float>(0.1f, 1.5f)));
            
            _harmony = new Harmony("com.harmonyzt.SixthSense");
            _harmony.PatchAll();
            
            Logger.LogInfo("Sixth Sense is loaded!");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}