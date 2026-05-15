namespace Game.Models;

public class RunState
{
    public string RunId { get; set; } = Guid.NewGuid().ToString();
    public int Seed { get; set; }
    public string ArcanaId { get; set; } = "";
    
    // Player progression
    public int CurrentFloor { get; set; } = 1;
    public int MaxFloor { get; set; } = 100;
    public int Gold { get; set; }
    public int HP { get; set; }
    public int MaxHP { get; set; }
    public int MaxEnergy { get; set; } = 3;
    public int MaxHandSize { get; set; } = 10;
    
    // Collections
    public List<CardInstance> Deck { get; set; } = new();
    public List<string> RelicIds { get; set; } = new();
    public List<string> ArtifactIds { get; set; } = new();
    public List<string> BlessingIds { get; set; } = new();
    public List<string> Inventory { get; set; } = new();
    public List<string> CompletedNodeIds { get; set; } = new();
    public Dictionary<string, List<string>> ShopInventories { get; set; } = new(); // NodeId -> ItemIds
    
    // Map
    public MapState Map { get; set; } = new();
    
    // Run statistics (anti-exploit tracking)
    public int TotalCombats { get; set; }
    public int TotalDamageDealt { get; set; }
    public int TotalDamageTaken { get; set; }
    public int CardsPlayed { get; set; }
    public DateTime RunStartTime { get; set; } = DateTime.UtcNow;
    public bool IsRunActive { get; set; } = true;
    
    // Validation hash (prevent save scumming)
    public string ValidationHash { get; set; } = "";
}

public class MapState
{
    public List<MapNode> Nodes { get; set; } = new();
    public string? CurrentNodeId { get; set; }
}

public class MapNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int Floor { get; set; }
    public string Type { get; set; } = ""; // COMBAT, ELITE, BOSS, EVENT, SHOP, REST, TREASURE, NPC, MARKET, TRADER, BLESSING, CAMP
    public List<string> ConnectedNodeIds { get; set; } = new();
    public bool IsCompleted { get; set; }
    public bool IsAccessible { get; set; }
    public string? EnemyId { get; set; } // For combat nodes
    public string? ArtifactId { get; set; } // For enemy artifacts
    public string? EventId { get; set; } // For event nodes
    public string? NPCId { get; set; } // For NPC/trader nodes
    public string? BlessingId { get; set; } // For blessing nodes
}

public class Relic
{
    public string Id { get; set; } = "";
    public int AcquiredFloor { get; set; }
}
