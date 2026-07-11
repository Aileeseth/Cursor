using System;
using System.IO;
using UnityEngine;

namespace SketchToImage.Config
{
    [Serializable]
    public class ExhibitionRuntimeConfigData
    {
        public bool autoSendEnabled = true;
        public float autoSendInterval = 1f;
    }

    /// <summary>
    /// Loads exhibition settings from StreamingAssets/exhibition_config.json.
    /// Edit JSON without rebuilding; falls back to defaults if missing.
    /// </summary>
    public static class ExhibitionRuntimeConfig
    {
        public const string DefaultFileName = "exhibition_config.json";

        public static ExhibitionRuntimeConfigData Load(string fileName = DefaultFileName)
        {
            var data = new ExhibitionRuntimeConfigData();
            var path = Path.Combine(Application.streamingAssetsPath, fileName);

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[Exhibition] Config not found, using defaults: {path}");
                return data;
            }

            try
            {
                var json = File.ReadAllText(path);
                JsonUtility.FromJsonOverwrite(json, data);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Exhibition] Failed to load config, using defaults: {ex.Message}");
            }

            data.autoSendInterval = Mathf.Max(0.2f, data.autoSendInterval);
            return data;
        }
    }
}
