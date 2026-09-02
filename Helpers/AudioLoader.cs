using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace SixthSense.Helpers;

public static class AudioLoader
{
    private static readonly Dictionary<string, AudioClip> AudioCache = new();
    private static string MainDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    
    public static async Task<AudioClip> LoadAudioAsync(string fileName)
    {
        if (!fileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
        {
            fileName += ".mp3";
        }

        if (AudioCache.TryGetValue(fileName, out var cachedClip) && cachedClip != null)
        {
            return cachedClip;
        }

        string fullPath = Path.Combine(MainDirectory, fileName);
        if (!File.Exists(fullPath))
        {
            Plugin.LogSource.LogError($"[SixthSense] Audio file not found at: {fullPath}");
            return null;
        }
        
        string url = new Uri(fullPath).AbsoluteUri;

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            var operation = www.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                Plugin.LogSource.LogError($"[SixthSense] Error loading audio from {url}: {www.error}");
                return null;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            if (clip)
            {
                clip.name = fileName;
                AudioCache[fileName] = clip;
            }

            return clip;
        }
    }
}