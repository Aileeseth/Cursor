using SketchToImage.ComfyUI;
using UnityEngine;
using UnityEngine.UI;

namespace SketchToImage.UI
{
    /// <summary>
    /// Draw on pad while auto-sending sketch to ComfyUI every interval.
    /// No generating/result page switch — big screen updates when ready.
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

        [Header("Auto Send")]
        [SerializeField] bool autoSendEnabled = true;
        [SerializeField] float autoSendInterval = 1f;

        Texture2D _resultTexture;
        float _autoSendTimer;
        bool _autoSendActive = true;

        void Start()
        {
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
            if (clearButton != null) clearButton.onClick.AddListener(OnClear);
            if (homeButton != null) homeButton.onClick.AddListener(GoHome);

            GoHome();
        }

        void Update()
        {
            if (!_autoSendActive || !autoSendEnabled) return;
            if (sketchPad == null || workflowRunner == null) return;
            if (workflowRunner.IsBusy) return;

            _autoSendTimer += Time.deltaTime;
            if (_autoSendTimer < autoSendInterval) return;

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
            _autoSendTimer = 0f;
            RequestGenerate();
        }

        void RequestGenerate()
        {
            if (sketchPad == null || workflowRunner == null) return;
            if (workflowRunner.IsBusy) return;

            workflowRunner.GenerateFromSketch(
                sketchPad.GetSketchTexture(),
                OnGenerateSuccess,
                OnGenerateFailed);
        }

        void OnGenerateSuccess(Texture2D refined)
        {
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
    }
}
