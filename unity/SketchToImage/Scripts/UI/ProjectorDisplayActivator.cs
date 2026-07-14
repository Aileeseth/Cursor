using UnityEngine;

namespace SketchToImage.UI
{
    /// <summary>
    /// Optional: show ComfyUI output on a second display (projector).
    /// Attach to a GameObject in the first scene.
    /// </summary>
    public class ProjectorDisplayActivator : MonoBehaviour
    {
        [SerializeField] int displayIndex = 1;

        void Start()
        {
            if (Display.displays.Length <= displayIndex)
            {
                Debug.LogWarning($"[Projector] Display {displayIndex} not found. Connected displays: {Display.displays.Length}");
                return;
            }

            Display.displays[displayIndex].Activate();
        }
    }
}
