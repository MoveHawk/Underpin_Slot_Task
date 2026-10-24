
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SlotMachineManager : MonoBehaviour
{
    [Header("Reels — Left to Right")]
    [SerializeField] private SlotReel[] reels = new SlotReel[3];

    [Header("Spin Timing")]
    [SerializeField, Min(0.1f)] private float spinDuration = 1.5f;
    [SerializeField, Min(0f)] private float stopGap = 0.25f;

    [Header("Credits")]
    [SerializeField] private int startingBalance = 1000;
    [SerializeField, Min(1)] private int betAmount = 10;
    [SerializeField, Min(1)] private int winMultiplier = 5;

    [Header("UI — Optional")]
    [SerializeField] private Button spinButton;
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private TMP_Text resultText;

    private int balance;
    private bool isSpinning;
    private Sequence spinSequence;

    private void Awake()
    {
        balance = startingBalance;
        UpdateUI();

        if (reels == null || reels.Length != 3)
        {
            Debug.LogError("Assign exactly 3 reels.", this);
            enabled = false;
            return;
        }

        foreach (SlotReel reel in reels)
        {
            if (reel == null)
            {
                Debug.LogError("One or more reels are missing.", this);
                enabled = false;
                return;
            }
        }

        if (spinButton != null)
            spinButton.onClick.AddListener(Spin);
    }

    public void Spin()
    {
        if (isSpinning || balance < betAmount)
        {
            if (balance < betAmount && resultText != null)
                resultText.text = "Not enough credits!";

            return;
        }

        isSpinning = true;
        balance -= betAmount;

        if (spinButton != null)
            spinButton.interactable = false;

        if (resultText != null)
            resultText.text = "Spinning...";

        UpdateUI();

        // Determine all results before starting the animation.
        // All reels must use the same symbol ordering.
        int[] results = new int[3];

        for (int i = 0; i < reels.Length; i++)
        {
            results[i] = Random.Range(0, 5);
            reels[i].StartSpin();
        }

        spinSequence?.Kill();

        spinSequence = DOTween.Sequence();

        // Let all reels spin before the first reel stops.
        spinSequence.AppendInterval(spinDuration);

        // Stop left to right, allowing each reel to settle.
        for (int i = 0; i < reels.Length; i++)
        {
            int reelIndex = i;

            spinSequence.AppendCallback(() =>
            {
                reels[reelIndex].StopOnSymbol(results[reelIndex]);
            });

            spinSequence.AppendInterval(
                reels[i].StopDuration + stopGap
            );
        }

        spinSequence.AppendCallback(() =>
        {
            FinishSpin(results);
        });
    }

    private void FinishSpin(int[] results)
    {
        bool won = results[0] == results[1]
                && results[1] == results[2];

        if (won)
        {
            int payout = betAmount * winMultiplier;
            balance += payout;

            if (resultText != null)
                resultText.text = $"JACKPOT! +{payout} credits!";
        }
        else
        {
            if (resultText != null)
                resultText.text = "No win. Try again!";
        }

        isSpinning = false;

        if (spinButton != null)
            spinButton.interactable = true;

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (balanceText != null)
            balanceText.text = $"Credits: {balance}";
    }

    private void OnDisable()
    {
        spinSequence?.Kill();
        spinSequence = null;

        if (spinButton != null)
            spinButton.onClick.RemoveListener(Spin);
    }
}
