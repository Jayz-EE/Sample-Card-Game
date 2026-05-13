# Random Encounters & Rewards Update

## Card Reward Rarity System

### New Random Rarity Distribution
Card rewards after combat now use weighted random selection:

- **5% chance** - EPIC cards (powerful 4-7 cost cards)
- **20% chance** - RARE cards (strong 2-4 cost cards)
- **35% chance** - UNCOMMON cards (solid 1-3 cost cards)
- **40% chance** - COMMON cards (basic 0-2 cost cards)

### Implementation
- Added `GetRandomRarity()` method to RunManager
- Combat victories now roll for rarity before selecting cards
- Combat log shows the rarity tier of rewards (e.g., "✨ RARE card rewards available!")

## Random Encounter System

### Old System (Fixed Routes)
- Predictable node types based on floor number
- Every 4th floor = Shop
- Every 5th floor = Elite
- Every 6th floor = Rest
- Limited variety and replayability

### New System (Random Encounters)
Encounters are now randomly generated with weighted probabilities that change by floor progression:

#### Early Floors (2-4)
- 60% Combat
- 15% Shop
- 10% Rest
- 10% Event
- 5% Treasure

#### Mid Floors (5-9)
- 45% Combat
- 10% Elite
- 10% Shop
- 10% Rest
- 10% Event
- 7% NPC
- 8% Treasure

#### Late Floors (10-14)
- 40% Combat
- 15% Elite
- 10% Event
- 8% Shop
- 7% Rest
- 7% NPC
- 6% Blessing
- 7% Treasure

### Special Floors
- **Floor 1**: Always Combat (tutorial)
- **Every 5th floor**: 80% Elite, 20% Combat
- **Floor 15**: Always Boss

## Enemy Variety

### Tier 1 (Floors 1-5)
- Goblin Raider
- Bandit
- Dark Cultist

### Tier 2 (Floors 6-10)
- Orc Warrior
- Necromancer

### Tier 3 (Floors 11+)
- Elite Guard
- Fire Elemental (NEW)
- Shadow Assassin (NEW)
- Holy Paladin (NEW)
- Iron Golem (NEW)

### Bosses (Floor 15)
Random selection from:
- Ancient Dragon
- Lich King
- Demon Lord (NEW)
- Void Horror (NEW)
- Titan King (NEW)

## Benefits

### Increased Replayability
- Every run has different encounter patterns
- No two maps are the same
- Players must adapt to what they find

### Better Reward Excitement
- Chance for epic cards keeps combat rewarding
- Higher rarity rewards feel more special
- Progression feels more dynamic

### Strategic Depth
- Can't rely on knowing shop/rest locations
- Must manage resources more carefully
- Risk/reward decisions are more meaningful

## Files Modified
- `GameBlazor/Engines/RunManager.cs` - Added GetRandomRarity()
- `GameBlazor/Engines/MapGenerator.cs` - Rewrote DetermineNodeType() for random encounters
- `GameBlazor/Components/CombatScreen.razor` - Uses random rarity for rewards
