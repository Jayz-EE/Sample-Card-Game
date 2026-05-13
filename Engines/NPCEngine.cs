using Game.Data;
using Game.Definitions;
using Game.Models;

namespace Game;

public class NPCEngine
{
    private readonly Database _db;
    private readonly RunManager _manager;
    
    public NPCEngine(RunManager manager)
    {
        _db = Database.Instance;
        _manager = manager;
    }
    
    public void ProcessNPCChoice(RunState run, string npcId, int choiceIndex, Random rng)
    {
        var npc = _db.GetNPC(npcId);
        if (npc == null || choiceIndex < 0 || choiceIndex >= npc.Choices.Count) return;
        
        var choice = npc.Choices[choiceIndex];
        
        // Check costs
        if (choice.GoldCost > 0)
        {
            if (run.Gold < choice.GoldCost) return;
            run.Gold -= choice.GoldCost;
        }
        
        if (choice.HpCost > 0)
        {
            run.HP -= choice.HpCost;
            if (run.HP < 0) run.HP = 0;
        }
        
        // Apply effects
        foreach (var effect in choice.Effects)
        {
            ApplyNPCEffect(run, effect, rng);
        }
    }
    
    private void ApplyNPCEffect(RunState run, EffectDefinition effect, Random rng)
    {
        switch (effect.Type)
        {
            case "GAIN_CARD":
                var cards = _manager.GetAvailableCards(effect.Rarity ?? "COMMON", rng);
                if (cards.Count > 0)
                {
                    var card = cards[rng.Next(cards.Count)];
                    _manager.AddCardToRun(run, card);
                }
                break;
                
            case "GAIN_RELIC":
                var relics = _manager.GetAvailableRelics(effect.Rarity ?? "COMMON", rng);
                if (relics.Count > 0)
                {
                    var relic = relics[rng.Next(relics.Count)];
                    run.RelicIds.Add(relic);
                }
                break;
                
            case "GAIN_GOLD":
                run.Gold += effect.Value;
                break;
                
            case "HEAL":
                var healAmount = Math.Min(effect.Value, run.MaxHP - run.HP);
                run.HP += healAmount;
                break;
                
            case "DAMAGE":
                run.HP -= effect.Value;
                if (run.HP < 0) run.HP = 0;
                break;
                
            case "GAIN_MAX_HP":
                run.MaxHP += effect.Value;
                run.HP += effect.Value;
                break;
                
            case "REMOVE_CARD":
                RemoveCard(run);
                break;
                
            case "UPGRADE_CARD":
                UpgradeCard(run);
                break;
                
            case "UPGRADE_RANDOM_CARD":
                UpgradeRandomCard(run, rng);
                break;
                
            case "RANDOM_OUTCOME":
                ProcessRandomOutcome(run, effect, rng);
                break;
        }
    }
    
    private void RemoveCard(RunState run)
    {
        if (run.Deck.Count == 0) return;
        run.Deck.RemoveAt(0);
    }
    
    private void UpgradeCard(RunState run)
    {
        var upgradeable = run.Deck.Where(c => !c.IsUpgraded).ToList();
        if (upgradeable.Count == 0) return;
        upgradeable[0].IsUpgraded = true;
    }
    
    private void UpgradeRandomCard(RunState run, Random rng)
    {
        var upgradeable = run.Deck.Where(c => !c.IsUpgraded).ToList();
        if (upgradeable.Count == 0)
        {
            return;
        }
        
        var card = upgradeable[rng.Next(upgradeable.Count)];
        card.IsUpgraded = true;
    }
    
    private void ProcessRandomOutcome(RunState run, EffectDefinition effect, Random rng)
    {
        if (effect.Outcomes == null || effect.Outcomes.Count == 0) return;
        
        var totalWeight = effect.Outcomes.Sum(o => o.Weight);
        var roll = rng.Next(totalWeight);
        var cumulative = 0;
        
        foreach (var outcome in effect.Outcomes)
        {
            cumulative += outcome.Weight;
            if (roll < cumulative)
            {
                foreach (var outcomeEffect in outcome.Effects)
                {
                    ApplyNPCEffect(run, outcomeEffect, rng);
                }
                break;
            }
        }
    }
}
