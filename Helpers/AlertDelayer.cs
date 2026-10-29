using System;
using UnityEngine;

namespace SixthSense.Helpers
{
    public class AlertDelayer : MonoBehaviour
    {
        private static AlertDelayer _instance;
        private static float _pendingTriggerTime = -1f;

        public static void Initialize()
        {
            try
            {
                if (_instance != null)
                {
                    if (!_instance.gameObject.activeSelf)
                    {
                        _instance.gameObject.SetActive(true);
                    }

                    return;
                }

                GameObject host = new GameObject("SixthSense_AlertDelayer");
                DontDestroyOnLoad(host);
                _instance = host.AddComponent<AlertDelayer>();
            }
            catch (Exception ex)
            {
                Plugin.LogSource?.LogError($"[SixthSense] Could not create the alert delayer: {ex}");
            }
        }

        // Schedules an alert 
        public static void TriggerDelayedAlert()
        {
            Initialize();

            if (_instance == null)
            {
                return;
            }

            if (_pendingTriggerTime >= 0f)
            {
                return;
            }

            _pendingTriggerTime = Time.time + Mathf.Max(0f, Plugin.AlertDelay);
        }

        private void Update()
        {
            if (_pendingTriggerTime < 0f || Time.time < _pendingTriggerTime)
            {
                return;
            }

            _pendingTriggerTime = -1f;

            SixthSenseAlert.Trigger();
        }

        private void OnDestroy()
        {
            _pendingTriggerTime = -1f;

            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}