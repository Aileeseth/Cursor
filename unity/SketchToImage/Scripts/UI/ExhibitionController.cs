using SketchToImage.ComfyUI;
using SketchToImage.Config;
using UnityEngine;
using UnityEngine.UI;

namespace SketchToImage.UI
{
    /// <summary>
    /// Auto-sends sketch to ComfyUI when the drawing has changed.
    /// Keeps drawing UI interactive; shows non-blocking status so users are not left with silence.
    /// </summary>
    public class ExhibitionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] SketchPad sketchPad;
        [SerializeField] ComfyUIWorkflowRunner workflowRunner;
        [SerializeField] RawImage bigScreenImage;
        [SerializeField] GameObject drawingPanel;
        [SerializeField] GameObject generatingPanel;
        [SerializeField] GameObject resultPanel;
        [SerializeField] Button confirmButton;
        [SerializeField] Button clearButton;
        [SerializeField] Button homeButton;

        [Header("Status Feedback (recommended)")]
        [Tooltip("Optional Text under the pad. Without this, generate start/fail looks like no response.")]
        [SerializeField] Text statusText;
        [SerializeField] float errorStatusSeconds = 5f;
        [SerializeField] float successStatusSeconds = 2f;

        [Header("Color Buttons (optional)")]
        [SerializeField] Button[] colorButtons;

        [Header("External Config")]
        [SerializeField] string configFileName = ExhibitionRuntimeConfig.DefaultFileName;
        [SerializeField] bool useExternalConfig = true;

        [Header("Fallback (if config missing)")]
        [SerializeField] bool autoSendEnabled = true;
        [SerializeField] float autoSendInterval = 1f;

        Texture2D _resultTexture;
        float _autoSendTimer;
        bool _autoSendActive = true;
        ExhibitionRuntimeConfigData _runtimeConfig;
        float _statusClearAt = -1f;
        bool _isGenerating;

        void Start()
        {
            LoadRuntimeConfig();
            BindColorButtons();

            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
            if (clearButton != null) clearButton.onClick.AddListener(OnClear);
            if (homeButton != null) homeButton.onClick.AddListener(GoHome);

            GoHome();
        }

        void LoadRuntimeConfig()
        {
            _runtimeConfig = useExternalConfig
                ? ExhibitionRuntimeConfig.Load(configFileName)
                : new ExhibitionRuntimeConfigData
                {
                    autoSendEnabled = autoSendEnabled,
                    autoSendInterval = autoSendInterval
                };
        }

        void BindColorButtons()
        {
            if (sketchPad == null || colorButtons == null) return;

            for (var i = 0; i < colorButtons.Length; i++)
            {
                if (colorButtons[i] == null) continue;
                var index = i;
                colorButtons[i].onClick.AddListener(() => sketchPad.SetBrushColorIndex(index));
            }
        }

        void Update()
        {
            if (_statusClearAt > 0f && Time.unscaledTime >= _statusClearAt && !_isGenerating)
            {
                _statusClearAt = -1f;
                SetStatus("继续涂鸦，完成后会自动生成", false);
            }

            if (!_autoSendActive || _runtimeConfig == null || !_runtimeConfig.autoSendEnabled) return;
            if (sketchPad == null || workflowRunner == null) return;
            if (!sketchPad.HasChanges) return;
            if (workflowRunner.IsBusy || _isGenerating) return;

            _autoSendTimer += Time.deltaTime;
            if (_autoSendTimer < _runtimeConfig.autoSendInterval) return;

            _autoSendTimer = 0f;
            RequestGenerate();
        }

        void OnDestroy()
        {
            if (_resultTexture != null)
                Destroy(_resultTexture);
        }

        public void OnConfirm()
        {
            if (sketchPad == null)
            {
                SetStatus("画板未绑定", true);
                return;
            }

            if (!sketchPad.HasChanges)
            {
                SetStatus("请先涂鸦，再确认生成", true);
                return;
            }

            if (workflowRunner != null && (workflowRunner.IsBusy || _isGenerating))
            {
                SetStatus("正在生成中，请稍候…", false);
                return;
            }

            _autoSendTimer = 0f;
            RequestGenerate();
        }

        void RequestGenerate()
        {
            if (sketchPad == null || workflowRunner == null)
            {
                SetStatus("生成组件未绑定（SketchPad / WorkflowRunner）", true);
                return;
            }

            if (!sketchPad.HasChanges) return;
            if (workflowRunner.IsBusy || _isGenerating) return;

            _isGenerating = true;
            SetGeneratingVisible(true);
            SetStatus("正在生成，请稍候…", false);

            workflowRunner.GenerateFromSketch(
                sketchPad.GetSketchTexture(),
                OnGenerateSuccess,
                OnGenerateFailed);
        }

        void OnGenerateSuccess(Texture2D refined)
        {
            _isGenerating = false;
            SetGeneratingVisible(false);
            sketchPad?.MarkClean();

            if (_resultTexture != null)
                Destroy(_resultTexture);

            _resultTexture = refined;
            if (bigScreenImage != null)
            {
                bigScreenImage.texture = _resultTexture;
                SetStatus("生成完成，大屏已更新", false);
                _statusClearAt = Time.unscaledTime + successStatusSeconds;
            }
            else
            {
                SetStatus("生成成功，但大屏 RawImage 未绑定", true);
                _statusClearAt = Time.unscaledTime + errorStatusSeconds;
            }
        }

        void OnGenerateFailed(string error)
        {
            _isGenerating = false;
            SetGeneratingVisible(false);
            Debug.LogWarning($"[Exhibition] Generate failed: {error}");
            SetStatus($"生成失败：{error}", true);
            _statusClearAt = Time.unscaledTime + errorStatusSeconds;
            // Keep HasChanges=true so auto-send can retry after interval.
            _autoSendTimer = 0f;
        }

        public void OnClear()
        {
            sketchPad?.Clear();
            SetStatus("画板已清空", false);
            _statusClearAt = Time.unscaledTime + successStatusSeconds;
        }

        public void GoHome()
        {
            _autoSendTimer = 0f;
            _isGenerating = false;
            _statusClearAt = -1f;

            if (_resultTexture != null)
            {
                Destroy(_resultTexture);
                _resultTexture = null;
            }

            if (bigScreenImage != null)
                bigScreenImage.texture = null;

            sketchPad?.Clear();
            sketchPad?.MarkClean();
            ApplyDrawingLayout();
            SetStatus("继续涂鸦，完成后会自动生成", false);
        }

        void ApplyDrawingLayout()
        {
            // Drawing stays available; generating panel is only a non-blocking indicator.
            if (drawingPanel != null) drawingPanel.SetActive(true);
            SetGeneratingVisible(false);
            if (resultPanel != null) resultPanel.SetActive(false);

            if (confirmButton != null)
                confirmButton.interactable = true;
        }

        void SetGeneratingVisible(bool visible)
        {
            if (generatingPanel != null)
                generatingPanel.SetActive(visible);
        }

        void SetStatus(string message, bool isError)
        {
            if (statusText == null) return;
            statusText.text = message;
            statusText.color = isError ? new Color(1f, 0.35f, 0.35f) : Color.white;
        }

        public void SetAutoSendActive(bool active)
        {
            _autoSendActive = active;
            if (active) _autoSendTimer = 0f;
        }

        public void ReloadRuntimeConfig()
        {
            LoadRuntimeConfig();
            _autoSendTimer = 0f;
        }
    }
}
