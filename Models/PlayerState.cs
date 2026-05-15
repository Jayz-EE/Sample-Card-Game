namespace Game.Models;

public class PlayerState
{
    public int PlayerId { get; set; }
    public string Name { get; set; } = "";

    public int HP { get; set; }
    public int MaxHP { get; set; }

    public int Energy { get; set; }
    public int MaxEnergy { get; set; }
    
    public int MaxHandSize { get; set; } = 10; // Default hand size limit

    public List<CardInstance> Deck { get; set; } = new();
    public List<CardInstance> Hand { get; set; } = new();
    public List<CardInstance> Discard { get; set; } = new();
    public List<CardInstance> Exhaust { get; set; } = new(); // Cards removed from combat

    public List<StatusEffect> StatusEffects { get; set; } = new();
    public List<string> Inventory { get; set; } = new();

    public string ArcanaId { get; set; } = "";
    public List<string> BlessingIds { get; set; } = new(); // Track active blessings
    
    // Combat statistics (for validation)
    public int DamageDealtThisCombat { get; set; }
    public int DamageTakenThisCombat { get; set; }
    public int CardsPlayedThisCombat { get; set; }
    
    // Minion system
    public int MinionHP { get; set; }
}
