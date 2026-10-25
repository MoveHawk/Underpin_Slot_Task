# Underpin_Slot_Task - Mrunal Mendole
Slot game assignment for Unity Developer Internship position at Underpin Services - Mrunal Mendole

## Game Overview
Underpin Slot Machine is a 2D slot machine task developed in Unity 6.3 LTS. It features three animated reels, slot symbol-based outcomes, a credit balance, adjustable bets, and weighted random outcome selection.

### Features
- Three animated reels with sequential stopping using DOTween.
- Six Slot symbols: Cherry, Seven, Bell, Bar, Diamond, and Clown.
- Total 8 Possible Combinations:-
  5 Main Combinations: Triple Cherry (2x Bet Multiplier), Triple Seven (3x Bet Multiplier), Triple Bell (4x Bet Multiplier), Triple Bar (5x Bet Multiplier), Triple Diamond (10x Bet Multiplier)
  3 Special Combinations: Triple Clown (No Credit Reward
- Adjustable bet amount, from 10 to 100 credits.
- Weighted outcomes with different payout multipliers.
- Lever visual feedback when spinning.
- Background music and sound effects for button clicks, payouts, and the Diamond jackpot.
- Theoretical and observed Return to Player (RTP) calculations available in the game logic.

## Instructions to Run WebGL Build
The WebGL build uses compressed assets and must be served over HTTP. Opening `index.html` directly using a `file://` URL will not work reliably.
1. Please install Python 3 if it is not already installed.
2. Clone or download this repository and extract Build/WebGL Contents.
3. Open a terminal in the repository root in File explorer using "cmd" in the address bar.
4. To start a local web server:
   ```
   python -m http.server 8000 --directory Build/WebGL
   ```
   On some systems, use `python3` instead of `python`.
5. Open the following URL in a browser: http://localhost:8000
6. To stop the server, press `Ctrl+C` in the terminal.

## Bonus Features
- Animated lever feedback when a spin begins.
- Separate audio effects for regular payouts and the Diamond jackpot.
- Weighted outcome selection with predefined payout multipliers.
- Bet controls with minimum and maximum limits.
- Sequential reel stopping for a more engaging slot machine experience.

## Thought Process and Approach
My main goal was to keep the game logic simple, readable, and easy to extend. I used DOTween for smooth slot reel animations and TextMeshPro for customizable, clear UI text.
Each reel contains multiple UI Images that move continuously inside a masked area. When a reel reaches its stopping point, DOTween smoothly moves it to the selected symbol. The reels stop one after another to make the spin feel more natural.
Initially, I considered using weighted random selection independently for each reel. I changed this to selecting the complete outcome first, using a weighted outcome table. The game decides all three final symbols before the animation starts, and the reels only animate to display that result. This keeps the game logic separate from the visual animation and makes the payout rules easier to control and test.
The game uses 9 outcome categories. Their RNG weights add up to 1000, so each outcome's probability is its weight divided by 1,000.

 Matching triples pay 2x for Cherry, 3x for Seven, 4x for Bell, 5x for Bar, and 10x for Diamond. A Clown triple pays nothing. One Bar with two matching non-Bar symbols pays 2x, while exactly two Bars with any non-Bar symbol pays 1x. All other combinations pay nothing. Payout multipliers include the original bet.

- **RTP Calculation:** The theoretical Return to Player (RTP) is calculated by multiplying each outcome's probability by its gross payout multiplier and adding the results. With the current weights and payouts, the theoretical RTP is approximately 95%.
  Formula: `RTP = Σ (Outcome Weight / Total Weight × Payout Multiplier) × 100`

The observed RTP is calculated from completed spins:  `Observed RTP = (Total Credits Paid / Total Credits Wagered) × 100`

### Theoretical RTP Breakdown
The following table shows the current outcome weights, probabilities, payouts, and each category's contribution to the theoretical RTP.

| Outcome | Weight | Probability | Payout | RTP Contribution |
|---|---:|---:|---:|---:|
| Cherry Triple | 170 | 17% | 2x | 34% |
| Seven Triple | 65 | 6.5% | 3x | 19.5% |
| Bell Triple | 35 | 3.5% | 4x | 14% |
| Bar Triple | 15 | 1.5% | 5x | 7.5% |
| Diamond Triple | 5 | 0.5% | 10x | 5% |
| Clown Triple | 1 | 0.1% | 0x | 0% |
| Bar + Matching Pair | 50 | 5% | 2x | 10% |
| Exactly Two Bars | 50 | 5% | 1x | 5% |
| Non-winning Combination | 609 | 60.9% | 0x | 0% |
| **Total** | **1,000** | **100%** | — | **95%** |

The calculation is:
`RTP = Σ (Probability × Payout Multiplier) × 100`

`RTP = (0.17 × 2) + (0.065 × 3) + (0.035 × 4) + (0.015 × 5) + (0.005 × 10) + (0.001 × 0) + (0.05 × 2) + (0.05 × 1) + (0.609 × 0)`

`RTP = 0.95 × 100 = 95%`

This means that, over a sufficiently large number of spins, the expected gross payout is 95 credits for every 100 credits wagered. This is a theoretical long-term average, not a guarantee for an individual session.
The 5% difference represents the theoretical house edge:
`House Edge = 100% - RTP (95%) = 5%`

I configured the game for browser delivery and tested it through a local HTTP server rather than opening index.html directly. The compressed build initially failed because the server did not provide the correct Brotli response headers. I addressed this by enabling Decompression Fallback in the WebGL Publishing Settings so Unity can handle decompression when the server does not serve the expected headers. The build must still be tested in the target browser and hosting environment.

- **Code Structure:** SlotMachineManager.cs handles the balance, bet controls, weighted outcome selection, payout calculations, and coordination between reels. SlotReel.cs handles reel movement and stopping, while AudioManager.cs handles music and sound effects. This keeps each component focused on its own responsibility.
