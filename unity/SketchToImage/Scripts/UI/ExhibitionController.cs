using System.Collections;
using SketchToImage.ComfyUI;
using UnityEngine;
using UnityEngine.UI;

namespace SketchToImage.UI
{
    public enum ExhibitionState
    {
        Drawing,
        Generating,
        ShowingResult
    }

    /// <summary>
    /// Exhibition flow: draw sketch -> confirm -> ComfyUI refine -> show on display.
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

        ExhibitionState _state = ExhibitionState.Drawing;
        Texture2D _resultTexture;

        void Start()
        {
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
            if (clearButton != null) clearButton.onClick.AddListener(OnClear);
            if (homeButton != null) homeButton.onClick.AddListener(GoHome);

            GoHome();
        }

        void OnDestroy()
        {
            if (_resultTexture != null)
                Destroy(_resultTexture);
        }

        public void OnConfirm()
        {
            if (_state != ExhibitionState.Drawing) return;
            if (sketchPad == null || workflowRunner == null) return;

            SetState(ExhibitionState.Generating);

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

            SetState(ExhibitionState.ShowingResult);
        }

        void OnGenerateFailed(string error)
        {
            Debug.LogWarning($"[Exhibition] Generate failed: {error}");
            // Stay calm for exhibition: return to drawing, no blocking error UI required.
            SetState(ExhibitionState.Drawing);
        }

        public void OnClear()
        {
            sketchPad?.Clear();
        }

        public void GoHome()
        {
            if (_resultTexture != null)
            {
                Destroy(_resultTexture);
                _resultTexture = null;
            }

            if (bigScreenImage != null)
                bigScreenImage.texture = null;

            sketchPad?.Clear();
            SetState(ExhibitionState.Drawing);
        }

        void SetState(ExhibitionState state)
        {
            _state = state;

            if (drawingPanel != null) drawingPanel.SetActive(state == ExhibitionState.Drawing);
            if (generatingPanel != null) generatingPanel.SetActive(state == ExhibitionState.Generating);
            if (resultPanel != null) resultPanel.SetActive(state == ExhibitionState.ShowingResult);

            if (confirmButton != null)
                confirmButton.interactable = state == ExhibitionState.Drawing;
        }
    }
}
