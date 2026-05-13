# Item Inventory System

## Overview
Added a consumable item inventory system where players can use items during combat. **Using any item costs ALL energy**, making it a strategic decision.

## Features

### Item Types
Created 10 consumable items with various effects:

**Common Items:**
- **Health Potion** - Restore 30 HP
- **Energy Elixir** - Gain 3 energy next turn
- **Poison Vial** - Apply 5 poison to enemy
- **Shield Potion** - Gain 15 shield

**Uncommon Items:**
- **Strength Tonic** - Gain 3 strength for 2 turns
- **Smoke Bomb** - Apply weak to enemy for 2 turns
- **Explosive Flask** - Deal 35 damage

**Rare Items:**
- **Fire Bomb** - Deal 20 damage and apply 3 burn
- **Regeneration Salve** - Gain 5 regeneration for 3 turns
- **Miracle Potion** - Restore 50 HP and gain 10 shield

### Usage Rules
- Items can only be used during combat (MAIN phase)
- **Using an item costs ALL energy** (must have full energy)
- Items are consumed on use (removed from inventory)
- Items apply their effects immediately

## Implementation

### 1. Data Models

**RunState.cs** - Added inventory to run state:
```csharp
public List<string> Inventory { get; set; } = new();
```

**PlayerState.cs** - Added inventory to combat state:
```csharp
public List<string> Inventory { get; set; } = new();
```

**GameAction.cs** - Added item action support:
```csharp
public string Type { get; set; } = ""; // PLAY_CARD, END_TURN, DRAW_CARD, USE_ITEM
public string? ItemId { get; set; }
```

### 2. Item Definitions

**Definitions.cs** - New ItemDefinition class:
```csharp
public class ItemDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Rarity { get; set; } = "COMMON";
    public List<EffectDefinition> Effects { get; set; } = new();
}
```

**items.json** - Item data file with 10 consumables

### 3. Game Engine

**GameEngine.cs** - HandleUseItem method:
```csharp
private GameState HandleUseItem(GameState state, GameAction action)
{
    if (state.Phase != "MAIN") return state;
    if (action.ItemId == null) return state;

    var player = state.Players.First(p => p.PlayerId == action.PlayerId);
    
    // Check if player has the item
    if (!player.Inventory.Contains(action.ItemId)) return state;
    
    var itemDef = _db.GetItem(action.ItemId);
    if (itemDef == null) return state;
    
    // Using an item costs ALL energy
    if (player.Energy < player.MaxEnergy) return state;
    
    player.Energy = 0;
    player.Inventory.Remove(action.ItemId);
    
    // Apply item effects
    foreach (var effect in itemDef.Effects)
    {
        state = EffectEngine.ApplyEffect(state, effect, action.PlayerId);
    }
    
    return state;
}
```

### 4. UI Components

**CombatScreen.razor** - Inventory display:
- Shows inventory section below hand when player has items
- Items displayed as buttons with name and "All ⚡" cost
- Only usable when player has full energy
- Tooltip shows item description

**UseItem method:**
```csharp
private async Task UseItem(string itemId)
{
    if (state == null) return;
    
    var item = Database.Instance.GetItem(itemId);
    
    state = engine.ProcessAction(state, new GameAction
    {
        Type = "USE_ITEM",
        PlayerId = 1,
        ItemId = itemId
    });
    
    UpdatePlayerRefs();
    AddLog($"💊 Used {item?.Name}");
    
    StateHasChanged();
    await CheckGameOver();
}
```

### 5. Styling

**game.css** - Inventory section styling:
- Purple gradient theme for items
- Hover effects and animations
- Disabled state when not enough energy
- Responsive layout

## Usage

### Adding Items to Inventory
```csharp
// Add item to run
run.Inventory.Add("health_potion");
run.Inventory.Add("fire_bomb");
```

### In Combat
1. Items automatically appear in inventory section
2. Player must have **full energy** to use an item
3. Click item button to use
4. Item is consumed and effects apply immediately
5. Player's energy drops to 0

## Strategic Considerations

### Energy Cost
- Using an item costs ALL energy, so timing is crucial
- Best used at start of turn with full energy
- Trade-off between playing cards vs using items

### Item Types
- **Healing items** - Best when low on HP
- **Damage items** - Burst damage when needed
- **Buff items** - Set up for future turns
- **Debuff items** - Control enemy actions

### Synergies
- Energy Elixir can help recover from item use
- Strength Tonic amplifies future attacks
- Shield Potion provides immediate defense

## Files Modified
1. `Models/RunState.cs` - Added Inventory list
2. `Models/PlayerState.cs` - Added Inventory list
3. `Models/GameAction.cs` - Added ItemId and USE_ITEM type
4. `Definitions/Definitions.cs` - Added ItemDefinition class
5. `Data/Database.cs` - Added Items dictionary and loading
6. `wwwroot/Data/items.json` - Created item definitions
7. `Engines/GameEngine.cs` - Added HandleUseItem method
8. `Engines/ValidationEngine.cs` - Added USE_ITEM validation
9. `Components/CombatScreen.razor` - Added inventory UI and UseItem method
10. `wwwroot/css/game.css` - Added inventory styling

## Future Enhancements
- Add items to shop inventory
- Add items as combat rewards
- Add items to treasure chests
- Add item rarity-based pricing
- Add item crafting system
- Add item stacking (multiple of same item)
