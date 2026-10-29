using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SixthSense.Helpers;

public static class SixthSenseAlert
{
    private static SixthSenseAlertComponent _alertInstance;
    private static GameObject _canvasObject;

    private static float _lastTriggerTime = float.NegativeInfinity;

    public static void ResetCooldown()
    {
        _lastTriggerTime = float.NegativeInfinity;
    }

    public static void Trigger()
    {
        try
        {
            bool showIcon = Plugin.EnableAlert.Value;
            bool playSound = Plugin.EnableSound.Value;

            if (!showIcon && !playSound)
            {
                return;
            }

            float cooldown = Mathf.Max(0f, Plugin.AlertCooldown);
            if (Time.time - _lastTriggerTime < cooldown)
            {
                Plugin.Log(
                    $"[SixthSense] Alert skipped, cooldown active ({cooldown - (Time.time - _lastTriggerTime):0}s left).");
                return;
            }

            _lastTriggerTime = Time.time;

            if (showIcon)
            {
                ShowIcon();
            }

            if (playSound)
            {
                PlaySound();
            }
        }
        catch (Exception ex)
        {
            Plugin.LogSource?.LogError($"[SixthSense] Error while triggering the alert: {ex}");
        }
    }

    private static void ShowIcon()
    {
        SixthSenseAlertComponent instance = EnsureOverlay();

        if (instance == null)
        {
            Plugin.LogSource?.LogError("[SixthSense] Could not create the alert overlay.");
            return;
        }

        instance.ShowAlert();
    }

    private static void PlaySound()
    {
        _ = PlaySoundAsync();
    }

    private static async Task PlaySoundAsync()
    {
        try
        {
            AudioClip clip = await AudioLoader.LoadAudioAsync(Plugin.AlertSoundFile);

            if (clip == null)
            {
                Plugin.LogSource?.LogError("[SixthSense] Failed to play spot sound: clip could not be loaded.");
                return;
            }

            AudioLoader.PlayClip(clip, Plugin.SoundVolume.Value);
        }
        catch (Exception ex)
        {
            Plugin.LogSource?.LogError($"[SixthSense] Failed to play spot sound: {ex}");
        }
    }

    private static SixthSenseAlertComponent EnsureOverlay()
    {
        if (_alertInstance != null && _alertInstance.transform.parent != null)
        {
            return _alertInstance;
        }

        if (_canvasObject != null)
        {
            UnityEngine.Object.Destroy(_canvasObject);
            _canvasObject = null;
        }

        _alertInstance = null;

        _canvasObject = new GameObject("SixthSenseCanvas");
        UnityEngine.Object.DontDestroyOnLoad(_canvasObject);

        Canvas canvas = _canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // Render on top of everything

        CanvasScaler scaler = _canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        GameObject alertObject = new GameObject("SixthSenseAlert", typeof(RectTransform));
        alertObject.transform.SetParent(_canvasObject.transform, false);

        _alertInstance = alertObject.AddComponent<SixthSenseAlertComponent>();

        Plugin.Log("[SixthSense] Alert overlay created.");

        return _alertInstance;
    }
}