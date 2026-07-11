using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SketchToImage.UI
{
    /// <summary>
    /// Touch/mouse drawing pad with multi-color brush and change detection.
    /// </summary>
    public class SketchPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] RawImage canvasImage;
        [SerializeField] Color backgroundColor = Color.black;
        [SerializeField] Color brushColor = Color.white;
        [SerializeField] Color[] presetColors = { Color.white, Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta };
        [SerializeField] int textureSize = 512;
        [SerializeField] int brushRadius = 12;

        Texture2D _texture;
        bool _isDrawing;
        bool _hasChanges;

        public event Action TextureChanged;
        public bool HasChanges => _hasChanges;
        public Color CurrentBrushColor => brushColor;
        public IReadOnlyList<Color> PresetColors => presetColors;

        void Awake()
        {
            InitTexture();
        }

        void OnDestroy()
        {
            if (_texture != null)
                Destroy(_texture);
        }

        public void SetBrushColor(Color color)
        {
            brushColor = color;
        }

        public void SetBrushColorIndex(int index)
        {
            if (presetColors == null || presetColors.Length == 0) return;
            index = Mathf.Clamp(index, 0, presetColors.Length - 1);
            brushColor = presetColors[index];
        }

        public void MarkClean()
        {
            _hasChanges = false;
        }

        public void Clear()
        {
            Fill(backgroundColor);
            _hasChanges = true;
            TextureChanged?.Invoke();
        }

        public Texture2D GetSketchTexture()
        {
            return _texture;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isDrawing = true;
            DrawAt(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDrawing) return;
            DrawAt(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isDrawing = false;
        }

        void InitTexture()
        {
            _texture = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);
            _texture.filterMode = FilterMode.Point;
            Fill(backgroundColor);
            _hasChanges = false;
            if (canvasImage != null)
                canvasImage.texture = _texture;
        }

        void Fill(Color color)
        {
            var pixels = new Color[textureSize * textureSize];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            _texture.SetPixels(pixels);
            _texture.Apply();
        }

        void DrawAt(Vector2 screenPos)
        {
            if (canvasImage == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasImage.rectTransform, screenPos, null, out var local))
                return;

            var rect = canvasImage.rectTransform.rect;
            var u = (local.x - rect.x) / rect.width;
            var v = (local.y - rect.y) / rect.height;
            if (u < 0f || u > 1f || v < 0f || v > 1f) return;

            var cx = Mathf.RoundToInt(u * textureSize);
            var cy = Mathf.RoundToInt(v * textureSize);
            var changed = false;

            for (var y = -brushRadius; y <= brushRadius; y++)
            {
                for (var x = -brushRadius; x <= brushRadius; x++)
                {
                    if (x * x + y * y > brushRadius * brushRadius) continue;
                    var px = cx + x;
                    var py = cy + y;
                    if (px < 0 || px >= textureSize || py < 0 || py >= textureSize) continue;
                    var prev = _texture.GetPixel(px, py);
                    if (prev == brushColor) continue;
                    _texture.SetPixel(px, py, brushColor);
                    changed = true;
                }
            }

            if (!changed) return;

            _texture.Apply();
            _hasChanges = true;
            TextureChanged?.Invoke();
        }
    }
}
