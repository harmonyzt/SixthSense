using System.Threading.Tasks;
using UnityEngine;

namespace SixthSense.Helpers;

public static class SixthSenseAlert
{
    private static SixthSenseAlertComponent _alertInstance;
    private static float _lastTriggerTime = -999f;

    public static async Task Trigger()
    {
        if (!Plugin.EnableAlert.Value)
        {
            return;
        }
            
        float cooldown = Plugin.AlertCooldown;
        if (Time.time - _lastTriggerTime < cooldown)
        {
            return;
        }

        _lastTriggerTime = Time.time;
        
        if (!_alertInstance || !_alertInstance.gameObject)
        {
            var rootCanvas = Object.FindObjectOfType<Canvas>();
            if (!rootCanvas)
            {
                Plugin.LogSource.LogError("[SixthSense] Could not find canvas in raid.");
                return;
            }

            var alertObj = new GameObject("SixthSenseAlert");
            alertObj.transform.SetParent(rootCanvas.transform, false);
            _alertInstance = alertObj.AddComponent<SixthSenseAlertComponent>();

            // Hide it, just in case, idk im already lost in this
            alertObj.SetActive(false);
        }

        await _alertInstance.ShowAlert();
    }
}