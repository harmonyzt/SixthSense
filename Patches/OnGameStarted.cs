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
                Plugin.LogSource.LogInfo("[Sixth Sense] Preloaded sounds.");
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error initializing Sixth Sense: {ex.Message}");
            }
        }
    }
}