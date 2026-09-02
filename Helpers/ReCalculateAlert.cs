using System;

namespace SixthSense.Helpers;

public static class ReCalculateAlert
{
    private static float BaseAlertDelay = 5f;
    private static float BaseAlertDuration = 4f;
    private static float BaseAlertCooldown = 180f;
    
    private const float DelayReductionPerLevel = 0.05f;     // 5% reduction
    private const float DurationIncreasePerLevel = 0.04f;   // 4% increase
    private const float CooldownReductionPerLevel = 0.04f;  // 4% reduction

    private const float MinDelay = 0.5f;
    private const float MaxDuration = 7f;
    private const float MinCooldown = 10f;
    
    public static void Start(int level)
    {
        if (level <= 0)
        {
            Plugin.AlertDelay = BaseAlertDelay;
            Plugin.AlertDuration = BaseAlertDuration;
            Plugin.AlertCooldown = BaseAlertCooldown;
            return;
        }
        
        float newDelay = BaseAlertDelay - (BaseAlertDelay * (level * DelayReductionPerLevel));
        float newDuration = BaseAlertDuration + (BaseAlertDuration * (level * DurationIncreasePerLevel));
        float newCooldown = BaseAlertCooldown - (BaseAlertCooldown * (level * CooldownReductionPerLevel));

        newDelay = Math.Max(newDelay, MinDelay);
        newDuration = Math.Min(newDuration, MaxDuration); 
        newCooldown = Math.Max(newCooldown, MinCooldown);

        Plugin.AlertDelay = (float)Math.Round(newDelay, 1);
        Plugin.AlertDuration = (float)Math.Round(newDuration, 1);
        Plugin.AlertCooldown = (float)Math.Round(newCooldown, 0);
    }
}