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
        [Tooltip("Export from ComfyUI: Save (API Format). Place in StreamingAssets.")]
        public TextAsset workflowJson;

        [Tooltip("Load Image node id in workflow_api.json, e.g. \"14\"")]
        public string loadImageNodeId = "14";

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
