
using System.Collections;
using System.Security.Cryptography;
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

    [Header("Bet Controls & Credits")]
    [SerializeField] private int startingBalance = 1000;
    [SerializeField] private int betAmount = 10;
    [SerializeField] private int maxBet = 100;

    [Header("Outcome Weights")]
    /* These weights decide the complete result of a spin.
Order: Cherry triple, Seven triple, Bell triple, Bar triple, Diamond triple, Clown triple, Bar + matching pair, Exactly two Bars, Non-winning combination.*/
    [SerializeField]
    private int[] outcomeWeights =
    { 170, 65, 35, 15, 5, 1, 50, 50, 609 }; //total Random Number Generator (RNG) weight = 1000

    [Header("Gross Payout Multipliers")]
    // Payout includes the original bet, Clown triple and non-winning combinations pay 0.
    [SerializeField]
    private int[] payoutMultipliers =
        { 2, 3, 4, 5, 10, 0, 2, 1, 0 };

    [Header("UI")]
    [SerializeField] private Button spinButton;
    [SerializeField] private Button increaseBetButton;
    [SerializeField] private Button decreaseBetButton;
    [SerializeField] private TMP_Text betText;
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text rtpText;

    private long balance;
    private bool isSpinning;
    private Coroutine spinRoutine;

    private int totalWeight;
    private double theoreticalRtp;

    // These counters include completed spins only.
    private long completedSpins;
    private long totalWagered;
    private long totalPaid;

    private const int SymbolCount = 6;
    private const int BarIndex = 3;
    private const int ClownIndex = 5;

    private void Awake()
    {
        SlotCheck();
    }

    private bool SlotCheck()
    {
        balance = startingBalance;

        if (reels == null || reels.Length != 3)
        {
            Debug.LogError("Assign exactly 3 reels.", this);
            enabled = false;
            return false;
        }

        foreach (SlotReel reel in reels)
        {
            if (reel == null)
            {
                Debug.LogError("One or more reels are missing.", this);
                enabled = false;
                return false;
            }
        }

        if (outcomeWeights == null || outcomeWeights.Length != 9 ||
            payoutMultipliers == null || payoutMultipliers.Length != 9)
        {
            Debug.LogError("Assign exactly 9 outcome weights and 9 payout multipliers.",this);
            enabled = false;
            return false;
        }

        int weightSum = 0;

        for (int i = 0; i < outcomeWeights.Length; i++)
        {
            if (outcomeWeights[i] < 0 || payoutMultipliers[i] < 0)
            {
                Debug.LogError("Weights and payouts cannot be negative.", this);
                enabled = false;
                return false;
            }

            weightSum += outcomeWeights[i];
        }

        if (weightSum != 1000)
        {
            Debug.LogError($"Outcome weights must add up to 1000. Current total: {weightSum}.",this);
            enabled = false;
            return false;
        }

        if (startingBalance < 0 || betAmount <= 0)
        {
            Debug.LogError("Starting balance cannot be negative and the bet must be positive.", this);
            enabled = false;
            return false;
        }

        totalWeight = CalculateTotalWeight();
        theoreticalRtp = CalculateTheoreticalRtp();

        if (spinButton != null)
            spinButton.onClick.AddListener(Spin);

        if (increaseBetButton != null)
            increaseBetButton.onClick.AddListener(IncreaseBet);

        if (decreaseBetButton != null)
            decreaseBetButton.onClick.AddListener(DecreaseBet);

        UpdateBetUI();

        UpdateUI();

        Debug.Log($"Slot ready. Theoretical RTP: {theoreticalRtp:F2}%");

        return true;
    }

    private int CalculateTotalWeight()
    {
        int total = 0;

        foreach (int weight in outcomeWeights)
            total += weight;

        return total;
    }

    private double CalculateTheoreticalRtp()
    {
        double expectedPayout = 0.0;

        for (int i = 0; i < outcomeWeights.Length; i++)
        {
            // Example: 169 / 1000 = 16.9% chance.
            double probability = (double)outcomeWeights[i] / totalWeight;

            // Expected payout = chance of outcome × payout multiplier.
            expectedPayout += probability * payoutMultipliers[i];
        }

        // RTP = average payout divided by the bet, shown as a percentage.
        return expectedPayout * 100.0;
    }

    private int GetWeightedOutcomeIndex()
    {
        // Random integer from 0 to totalWeight - 1.
        int roll = RandomNumberGenerator.GetInt32(totalWeight);

        int cumulativeWeight = 0;

        for (int i = 0; i < outcomeWeights.Length; i++)
        {
            cumulativeWeight += outcomeWeights[i];

            if (roll < cumulativeWeight)
                return i;
        }

        return outcomeWeights.Length - 1;
    }

    private int GetRandomSymbol()
    {
        return RandomNumberGenerator.GetInt32(SymbolCount);
    }

    private void GenerateOutcome(int outcomeIndex, int[] results)
    {
        // Outcomes 0-5 are three matching symbols.
        if (outcomeIndex <= ClownIndex)
        {
            results[0] = outcomeIndex;
            results[1] = outcomeIndex;
            results[2] = outcomeIndex;
            return;
        }

        // Outcome 6: one Bar and two matching non-Bar symbols.
        if (outcomeIndex == 6)
        {
            int pairSymbol;

            do
            {
                pairSymbol = GetRandomSymbol();
            }
            while (pairSymbol == BarIndex);

            results[0] = pairSymbol;
            results[1] = pairSymbol;
            results[2] = pairSymbol;

            // Place the Bar randomly in one of the three positions.
            int barPosition = RandomNumberGenerator.GetInt32(3);
            results[barPosition] = BarIndex;
            return;
        }

        // Outcome 7: exactly two Bars and one non-Bar symbol.
        if (outcomeIndex == 7)
        {
            int otherSymbol;

            do
            {
                otherSymbol = GetRandomSymbol();
            }
            while (otherSymbol == BarIndex);

            results[0] = BarIndex;
            results[1] = BarIndex;
            results[2] = otherSymbol;

            // Shuffle the three positions so the non-Bar can appear anywhere.
            for (int i = results.Length - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);

                int temp = results[i];
                results[i] = results[j];
                results[j] = temp;
            }

            return;
        }

        // Outcome 8: any combination that is not a triple
        // and does not match either special Bar combination.
        do
        {
            results[0] = GetRandomSymbol();
            results[1] = GetRandomSymbol();
            results[2] = GetRandomSymbol();
        }
        while (IsWinningCombination(results));
    }

    private bool IsWinningCombination(int[] results)
    {
        // Three matching symbols are handled by outcomes 0-5.
        if (results[0] == results[1] &&
            results[1] == results[2])
        {
            return true;
        }

        int barCount = 0;

        foreach (int symbol in results)
        {
            if (symbol == BarIndex)
                barCount++;
        }

        // Exactly one Bar and two matching non-Bar symbols.
        if (barCount == 1)
        {
            int first = -1;
            int second = -1;

            foreach (int symbol in results)
            {
                if (symbol == BarIndex)
                    continue;

                if (first == -1)
                    first = symbol;
                else
                    second = symbol;
            }

            if (first == second)
                return true;
        }

        // Exactly two Bars.
        return barCount == 2;
    }

    public void Spin()
    {
        if (isSpinning || balance < betAmount)
        {
            if (!isSpinning && balance < betAmount && resultText != null)
                resultText.text = "Not enough credits!";

            return;
        }

        // Decide the outcome category and all three symbols first.
        // The reels only animate these results; they never reroll them.
        int outcomeIndex = GetWeightedOutcomeIndex();
        int[] results = new int[reels.Length];

        GenerateOutcome(outcomeIndex, results);

        balance -= betAmount;
        isSpinning = true;

        if (spinButton != null)
            spinButton.interactable = false;

        if (resultText != null)
            resultText.text = "Spinning...";

        UpdateUI();

        foreach (SlotReel reel in reels)
            reel.StartSpin();

        spinRoutine = StartCoroutine(SpinRoutine(results, outcomeIndex));
    }

    private IEnumerator SpinRoutine(int[] results, int outcomeIndex)
    {
        yield return new WaitForSeconds(spinDuration);

        // Stop reels one at a time and wait for each reel to settle.
        for (int i = 0; i < reels.Length; i++)
        {
            Tween stoppingTween = reels[i].StopOnSymbol(results[i]);

            if (stoppingTween != null)
                yield return stoppingTween.WaitForCompletion();

            if (i < reels.Length - 1 && stopGap > 0f)
                yield return new WaitForSeconds(stopGap);
        }

        FinishSpin(results, outcomeIndex);
        spinRoutine = null;
    }

    private void FinishSpin(int[] results, int outcomeIndex)
    {
        completedSpins++;
        totalWagered += betAmount;

        // Each outcome category has one fixed payout multiplier.
        long payout = (long)betAmount * payoutMultipliers[outcomeIndex];

        balance += payout;
        totalPaid += payout;

        if (resultText != null)
            resultText.text = GetOutcomeMessage(outcomeIndex, payout);

        isSpinning = false;

        if (spinButton != null)
            spinButton.interactable = true;

        UpdateUI();
        //Return To Player (RTP) calculation is logged to the console for observing/debugging purposes.
        Debug.Log($"Spin {completedSpins} | " + $"Outcome: {GetOutcomeName(outcomeIndex)} | " + $"Wagered: {totalWagered} | " + $"Paid: {totalPaid} | " + $"Observed RTP: {GetObservedRtp():F3}%");
    }

    private string GetOutcomeName(int outcomeIndex)
    {
        switch (outcomeIndex)
        {
            case 0: return "Cherry Triple";
            case 1: return "Seven Triple";
            case 2: return "Bell Triple";
            case 3: return "Bar Triple";
            case 4: return "Diamond Triple";
            case 5: return "Clown Triple";
            case 6: return "Bar + Matching Pair";
            case 7: return "Two Bars";
            case 8: return "Non-winning Combination";
            default: return "Unknown";
        }
    }

    private string GetOutcomeMessage(int outcomeIndex, long payout)
    {
        switch (outcomeIndex)
        {
            case 0: return $"Cherry triple! +{payout} credits!";
            case 1: return $"Seven triple! +{payout} credits!";
            case 2: return $"Bell triple! +{payout} credits!";
            case 3: return $"Bar triple! +{payout} credits!";
            case 4: return $"DIAMOND JACKPOT! +{payout} credits!";
            case 5: return "Clown triple! No payout!";
            case 6: return $"Bar + matching pair! +{payout} credits!";
            case 7: return $"Two Bars! +{payout} credits!";
            default: return "No win. Try again!";
        }
    }

    private double GetObservedRtp()
    {
        if (totalWagered == 0)
            return 0.0;

        // Observed RTP = total credits paid / total credits wagered.
        return (double)totalPaid / totalWagered * 100.0;
    }

    public void IncreaseBet()
    {
        if (isSpinning || betAmount >= maxBet)
            return;

        betAmount = Mathf.Min(betAmount + 10, maxBet);
        UpdateBetUI();
    }

    public void DecreaseBet()
    {
        if (isSpinning || betAmount <= 1)
            return;

        betAmount = Mathf.Max(betAmount - 10, 10);
        UpdateBetUI();
    }

    private void UpdateBetUI()
    {
        if (increaseBetButton != null)
            increaseBetButton.interactable =
                !isSpinning && betAmount < maxBet;

        if (decreaseBetButton != null)
            decreaseBetButton.interactable =
                !isSpinning && betAmount > 1;

        if (betText != null)
            betText.text = $"$ {betAmount}";
    }

    private void UpdateUI()
    {
        if (balanceText != null)
            balanceText.text = $"$ {balance}";

        //Return To Player (RTP) display is commented out to avoid cluttering the UI, but can be enabled if we want to observe during the Build itself.
        //if (rtpText != null)
        //{
        //    string observed = totalWagered == 0
        //        ? "Not available yet"
        //        : $"{GetObservedRtp():F2}%";

        //    rtpText.text = $"Theoretical RTP: {theoreticalRtp:F2}%\n" + $"Observed RTP: {observed}\n" + $"Completed spins: {completedSpins}";
        //}
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
