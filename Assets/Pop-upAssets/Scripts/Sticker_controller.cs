
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

    [Header("Green Button - Spin Animation")]
    [SerializeField] private int totalSpins = 12;
    [SerializeField] private float initialSpinDuration = 0.65f;
    [SerializeField] private float minimumSpinDuration = 0.12f;
    [SerializeField] private float spinAcceleration = 0.85f;
    [SerializeField] private float minimumScale = 0.5f;
    [SerializeField] private float enlargedScale = 1.25f;

    [Header("Return Animation")]
    [SerializeField] private float returnDuration = 2f;
    [SerializeField] private float returnSpins = 3f;
    [SerializeField] private Ease returnEase = Ease.InOutCubic;

    private enum StickerState
    {
        Closed,
        Opening,
        Preview,
        ConfirmSpinning,
        WaitingForReturn,
        SpinningReturn
    }

    private StickerState state = StickerState.Closed;

    private RectTransform stickerRect;
    private Image selectedSticker;

    private Vector3 finalPosition;
    private Vector3 finalScale;
    private Quaternion finalRotation;

    private Vector3 redFinalScale;
    private Vector3 greenFinalScale;

    private Vector3 originalPosition;
    private Vector3 originalScale;

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

    public void Open(Image newSelectedSticker)
    {
        if (newSelectedSticker == null)
            return;

        sequence?.Kill();

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        selectedSticker = newSelectedSticker;
        state = StickerState.Opening;

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

        originalPosition =
            selectedSticker.rectTransform.position;

        originalScale = finalScale * scaleRatio;

        // Starting sticker state.
        stickerRect.position = originalPosition;
        stickerRect.localScale = originalScale;

        stickerRect.localRotation =
            Quaternion.Euler(0f, 0f, startRotation);

        // Background fades, buttons do not.
        background.alpha = 0f;
        controls.alpha = 1f;
        controls.gameObject.SetActive(false);

        redButton.localScale =
            redFinalScale * buttonStartScale;

        greenButton.localScale =
            greenFinalScale * buttonStartScale;

        controls.interactable = false;
        controls.blocksRaycasts = false;

        sequence = DOTween.Sequence();

        // Move to center.
        sequence.Join(
            stickerRect.DOMove(finalPosition, duration)
                .SetEase(moveEase)
        );

        // Scale to preview size.
        sequence.Join(
            stickerRect.DOScale(finalScale, duration)
                .SetEase(scaleEase)
        );

        // Straighten the sticker.
        sequence.Join(
            stickerRect.DOLocalRotateQuaternion(
                finalRotation, duration
            ).SetEase(Ease.OutCubic)
        );

        // Background fade.
        sequence.Join(
            background.DOFade(1f, 0.25f)
        );

        // Show buttons without fading.
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
            state = StickerState.Preview;

            controls.interactable = true;
            controls.blocksRaycasts = true;
        });
    }

    // GREEN BUTTON
    // Performs 12 progressively faster spins.
    public void GreenButtonClicked()
    {
        if (state != StickerState.Preview)
            return;

        sequence?.Kill();

        state = StickerState.ConfirmSpinning;

        controls.interactable = false;
        controls.blocksRaycasts = false;
        controls.gameObject.SetActive(false);

        stickerRect.position = finalPosition;
        stickerRect.localScale = finalScale;
        stickerRect.localRotation = finalRotation;

        sequence = DOTween.Sequence();

        float currentTime = 0f;
        float currentSpinDuration =
            Mathf.Max(0.01f, initialSpinDuration);

        int spins = Mathf.Max(12, totalSpins);

        for (int i = 1; i <= spins; i++)
        {
            float targetScale = 1f;

            // Spins 9-11: gradually shrink.
            if (i >= 9 && i <= 11)
            {
                float progress = (i - 8f) / 3f;

                targetScale = Mathf.Lerp(
                    1f,
                    minimumScale,
                    progress
                );
            }
            // Spin 12 onwards: grow to 125%.
            else if (i >= 12)
            {
                targetScale = enlargedScale;
            }

            float halfDuration = currentSpinDuration * 0.5f;

            // First half: sticker becomes thin.
            sequence.Insert(
                currentTime,
                stickerRect.DOScaleX(
                    finalScale.x * targetScale * 0.01f,
                    halfDuration
                ).SetEase(Ease.InCubic)
            );

            // Second half: sticker expands again.
            sequence.Insert(
                currentTime + halfDuration,
                stickerRect.DOScaleX(
                    finalScale.x * targetScale,
                    halfDuration
                ).SetEase(Ease.OutCubic)
            );

            // Smooth vertical scaling.
            sequence.Insert(
                currentTime,
                stickerRect.DOScaleY(
                    finalScale.y * targetScale,
                    currentSpinDuration
                ).SetEase(Ease.InOutSine)
            );

            currentTime += currentSpinDuration;

            // Accelerate the next rotation.
            currentSpinDuration = Mathf.Max(
                minimumSpinDuration,
                currentSpinDuration * spinAcceleration
            );
        }

        sequence.OnComplete(() =>
        {
            stickerRect.localScale =
                finalScale * enlargedScale;

            state = StickerState.WaitingForReturn;
        });
    }

    // BACKGROUND CLICK
    public void BackgroundClicked()
    {
        switch (state)
        {
            case StickerState.Preview:
                // Close normally before confirmation.
                gameObject.SetActive(false);
                break;

            case StickerState.WaitingForReturn:
                // After confirmation, start slow return.
                StartSpinningReturn();
                break;

            // Ignore clicks during animations.
            default:
                break;
        }
    }

    // SLOW SPINNING RETURN
    private void StartSpinningReturn()
    {
        if (selectedSticker == null)
            return;

        sequence?.Kill();

        state = StickerState.SpinningReturn;

        Vector3 startPosition = stickerRect.position;
        Vector3 startScale = stickerRect.localScale;

        Vector3 targetPosition =
            selectedSticker.rectTransform.position;

        Vector3 targetScale = originalScale;

        int spins = Mathf.Max(
            1,
            Mathf.RoundToInt(returnSpins)
        );

        float elapsed = 0f;

        sequence = DOTween.Sequence();

        // Animate movement, shrinking, and spinning together.
        sequence.Append(
            DOTween.To(
                () => elapsed,
                value =>
                {
                    elapsed = value;

                    float movementProgress =
                        DOVirtual.EasedValue(
                            0f,
                            1f,
                            value,
                            returnEase
                        );

                    // Move toward original position.
                    stickerRect.position =
                        Vector3.LerpUnclamped(
                            startPosition,
                            targetPosition,
                            movementProgress
                        );

                    // Gradually shrink.
                    Vector3 currentScale =
                        Vector3.LerpUnclamped(
                            startScale,
                            targetScale,
                            movementProgress
                        );

                    // Simulated Y-axis card flips.
                    float spinProgress = value * spins;

                    float flipWidth = Mathf.Abs(
                        Mathf.Cos(
                            spinProgress * Mathf.PI * 2f
                        )
                    );

                    flipWidth = Mathf.Max(
                        0.01f,
                        flipWidth
                    );

                    currentScale.x *= flipWidth;

                    stickerRect.localScale = currentScale;
                },
                1f,
                returnDuration
            ).SetEase(Ease.Linear)
        );

        sequence.OnComplete(() =>
        {
            stickerRect.position = targetPosition;
            stickerRect.localScale = targetScale;
            stickerRect.localRotation = finalRotation;

            state = StickerState.Closed;

            // Disable overlay only after return completes.
            gameObject.SetActive(false);
        });
    }

    private void OnDisable()
    {
        sequence?.Kill();
        sequence = null;

        state = StickerState.Closed;

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

        // Restore controls for next opening.
        if (controls != null)
        {
            controls.gameObject.SetActive(true);
            controls.alpha = 1f;
            controls.interactable = false;
            controls.blocksRaycasts = false;
        }
    }

    private void OnDestroy()
    {
        sequence?.Kill();
    }
}
