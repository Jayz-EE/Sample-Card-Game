using Game.Models;
using Game.Data;
using Game.Definitions;

namespace Game;

public class RunManager
{
    private readonly Database _db;
    
    public RunManager()
    {
        _db = Database.Instance;
    }
    
    public RunState StartNewRun(string arcanaId, int? seed = null)
    {
        var arcana = _db.GetArcana(arcanaId);
        if (arcana == null) throw new ArgumentException($"Invalid arcana: {arcanaId}");
        
        var runSeed = seed ?? new Random().Next();
        var rng = new Random(runSeed);
        
        var run = new RunState
        {
            Seed = runSeed,
            ArcanaId = arcanaId,
            Gold = arcana.StartingGold,
            HP = arcana.StartingHP,
            MaxHP = arcana.StartingHP,
            CurrentFloor = 1,
            MaxFloor = 100
        };
        
        // Build starting deck
        foreach (var cardId in arcana.StartingDeck)
        {
            if (_db.IsValidCard(cardId))
            {
                run.Deck.Add(new CardInstance
                {
                    CardId = cardId,
                    OwnerPlayerId = 1,
                    IsUpgraded = false
                });
            }
        }
        
        // Add starting items
        run.Inventory.Add("health_potion");
        run.Inventory.Add("shield_potion");
        
        // Generate map
        run.Map = MapGenerator.GenerateMap(runSeed, run.MaxFloor);
        
        // Set starting node
        var startNode = run.Map.Nodes.FirstOrDefault(n => n.Floor == 1);
        if (startNode != null)
        {
            run.Map.CurrentNodeId = startNode.Id;
        }
        
        ValidationEngine.UpdateRunHash(run);
        
        return run;
    }
    
    public void CompleteNode(RunState run, MapNode node)
    {
        if (!ValidationEngine.ValidateRunState(run))
            throw new InvalidOperationException("Run state validation failed");
        
        run.CompletedNodeIds.Add(node.Id);
        MapGenerator.UnlockConnectedNodes(run.Map, node.Id);
        
        // Advance floor if all nodes on current floor are completed
        var currentFloorNodes = run.Map.Nodes.Where(n => n.Floor == run.CurrentFloor).ToList();
        if (currentFloorNodes.All(n => n.IsCompleted))
        {
            run.CurrentFloor++;
        }
        
        ValidationEngine.UpdateRunHash(run);
    }
    
    public void AddCardToRun(RunState run, string cardId, bool isUpgraded = false)
    {
        if (!_db.IsValidCard(cardId))
            throw new ArgumentException($"Invalid card: {cardId}");
        
        if (run.Deck.Count >= 100)
            throw new InvalidOperationException("Deck is full");
        
        run.Deck.Add(new CardInstance
        {
            CardId = cardId,
            OwnerPlayerId = 1,
            IsUpgraded = isUpgraded
        });
        
        ValidationEngine.UpdateRunHash(run);
    }
    
    public void RemoveCardFromRun(RunState run, string instanceId)
    {
        var card = run.Deck.FirstOrDefault(c => c.InstanceId == instanceId);
        if (card != null)
        {
            run.Deck.Remove(card);
            ValidationEngine.UpdateRunHash(run);
        }
    }
    
    public void UpgradeCard(RunState run, string instanceId)
    {
        var card = run.Deck.FirstOrDefault(c => c.InstanceId == instanceId);
        if (card != null && !card.IsUpgraded)
        {
            card.IsUpgraded = true;
            ValidationEngine.UpdateRunHash(run);
        }
    }
    
    public void AddRelic(RunState run, string relicId)
    {
        if (!_db.IsValidRelic(relicId))
            throw new ArgumentException($"Invalid relic: {relicId}");
        
        if (!run.RelicIds.Contains(relicId))
        {
            run.RelicIds.Add(relicId);
            ApplyRelicEffect(run, relicId);
            ValidationEngine.UpdateRunHash(run);
        }
    }
    
    private void ApplyRelicEffect(RunState run, string relicId)
    {
        var relic = _db.GetRelic(relicId);
        if (relic == null) return;
        
        switch (relic.Effect)
        {
            case "MAX_ENERGY_BOOST":
                // Applied during combat initialization
                break;
            case "CURSED_GROWTH":
                run.HP = Math.Max(1, run.HP - 10);
                break;
        }
    }
    
    public void AddGold(RunState run, int amount)
    {
        run.Gold = Math.Min(9999, run.Gold + Math.Max(0, amount));
        ValidationEngine.UpdateRunHash(run);
    }
    
    public bool SpendGold(RunState run, int amount)
    {
        if (amount < 0 || amount > run.Gold) return false;
        
        run.Gold -= amount;
        ValidationEngine.UpdateRunHash(run);
        return true;
    }
    
    public void HealPlayer(RunState run, int amount)
    {
        run.HP = Math.Min(run.MaxHP, run.HP + Math.Max(0, amount));
        ValidationEngine.UpdateRunHash(run);
    }
    
    public void ApplyBlessing(RunState run, string blessingId)
    {
        var blessing = _db.GetBlessing(blessingId);
        if (blessing == null) return;
        
        run.BlessingIds.Add(blessingId);
        
        switch (blessing.Effect)
        {
            case "MAX_HP_BOOST":
                run.MaxHP += blessing.Value;
                run.HP += blessing.Value;
                break;
            case "GOLD_BOOST":
                run.Gold += blessing.Value;
                break;
            case "HAND_SIZE_BOOST":
                run.MaxHandSize += blessing.Value;
                break;
        }
        
        ValidationEngine.UpdateRunHash(run);
    }
    
    public void DamagePlayer(RunState run, int amount)
    {
        run.HP = Math.Max(0, run.HP - Math.Max(0, amount));
        ValidationEngine.UpdateRunHash(run);
    }
    
    public List<string> GetAvailableCards(string rarity, Random rng)
    {
        var cards = _db.Cards.Values
            .Where(c => c.Rarity == rarity && c.Rarity != "CURSE")
            .Select(c => c.Id)
            .OrderBy(_ => rng.Next())
            .Take(3)
            .ToList();
        
        return cards;
    }
    
    public string GetRandomRarity(Random rng)
    {
        var roll = rng.Next(100);
        if (roll < 5) return "EPIC";      // 5% chance
        if (roll < 25) return "RARE";     // 20% chance
        if (roll < 60) return "UNCOMMON"; // 35% chance
        return "COMMON";                   // 40% chance
    }
    
    public List<string> GetAvailableRelics(string rarity, Random rng)
    {
        var relics = _db.Relics.Values
            .Where(r => r.Rarity == rarity)
            .Select(r => r.Id)
            .OrderBy(_ => rng.Next())
            .Take(3)
            .ToList();
        
        return relics;
    }
}
