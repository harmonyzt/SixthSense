using System;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SixthSense.Helpers;
using SixthSense.Logic;

namespace SixthSense.Patches
{
    [HarmonyPatch(typeof(EnemyList), nameof(EnemyList.AddEnemy))]
    public static class OnAddEnemy
    {
        [HarmonyPostfix]
        public static void Postfix(EnemyList __instance, Enemy enemy)
        {
            try
            {
                if (__instance == null || enemy == null)
                {
                    return;
                }

                if (enemy.IsAI ||
                    !enemy.EnemyPlayer ||
                    !enemy.EnemyPlayer.IsYourPlayer)
                {
                    return;
                }

                if (!Singleton<AbstractGame>.Instance.InRaid)
                {
                    return;
                }
                
                SixthSenseAlert.Trigger();
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError(
                    $"[SixthSense] Patch error: {ex}"
                );
            }
        }
    }
}