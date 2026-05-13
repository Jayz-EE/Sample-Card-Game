namespace Game.Models;

public class CardInstance
{
    public string InstanceId { get; set; } = Guid.NewGuid().ToString();
    public string CardId { get; set; } = "";
    public int OwnerPlayerId { get; set; }
    public bool IsUpgraded { get; set; }
    public List<Modifier> Modifiers { get; set; } = new();
    
    // Validation (prevent card duplication exploits)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class StatusEffect
{
    public string Type { get; set; } = ""; // POISON, SHIELD, WEAK, STRENGTH, REGENERATION
    public int Value { get; set; }
    public int Duration { get; set; }
    
    // Prevent stacking exploits
    public int MaxStacks { get; set; } = 99;
}

public class Modifier
{
    public string Type { get; set; } = ""; // COST_REDUCTION, DAMAGE_BOOST
    public int Value { get; set; }
}
