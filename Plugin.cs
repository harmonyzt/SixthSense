using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SixthSense.Helpers;
using SixthSense.Patches;

namespace SixthSense
{
    [BepInPlugin("com.harmonyzt.SixthSense", "SixthSense", "1.1.0")]
    [BepInDependency("me.sol.sain")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;
        private static Harmony _harmony;
        
        public static ConfigEntry<bool> EnableAlert;
        public static ConfigEntry<bool> EnableSound;
        public static ConfigEntry<float> SoundVolume;
        
        public static float AlertDelay = 5f;
        public static float AlertDuration = 4f;
        public static float AlertCooldown = 180f;

        private void Awake()
        {
            LogSource = Logger;
            
            EnableAlert = Config.Bind("General", "Enable Alert", true, new ConfigDescription("Should the Sixth Sense icon be shown?"));
            EnableSound = Config.Bind("General", "Enable Alert Sound", true, new ConfigDescription("Should the Sixth Sense audio play?"));
            SoundVolume = Config.Bind("Audio", "Sound Volume", 1.0f, new ConfigDescription("Even though sound is tied to EFT UI Sounds, if your audio is quiet or too loud this might help.", new AcceptableValueRange<float>(0.1f, 3.0f)));
            
            _harmony = new Harmony("com.harmonyzt.SixthSense");
            _harmony.PatchAll();
            
            new GameStartedAlreadyPatch().Enable();
            
            AlertDelayer.Initialize();
            
            Logger.LogInfo("Sixth Sense is loaded!");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}