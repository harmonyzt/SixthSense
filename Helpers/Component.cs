using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace SixthSense.Helpers
{
    public class SixthSenseAlertComponent : MonoBehaviour
    {
        private CanvasGroup _canvasGroup;
        private Image _iconImage;
        private Coroutine _fadeCoroutine;
        private Texture2D _iconTexture;
        private Sprite _iconSprite;

        private bool _isShowing;
        private float _showStartTime;

        // Fade timings
        private const float FadeInTime = 0.15f;
        private const float FadeOutTime = 0.35f;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_canvasGroup != null)
            {
                return;
            }

            // Container
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = gameObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -90f);
            rect.sizeDelta = new Vector2(140f, 140f);

            // Canvas
            _canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            GameObject iconObject = new GameObject("SixthSenseIcon", typeof(RectTransform));
            iconObject.transform.SetParent(transform, false);

            _iconImage = iconObject.AddComponent<Image>();
            _iconImage.color = Color.white;
            _iconImage.raycastTarget = false;
            _iconImage.preserveAspect = true;

            _iconSprite = LoadSpriteFromPluginDir(Plugin.AlertIconFile);
            _iconImage.sprite = _iconSprite;

            // Don't render white square if no Image
            _iconImage.enabled = _iconSprite != null;

            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(120f, 120f);
            iconRect.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Shows the icon and starts the fade. Never throws, never blocks: if a previous fade was
        /// interrupted (scene change, object disabled, ...) the state is recovered instead of
        /// latching <c>_isShowing</c> to true forever - which used to stop every following alert.
        /// </summary>
        public void ShowAlert()
        {
            if (_isShowing && Time.time - _showStartTime < Plugin.AlertDuration + FadeInTime + FadeOutTime + 1f)
            {
                return;
            }

            _isShowing = true;
            _showStartTime = Time.time;

            try
            {
                EnsureInitialized();

                if (_canvasGroup == null)
                {
                    Plugin.LogSource?.LogError(
                        "[SixthSense] Alert UI was not initialised; cannot show the alert icon.");
                    _isShowing = false;
                    return;
                }

                _canvasGroup.alpha = 0f;

                if (_fadeCoroutine != null)
                {
                    StopCoroutine(_fadeCoroutine);
                    _fadeCoroutine = null;
                }

                _fadeCoroutine = StartCoroutine(FadeAlertRoutine());
            }
            catch (Exception ex)
            {
                Plugin.LogSource?.LogError($"[SixthSense] Error showing alert: {ex}");
                _isShowing = false;
            }
        }

        private IEnumerator FadeAlertRoutine()
        {
            try
            {
                // Fade In
                float elapsed = 0f;
                float startAlpha = _canvasGroup.alpha;

                while (elapsed < FadeInTime)
                {
                    elapsed += Time.deltaTime;
                    _canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / FadeInTime);
                    yield return null;
                }

                _canvasGroup.alpha = 1f;

                yield return new WaitForSeconds(Plugin.AlertDuration);

                // Fade Out
                elapsed = 0f;
                while (elapsed < FadeOutTime)
                {
                    elapsed += Time.deltaTime;
                    _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / FadeOutTime);
                    yield return null;
                }
            }
            finally
            {
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 0f;
                }

                _isShowing = false;
                _fadeCoroutine = null;
            }
        }

        private Sprite LoadSpriteFromPluginDir(string fileName)
        {
            try
            {
                string dllPath = Assembly.GetExecutingAssembly().Location;
                string pluginDir = Path.GetDirectoryName(dllPath);
                string filePath = Path.Combine(pluginDir ?? string.Empty, fileName);

                if (!File.Exists(filePath))
                {
                    Plugin.LogSource?.LogError($"[SixthSense] Icon file not found at path: {filePath}");
                    return null;
                }

                byte[] fileData = File.ReadAllBytes(filePath);

                // Create texture
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (ImageConversion.LoadImage(tex, fileData))
                {
                    tex.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                    _iconTexture = tex; // Store reference

                    Sprite sprite = Sprite.Create(
                        tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f),
                        100f,
                        0,
                        SpriteMeshType.FullRect);

                    sprite.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                    return sprite;
                }

                Destroy(tex); // Clean up if LoadImage fail
                Plugin.LogSource?.LogError($"[SixthSense] Could not decode icon '{filePath}'.");
            }
            catch (Exception ex)
            {
                Plugin.LogSource?.LogError($"[SixthSense] Error loading sprite: {ex}");
            }

            return null;
        }

        private void OnDestroy()
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            _isShowing = false;

            if (_iconImage != null)
            {
                _iconImage.sprite = null;
            }

            if (_iconSprite != null)
            {
                Destroy(_iconSprite);
                _iconSprite = null;
            }

            if (_iconTexture != null)
            {
                Destroy(_iconTexture);
                _iconTexture = null;
            }
        }
    }
}