using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SixthSense.Helpers;
using SixthSense.Patches;

namespace SixthSense
{
    [BepInPlugin("com.harmonyzt.SixthSense", "SixthSense", "1.1.1")]
    [BepInDependency("me.sol.sain")]
    public class Plugin : BaseUnityPlugin
    {
        public const string AlertSoundFile = "alert_sound.mp3";
        public const string AlertIconFile = "alert_icon.png";

        public static ManualLogSource LogSource;
        private static Harmony _harmony;

        public static ConfigEntry<bool> EnableAlert;
        public static ConfigEntry<bool> EnableSound;
        public static ConfigEntry<float> SoundVolume;
        public static ConfigEntry<float> MinAlertDistance;
        public static ConfigEntry<float> MaxAlertDistance;
        public static ConfigEntry<bool> VerboseLogging;

        public static float AlertDelay = 5f;
        public static float AlertDuration = 4f;
        public static float AlertCooldown = 180f;

        private void Awake()
        {
            LogSource = Logger;

            EnableAlert = Config.Bind("General", "Enable Alert", true,
                new ConfigDescription("Should the Sixth Sense icon be shown? (the sound has its own setting)"));

            EnableSound = Config.Bind("General", "Enable Alert Sound", true,
                new ConfigDescription("Should the Sixth Sense audio play?"));

            SoundVolume = Config.Bind("Audio", "Sound Volume", 1.0f,
                new ConfigDescription(
                    "Even though sound is tied to EFT UI Sounds, if your audio is quiet or too loud this might help.",
                    new AcceptableValueRange<float>(0.1f, 3.0f)));

            MinAlertDistance = Config.Bind("Distance", "Minimum Alert Distance", 3f,
                new ConfigDescription(
                    "Bots closer than this (in metres) will not trigger the alert - at that range you can already see/hear them. Set to 0 to disable this check.",
                    new AcceptableValueRange<float>(0f, 1000f)));

            MaxAlertDistance = Config.Bind("Distance", "Maximum Alert Distance", 300f,
                new ConfigDescription(
                    "Bots farther away than this (in metres) will not trigger the alert, so a bot across the map noticing you stays quiet. Set to 0 to disable this check.",
                    new AcceptableValueRange<float>(0f, 3000f)));

            VerboseLogging = Config.Bind("Debug", "Verbose Logging", false,
                new ConfigDescription(
                    "Log extra information about alerts and audio loading. Only needed when reporting a problem."));

            _harmony = new Harmony("com.harmonyzt.SixthSense");
            _harmony.PatchAll();

            new GameStartedAlreadyPatch().Enable();

            AlertDelayer.Initialize();
            AudioLoader.Preload(AlertSoundFile);

            Logger.LogInfo("Sixth Sense is loaded!");
        }

        public static void Log(string message)
        {
            if (VerboseLogging != null && VerboseLogging.Value)
            {
                LogSource?.LogInfo(message);
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}