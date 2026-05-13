using Game.Models;

namespace Game;

public static class MapGenerator
{
    private const int NODES_PER_FLOOR = 3;
    private const int BOSS_INTERVAL = 15;
    
    public static MapState GenerateMap(int seed, int maxFloor = 100)
    {
        var rng = new Random(seed);
        var map = new MapState();
        
        for (int floor = 1; floor <= maxFloor; floor++)
        {
            var nodeCount = (floor % BOSS_INTERVAL == 0) ? 1 : NODES_PER_FLOOR;
            
            for (int i = 0; i < nodeCount; i++)
            {
                var node = new MapNode
                {
                    Floor = floor,
                    Type = DetermineNodeType(floor, maxFloor, rng),
                    IsAccessible = floor == 1 // First floor is accessible
                };
                
                // Assign enemy for combat nodes
                if (node.Type == "COMBAT" || node.Type == "ELITE")
                {
                    node.EnemyId = SelectEnemy(floor, node.Type == "ELITE", rng);
                    
                    // 25% chance for enemy to have an artifact
                    if (rng.Next(100) < 25)
                    {
                        node.ArtifactId = SelectArtifact(rng);
                    }
                }
                else if (node.Type == "BOSS")
                {
                    node.EnemyId = SelectBoss(rng);
                    
                    // Bosses have 50% chance for artifact
                    if (rng.Next(100) < 50)
                    {
                        node.ArtifactId = SelectArtifact(rng);
                    }
                }
                else if (node.Type == "EVENT")
                {
                    node.EventId = SelectEvent(rng);
                }
                else if (node.Type == "NPC" || node.Type == "TRADER")
                {
                    node.NPCId = SelectNPC(rng);
                }
                else if (node.Type == "BLESSING")
                {
                    node.BlessingId = SelectBlessing(rng);
                }
                
                map.Nodes.Add(node);
            }
        }
        
        // Connect nodes
        ConnectNodes(map, maxFloor);
        
        return map;
    }
    
    private static string DetermineNodeType(int floor, int maxFloor, Random rng)
    {
        // Boss floor every 15 floors
        if (floor % BOSS_INTERVAL == 0) return "BOSS";
        
        // First floor is always combat
        if (floor == 1) return "COMBAT";
        
        // Elite floors (every 5 floors, but not boss floors)
        if (floor % 5 == 0) return rng.Next(100) < 80 ? "ELITE" : "COMBAT";
        
        // Random encounters with weighted probabilities
        var roll = rng.Next(100);
        
        // Early floors (2-4): More combat, some shops/rest
        if (floor <= 4)
        {
            if (roll < 60) return "COMBAT";
            if (roll < 75) return "SHOP";
            if (roll < 85) return "REST";
            if (roll < 95) return "EVENT";
            return "TREASURE";
        }
        
        // Mid floors (5-9): Balanced mix
        if (floor <= 9)
        {
            if (roll < 45) return "COMBAT";
            if (roll < 55) return "ELITE";
            if (roll < 65) return "SHOP";
            if (roll < 75) return "REST";
            if (roll < 85) return "EVENT";
            if (roll < 92) return "NPC";
            return "TREASURE";
        }
        
        // Late floors (10+): More challenging encounters
        if (roll < 40) return "COMBAT";
        if (roll < 55) return "ELITE";
        if (roll < 65) return "EVENT";
        if (roll < 73) return "SHOP";
        if (roll < 80) return "REST";
        if (roll < 87) return "NPC";
        if (roll < 93) return "BLESSING";
        if (roll < 96) return "TREASURE";
        if (roll < 98) return "BLACK_MARKET";
        return "TREASURE";
    }
    
    private static string SelectEnemy(int floor, bool isElite, Random rng)
    {
        var tier = floor switch
        {
            <= 5 => 1,
            <= 10 => 2,
            _ => 3
        };
        
        if (isElite) tier = Math.Min(tier + 1, 3);
        
        // Enemy selection based on tier with new enemies
        return tier switch
        {
            1 => rng.Next(3) switch { 0 => "goblin", 1 => "bandit", _ => "cultist" },
            2 => rng.Next(2) switch { 0 => "orc_warrior", _ => "necromancer" },
            3 => rng.Next(5) switch { 
                0 => "elite_guard", 
                1 => "fire_elemental", 
                2 => "shadow_assassin",
                3 => "holy_paladin",
                _ => "iron_golem"
            },
            _ => "goblin"
        };
    }
    
