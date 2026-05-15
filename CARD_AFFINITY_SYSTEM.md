# Card Affinity System

## Overview
The Card Affinity System ensures that both player classes and enemies acquire cards that match their combat style and archetype. This creates more thematic and balanced gameplay where physical fighters prefer physical cards, magic users prefer magic cards, and each entity has specific tag preferences.

## Implementation Status
✅ **COMPLETED** - All components implemented and tested in both Console and Blazor versions

## Recent Updates
- **Tier-Based Card Scaling**: Enemy additional cards now scale based on tier (Tier 1: +2, Tier 2: +3, Tier 3: +4, Tier 4: +5)

## System Components

### 1. Archetype Types
Three archetype types determine base card preferences:

- **PHYSICAL**: Prefers non-magic cards (3x multiplier), avoids magic cards (0.2x multiplier)
- **MAGIC**: Prefers magic cards (3x multiplier), slightly avoids physical cards (0.4x multiplier)
- **BALANCED**: No archetype modifiers, relies only on tag preferences

### 2. Tag Preferences
Each entity has two tag lists:

- **PreferredTags**: Cards with these tags get 4x multiplier for enemies, 3x for players
- **AvoidedTags**: Cards with these tags get 0.15x multiplier for enemies, 0.2x for players

### 3. Weighted Random Selection
The system uses weighted random selection instead of hard exclusions:
1. Calculate a score for each card based on archetype and tags
2. Sum all scores to get total weight
3. Generate random value between 0 and total weight
4. Select card where cumulative score exceeds random value

This ensures variety while maintaining strong preferences.

## Player Class Affinities

### Pyromancer (MAGIC)
- **Preferred Tags**: FIRE, BURN, MAGIC, DAMAGE
- **Avoided Tags**: HOLY, POISON, PHYSICAL
- **Strategy**: Fire damage specialist, avoids physical and holy magic

### Cleric (MAGIC)
- **Preferred Tags**: HOLY, HEAL, SHIELD, MAGIC
- **Avoided Tags**: FIRE, POISON, BURN
- **Strategy**: Support and defense specialist, avoids offensive magic

### Rogue (PHYSICAL)
- **Preferred Tags**: POISON, PHYSICAL, DAMAGE, DEBUFF
- **Avoided Tags**: HOLY, FIRE, SHIELD
- **Strategy**: Poison and debuff specialist, avoids defensive cards

### Knight (PHYSICAL)
- **Preferred Tags**: PHYSICAL, SHIELD, HOLY, DEFENSE
- **Avoided Tags**: POISON, BURN, FIRE
- **Strategy**: Tank and defender, avoids damage-over-time effects

## Enemy Affinities

### Physical Enemies
- **Goblin Raider**: PHYSICAL type, prefers PHYSICAL/DAMAGE
- **Bandit**: PHYSICAL type, prefers PHYSICAL/POISON/DEBUFF
- **Orc Warrior**: PHYSICAL type, prefers PHYSICAL/DAMAGE/SHIELD
- **Elite Guard**: PHYSICAL type, prefers PHYSICAL/SHIELD/DEFENSE/HOLY
- **Dullahan**: PHYSICAL type, prefers PHYSICAL/DAMAGE/SHIELD
- **Grave Robber**: PHYSICAL type, prefers PHYSICAL/POISON/DEBUFF
- **Wild Beast**: PHYSICAL type, prefers PHYSICAL/DAMAGE
- **Assassin**: PHYSICAL type, prefers PHYSICAL/POISON/DEBUFF/DAMAGE

### Magic Enemies
- **Dark Cultist**: MAGIC type, prefers MAGIC/FIRE/CURSE/SUMMON
- **Necromancer**: MAGIC type, prefers MAGIC/CURSE/SUMMON/HEAL
- **Lich King**: MAGIC type, prefers MAGIC/CURSE/SUMMON/HEAL

### Balanced Enemies
- **Ancient Dragon**: BALANCED type, prefers FIRE/BURN/DAMAGE/PHYSICAL

## Code Changes

### Files Modified

#### Blazor Version (`/GameBlazor/`)
1. **Definitions/Definitions.cs**
   - Added `ArchetypeType`, `PreferredTags`, `AvoidedTags` to `ArcanaDefinition`
   - Added `ArchetypeType`, `PreferredTags`, `AvoidedTags` to `EnemyDefinition`

2. **Data/arcanas.json**
   - Added affinity data for all 4 player classes

3. **Data/enemies.json**
   - Added affinity data for all 9 enemies

4. **Engines/RunManager.cs**
   - Added `SelectCardByAffinity()` method for player card selection
   - Modified `AddRandomCardsToStartingDeck()` to use affinity-based selection

5. **Engines/GameEngine.cs**
   - Added `GetRandomEnemyCardRarity()` method
   - Added `SelectCardByEnemyAffinity()` method for enemy card selection
   - Modified `CreateEnemyState()` to add 5 random cards based on affinity
   - Fixed syntax error (removed duplicate code fragment)

6. **Data/Database.cs**
   - Added `GetAllCards()` method to retrieve all card definitions

## Multiplier Summary

### Player Card Selection
- Physical archetype + non-magic card: **2.5x**
- Physical archetype + magic card: **0.3x**
- Magic archetype + magic card: **2.5x**
- Magic archetype + physical card: **0.6x**
- Preferred tag match: **3x**
- Avoided tag match: **0.2x**

### Enemy Card Selection
- Physical archetype + non-magic card: **3x**
- Physical archetype + magic card: **0.2x**
- Magic archetype + magic card: **3x**
- Magic archetype + physical card: **0.4x**
- Preferred tag match: **4x**
- Avoided tag match: **0.15x**

Enemies have stronger multipliers to create more distinct playstyles.

## Testing

### Build Status
- ✅ Console version: Builds successfully
- ✅ Blazor version: Builds successfully

### Expected Behavior
1. **Starting Decks**: Players now start with 7 class cards + 5 random cards matching their affinity
2. **Enemy Decks**: Enemies start with base deck + tier-scaled random cards:
   - **Tier 1** (Goblin, Bandit, Cultist): Base deck + **2 cards** (1 uncommon, 1 common)
   - **Tier 2** (Orc, Necromancer): Base deck + **3 cards** (1 rare, 2 uncommon)
   - **Tier 3** (Elite Guard): Base deck + **4 cards** (1 rare, 2 uncommon, 1 common)
   - **Tier 4** (Dragon, Lich - Bosses): Base deck + **5 cards** (2 rare, 2 uncommon, 1 common)
3. **Card Distribution**: Physical classes rarely get magic cards, magic classes rarely get physical cards
4. **Tag Preferences**: Classes strongly favor their preferred tags (e.g., Pyromancer gets lots of fire cards)

### Example Scenarios
- **Knight** starting deck: Mostly physical/shield cards, occasional holy card, very rare fire/poison cards
- **Pyromancer** starting deck: Mostly fire/magic cards, very rare physical/holy cards
- **Bandit enemy**: Mostly physical/poison cards, extremely rare magic cards
- **Necromancer enemy**: Mostly magic/curse/summon cards, rare physical cards

## Future Enhancements
- Add affinity visualization in UI (show class preferences)
- Add card reward filtering based on affinity
- Add affinity-based card shop inventory
- Add relics that modify affinity preferences
- Add blessings that expand affinity ranges

## Notes
- The system uses weighted random selection, not hard exclusions
- All cards remain obtainable, just with different probabilities
- Multipliers stack multiplicatively (archetype × tags)
- BALANCED archetype only uses tag preferences, no archetype modifier
