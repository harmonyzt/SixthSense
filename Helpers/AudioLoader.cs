using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.UI;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;

namespace SixthSense.Helpers;

public static class AudioLoader
{
    private static readonly Dictionary<string, AudioClip> AudioCache =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, Task<AudioClip>> PendingLoads =
        new(StringComparer.OrdinalIgnoreCase);

    private static AudioSource _fallbackSource;

    private static string PluginDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    public static async void Preload(string fileName)
    {
        try
        {
            await LoadAudioAsync(fileName);
        }
        catch (Exception ex)
        {
            LogError($"Preload of '{fileName}' failed: {ex}");
        }
    }

    public static async Task<AudioClip> LoadAudioAsync(string fileName)
    {
        string normalizedName = NormalizeFileName(fileName);
        string cacheKey = normalizedName.ToLowerInvariant();

        if (AudioCache.TryGetValue(cacheKey, out AudioClip cached))
        {
            if (IsClipUsable(cached))
            {
                return cached;
            }

            AudioCache.Remove(cacheKey);
            Plugin.Log($"[SixthSense] Cached audio '{normalizedName}' was no longer usable, reloading.");
        }

        if (PendingLoads.TryGetValue(cacheKey, out Task<AudioClip> pending) && pending != null)
        {
            try
            {
                return await pending;
            }
            catch (Exception ex)
            {
                LogError($"Shared load of '{normalizedName}' failed: {ex}");
                return null;
            }
        }

        Task<AudioClip> loadTask = LoadFromDiskAsync(normalizedName, cacheKey);

        PendingLoads[cacheKey] = loadTask;
        try
        {
            return await loadTask;
        }
        finally
        {
            PendingLoads.Remove(cacheKey);
        }
    }

    private static async Task<AudioClip> LoadFromDiskAsync(string fileName, string cacheKey)
    {
        AudioClip clip = null;

        try
        {
            string fullPath = Path.Combine(PluginDirectory, fileName);
            if (!File.Exists(fullPath))
            {
                LogError($"Audio file not found at: {fullPath}");
                return null;
            }

            string url = new Uri(fullPath).AbsoluteUri;

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
            {
                UnityWebRequestAsyncOperation operation = www.SendWebRequest();

                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (www.result != UnityWebRequest.Result.Success)
                {
                    LogError($"Error loading audio from {url}: {www.error}");
                    return null;
                }

                clip = DownloadHandlerAudioClip.GetContent(www);
            }

            if (clip == null)
            {
                LogError($"Audio clip for '{fileName}' could not be decoded.");
                return null;
            }

            if (clip.loadState == AudioDataLoadState.Failed)
            {
                LogError($"Audio clip for '{fileName}' failed to decode (the audio system may not be ready yet).");
                UnityEngine.Object.Destroy(clip);
                return null;
            }

            clip.name = fileName;

            // Survive the Resources.UnloadUnusedAssets()
            clip.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            AudioCache[cacheKey] = clip;
            Plugin.Log($"[SixthSense] Audio '{fileName}' loaded ({clip.length:0.00}s).");

            return clip;
        }
        catch (Exception ex)
        {
            LogError($"Error loading audio '{fileName}': {ex}");
            return null;
        }
    }

    private static bool IsClipUsable(AudioClip clip)
    {
        if (clip == null)
        {
            return false;
        }

        try
        {
            switch (clip.loadState)
            {
                case AudioDataLoadState.Loaded:
                case AudioDataLoadState.Loading:
                    return true;

                case AudioDataLoadState.Unloaded:
                    return clip.LoadAudioData();

                default:
                    return false;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log($"[SixthSense] Audio clip state check failed: {ex.Message}");
            return false;
        }
    }

    public static bool PlayClip(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            LogError("Cannot play a null clip.");
            return false;
        }

        volume = Mathf.Clamp(volume, 0.01f, 3f);

        try
        {
            if (Singleton<GUISounds>.Instantiated)
            {
                Singleton<GUISounds>.Instance.PlaySound(
                    clip,
                    single: true,
                    commonUiSound: false,
                    volume: volume);

                Plugin.Log($"[SixthSense] Alert sound played through GUISounds (volume {volume:0.00}).");
                return true;
            }
        }
        catch (Exception ex)
        {
            LogError($"GUISounds playback failed, using fallback source: {ex}");
        }

        return PlayOnFallbackSource(clip, volume);
    }

    private static bool PlayOnFallbackSource(AudioClip clip, float volume)
    {
        try
        {
            EnsureFallbackSource();

            if (_fallbackSource == null)
            {
                LogError("No audio source available to play the alert sound.");
                return false;
            }

            _fallbackSource.PlayOneShot(clip, volume);
            Plugin.Log($"[SixthSense] Alert sound played through fallback source (volume {volume:0.00}).");
            return true;
        }
        catch (Exception ex)
        {
            LogError($"Fallback playback failed: {ex}");
            return false;
        }
    }

    private static void EnsureFallbackSource()
    {
        if (_fallbackSource != null)
        {
            return;
        }

        GameObject audioObject = new GameObject("SixthSense_Audio");
        UnityEngine.Object.DontDestroyOnLoad(audioObject);

        _fallbackSource = audioObject.AddComponent<AudioSource>();
        _fallbackSource.playOnAwake = false;
        _fallbackSource.loop = false;
        _fallbackSource.spatialBlend = 0f;
        _fallbackSource.volume = 1f;
        _fallbackSource.ignoreListenerPause = true;

        // Route it through the same mixer group the game uses
        try
        {
            if (Singleton<GUISounds>.Instantiated && Singleton<GUISounds>.Instance.MasterMixer != null)
            {
                AudioMixerGroup[] groups = Singleton<GUISounds>.Instance.MasterMixer.FindMatchingGroups("UI");
                if (groups != null && groups.Length > 0)
                {
                    _fallbackSource.outputAudioMixerGroup = groups[0];
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log($"[SixthSense] Could not attach UI mixer group to fallback source: {ex.Message}");
        }
    }

    private static string NormalizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Audio file name must not be empty.", nameof(fileName));
        }

        fileName = fileName.Trim();

        if (Path.GetExtension(fileName).Length == 0)
        {
            fileName += ".mp3";
        }

        return fileName;
    }

    private static void LogError(string message)
    {
        Plugin.LogSource?.LogError($"[SixthSense] {message}");
    }
}