    private static string SelectBoss(Random rng)
    {
        var bosses = new[] { "dragon", "lich", "demon_lord", "void_horror", "titan_king" };
        return bosses[rng.Next(bosses.Length)];
    }
    
    private static string SelectEvent(Random rng)
    {
        var events = new[] { "mysterious_shrine", "merchant_ambush", "ancient_library", 
                            "healing_fountain", "card_master", "cursed_chest",
                            "dragon_omen", "lich_omen" };
        return events[rng.Next(events.Length)];
    }
    
    private static string SelectNPC(Random rng)
    {
        var npcs = new[] { "wandering_merchant", "mysterious_stranger", "old_hermit", 
                          "fortune_teller", "blacksmith", "wounded_knight", "grave_keeper" };
        return npcs[rng.Next(npcs.Length)];
    }
    
    private static string SelectBlessing(Random rng)
    {
        var blessings = new[] { "strength_blessing", "vitality_blessing", "fortune_blessing",
                               "protection_blessing", "wisdom_blessing", "capacity_blessing",
                               "fire_resistance", "poison_resistance", "physical_resistance", "magic_resistance" };
        return blessings[rng.Next(blessings.Length)];
    }
    
    private static string SelectArtifact(Random rng)
    {
        var artifacts = new[] { "strength_artifact", "shield_artifact", "regeneration_artifact",
                               "energy_artifact", "poison_artifact", "draw_artifact",
                               "hp_artifact", "damage_artifact", "thorns_artifact", "burn_artifact" };
        return artifacts[rng.Next(artifacts.Length)];
    }
    
    private static void ConnectNodes(MapState map, int maxFloor)
    {
        for (int floor = 1; floor < maxFloor; floor++)
        {
            var currentFloorNodes = map.Nodes.Where(n => n.Floor == floor).ToList();
            var nextFloorNodes = map.Nodes.Where(n => n.Floor == floor + 1).ToList();
            
            if (nextFloorNodes.Count == 0) continue;
            
            // Ensure every next floor node is reachable from at least one current floor node
            var connectedNextNodes = new HashSet<string>();
            
            foreach (var currentNode in currentFloorNodes)
            {
                var nodeRng = new Random(currentNode.Id.GetHashCode());
                
                // Each node connects to 1-2 nodes on next floor
                var connectionCount = nextFloorNodes.Count == 1 ? 1 : nodeRng.Next(1, Math.Min(3, nextFloorNodes.Count + 1));
                var targets = nextFloorNodes.OrderBy(n => nodeRng.Next()).Take(connectionCount).ToList();
                
                foreach (var nextNode in targets)
                {
                    if (!currentNode.ConnectedNodeIds.Contains(nextNode.Id))
                    {
                        currentNode.ConnectedNodeIds.Add(nextNode.Id);
                        connectedNextNodes.Add(nextNode.Id);
                    }
                }
            }
            
            // Ensure all next floor nodes are connected
            foreach (var nextNode in nextFloorNodes)
            {
                if (!connectedNextNodes.Contains(nextNode.Id) && currentFloorNodes.Count > 0)
                {
                    var randomCurrent = currentFloorNodes[new Random(nextNode.Id.GetHashCode()).Next(currentFloorNodes.Count)];
                    if (!randomCurrent.ConnectedNodeIds.Contains(nextNode.Id))
                    {
                        randomCurrent.ConnectedNodeIds.Add(nextNode.Id);
                    }
                }
            }
        }
    }
    
    public static void UnlockConnectedNodes(MapState map, string completedNodeId)
    {
        var node = map.Nodes.FirstOrDefault(n => n.Id == completedNodeId);
        if (node == null) return;
        
        node.IsCompleted = true;
        
        foreach (var connectedId in node.ConnectedNodeIds)
        {
            var connectedNode = map.Nodes.FirstOrDefault(n => n.Id == connectedId);
            if (connectedNode != null)
            {
                connectedNode.IsAccessible = true;
            }
        }
    }
}
