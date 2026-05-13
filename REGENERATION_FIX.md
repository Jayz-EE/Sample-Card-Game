# Regeneration Status Effect Fix

## Issue
Regeneration was being applied at the wrong time, causing it to "cut off" damage during attacks. The enemy would regenerate immediately after taking damage, rather than at the start of their next turn.

## Root Cause
The game was processing status effects (poison, burn, regeneration) at the **end** of the current player's turn, rather than at the **start** of each player's turn. This meant:

1. Player attacks enemy
2. Enemy takes damage
3. Player ends turn
4. Enemy's regeneration triggers immediately (wrong!)
5. Enemy's turn starts

This made it appear that damage was being "cut off" by regeneration.

## Correct Behavior
Status effects should trigger at the **start** of each player's turn:

1. Player ends turn
2. Turn switches to enemy
3. Enemy's turn starts
4. Enemy's status effects trigger (poison, burn, regen)
5. Enemy draws cards and plays

## Fix Applied

### 1. Split Effect Processing
**File**: `Engines/EffectEngine.cs`

Split `ProcessEndOfTurnEffects` into two methods:

- **`ProcessStartOfTurnEffects()`** - Applies poison, burn, and regeneration damage/healing
- **`TickDownStatusDurations()`** - Decrements status effect durations and removes expired effects

```csharp
public static void ProcessStartOfTurnEffects(PlayerState player)
{
    // Process poison
    var poison = player.StatusEffects.FirstOrDefault(s => s.Type == "POISON");
    if (poison != null)
    {
        player.HP -= poison.Value;
        player.HP = Math.Max(0, player.HP);
    }
    
    // Process burn damage
    var burn = player.StatusEffects.FirstOrDefault(s => s.Type == "BURN");
    if (burn != null)
    {
        player.HP -= burn.Value;
        player.HP = Math.Max(0, player.HP);
    }
    
    // Process regeneration
    var regen = player.StatusEffects.FirstOrDefault(s => s.Type == "REGENERATION");
    if (regen != null)
    {
        player.HP = Math.Min(player.MaxHP, player.HP + regen.Value);
    }
}

public static void TickDownStatusDurations(PlayerState player)
{
    // Tick down durations
    foreach (var status in player.StatusEffects.ToList())
    {
        status.Duration--;
        if (status.Duration <= 0)
            player.StatusEffects.Remove(status);
    }
}
```

### 2. Updated Turn Flow
**File**: `Engines/GameEngine.cs`

**HandleDraw** - Now processes start-of-turn effects:
```csharp
private GameState HandleDraw(GameState state, GameAction action)
{
    if (state.Phase != "DRAW") return state;
    var player = state.Players.First(p => p.PlayerId == action.PlayerId);
    var rng = new Random(state.RNGSeed + state.TurnNumber);
    
    // Process start-of-turn effects (poison, regen, burn)
    EffectEngine.ProcessStartOfTurnEffects(player);
    
    // Draw cards...
}
```

**HandleEndTurn** - Now only ticks down durations:
```csharp
private GameState HandleEndTurn(GameState state, GameAction action)
{
    var player = state.Players.First(p => p.PlayerId == action.PlayerId);

    // Tick down status effect durations at end of turn
    EffectEngine.TickDownStatusDurations(player);

    // Switch turn...
}
```

## Turn Flow (Corrected)

### Player Turn:
1. **Start of turn** → Process poison/burn/regen
2. Draw cards
3. Play cards
4. End turn → Tick down status durations
5. Switch to enemy

### Enemy Turn:
1. **Start of turn** → Process poison/burn/regen
2. Draw cards
3. AI plays cards
4. End turn → Tick down status durations
5. Switch to player

## Result
✅ Regeneration only triggers at the start of each turn  
✅ Poison and burn also trigger at the correct time  
✅ Damage is no longer "cut off" by regeneration  
✅ Status effect durations tick down at end of turn  
✅ Game flow is more predictable and fair

## Files Modified
- `Engines/GameEngine.cs` - Updated HandleDraw and HandleEndTurn
- `Engines/EffectEngine.cs` - Split effect processing into two methods
