using Game.Models;
using Game.Data;
using Game.Definitions;

namespace Game;

public class AIEngine
{
    private readonly Database _db;
    private readonly GameEngine _engine;
    public CombatLog Log { get; } = new();
    
    public AIEngine(GameEngine engine)
    {
        _db = Database.Instance;
        _engine = engine;
    }
    
    public GameState ExecuteAITurn(GameState state, int aiPlayerId, string aiPattern, string enemyId)
    {
        var ai = state.Players.FirstOrDefault(p => p.PlayerId == aiPlayerId);
        var player = state.Players.FirstOrDefault(p => p.PlayerId != aiPlayerId);
        
        if (ai == null || player == null) return state;
        
        var enemy = _db.GetEnemy(enemyId);
        var rng = new Random(state.RNGSeed + state.TurnNumber);
        
        // Subscribe to damage events for hurt lines
        var prevHp = ai.HP;
        EffectEngine.OnDamageDealt = (targetId, damage) =>
        {
            if (targetId == aiPlayerId && damage > 0 && enemy?.HurtLines.Count > 0)
                Log.Add($"{enemy.Name}: \"{enemy.HurtLines[rng.Next(enemy.HurtLines.Count)]}\"");
        };
        
        // Random dialogue at turn start
        if (enemy?.Dialogue.Count > 0 && rng.Next(100) < 30)
            Log.Add($"{enemy.Name}: \"{enemy.Dialogue[rng.Next(enemy.Dialogue.Count)]}\"");
        
        // Draw 3 cards at start of turn
        for (int i = 0; i < 3; i++)
        {
            state = _engine.ProcessAction(state, new GameAction 
            { 
                Type = "DRAW_CARD", 
                PlayerId = aiPlayerId 
            });
            
            if (state.IsGameOver) return state;
        }
        
        // If AI has no cards after drawing, just end turn
        if (ai.Hand.Count == 0)
        {
            Log.Add($"{enemy?.Name ?? "Enemy"} has no cards to play");
            state = _engine.ProcessAction(state, new GameAction 
            { 
                Type = "END_TURN", 
                PlayerId = aiPlayerId 
            });
            EffectEngine.OnDamageDealt = null;
            return state;
        }
        
        // Play cards based on AI pattern
        var cardsToPlay = SelectCardsToPlay(state, ai, player, aiPattern);
        
        foreach (var cardInstanceId in cardsToPlay)
        {
            if (state.IsGameOver) break;
            
            var card = ai.Hand.FirstOrDefault(c => c.InstanceId == cardInstanceId);
            if (card != null)
            {
                var cardDef = _db.GetCard(card.CardId);
                var actionLine = GetActionLine(cardDef, enemy, rng);
                Log.Add($"{enemy?.Name ?? "Enemy"} {actionLine}");
            }
            
            state = _engine.ProcessAction(state, new GameAction
            {
                Type = "PLAY_CARD",
                PlayerId = aiPlayerId,
                CardInstanceId = cardInstanceId,
                TargetPlayerId = player.PlayerId
            });
        }
        
        // End turn
        if (!state.IsGameOver)
        {
            state = _engine.ProcessAction(state, new GameAction 
            { 
                Type = "END_TURN", 
                PlayerId = aiPlayerId 
            });
        }
        
        // Cleanup event subscription
        EffectEngine.OnDamageDealt = null;
        
        return state;
    }
    
    private string GetActionLine(CardDefinition? cardDef, EnemyDefinition? enemy, Random rng)
    {
        if (cardDef == null || enemy == null) return "acts";
        
        var hasAttack = cardDef.Effects.Any(e => e.Type == "DAMAGE");
        
        if (hasAttack && enemy.AttackLines.Count > 0)
            return enemy.AttackLines[rng.Next(enemy.AttackLines.Count)];
        
        return cardDef.Tags.Contains("ATTACK") ? "attacks" : $"plays {cardDef.Name}";
    }
    
    private List<string> SelectCardsToPlay(GameState state, PlayerState ai, PlayerState player, string pattern)
    {
        var cardsToPlay = new List<string>();
        var availableEnergy = ai.Energy;
        var hand = new List<CardInstance>(ai.Hand);
        var hasPlayedDraw = false;
        
        // Play cards until energy is exhausted
        while (availableEnergy > 0 && hand.Count > 0)
        {
            CardInstance? selectedCard = null;
            
            // Priority 1: Draw cards first (once per turn for utility)
            if (!hasPlayedDraw && pattern != "DEFENSIVE")
            {
                selectedCard = hand
                    .Where(c => GetCardCost(c, ai) <= availableEnergy)
                    .FirstOrDefault(c => HasEffect(c, "DRAW"));
                
                if (selectedCard != null)
                    hasPlayedDraw = true;
            }
            
            // Priority 2: Pattern-specific card selection
            if (selectedCard == null)
            {
                selectedCard = pattern switch
                {
                    "AGGRESSIVE" => SelectAggressiveCard(hand, availableEnergy, player, ai),
                    "DEFENSIVE" => SelectDefensiveCard(hand, availableEnergy, ai),
                    "BOSS" => SelectBossCard(hand, availableEnergy, ai, player),
                    _ => SelectBalancedCard(hand, availableEnergy, ai)
                };
            }
            
            if (selectedCard == null)
                break;
            
            var cost = GetCardCost(selectedCard, ai);
            cardsToPlay.Add(selectedCard.InstanceId);
            availableEnergy -= cost;
            hand.Remove(selectedCard);
        }
        
        return cardsToPlay;
    }
    
