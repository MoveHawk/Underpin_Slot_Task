
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SlotReel : MonoBehaviour
{
    [Header("Symbols")]
    //the no. of images to be shown on the reel strip are 3, the extra images are hidden by the RectMask2D, they are used for seamless animation.
    [SerializeField] private Image[] symbolImages = new Image[9];
    [SerializeField] private Sprite[] symbols = new Sprite[6]; //reference to the original symbol sprites from the asset library.

    [Header("Reel Settings")]
    [SerializeField, Min(96f)] private float symbolHeight = 100f; //Very important to put the correct height of the symbol, otherwise the symbols with clutter together.
    [SerializeField] private float spinSpeed = 1200f;
    [SerializeField] private float minStopDistance = 150f;
    [SerializeField, Min(0.25f)] private float brakingDistanceInRows = 2f; //the distance in symbol rows over which the reel will decelerate to a stop.
    [SerializeField] private float overshootInRows = 0.06f; //the distance of the overshooting animation for the target symbol.
    [SerializeField, Min(0.01f)] private float overshootStopDuration = 0.09f; 

    private bool isSpinning;
    private float recycleDistance;
    private Tween stopTween;

    public bool IsSpinning => isSpinning;

    private void Awake()
    {
        InitializeReel();
    }

    private void Update()
    {
        if (isSpinning)
            MoveDown(spinSpeed * Time.deltaTime);
    }

    private void InitializeReel()
    {
        //It is really important to have the correct number of images and symbols assigned, otherwise the reel will not work properly.
        if (symbolImages == null || symbolImages.Length != 9 ||
            symbols == null || symbols.Length != 6)
        {
            Debug.LogError( $"{name}: Assign exactly 9 Images and 6 symbol Sprites.",this);
            enabled = false;
            return;
        }

        //Here we calculate the distance at which the symbols will be recycled, this is based on the height of the symbol and the number of images in the reel.
        recycleDistance = symbolHeight * symbolImages.Length;

        for (int i = 0; i < symbolImages.Length; i++)
        {
            Image image = symbolImages[i];

            if (image == null)
            {
                Debug.LogError($"{name}: Missing symbol Image {i}.",this);
                enabled = false;
                return;
            }

            RectTransform rt = image.rectTransform;
            rt.DOKill();

            rt.anchoredPosition = new Vector2(
                rt.anchoredPosition.x,
                (4 - i) * symbolHeight
            );

            image.sprite = symbols[i % symbols.Length];
        }
    }

    public void StartSpin()
    {
        if (!enabled)
            return;

        stopTween?.Kill();
        stopTween = null;
        isSpinning = true;
    }


    private void MoveDown(float distance)
    {
        float threshold = recycleDistance * 0.5f;

        foreach (Image image in symbolImages)
        {
            RectTransform rt = image.rectTransform;
            Vector2 pos = rt.anchoredPosition;

            pos.y -= distance;

            // Wrap in either direction without changing the sprite.
            while (pos.y < -threshold)
                pos.y += recycleDistance;

            while (pos.y >= threshold)
                pos.y -= recycleDistance;

            rt.anchoredPosition = pos;
        }
    }

    public Tween StopOnSymbol(int symbolIndex)
    {
        if (!enabled || !isSpinning)
            return null;

        symbolIndex = Mathf.Clamp(symbolIndex, 0, symbols.Length - 1);
        isSpinning = false;

        stopTween?.Kill();

        // Here we find the closest target image that allows enough travel for a convincing/clicky stop.
        //Why?: If this is not implemented, the reel may stop too soon or too late, resulting in a jarring/teleport animation.
        float targetDistance = float.MaxValue;
        float fallbackDistance = float.MaxValue;

        foreach (Image image in symbolImages)
        {
            if (image.sprite != symbols[symbolIndex])
                continue;

            float y = image.rectTransform.anchoredPosition.y;
            float distance = Mathf.Repeat(y, recycleDistance);

            if (distance < fallbackDistance)
                fallbackDistance = distance;

            if (distance >= minStopDistance &&
                distance < targetDistance)
            {
                targetDistance = distance;
            }
        }

        if (targetDistance == float.MaxValue)
            targetDistance = fallbackDistance;

        if (targetDistance == float.MaxValue)
        {
            Debug.LogError($"{name}: Target symbol not found.", this);
            isSpinning = true;
            return null;
        }

        float overshoot = symbolHeight * overshootInRows;

        // The target passes just above the payline, then settles back. (Overshooting animation)
        float totalDistance = targetDistance + overshoot;

        // Keep the final braking phase short and predictable.
        float brakingDistance = Mathf.Min(
            totalDistance,
            symbolHeight * brakingDistanceInRows
        );

        float cruiseDistance = totalDistance - brakingDistance;

        Sequence sequence = DOTween.Sequence();

        // Stage 1: constant-speed scrolling.
        if (cruiseDistance > 0.001f)
        {
            float moved = 0f;

            sequence.Append(
                DOVirtual.Float(
                    0f,
                    cruiseDistance,
                    cruiseDistance / spinSpeed,
                    value =>
                    {
                        MoveDown(value - moved);
                        moved = value;
                    }
                ).SetEase(Ease.Linear)
            );
        }

        // Stage 2: smooth deceleration, beginning at spinSpeed.
        if (brakingDistance > 0.001f)
        {
            float moved = 0f;

            float brakingTime = 3f * brakingDistance / spinSpeed;

            sequence.Append(
                DOVirtual.Float(
                    0f,
                    brakingDistance,
                    brakingTime,
                    value =>
                    {
                        MoveDown(value - moved);
                        moved = value;
                    }
                ).SetEase(Ease.OutCubic)
            );
        }

        // Stage 3: tiny reverse movement to settle on the exact row.
        if (overshoot > 0.001f)
        {
            float returned = 0f;

            sequence.Append(
                DOVirtual.Float(
                    0f,
                    overshoot,
                    overshootStopDuration,
                    value =>
                    {
                        MoveDown(-(value - returned));
                        returned = value;
                    }
                ).SetEase(Ease.OutSine)
            );
        }

        sequence.OnComplete(() =>
        {
            // To avoid any floating-point inaccuracies caused by Unity's distance system, we snap the final position of each symbol to the exact rounded off position.
            foreach (Image image in symbolImages)
            {
                RectTransform rt = image.rectTransform;
                Vector2 pos = rt.anchoredPosition;
                //Formula: finalPosition = round(currentPosition / symbolHeight) * symbolHeight
                pos.y = Mathf.Round(pos.y / symbolHeight) * symbolHeight;
                rt.anchoredPosition = pos;
            }

            stopTween = null;
        });

        stopTween = sequence;
        return sequence;
    }

    private void OnDisable()
    {
        isSpinning = false;
        stopTween?.Kill();
        stopTween = null;
    }
}
