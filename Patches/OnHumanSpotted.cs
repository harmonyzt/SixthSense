using System;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SixthSense.Helpers;
using UnityEngine;

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

                if (enemy.IsAI)
                {
                    return;
                }

                Player enemyPlayer = enemy.EnemyPlayer;
                if (enemyPlayer == null || !enemyPlayer.IsYourPlayer)
                {
                    return;
                }

                if (!Singleton<AbstractGame>.Instantiated || !Singleton<AbstractGame>.Instance.InRaid)
                {
                    return;
                }

                // There is squad guy screaming in pain and shitting all over the map that's fucking shit up and lets anyone else in squad know about you
                float distance = GetDistance(enemy, enemyPlayer);

                if (!IsDistanceAllowed(distance))
                {
                    Plugin.Log(
                        $"[SixthSense] Bot '{__instance.Name}' knows about you at {distance:0}m, outside the alert range ({DescribeRange()}). Ignored.");
                    return;
                }

                Plugin.Log(
                    $"[SixthSense] Bot '{__instance.Name}' added you to its enemy list ({DistanceText(distance)}).");

                AlertDelayer.TriggerDelayedAlert();
            }
            catch (Exception ex)
            {
                Plugin.LogSource?.LogError($"[SixthSense] Patch error: {ex}");
            }
        }

        private static float GetDistance(Enemy enemy, Player player)
        {
            try
            {
                float realDistance = enemy.RealDistance;
                if (realDistance > 0f && !float.IsNaN(realDistance) && !float.IsInfinity(realDistance))
                {
                    return realDistance;
                }

                if (enemy.Bot != null && player != null)
                {
                    return Vector3.Distance(enemy.Bot.Position, player.Transform.position);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log($"[SixthSense] Could not read the distance of '{enemy.EnemyProfileId}': {ex.Message}");
            }

            return -1f;
        }

        private static bool IsDistanceAllowed(float distance)
        {
            if (distance < 0f)
            {
                return true;
            }

            float min = Plugin.MinAlertDistance != null ? Plugin.MinAlertDistance.Value : 0f;
            float max = Plugin.MaxAlertDistance != null ? Plugin.MaxAlertDistance.Value : 0f;

            if (min > 0f && distance < min)
            {
                return false;
            }

            if (max > 0f && distance > max)
            {
                return false;
            }

            return true;
        }

        private static string DescribeRange()
        {
            float min = Plugin.MinAlertDistance != null ? Plugin.MinAlertDistance.Value : 0f;
            float max = Plugin.MaxAlertDistance != null ? Plugin.MaxAlertDistance.Value : 0f;

            string minText = min > 0f ? $"{min:0}m" : "off";
            string maxText = max > 0f ? $"{max:0}m" : "off";

            return $"min {minText}, max {maxText}";
        }

        private static string DistanceText(float distance)
        {
            return distance >= 0f ? $"{distance:0}m" : "distance unknown";
        }
    }
}