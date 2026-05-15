using Game.Definitions;
using Game.Models;
using Game.Data;

namespace Game;

public static class EffectEngine
{
    private static int _lastDamageDealt = 0; // For lifesteal tracking
    public static Action<int, int>? OnDamageDealt; // playerId, damage
    
    public static GameState ApplyEffect(GameState state, EffectDefinition effect, int actingPlayerId, Dictionary<string, CardDefinition>? cards = null)
    {
        var actor = state.Players.FirstOrDefault(p => p.PlayerId == actingPlayerId);
        var enemy = state.Players.FirstOrDefault(p => p.PlayerId != actingPlayerId);
        
        if (actor == null || enemy == null)
        {
            Console.WriteLine($"WARNING: ApplyEffect failed - actor or enemy is null");
            return state;
        }
        
        var target = effect.Target switch
        {
            "SELF" => actor,
            "ENEMY" => enemy,
            _ => enemy
        };

        Console.WriteLine($"Applying effect: {effect.Type} (value={effect.Value}, target={effect.Target}) from player {actingPlayerId}");
        _lastDamageDealt = 0;

        switch (effect.Type)
        {
            case "DAMAGE":
                ApplyDamage(state, target, effect.Value, actor);
                break;

            case "HEAL":
                ApplyHeal(target, effect.Value);
                break;

            case "DRAW":
                var rng = new Random(state.RNGSeed + state.TurnNumber + actor.CardsPlayedThisCombat);
                for (int i = 0; i < effect.Value && actor.Hand.Count < actor.MaxHandSize; i++)
                    DrawCard(actor, rng);
                break;

            case "SHIELD":
                ApplyShield(target, effect.Value);
                break;
                
            case "APPLY_STATUS":
                if (!string.IsNullOrEmpty(effect.StatusType))
                    ApplyStatus(target, effect.StatusType, effect.Value, effect.Duration);
                break;
                
            case "GAIN_ENERGY":
                actor.Energy = Math.Min(actor.MaxEnergy, actor.Energy + effect.Value);
                break;
                
            case "RESTORE_ENERGY":
                actor.Energy = Math.Min(actor.MaxEnergy, actor.Energy + effect.Value);
                break;
                
            case "EXECUTE":
                if (target.HP <= effect.Threshold)
                    ApplyDamage(state, target, effect.Damage, actor);
                break;
                
            case "LIFESTEAL":
                if (_lastDamageDealt > 0)
                {
                    var healAmount = (_lastDamageDealt * effect.Percentage) / 100;
                    ApplyHeal(actor, healAmount);
                }
                break;
                
            case "SUMMON":
                actor.MinionHP = Math.Max(actor.MinionHP, effect.Value);
                break;
                
            case "UNPLAYABLE":
                // Card does nothing (curse)
                break;
                
            case "BURN":
                ApplyStatus(target, "BURN", effect.Value, effect.Duration > 0 ? effect.Duration : 3);
                break;
                
            case "STUN":
                ApplyStatus(target, "STUN", 1, 1);
                break;
                
            case "DISCARD":
                DiscardCards(target, effect.Value, state.RNGSeed + state.TurnNumber);
                break;
                
            case "ENERGY_CHARGE":
                actor.MaxEnergy = Math.Min(10, actor.MaxEnergy + effect.Value);
                actor.Energy = actor.MaxEnergy;
                break;
                
            case "SILENT":
                ApplyStatus(target, "SILENT", 1, effect.Duration > 0 ? effect.Duration : 1);
                break;
                
            case "CURE":
                CureNegativeEffects(target, effect.Value);
                break;
                
            case "FEAR":
                ApplyStatus(target, "FEAR", effect.Value, effect.Duration > 0 ? effect.Duration : 1);
                break;
                
            case "ENERGY_SURGE":
                ApplyStatus(actor, "ENERGY_SURGE", effect.Value, 1);
                break;
                
            default:
                Console.WriteLine($"WARNING: Unhandled effect type: {effect.Type}");
                break;
        }

        // Check win condition
        if (state.Players.Any(p => p.HP <= 0))
        {
            state.IsGameOver = true;
            var winner = state.Players.FirstOrDefault(p => p.HP > 0);
            state.WinnerPlayerId = winner?.PlayerId;
        }

        return state;
    }
    
    private static void ApplyDamage(GameState state, PlayerState target, int baseDamage, PlayerState attacker)
    {
        // Apply strength modifier
        var strength = attacker.StatusEffects.FirstOrDefault(s => s.Type == "STRENGTH");
        var damage = baseDamage + (strength?.Value ?? 0);
        
        // Apply weak modifier
        var weak = attacker.StatusEffects.FirstOrDefault(s => s.Type == "WEAK");
        if (weak != null)
            damage = (damage * 75) / 100; // 25% reduction
        
        // Apply resistance modifiers (physical/magic)
        var physicalRes = target.StatusEffects.FirstOrDefault(s => s.Type == "PHYSICAL_RESISTANCE");
        var magicRes = target.StatusEffects.FirstOrDefault(s => s.Type == "MAGIC_RESISTANCE");
        
        if (physicalRes != null)
            damage = (damage * (100 - physicalRes.Value)) / 100;
        if (magicRes != null)
            damage = (damage * (100 - magicRes.Value)) / 100;
        
        // Prevent negative damage exploit
        damage = Math.Max(0, damage);
        
        // Check if minion absorbs damage
        if (target.MinionHP > 0)
        {
            var absorbed = Math.Min(target.MinionHP, damage);
            target.MinionHP -= absorbed;
            damage -= absorbed;
        }
        
        // Apply shield
        var shield = target.StatusEffects.FirstOrDefault(s => s.Type == "SHIELD");
        if (shield != null && damage > 0)
        {
            var absorbed = Math.Min(shield.Value, damage);
            shield.Value -= absorbed;
            damage -= absorbed;
            if (shield.Value <= 0) 
                target.StatusEffects.Remove(shield);
        }
        
        // Apply remaining damage
        if (damage > 0)
        {
            target.HP -= damage;
            target.HP = Math.Max(0, target.HP);
            target.DamageTakenThisCombat += damage;
            attacker.DamageDealtThisCombat += damage;
            OnDamageDealt?.Invoke(target.PlayerId, damage);
        }
        
        _lastDamageDealt = damage;
    }
    
