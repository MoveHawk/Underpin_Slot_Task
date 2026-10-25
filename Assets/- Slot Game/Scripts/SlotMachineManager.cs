
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SlotMachineManager : MonoBehaviour
{
    [SerializeField] private SlotReel[] reels = new SlotReel[3];

    [Header("Spin Timing")]
    [SerializeField, Min(0.1f)] private float spinDuration = 1.5f;
    [SerializeField, Min(0f)] private float stopGap = 0.25f;

    [Header("Score/Credits")]
    [SerializeField] private int startingBalance = 1000;
    [SerializeField, Min(1)] private int betAmount = 10;
    [SerializeField, Min(1)] private int winMultiplier = 5;

    [Header("UI")]
    [SerializeField] private Button spinButton;
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private TMP_Text resultText;

    private int balance;
    private bool isSpinning;
    private Coroutine spinRoutine;

    private void Awake()
    {
        balance = startingBalance;
        ReelCheck();
    }

    //Here we check if the reels are assigned and if they are not null. Also added a direct listener to the spin button to call the Spin method when clicked.
    private void ReelCheck()
    {
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

        UpdateUI();
    }

    public void Spin()
    {
        if (isSpinning || balance < betAmount)
        {
            if (balance < betAmount && resultText != null)
                resultText.text = "Not enough credits!";

            return;
        }

        // Decide the entire result before the animation begins.
        int[] results = new int[3];

        for (int i = 0; i < results.Length; i++)
            results[i] = Random.Range(0, 5);

        balance -= betAmount;
        isSpinning = true;

        if (spinButton != null)
            spinButton.interactable = false;

        if (resultText != null)
            resultText.text = "Spinning...";

        UpdateUI();

        foreach (SlotReel reel in reels)
            reel.StartSpin();

        spinRoutine = StartCoroutine(SpinRoutine(results));
    }

    private IEnumerator SpinRoutine(int[] results)
    {
        yield return new WaitForSeconds(spinDuration);

        // Stop reels one by one, waiting for each to settle.
        for (int i = 0; i < reels.Length; i++)
        {
            Tween stoppingTween = reels[i].StopOnSymbol(results[i]);

            if (stoppingTween != null)
                yield return stoppingTween.WaitForCompletion();

            if (i < reels.Length - 1 && stopGap > 0f)
                yield return new WaitForSeconds(stopGap);
        }

        FinishSpin(results);
        spinRoutine = null;
    }

    //If all the results of the reels are the same, player wins credits otherwise loses.
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
            balanceText.text = $"$ {balance}";
    }

    private void OnDisable()
    {
        if (spinRoutine != null)
        {
            StopCoroutine(spinRoutine);
            spinRoutine = null;
        }

        isSpinning = false;

        if (spinButton != null)
            spinButton.onClick.RemoveListener(Spin);
    }
}
