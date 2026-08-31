using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SixthSense.Helpers
{
    public class SixthSenseAlertComponent : MonoBehaviour
    {
        private CanvasGroup _canvasGroup;
        private AudioSource _audioSource;
        private Image _iconImage;
        private Coroutine _fadeCoroutine;

        // After looking through dozens of articles, all I found is this, and I can't find a native efts way of displaying icons.
        // Please, if someone is better at this, Make a PR. I would appreciate it <3
        private void Awake()
        {
            // Container
            var rect = gameObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -90f);
            rect.sizeDelta = new Vector2(140f, 140f);

            // Canvas
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;

            var iconObject = new GameObject("SixthSenseIcon");
            iconObject.transform.SetParent(transform, false);

            _iconImage = iconObject.AddComponent<Image>();
            _iconImage.color = Color.white; 

            var sprite = LoadSpriteFromPluginDir("alert_icon.png");
            if (sprite)
            {
                _iconImage.sprite = sprite;
            }

            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(96f, 96f);
            iconRect.anchoredPosition = Vector2.zero;

            // Audio
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;

            gameObject.SetActive(false);
        }

        public async Task ShowAlert()
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }

            _fadeCoroutine = StartCoroutine(FadeAlertRoutine());
            
            if (Plugin.EnableSound.Value)
            {
                await PlaySpotSound();
            }
            
            gameObject.SetActive(true);
        }

        private IEnumerator FadeAlertRoutine()
        {
            // Fade In
            float elapsed = 0f;
            float startAlpha = _canvasGroup.alpha;

            while (elapsed < 0.15f)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / 0.25f);
                yield return null;
            }

            _canvasGroup.alpha = 1f;
            
            yield return new WaitForSeconds(Plugin.AlertDuration.Value);

            // Fade Out
            elapsed = 0f;
            while (elapsed < 0.35f)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.35f);
                yield return null;
            }

            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private async Task PlaySpotSound()
        {
            AudioClip clip = await AudioLoader.LoadAudioAsync("alert_sound.mp3");

            if (clip == null)
            {
                Plugin.LogSource.LogError("[SixthSense] Failed to play spot sound: clip is null.");
                return;
            }
            
            if (Singleton<GUISounds>.Instantiated)
            {
                Singleton<GUISounds>.Instance.PlaySound(
                    clip, 
                    single: false, 
                    commonUiSound: true, 
                    volume: Plugin.SoundVolume.Value
                );
            }
        }
        
        private Sprite LoadSpriteFromPluginDir(string fileName)
        {
            try
            {
                string dllPath = Assembly.GetExecutingAssembly().Location;
                string pluginDir = Path.GetDirectoryName(dllPath);
                string filePath = Path.Combine(pluginDir, fileName);

                if (File.Exists(filePath))
                {
                    byte[] fileData = File.ReadAllBytes(filePath);
                    
                    // Create texture
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (ImageConversion.LoadImage(tex, fileData))
                    {
                        return Sprite.Create(
                            tex, 
                            new Rect(0, 0, tex.width, tex.height), 
                            new Vector2(0.5f, 0.5f), 
                            100f
                        );
                    }
                }
                else
                {
                    Debug.LogError($"[SixthSense] Icon file not found at path: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SixthSense] Error loading sprite: {ex.Message}");
            }

            return null;
        }
    }
}