    private static void ApplyHeal(PlayerState target, int amount)
    {
        // Prevent negative heal exploit
        amount = Math.Max(0, amount);
        target.HP = Math.Min(target.MaxHP, target.HP + amount);
    }
    
    private static void ApplyShield(PlayerState target, int amount)
    {
        // Prevent negative shield exploit
        amount = Math.Max(0, amount);
        
        var existing = target.StatusEffects.FirstOrDefault(s => s.Type == "SHIELD");
        if (existing != null)
        {
            existing.Value = Math.Min(existing.MaxStacks, existing.Value + amount);
        }
        else
        {
            target.StatusEffects.Add(new StatusEffect 
            { 
                Type = "SHIELD", 
                Value = Math.Min(amount, 99), 
                Duration = 999, // Shield persists until used
                MaxStacks = 99
            });
        }
    }
    
    private static void ApplyStatus(PlayerState target, string statusType, int value, int duration)
    {
        // Prevent negative values
        value = Math.Max(0, value);
        duration = Math.Max(0, duration);
        
        var existing = target.StatusEffects.FirstOrDefault(s => s.Type == statusType);
        if (existing != null)
        {
            // Stack value, refresh duration
            existing.Value = Math.Min(existing.MaxStacks, existing.Value + value);
            existing.Duration = Math.Max(existing.Duration, duration);
        }
        else
        {
            target.StatusEffects.Add(new StatusEffect
            {
                Type = statusType,
                Value = Math.Min(value, 99),
                Duration = duration,
                MaxStacks = 99
            });
        }
    }

    public static void DrawCard(PlayerState player, Random rng)
    {
        // Check if deck is empty, reshuffle discard if needed
        if (player.Deck.Count == 0 && player.Discard.Count > 0)
        {
            Console.WriteLine($"Player {player.PlayerId} reshuffling {player.Discard.Count} cards from discard into deck");
            player.Deck = player.Discard.OrderBy(_ => rng.Next()).ToList();
            player.Discard.Clear();
        }
        
        // If still no cards, can't draw
        if (player.Deck.Count == 0)
        {
            Console.WriteLine($"Player {player.PlayerId} has no cards to draw");
            return;
        }

        var card = player.Deck[0];
        player.Deck.RemoveAt(0);
        player.Hand.Add(card);
        Console.WriteLine($"Player {player.PlayerId} drew card: {card.CardId}");
    }
    
    public static void ProcessStartOfTurnEffects(PlayerState player)
    {
        // Process poison with resistance
        var poison = player.StatusEffects.FirstOrDefault(s => s.Type == "POISON");
        if (poison != null)
        {
            var poisonDamage = poison.Value;
            var poisonRes = player.StatusEffects.FirstOrDefault(s => s.Type == "POISON_RESISTANCE");
            if (poisonRes != null)
                poisonDamage = (poisonDamage * (100 - poisonRes.Value)) / 100;
            
            player.HP -= poisonDamage;
            player.HP = Math.Max(0, player.HP);
        }
        
        // Process burn damage with resistance
        var burn = player.StatusEffects.FirstOrDefault(s => s.Type == "BURN");
        if (burn != null)
        {
            var burnDamage = burn.Value;
            var burnRes = player.StatusEffects.FirstOrDefault(s => s.Type == "BURN_RESISTANCE");
            if (burnRes != null)
                burnDamage = (burnDamage * (100 - burnRes.Value)) / 100;
            
            player.HP -= burnDamage;
            player.HP = Math.Max(0, player.HP);
        }
        
        // Process regeneration
        var regen = player.StatusEffects.FirstOrDefault(s => s.Type == "REGENERATION");
        if (regen != null)
        {
            player.HP = Math.Min(player.MaxHP, player.HP + regen.Value);
        }
    }
    
    public static void TickDownStatusDurations(PlayerState player)
    {
        // Tick down durations
        foreach (var status in player.StatusEffects.ToList())
        {
            status.Duration--;
            if (status.Duration <= 0)
                player.StatusEffects.Remove(status);
        }
    }
    
    private static void DiscardCards(PlayerState player, int count, int seed)
    {
        var rng = new Random(seed);
        for (int i = 0; i < count && player.Hand.Count > 0; i++)
        {
            var index = rng.Next(player.Hand.Count);
            player.Discard.Add(player.Hand[index]);
            player.Hand.RemoveAt(index);
        }
    }
    
    private static void CureNegativeEffects(PlayerState player, int count)
    {
        var negativeEffects = player.StatusEffects
            .Where(s => s.Type == "POISON" || s.Type == "BURN" || s.Type == "WEAK" || s.Type == "STUN" || s.Type == "SILENT" || s.Type == "FEAR")
            .Take(count)
            .ToList();
        
        foreach (var effect in negativeEffects)
        {
            player.StatusEffects.Remove(effect);
        }
    }
}

