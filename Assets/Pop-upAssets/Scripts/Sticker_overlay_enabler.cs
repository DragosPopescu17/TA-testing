
using UnityEngine;
using UnityEngine.UI;

public class StickerOverlayEnabler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private StickerOverlayController overlay;
    [SerializeField] private Image stickerImage;

    private void Awake()
    {
        if (stickerImage == null)
            stickerImage = GetComponent<Image>();
    }

    public void SelectSticker()
    {
        if (overlay != null && stickerImage != null)
        {
            overlay.Open(stickerImage);
        }
    }
}
