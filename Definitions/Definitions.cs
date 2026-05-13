namespace Game.Definitions;

public class CardDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int Cost { get; set; }
    public string Rarity { get; set; } = "COMMON"; // COMMON, UNCOMMON, RARE, LEGENDARY, CURSE
    public List<string> Tags { get; set; } = new();
    public List<EffectDefinition> Effects { get; set; } = new();
    public List<EffectDefinition> UpgradedEffects { get; set; } = new();
}

public class EffectDefinition
{
    public string Type { get; set; } = ""; // DAMAGE, HEAL, DRAW, SHIELD, APPLY_STATUS, etc.
    public int Value { get; set; }
    public string Target { get; set; } = "ENEMY"; // SELF, ENEMY, ALL
    public string? StatusType { get; set; } // For APPLY_STATUS
    public int Duration { get; set; } // For status effects
    public int Threshold { get; set; } // For EXECUTE
    public int Damage { get; set; } // For EXECUTE
    public int Percentage { get; set; } // For LIFESTEAL
    public string? EnemyId { get; set; } // For START_COMBAT
    public string? PotionId { get; set; } // For GAIN_POTION
    public string? Rarity { get; set; } // For GAIN_RELIC, GAIN_CARD
    public List<RandomOutcome>? Outcomes { get; set; } // For RANDOM_OUTCOME
}

public class RandomOutcome
{
    public int Weight { get; set; }
    public List<EffectDefinition> Effects { get; set; } = new();
}

public class ArcanaDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string PassiveEffect { get; set; } = "";
    public List<string> StartingDeck { get; set; } = new();
    public int StartingHP { get; set; } = 70;
    public int StartingGold { get; set; } = 100;
}

public class EnemyDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int HP { get; set; }
    public List<string> Deck { get; set; } = new();
    public string AIPattern { get; set; } = "BALANCED"; // AGGRESSIVE, DEFENSIVE, BALANCED, BOSS
    public int GoldReward { get; set; }
    public int Tier { get; set; } = 1;
    public string Icon { get; set; } = "fi-sr-skull";
    public List<string> Dialogue { get; set; } = new();
    public List<string> AttackLines { get; set; } = new();
    public List<string> HurtLines { get; set; } = new();
}

public class RelicDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Rarity { get; set; } = "COMMON";
    public string Effect { get; set; } = "";
    public int Value { get; set; }
}

public class EventDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<EventChoice> Choices { get; set; } = new();
}

public class EventChoice
{
    public string Text { get; set; } = "";
    public List<EffectDefinition> Effects { get; set; } = new();
}

public class NPCDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Greeting { get; set; } = "";
    public List<NPCChoice> Choices { get; set; } = new();
}

public class NPCChoice
{
    public string Text { get; set; } = "";
    public int GoldCost { get; set; }
    public int HpCost { get; set; }
    public List<EffectDefinition> Effects { get; set; } = new();
}

public class BlessingDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Effect { get; set; } = "";
    public int Value { get; set; }
}

public class ItemDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Rarity { get; set; } = "COMMON";
    public List<EffectDefinition> Effects { get; set; } = new();
    
    public bool IsSnack() => Effects.Any(e => e.Type == "RESTORE_ENERGY");
}
