
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SlotReel : MonoBehaviour
{
    [Header("Slot Symbols")]
    [SerializeField] private Image[] symbolImages = new Image[7];  //7 symbol images will be displayed on the reel (hidden by the RectMask2D)
    [SerializeField] private Sprite[] symbols = new Sprite[5]; //reference to the original symbol sprites from the asset library.

    [Header("Reel Settings")]
    [SerializeField, Min(96f)] private float symbolHeight = 100f;
    [SerializeField] private float spinSpeed = 1200f;
    [SerializeField] private float stopDuration = 0.2f;

    private bool isSpinning;
    private float recycleDistance;
    private float recycleThreshold;

    public bool IsSpinning => isSpinning;
    public float StopDuration => stopDuration;

    private void Awake()
    {
        if (symbolImages.Length < 5 || symbols.Length == 0)
        {
            Debug.LogError($"{name}: Assign at least 5 symbol Images and at least 1 symbol Sprite.",this);
            enabled = false;
            return;
        }

        recycleDistance = symbolHeight * symbolImages.Length;
        recycleThreshold = symbolHeight * (symbolImages.Length / 2f);

        // Arrange nine symbols in a centered vertical column.
        for (int i = 0; i < symbolImages.Length; i++)
        {
            if (symbolImages[i] == null)
            {
                Debug.LogError($"{name}: Symbol Image {i} is missing.", this);
                enabled = false;
                return;
            }

            RectTransform rt = symbolImages[i].rectTransform;
            rt.DOKill();

            rt.anchoredPosition = new Vector2(
                rt.anchoredPosition.x,
                (4 - i) * symbolHeight
            );

            SetRandomSymbol(symbolImages[i]);
        }
    }

    public void StartSpin()
    {
        if (!enabled)
            return;

        // Cancel any unfinished stopping tweens.
        foreach (Image image in symbolImages)
        {
            image.rectTransform.DOKill();
        }

        isSpinning = true;
    }

    private void Update()
    {
        if (!isSpinning)
            return;

        float movement = spinSpeed * Time.deltaTime;

        foreach (Image image in symbolImages)
        {
            RectTransform rt = image.rectTransform;
            Vector2 position = rt.anchoredPosition;

            // Symbols travel downward.
            position.y -= movement;

            // Recycle symbols that leave the bottom.
            while (position.y < -recycleThreshold)
            {
                position.y += recycleDistance;
                SetRandomSymbol(image);
            }

            rt.anchoredPosition = position;
        }
    }

    public void StopOnSymbol(int symbolIndex)
    {
        if (!enabled || symbols.Length == 0)
            return;

        symbolIndex = Mathf.Clamp(symbolIndex, 0, symbols.Length - 1);
        isSpinning = false;

        foreach (Image image in symbolImages)
        {
            RectTransform rt = image.rectTransform;

            // Find the nearest exact symbol row.
            float targetY = Mathf.Round(
                rt.anchoredPosition.y / symbolHeight
            ) * symbolHeight;

            // Smoothly settle onto the symbol grid.
            rt.DOAnchorPosY(targetY, stopDuration)
                .SetEase(Ease.OutCubic)
                .OnComplete(() => UpdateSettledSymbol(image, symbolIndex));
        }
    }

    private void UpdateSettledSymbol(Image image, int winningSymbolIndex)
    {
        if (image == null)
            return;

        RectTransform rt = image.rectTransform;

        // The center row is the payline.
        int row = Mathf.RoundToInt(
            rt.anchoredPosition.y / symbolHeight
        );

        if (row == 0)
        {
            image.sprite = symbols[winningSymbolIndex];
        }
        else
        {
            SetRandomSymbol(image);
        }
    }

    private void SetRandomSymbol(Image image)
    {
        if (image == null || symbols.Length == 0)
            return;

        image.sprite = symbols[Random.Range(0, symbols.Length)];
    }

    private void OnDisable()
    {
        isSpinning = false;

        if (symbolImages == null)
            return;

        foreach (Image image in symbolImages)
        {
            if (image != null)
                image.rectTransform.DOKill();
        }
    }
}
