using System.IO;
using UnityEngine;

namespace SketchToImage.ComfyUI
{
    [CreateAssetMenu(fileName = "ComfyUISettings", menuName = "SketchToImage/ComfyUI Settings")]
    public class ComfyUISettings : ScriptableObject
    {
        [Header("Server")]
        [Tooltip("Local ComfyUI address. Same machine as Unity.")]
        public string baseUrl = "http://127.0.0.1:8188";

        [Header("Workflow")]
        [Tooltip("Option A: drag workflow_api.json from Project window here.")]
        public TextAsset workflowJson;

        [Tooltip("Option B: if drag fails, put file in Assets/StreamingAssets/ and type filename here.")]
        public string workflowFileName = "workflow_api.json";

        [Tooltip("Load Image node id in workflow_api.json, e.g. \"14\"")]
        public string loadImageNodeId = "14";

        public bool TryGetWorkflowText(out string text, out string error)
        {
            if (workflowJson != null)
            {
                text = workflowJson.text;
                error = null;
                return true;
            }

            if (string.IsNullOrWhiteSpace(workflowFileName))
            {
                text = null;
                error = "Workflow not set. Drag workflow_api.json to Workflow Json, or fill Workflow File Name.";
                return false;
            }

            var path = Path.Combine(Application.streamingAssetsPath, workflowFileName);
            if (!File.Exists(path))
            {
                text = null;
                error = $"Workflow file not found: {path}";
                return false;
            }

            text = File.ReadAllText(path);
            error = null;
            return true;
        }

        [Header("Generation")]
        public int sketchSize = 512;
        public int pollIntervalMs = 500;
        public int timeoutSeconds = 60;

        [Header("Prompts (optional override)")]
        public bool overridePrompts;
        [TextArea(2, 4)] public string positivePrompt =
            "beautiful detailed artwork, soft lighting, clean composition, exhibition style, high quality";
        [TextArea(2, 4)] public string negativePrompt =
            "low quality, blurry, messy, distorted, bad anatomy, watermark, text";

        public string positivePromptNodeId = "12";
        public string negativePromptNodeId = "13";
    }
}
