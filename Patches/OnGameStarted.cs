using System;
using System.Reflection;
using EFT;
using HarmonyLib;
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
        public static void PatchPostfix(GameWorld __instance)
        {
            try
            {
                Player mainPlayer = __instance.MainPlayer;

                if (mainPlayer == null)
                {
                    Plugin.LogSource?.LogWarning(
                        "[SixthSense] Raid started without a main player, skipping initialisation.");
                    return;
                }

                MainPlayerId = mainPlayer.ProfileId;
                MainPlayerPerceptionSkill = mainPlayer.Skills.Perception.Level;
                ReCalculateAlert.Start(MainPlayerPerceptionSkill);

                // Clean once raid starts, because we never cleaned it
                SixthSenseAlert.ResetCooldown();

                // Cache hit p7
                AudioLoader.Preload(Plugin.AlertSoundFile);
            }
            catch (Exception ex)
            {
                Plugin.LogSource?.LogError($"Error initializing Sixth Sense: {ex}");
            }
        }
    }
}