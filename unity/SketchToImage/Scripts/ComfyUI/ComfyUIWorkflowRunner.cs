using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SketchToImage.ComfyUI
{
    /// <summary>
    /// Uploads sketch, patches workflow JSON, queues ComfyUI, returns refined texture.
    /// </summary>
    public class ComfyUIWorkflowRunner : MonoBehaviour
    {
        [SerializeField] ComfyUISettings settings;

        ComfyUIClient _client;
        bool _isRunning;

        public bool IsBusy => _isRunning;

        void Awake()
        {
            if (settings == null)
            {
                Debug.LogError("[ComfyUI] ComfyUISettings is not assigned.");
                return;
            }

            _client = new ComfyUIClient(settings.baseUrl, settings.pollIntervalMs, settings.timeoutSeconds);
        }

        public void GenerateFromSketch(Texture2D sketch, Action<Texture2D> onSuccess, Action<string> onError)
        {
            if (_isRunning) return;

            if (settings == null)
            {
                onError?.Invoke("ComfyUI Settings not assigned.");
                return;
            }

            if (!settings.TryGetWorkflowText(out _, out var configError))
            {
                onError?.Invoke(configError);
                return;
            }

            StartCoroutine(GenerateCoroutine(sketch, onSuccess, onError));
        }

        IEnumerator GenerateCoroutine(Texture2D sketch, Action<Texture2D> onSuccess, Action<string> onError)
        {
            _isRunning = true;

            if (!settings.TryGetWorkflowText(out var workflowText, out var configError))
            {
                _isRunning = false;
                onError?.Invoke(configError);
                yield break;
            }

            var prepared = PrepareSketch(sketch);
            var png = prepared.EncodeToPNG();
            Destroy(prepared);

            var uploadName = $"sketch_{DateTime.Now.Ticks}.png";
            ComfyUIClient.UploadResult uploadResult = null;
            string uploadError = null;

            yield return _client.UploadImage(png, uploadName,
                r => uploadResult = r,
                e => uploadError = e);

            if (uploadResult == null)
            {
                _isRunning = false;
                onError?.Invoke(uploadError ?? "Upload failed.");
                yield break;
            }

            string promptJson;
            try
            {
                promptJson = PatchWorkflow(workflowText, uploadResult.name);
            }
            catch (Exception ex)
            {
                _isRunning = false;
                onError?.Invoke($"Workflow patch failed: {ex.Message}");
                yield break;
            }

            string promptId = null;
            string queueError = null;
            yield return _client.QueuePrompt(promptJson,
                id => promptId = id,
                e => queueError = e);

            if (string.IsNullOrEmpty(promptId))
            {
                _isRunning = false;
                onError?.Invoke(queueError ?? "Queue failed.");
                yield break;
            }

            ComfyUIClient.OutputImage outputImage = null;
            string waitError = null;
            yield return _client.WaitForImage(promptId,
                img => outputImage = img,
                e => waitError = e);

            if (outputImage == null)
            {
                _isRunning = false;
                onError?.Invoke(waitError ?? "No output image.");
                yield break;
            }

            Texture2D result = null;
            string downloadError = null;
            yield return _client.DownloadTexture(outputImage,
                tex => result = tex,
                e => downloadError = e);

            _isRunning = false;

            if (result == null)
            {
                onError?.Invoke(downloadError ?? "Download failed.");
                yield break;
            }

            onSuccess?.Invoke(result);
        }

        Texture2D PrepareSketch(Texture2D source)
        {
            var size = settings.sketchSize;
            var rt = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, rt);

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return tex;
        }

        string PatchWorkflow(string workflow, string uploadedImageName)
        {
            var patched = workflow;

            // Load Image node: set "image" field
            patched = PatchNodeStringInput(patched, settings.loadImageNodeId, "image", uploadedImageName);

            if (settings.overridePrompts)
            {
                patched = PatchNodeStringInput(patched, settings.positivePromptNodeId, "text", settings.positivePrompt);
                patched = PatchNodeStringInput(patched, settings.negativePromptNodeId, "text", settings.negativePrompt);
            }

            // Randomize seed on KSampler nodes
            patched = Regex.Replace(patched, "(\"seed\"\\s*:\\s*)\\d+",
                m => m.Groups[1].Value + UnityEngine.Random.Range(0, int.MaxValue));

            return patched;
        }

        static string PatchNodeStringInput(string workflow, string nodeId, string inputKey, string value)
        {
            var escaped = EscapeJsonString(value);
            var pattern = $"(\"{nodeId}\"\\s*:\\s*\\{{[\\s\\S]*?\"inputs\"\\s*:\\s*\\{{[\\s\\S]*?\"{inputKey}\"\\s*:\\s*\")([^\"]*)(\")";
            if (!Regex.IsMatch(workflow, pattern))
                throw new InvalidOperationException($"Node {nodeId} input '{inputKey}' not found in workflow.");

            return Regex.Replace(workflow, pattern,
                m => m.Groups[1].Value + escaped + m.Groups[3].Value,
                RegexOptions.Singleline);
        }

        static string EscapeJsonString(string s)
        {
            var sb = new StringBuilder();
            foreach (var c in s)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }
    }
}
