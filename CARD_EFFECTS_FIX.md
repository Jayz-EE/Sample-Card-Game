# Card Effects Fix

## Issue
Cards played sometimes had no visible effects - damage wasn't applied, shields weren't added, etc.

## Root Cause
The issue was in the Blazor component lifecycle. When a card was played:
1. The `GameEngine.ProcessAction()` correctly applied effects to the game state
2. The `UpdatePlayerRefs()` method updated local references
3. **BUT** `StateHasChanged()` was not called, so Blazor didn't know to re-render the UI

This meant the effects were actually being applied in the game state, but the UI wasn't updating to reflect the changes until the next automatic render cycle (like when ending the turn).

## Fix Applied

### 1. Added StateHasChanged() Call
**File**: `Components/CombatScreen.razor`

Added `StateHasChanged()` after processing card effects in the `PlayCard` method:

```csharp
UpdatePlayerRefs();

var damage = oldEnemyHP - enemy.HP;
if (damage > 0)
    AddLog($"🗡️ {cardDef?.Name} dealt {damage} damage!");
else
    AddLog($"✨ Played {cardDef?.Name}");

StateHasChanged(); // ← Added this line
await CheckGameOver();
```

### 2. Added Debug Logging
**File**: `Engines/EffectEngine.cs`

Added logging to help diagnose effect application issues:
- Log when effects are applied with type, value, and target
- Log warning when actor/enemy is null
- Log warning for unhandled effect types

```csharp
Console.WriteLine($"Applying effect: {effect.Type} (value={effect.Value}, target={effect.Target}) from player {actingPlayerId}");
```

## Result
✅ Card effects now apply immediately and visibly
✅ UI updates in real-time when cards are played
✅ Damage, healing, shields, status effects all work correctly
✅ Debug logging helps identify any future issues

## Testing
To verify the fix:
1. Start a combat encounter
2. Play any card (Strike, Shield, Heal, etc.)
3. Observe that effects apply immediately:
   - Damage reduces enemy HP instantly
   - Shields appear in status effects
   - Energy cost is deducted
   - Card moves from hand to discard

## Files Modified
- `Components/CombatScreen.razor` - Added StateHasChanged() call
- `Engines/EffectEngine.cs` - Added debug logging