    private CardInstance? SelectAggressiveCard(List<CardInstance> hand, int energy, PlayerState player, PlayerState ai)
    {
        // Prioritize: Execute if low HP > Damage > Poison/Burn > Other
        return hand
            .Where(c => GetCardCost(c, ai) <= energy)
            .OrderByDescending(c => 
            {
                if (player.HP <= 20 && HasEffect(c, "EXECUTE")) return 1000;
                if (HasEffect(c, "DAMAGE")) return GetEffectValue(c, "DAMAGE") * 10;
                if (HasEffect(c, "POISON") || HasEffect(c, "BURN")) return 50;
                return 1;
            })
            .FirstOrDefault();
    }
    
    private CardInstance? SelectDefensiveCard(List<CardInstance> hand, int energy, PlayerState ai)
    {
        // Prioritize: Heal if low HP > Shield > Regeneration > Damage
        var hpPercent = (ai.HP * 100) / ai.MaxHP;
        
        return hand
            .Where(c => GetCardCost(c, ai) <= energy)
            .OrderByDescending(c =>
            {
                if (hpPercent < 40 && HasEffect(c, "HEAL")) return GetEffectValue(c, "HEAL") * 20;
                if (HasEffect(c, "SHIELD")) return GetEffectValue(c, "SHIELD") * 10;
                if (HasEffect(c, "REGENERATION")) return 50;
                if (HasEffect(c, "DAMAGE")) return GetEffectValue(c, "DAMAGE") * 5;
                return 1;
            })
            .FirstOrDefault();
    }
    
    private CardInstance? SelectBossCard(List<CardInstance> hand, int energy, PlayerState ai, PlayerState player)
    {
        // Smart balanced play
        var hpPercent = (ai.HP * 100) / ai.MaxHP;
        
        return hand
            .Where(c => GetCardCost(c, ai) <= energy)
            .OrderByDescending(c =>
            {
                var score = 0;
                if (hpPercent < 50 && HasEffect(c, "HEAL")) score += GetEffectValue(c, "HEAL") * 15;
                if (hpPercent < 70 && HasEffect(c, "SHIELD")) score += GetEffectValue(c, "SHIELD") * 8;
                if (HasEffect(c, "DAMAGE")) score += GetEffectValue(c, "DAMAGE") * 10;
                if (HasEffect(c, "DRAW")) score += 30;
                if (HasEffect(c, "STRENGTH")) score += 40;
                return score;
            })
            .FirstOrDefault();
    }
    
    private CardInstance? SelectBalancedCard(List<CardInstance> hand, int energy, PlayerState ai)
    {
        // Play lowest cost card
        return hand
            .Where(c => GetCardCost(c, ai) <= energy)
            .OrderBy(c => GetCardCost(c, ai))
            .FirstOrDefault();
    }
    
    private bool HasEffect(CardInstance card, string effectType)
    {
        var def = _db.GetCard(card.CardId);
        if (def == null) return false;
        
        var effects = card.IsUpgraded && def.UpgradedEffects.Count > 0 
            ? def.UpgradedEffects 
            : def.Effects;
        
        return effects.Any(e => e.Type == effectType || e.StatusType == effectType);
    }
    
    private int GetEffectValue(CardInstance card, string effectType)
    {
        var def = _db.GetCard(card.CardId);
        if (def == null) return 0;
        
        var effects = card.IsUpgraded && def.UpgradedEffects.Count > 0 
            ? def.UpgradedEffects 
            : def.Effects;
        
        return effects
            .Where(e => e.Type == effectType || e.StatusType == effectType)
            .Sum(e => e.Value);
    }
    
    private int GetCardCost(CardInstance card, PlayerState? owner = null)
    {
        var def = _db.GetCard(card.CardId);
        if (def == null) return 999;
        
        var cost = def.Cost + card.Modifiers
            .Where(m => m.Type == "COST_REDUCTION")
            .Sum(m => -m.Value);
        
        // Apply SLOW debuff
        if (owner != null)
        {
            var slow = owner.StatusEffects.FirstOrDefault(s => s.Type == "SLOW");
            if (slow != null)
                cost += slow.Value;
        }
        
        return Math.Max(0, cost);
    }
}
