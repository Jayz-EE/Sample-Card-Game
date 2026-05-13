using Game.Models;
using Game.Data;
using Game.Definitions;

namespace Game;

public class EventEngine
{
    private readonly Database _db;
    private readonly RunManager _runManager;
    
    public EventEngine(RunManager runManager)
    {
        _db = Database.Instance;
        _runManager = runManager;
    }
    
    public void ProcessEventChoice(RunState run, string eventId, int choiceIndex, Random rng)
    {
        var eventDef = _db.GetEvent(eventId);
        if (eventDef == null) return;
        
        if (choiceIndex < 0 || choiceIndex >= eventDef.Choices.Count) return;
        
        var choice = eventDef.Choices[choiceIndex];
        
        foreach (var effect in choice.Effects)
        {
            ProcessEventEffect(run, effect, rng);
        }
        
        ValidationEngine.UpdateRunHash(run);
    }
    
    public void ProcessEffect(RunState run, EffectDefinition effect, Random rng)
    {
        ProcessEventEffect(run, effect, rng);
        ValidationEngine.UpdateRunHash(run);
    }
    
    private void ProcessEventEffect(RunState run, EffectDefinition effect, Random rng)
    {
        switch (effect.Type)
        {
            case "DAMAGE":
                if (effect.Target == "SELF")
                    _runManager.DamagePlayer(run, effect.Value);
                break;
                
            case "HEAL":
                _runManager.HealPlayer(run, effect.Value);
                break;
                
            case "HEAL_FULL":
                run.HP = run.MaxHP;
                break;
                
            case "GAIN_GOLD":
                _runManager.AddGold(run, effect.Value);
                break;
                
            case "LOSE_GOLD":
                _runManager.SpendGold(run, effect.Value);
                break;
                
            case "GAIN_RELIC":
                var relics = _runManager.GetAvailableRelics(effect.Rarity ?? "COMMON", rng);
                if (relics.Count > 0)
                    _runManager.AddRelic(run, relics[0]);
                break;
                
            case "GAIN_CARD":
                var cards = _runManager.GetAvailableCards(effect.Rarity ?? "COMMON", rng);
                if (cards.Count > 0)
                    _runManager.AddCardToRun(run, cards[0]);
                break;
                
            case "ADD_CURSE":
                var curses = new[] { "blight", "despair" };
                var curse = curses[rng.Next(curses.Length)];
                _runManager.AddCardToRun(run, curse);
                break;
                
            case "REMOVE_CARD":
                // Handled by UI selection
                break;
                
            case "UPGRADE_CARD":
                // Handled by UI selection
                break;
                
            case "UPGRADE_RANDOM_CARD":
                var upgradeable = run.Deck.Where(c => !c.IsUpgraded).ToList();
                if (upgradeable.Count > 0)
                {
                    var card = upgradeable[rng.Next(upgradeable.Count)];
                    _runManager.UpgradeCard(run, card.InstanceId);
                }
                break;
                
            case "LOSE_MAX_HP":
                run.MaxHP = Math.Max(1, run.MaxHP - effect.Value);
                run.HP = Math.Min(run.HP, run.MaxHP);
                break;
                
            case "RANDOM_OUTCOME":
                if (effect.Outcomes != null && effect.Outcomes.Count > 0)
                {
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
                                ProcessEventEffect(run, outcomeEffect, rng);
                            }
                            break;
                        }
                    }
                }
                break;
        }
    }
    
    public void ProcessShop(RunState run, string itemType, string itemId, int cost)
    {
        if (!_runManager.SpendGold(run, cost)) return;
        
        switch (itemType)
        {
            case "CARD":
                if (_db.IsValidCard(itemId))
                    _runManager.AddCardToRun(run, itemId);
                break;
                
            case "RELIC":
                if (_db.IsValidRelic(itemId))
                    _runManager.AddRelic(run, itemId);
                break;
                
            case "REMOVE":
                _runManager.RemoveCardFromRun(run, itemId);
                break;
        }
    }
    
    public void ProcessRest(RunState run, string action)
    {
        switch (action)
        {
            case "HEAL":
                var healAmount = run.MaxHP / 3;
                _runManager.HealPlayer(run, healAmount);
                break;
                
            case "UPGRADE":
                // Handled by UI selection
                break;
        }
    }
}
