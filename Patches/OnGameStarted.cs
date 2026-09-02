using System;
using System.Reflection;
using HarmonyLib;
using EFT;
using SixthSense.Helpers;
using SPT.Reflection.Patching;

namespace SixthSense.Patches
{
    internal class GameStartedAlreadyPatch : ModulePatch
    {
        public static string MainPlayerId { get; private set; }
        public static int MainPlayerPerceptionSkill { get; private set; }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));
        }

        [PatchPostfix]
        public static async void PatchPostfix(GameWorld __instance)
        {
            try
            {
                await AudioLoader.LoadAudioAsync("alert_sound");

                MainPlayerId = __instance.MainPlayer?.ProfileId;

                // Refresh our delay of the trigger
                MainPlayerPerceptionSkill = __instance.MainPlayer.Skills.Perception.Level;
                ReCalculateAlert.Start(MainPlayerPerceptionSkill);
                
                Plugin.LogSource.LogInfo($"[Sixth Sense] Preloaded.");
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error initializing Sixth Sense: {ex.Message}");
            }
        }
    }
}

