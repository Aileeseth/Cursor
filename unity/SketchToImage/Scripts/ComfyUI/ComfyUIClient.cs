using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SketchToImage.ComfyUI
{
    /// <summary>
    /// Low-level HTTP client for local ComfyUI server.
    /// </summary>
    public class ComfyUIClient
    {
        readonly string _baseUrl;
        readonly int _pollIntervalMs;
        readonly int _timeoutSeconds;

        public ComfyUIClient(string baseUrl, int pollIntervalMs = 500, int timeoutSeconds = 60)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _pollIntervalMs = pollIntervalMs;
            _timeoutSeconds = timeoutSeconds;
        }

        public IEnumerator UploadImage(byte[] pngBytes, string filename, Action<UploadResult> onComplete, Action<string> onError)
        {
            var form = new WWWForm();
            form.AddBinaryData("image", pngBytes, filename, "image/png");
            form.AddField("overwrite", "true");

            using var req = UnityWebRequest.Post($"{_baseUrl}/upload/image", form);
            req.timeout = _timeoutSeconds;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"Upload failed: {req.error}. Is ComfyUI running at {_baseUrl}?");
                yield break;
            }

            var result = JsonUtility.FromJson<UploadResult>(req.downloadHandler.text);
            if (string.IsNullOrEmpty(result.name))
            {
                onError?.Invoke("Upload response missing filename.");
                yield break;
            }

            onComplete?.Invoke(result);
        }

        public IEnumerator QueuePrompt(string promptJson, Action<string> onPromptId, Action<string> onError)
        {
            var clientId = Guid.NewGuid().ToString();
            var body = $"{{\"prompt\":{promptJson},\"client_id\":\"{clientId}\"}}";
            var bytes = Encoding.UTF8.GetBytes(body);

            using var req = new UnityWebRequest($"{_baseUrl}/prompt", "POST");
            req.uploadHandler = new UploadHandlerRaw(bytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = _timeoutSeconds;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"Queue prompt failed: {req.error}. Check workflow node ids / ComfyUI console.");
                yield break;
            }

            var response = JsonUtility.FromJson<PromptQueueResponse>(req.downloadHandler.text);
            if (string.IsNullOrEmpty(response.prompt_id))
            {
                onError?.Invoke("Prompt response missing prompt_id.");
                yield break;
            }

            onPromptId?.Invoke(response.prompt_id);
        }

        public IEnumerator WaitForImage(string promptId, Action<OutputImage> onImage, Action<string> onError)
        {
            var elapsed = 0f;
            while (elapsed < _timeoutSeconds)
            {
                using var req = UnityWebRequest.Get($"{_baseUrl}/history/{promptId}");
                req.timeout = Mathf.Max(5, _timeoutSeconds);
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    var image = TryParseHistoryImage(req.downloadHandler.text);
                    if (image != null)
                    {
                        onImage?.Invoke(image);
                        yield break;
                    }
                }

                yield return new WaitForSeconds(_pollIntervalMs / 1000f);
                elapsed += _pollIntervalMs / 1000f;
            }

            onError?.Invoke($"Timed out after {_timeoutSeconds}s waiting for ComfyUI.");
        }

        public IEnumerator DownloadTexture(OutputImage image, Action<Texture2D> onComplete, Action<string> onError)
        {
            var url = $"{_baseUrl}/view?filename={UnityWebRequest.EscapeURL(image.filename)}" +
                      $"&subfolder={UnityWebRequest.EscapeURL(image.subfolder ?? "")}" +
                      $"&type={UnityWebRequest.EscapeURL(image.type ?? "output")}";

            using var req = UnityWebRequestTexture.GetTexture(url);
            req.timeout = _timeoutSeconds;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"Download image failed: {req.error}");
                yield break;
            }

            var tex = DownloadHandlerTexture.GetContent(req);
            onComplete?.Invoke(tex);
        }

        static OutputImage TryParseHistoryImage(string historyJson)
        {
            // History format: { "prompt_id": { "outputs": { "node_id": { "images": [...] } } } }
            // Use lightweight string parsing to avoid Newtonsoft dependency.
            var imagesKey = "\"images\"";
            var idx = historyJson.IndexOf(imagesKey, StringComparison.Ordinal);
            if (idx < 0) return null;

            var filename = ExtractJsonStringValue(historyJson, "filename", idx);
            if (string.IsNullOrEmpty(filename)) return null;

            return new OutputImage
            {
                filename = filename,
                subfolder = ExtractJsonStringValue(historyJson, "subfolder", idx) ?? "",
                type = ExtractJsonStringValue(historyJson, "type", idx) ?? "output"
            };
        }

        static string ExtractJsonStringValue(string json, string key, int searchFrom = 0)
        {
            var pattern = $"\"{key}\"";
            var idx = json.IndexOf(pattern, searchFrom, StringComparison.Ordinal);
            if (idx < 0) return null;

            var colon = json.IndexOf(':', idx);
            if (colon < 0) return null;

            var firstQuote = json.IndexOf('"', colon + 1);
            if (firstQuote < 0) return null;

            var secondQuote = json.IndexOf('"', firstQuote + 1);
            if (secondQuote < 0) return null;

            return json.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
        }

        [Serializable]
        public class UploadResult
        {
            public string name;
            public string subfolder;
            public string type;
        }

        [Serializable]
        class PromptQueueResponse
        {
            public string prompt_id;
        }

        public class OutputImage
        {
            public string filename;
            public string subfolder;
            public string type;
        }
    }
}
