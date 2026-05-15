# Hand Capacity System Fix

## Issue
The draw card effect was preventing drawing when the hand reached capacity. This was incorrect behavior - players should be able to draw cards beyond the hand capacity limit, with excess cards being discarded at the end of turn.

## Root Cause
1. **Console Version**: DrawCard method had a hard cap check (`if (player.Hand.Count >= 20) return;`) that prevented drawing
2. **Console Version**: Missing MaxHandSize property in PlayerState
3. **Console Version**: Missing end-of-turn logic to discard excess cards
4. **Blazor Version**: MaxHandSize was set too low (4 cards)

## Solution Implemented

### Changes Made

#### Console Version (`/Game/`)

**1. Models/PlayerState.cs**
- Added `MaxHandSize` property with default value of 10
```csharp
public int MaxHandSize { get; set; } = 10; // Default hand size limit
```

**2. EffectEngine.cs - DrawCard method**
- Removed the hard cap check that prevented drawing at 20 cards
- Added comment explaining that excess cards will be discarded at end of turn
```csharp
// Before:
if (player.Hand.Count >= 20) return;

// After:
// Allow drawing beyond hand capacity - excess cards will be discarded at end of turn
```

**3. GameEngine.cs - HandleEndTurn method**
- Added logic to discard excess cards when hand exceeds MaxHandSize
```csharp
// Discard excess cards if hand exceeds max hand size
var rng = new Random(state.RNGSeed + state.TurnNumber);
while (player.Hand.Count > player.MaxHandSize)
{
    var cardToDiscard = player.Hand[rng.Next(player.Hand.Count)];
    player.Hand.Remove(cardToDiscard);
    player.Discard.Add(cardToDiscard);
}
```

#### Blazor Version (`/GameBlazor/`)

**1. Models/PlayerState.cs**
- Updated MaxHandSize from 4 to 10 for consistency
```csharp
// Before:
public int MaxHandSize { get; set; } = 4;

// After:
public int MaxHandSize { get; set; } = 10; // Default hand size limit
```

Note: Blazor version already had the correct DrawCard and HandleEndTurn implementations.

## How It Works Now

### Drawing Cards
1. Players can draw cards even if their hand is at or above capacity
2. No hard limit during the draw phase
3. Cards like "Draw 2" or "Draw 3" will always work, regardless of current hand size

### End of Turn
1. After the player ends their turn, the game checks hand size
2. If hand size exceeds MaxHandSize (10 cards), excess cards are randomly discarded
3. Discarded cards go to the discard pile and can be reshuffled later

### Example Scenarios

**Scenario 1: Normal Play**
- Player has 7 cards in hand
- Plays a "Draw 2" card
- Hand now has 9 cards (within limit)
- End turn: No cards discarded

**Scenario 2: Exceeding Capacity**
- Player has 9 cards in hand
- Plays a "Draw 3" card
- Hand now has 12 cards (exceeds limit of 10)
- End turn: 2 random cards are discarded to bring hand back to 10

**Scenario 3: Multiple Draw Effects**
- Player has 8 cards in hand
- Plays multiple draw cards, hand reaches 15 cards
- All draws work correctly
- End turn: 5 random cards are discarded to bring hand back to 10

## Benefits

1. **Draw cards always work**: No more situations where draw effects fail
2. **Strategic depth**: Players can choose to draw many cards knowing they'll discard at end of turn
3. **Consistent behavior**: Both versions now work the same way
4. **Balanced**: 10-card hand limit prevents infinite hand size while allowing flexibility

## Build Status
- ✅ Console version: Builds successfully
- ✅ Blazor version: Builds successfully

## Testing Recommendations

Test the following scenarios:
1. Draw cards when hand is at 9 cards (should work)
2. Draw cards when hand is at 10 cards (should work, discard at end of turn)
3. Play multiple draw cards in one turn (all should work)
4. Verify random discard happens at end of turn when over capacity
5. Verify discarded cards go to discard pile and can be reshuffled
