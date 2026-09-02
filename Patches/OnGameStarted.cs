using System;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using EFT;
using SixthSense.Helpers;
using SPT.Reflection.Patching;

namespace SixthSense.Patches
{
    public class GameStartedPatch : ModulePatch
    {
        public static string MainPlayerId { get; private set; }
        public static int MainPlayerPerceptionSkill { get; private set; }
        
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));
        }

        [PatchPrefix]
        private static async Task PatchPrefix(GameWorld __instance)
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