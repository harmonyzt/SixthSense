using System.Collections;
using UnityEngine;

namespace SixthSense.Helpers
{
    public class AlertDelayer : MonoBehaviour
    {
        private static AlertDelayer _instance;
        
        public static void Initialize()
        {
            if (!_instance)
            {
                var gameObject = new GameObject("SixthSense_AlertDelayer");
                _instance = gameObject.AddComponent<AlertDelayer>();
                DontDestroyOnLoad(gameObject);
            }
        }
        
        public static void TriggerDelayedAlert()
        {
            if (!_instance)
            {
                Initialize();
            }
            
            _instance.StartCoroutine(_instance.DelayedTrigger());
        }
        
        private IEnumerator DelayedTrigger()
        {
            yield return new WaitForSeconds(Plugin.AlertDelay);

            SixthSenseAlert.Trigger();
        }
    }
}