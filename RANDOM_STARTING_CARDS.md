# Random Starting Cards Feature

## Overview
Players now receive 5 random cards from the card pool when starting a new run, in addition to their class-specific starting deck. This adds variety and replayability to each run.

## How It Works

### Starting Deck Composition
When starting a new run, players receive:
1. **Class-specific cards** (7 cards based on chosen class)
2. **5 random cards** from the general card pool

**Total starting deck: 12 cards**

### Random Card Selection

#### Rarity Distribution
The 5 random cards are selected with weighted probabilities:
- **Common**: 60% chance
- **Uncommon**: 25% chance
- **Rare**: 10% chance
- **Epic**: 4% chance
- **Legendary**: 1% chance

#### Card Pool
- Includes all player-usable cards
- Excludes curse cards
- Excludes enemy-only cards (cards starting with "monster_")

### Example Starting Decks

#### Pyromancer (70 HP, 100 Gold)
**Class Cards (7):**
- Strike x2
- Fireball x2
- Shield x2
- Heal x1

**Random Cards (5):**
- Could be any combination like:
  - Draw Two (Common)
  - Power Strike (Common)
  - Fortify (Uncommon)
  - Cleave (Common)
  - Poison Strike (Uncommon)

**Total: 12 cards**

#### Knight (100 HP, 80 Gold)
**Class Cards (7):**
- Strike x2
- Shield x3
- Heal x1
- Power Strike x1

**Random Cards (5):**
- Could be any combination like:
  - Fireball (Common)
  - Draw Two (Common)
  - Heal (Common)
  - Barrier (Rare)
  - Meditation (Uncommon)

**Total: 12 cards**

## Benefits

### 1. Increased Variety
- Every run starts differently
- No two starting decks are exactly the same
- Encourages different strategies

### 2. Better Early Game
- More options in early combats
- Reduces reliance on basic strikes
- Allows for more interesting plays

### 3. Replayability
- Each class feels fresh on replay
- Random cards can synergize in unexpected ways
- Encourages experimentation

### 4. Balanced Randomness
- Weighted towards common cards (60%)
- Small chance for powerful cards (1% legendary)
- Similar to enemy deck building system

## Implementation Details

### Code Location
**File:** `/Engines/RunManager.cs`

**Method:** `AddRandomCardsToStartingDeck()`
```csharp
private void AddRandomCardsToStartingDeck(RunState run, Random rng, int count)
{
    // Get all player cards (exclude curses and enemy-only cards)
    var availableCards = _db.Cards.Values
        .Where(c => c.Rarity != "CURSE" && !c.Id.StartsWith("monster_"))
        .ToList();
    
    for (int i = 0; i < count; i++)
    {
        // Determine rarity based on weighted random
        var rarity = GetRandomCardRarity(rng);
        
        // Get cards of that rarity
        var cardsOfRarity = availableCards
            .Where(c => c.Rarity == rarity)
            .ToList();
        
        // Pick a random card and add to deck
        var randomCard = cardsOfRarity[rng.Next(cardsOfRarity.Count)];
        run.Deck.Add(new CardInstance
        {
            CardId = randomCard.Id,
            OwnerPlayerId = 1,
            IsUpgraded = false
        });
    }
}
```

### Rarity Weights
**Method:** `GetRandomCardRarity()`
```csharp
private string GetRandomCardRarity(Random rng)
{
    var roll = rng.Next(100);
    
    if (roll < 60) return "COMMON";      // 60%
    if (roll < 85) return "UNCOMMON";    // 25%
    if (roll < 95) return "RARE";        // 10%
    if (roll < 99) return "EPIC";        // 4%
    return "LEGENDARY";                   // 1%
}
```

## Comparison with Enemy System

### Similarities
- Both use weighted random selection
- Both add cards from a pool
- Both use the same RNG seed for consistency

### Differences
| Aspect | Player | Enemy |
|--------|--------|-------|
| Base Deck | 7 class-specific cards | Defined in enemy JSON |
| Random Cards | 5 cards | 1 Rare + 2 Uncommon |
| Rarity Weights | 60/25/10/4/1 | Fixed rarities |
| Card Pool | All player cards | Enemy card pool |
| Scaling | Fixed at start | Scales with floor |

## Balance Considerations

### Advantages
- ✅ More strategic options early
- ✅ Reduces bad RNG in first few fights
- ✅ Makes each run unique
- ✅ Rewards deck-building skills

### Potential Issues
- ⚠️ Could make early game too easy
- ⚠️ Might dilute class identity
- ⚠️ Rare cards at start could be overpowered

### Mitigation
- Cards are not upgraded
- Weighted heavily towards common (60%)
- Only 5 cards (not overwhelming)
- Still need to build synergies

## Testing

### How to Test
1. Start a new run with any class
2. View deck (press 'D' or click "View Deck")
3. Check total card count: Should be 12 cards
4. Verify 5 cards are not from class starting deck
5. Restart multiple times to see variety

### Expected Results
- ✅ Deck has 12 cards total
- ✅ 7 cards match class starting deck
- ✅ 5 cards are random additions
- ✅ Random cards vary between runs
- ✅ Most random cards are Common/Uncommon

## Statistics

### Probability of Getting At Least One:
- **Uncommon or better**: ~87%
- **Rare or better**: ~41%
- **Epic or better**: ~18%
- **Legendary**: ~5%

### Expected Rarity Distribution (5 cards):
- **Common**: ~3 cards
- **Uncommon**: ~1.25 cards
- **Rare**: ~0.5 cards
- **Epic**: ~0.2 cards
- **Legendary**: ~0.05 cards

## Future Enhancements

### Possible Improvements
1. **Class-Themed Random Cards**
   - Pyromancer gets more fire cards
   - Knight gets more defensive cards
   - Cleric gets more healing cards

2. **Difficulty Scaling**
   - Easy mode: 7 random cards
   - Normal mode: 5 random cards (current)
   - Hard mode: 3 random cards

3. **Seed-Based Challenges**
   - Share seeds for specific starting decks
   - Daily challenge with fixed random cards

4. **Card Banning**
   - Option to exclude certain cards
   - Unlockable card pools

## Files Modified
- `/Engines/RunManager.cs` (Both console and Blazor versions)

## Status
✅ **IMPLEMENTED** - Players now receive 5 random cards at run start
