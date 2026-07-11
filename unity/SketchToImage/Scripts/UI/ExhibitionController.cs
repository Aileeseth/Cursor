using SketchToImage.ComfyUI;
using SketchToImage.Config;
using UnityEngine;
using UnityEngine.UI;

namespace SketchToImage.UI
{
    /// <summary>
    /// Auto-sends sketch to ComfyUI when the drawing has changed.
    /// Refresh interval loaded from StreamingAssets/exhibition_config.json.
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
            if (!_autoSendActive || _runtimeConfig == null || !_runtimeConfig.autoSendEnabled) return;
            if (sketchPad == null || workflowRunner == null) return;
            if (!sketchPad.HasChanges) return;
            if (workflowRunner.IsBusy) return;

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
            if (sketchPad == null || !sketchPad.HasChanges) return;
            _autoSendTimer = 0f;
            RequestGenerate();
        }

        void RequestGenerate()
        {
            if (sketchPad == null || workflowRunner == null) return;
            if (!sketchPad.HasChanges) return;
            if (workflowRunner.IsBusy) return;

            workflowRunner.GenerateFromSketch(
                sketchPad.GetSketchTexture(),
                OnGenerateSuccess,
                OnGenerateFailed);
        }

        void OnGenerateSuccess(Texture2D refined)
        {
            sketchPad?.MarkClean();

            if (_resultTexture != null)
                Destroy(_resultTexture);

            _resultTexture = refined;
            if (bigScreenImage != null)
                bigScreenImage.texture = _resultTexture;
        }

        void OnGenerateFailed(string error)
        {
            Debug.LogWarning($"[Exhibition] Generate failed: {error}");
        }

        public void OnClear()
        {
            sketchPad?.Clear();
        }

        public void GoHome()
        {
            _autoSendTimer = 0f;

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
        }

        void ApplyDrawingLayout()
        {
            if (drawingPanel != null) drawingPanel.SetActive(true);
            if (generatingPanel != null) generatingPanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);

            if (confirmButton != null)
                confirmButton.interactable = true;
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
