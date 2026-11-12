
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class StickerOverlayController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image overlaySticker;
    [SerializeField] private CanvasGroup background;
    [SerializeField] private CanvasGroup controls;
    [SerializeField] private RectTransform redButton;
    [SerializeField] private RectTransform greenButton;

    [Header("Card Animation")]
    [SerializeField] private float duration = 0.45f;
    [SerializeField] private Ease moveEase = Ease.OutCubic;
    [SerializeField] private Ease scaleEase = Ease.OutBack;
    [SerializeField] private float startRotation = 12f;

    [Header("Button Animation")]
    [SerializeField] private float buttonDelay = 0.25f;
    [SerializeField] private float buttonDuration = 0.25f;
    [SerializeField] private float buttonOvershoot = 1.08f;
    [SerializeField] private float buttonStartScale = 0.8f;

    private RectTransform stickerRect;

    private Vector3 finalPosition;
    private Vector3 finalScale;
    private Quaternion finalRotation;

    private Vector3 redFinalScale;
    private Vector3 greenFinalScale;

    private Sequence sequence;

    private void Awake()
    {
        stickerRect = overlaySticker.rectTransform;

        finalPosition = stickerRect.position;
        finalScale = stickerRect.localScale;
        finalRotation = stickerRect.localRotation;

        redFinalScale = redButton.localScale;
        greenFinalScale = greenButton.localScale;
    }

    public void Open(Image selectedSticker)
    {
        if (selectedSticker == null)
            return;

        sequence?.Kill();

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        // Copy selected sticker appearance.
        overlaySticker.sprite = selectedSticker.sprite;
        overlaySticker.color = selectedSticker.color;
        overlaySticker.preserveAspect = true;
        overlaySticker.enabled = true;

        // Reset card before calculating dimensions.
        stickerRect.position = finalPosition;
        stickerRect.localScale = finalScale;
        stickerRect.localRotation = finalRotation;

        Vector3[] sourceCorners = new Vector3[4];
        Vector3[] targetCorners = new Vector3[4];

        selectedSticker.rectTransform.GetWorldCorners(sourceCorners);
        stickerRect.GetWorldCorners(targetCorners);

        float sourceWidth = Vector3.Distance(
            sourceCorners[0], sourceCorners[3]);

        float targetWidth = Vector3.Distance(
            targetCorners[0], targetCorners[3]);

        float scaleRatio = targetWidth > 0f
            ? sourceWidth / targetWidth
            : 1f;

        // Starting card state.
        stickerRect.position =
            selectedSticker.rectTransform.position;

        stickerRect.localScale = finalScale * scaleRatio;

        stickerRect.localRotation =
            Quaternion.Euler(0f, 0f, startRotation);

        // Background fades, controls do not.
        background.alpha = 0f;
        controls.alpha = 1f;

        // Hide controls until their scale animation begins.
        controls.gameObject.SetActive(false);

        redButton.localScale =
            redFinalScale * buttonStartScale;

        greenButton.localScale =
            greenFinalScale * buttonStartScale;

        controls.interactable = false;
        controls.blocksRaycasts = false;

        // Build animation.
        sequence = DOTween.Sequence();

        sequence.Join(
            stickerRect.DOMove(finalPosition, duration)
                .SetEase(moveEase)
        );

        sequence.Join(
            stickerRect.DOScale(finalScale, duration)
                .SetEase(scaleEase)
        );

        sequence.Join(
            stickerRect.DOLocalRotateQuaternion(
                finalRotation, duration
            ).SetEase(Ease.OutCubic)
        );

        // Keep background fade.
        sequence.Join(
            background.DOFade(1f, 0.25f)
        );

        // Show controls instantly at the button delay.
        sequence.InsertCallback(buttonDelay, () =>
        {
            controls.gameObject.SetActive(true);
        });

        // Red button overshoot.
        sequence.Insert(
            buttonDelay,
            redButton.DOScale(
                redFinalScale * buttonOvershoot,
                buttonDuration * 0.6f
            ).SetEase(Ease.OutCubic)
        );

        sequence.Insert(
            buttonDelay + buttonDuration * 0.6f,
            redButton.DOScale(
                redFinalScale,
                buttonDuration * 0.4f
            ).SetEase(Ease.OutCubic)
        );

        // Green button overshoot.
        sequence.Insert(
            buttonDelay,
            greenButton.DOScale(
                greenFinalScale * buttonOvershoot,
                buttonDuration * 0.6f
            ).SetEase(Ease.OutCubic)
        );

        sequence.Insert(
            buttonDelay + buttonDuration * 0.6f,
            greenButton.DOScale(
                greenFinalScale,
                buttonDuration * 0.4f
            ).SetEase(Ease.OutCubic)
        );

        sequence.OnComplete(() =>
        {
            controls.interactable = true;
            controls.blocksRaycasts = true;
        });
    }

    private void OnDisable()
    {
        sequence?.Kill();
        sequence = null;

        if (stickerRect != null)
        {
            stickerRect.position = finalPosition;
            stickerRect.localScale = finalScale;
            stickerRect.localRotation = finalRotation;
        }

        if (redButton != null)
            redButton.localScale = redFinalScale;

        if (greenButton != null)
            greenButton.localScale = greenFinalScale;

        // Restore controls for the next opening.
        if (controls != null)
        {
            controls.gameObject.SetActive(true);
            controls.alpha = 1f;
        }
    }

    private void OnDestroy()
    {
        sequence?.Kill();
    }
}
