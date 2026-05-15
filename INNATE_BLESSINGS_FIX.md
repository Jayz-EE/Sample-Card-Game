# Innate Blessings Fix - Blazor Version

## Issue
The innate blessing system was implemented in the console version but was missing from the Blazor version. Players were not receiving their class-specific innate blessings when starting a new run.

## What Was Fixed

### 1. Data Files Updated

**arcanas.json** - Added `innateBlessingId` property to each class:
- Pyromancer: `pyromancer_innate`
- Cleric: `cleric_innate`
- Rogue: `rogue_innate`
- Knight: `knight_innate`

**blessings.json** - Added 4 innate blessing definitions:
- `pyromancer_innate` - Flames of Passion (Fire cards +2 damage, start with 3 shield)
- `cleric_innate` - Divine Grace (Heal cards +3 HP, regenerate 2 HP/turn)
- `rogue_innate` - Shadow Step (Draw 6 cards, start with 4 energy)
- `knight_innate` - Stalwart Defense (Shield cards +3, +10 max HP)

### 2. Code Changes

**Definitions/Definitions.cs**
- Added `InnateBlessingId` property to `ArcanaDefinition`

**Models/PlayerState.cs**
- Added `BlessingIds` list to track active blessings in combat

**Engines/RunManager.cs**
- Added code to apply innate blessing when starting a new run
- Added `ApplyInnateBlessingEffect()` method to handle run-start effects (Knight's +10 HP)

**Engines/GameEngine.cs**
- Added `ApplyInnateBlessingBonus()` method to boost card effects based on innate blessings
- Added combat-start blessing effects (Pyromancer's shield, Cleric's regeneration)
- Added Rogue's +1 energy bonus
- Updated `CreatePlayerState()` to copy blessings from run to player state
- Modified `HandlePlayCard()` to apply innate blessing bonuses to card effects

## How It Works

### Run Start (RunManager.StartNewRun)
1. Check if arcana has an `InnateBlessingId`
2. Add blessing to `run.BlessingIds`
3. Apply immediate effects:
   - **Knight**: +10 max HP and current HP

### Combat Start (GameEngine.InitializeCombat)
1. Copy blessings from run to player state
2. Calculate max energy (Rogue gets +1)
3. Apply combat-start effects:
   - **Pyromancer**: 3 shield (duration 1 turn)
   - **Cleric**: Regeneration 2 HP/turn (duration 999 turns)

### Card Play (GameEngine.HandlePlayCard)
1. For each card effect, apply innate blessing bonuses:
   - **Pyromancer**: Fire damage cards +2 damage
   - **Cleric**: Heal cards +3 healing
   - **Knight**: Shield cards +3 shield

## Testing

To verify the fix works:

1. **Knight**: Start a new run, should have 100/100 HP (not 90/90)
2. **Pyromancer**: Enter combat, should start with 3 shield
3. **Cleric**: Enter combat, should have regeneration status effect
4. **Rogue**: Enter combat, should have 4/4 energy and draw 6 cards

## Expected Behavior

### Knight (90 base HP → 100 HP)
- ✅ Starts with 100/100 HP
- ✅ Shield cards grant +3 extra shield
- ✅ Blessing shows in UI: "✨Stalwart Defense"

### Pyromancer (70 HP)
- ✅ Starts combat with 3 shield
- ✅ Fire cards deal +2 damage
- ✅ Blessing shows in UI: "✨Flames of Passion"

### Cleric (80 HP)
- ✅ Regenerates 2 HP at end of each turn
- ✅ Heal cards restore +3 HP
- ✅ Blessing shows in UI: "✨Divine Grace"

### Rogue (60 HP)
- ✅ Starts combat with 4 energy (instead of 3)
- ✅ Draws 6 cards at start (instead of 5)
- ✅ Blessing shows in UI: "✨Shadow Step"

## Files Modified

### Blazor Version
- `/Data/arcanas.json`
- `/Data/blessings.json`
- `/Definitions/Definitions.cs`
- `/Models/PlayerState.cs`
- `/Engines/RunManager.cs`
- `/Engines/GameEngine.cs`

### Console Version (already working)
- Debug statements removed
- Input handling restored to normal

## Status
✅ **FIXED** - Both console and Blazor versions now have full innate blessing support
