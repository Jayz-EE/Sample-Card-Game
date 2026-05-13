using Game.Definitions;
using Game.Models;
using Game.Data;

namespace Game;

public class GameEngine
{
    private readonly Database _db;
    private RunState? _currentRun;

    public GameEngine()
    {
        _db = Database.Instance;
    }
    
    private EffectDefinition ApplyRelicBoosts(EffectDefinition effect, PlayerState player)
    {
        if (_currentRun == null) return effect;
        
        var boostedEffect = new EffectDefinition
        {
            Type = effect.Type,
            Value = effect.Value,
            Target = effect.Target,
            StatusType = effect.StatusType,
            Duration = effect.Duration,
            Threshold = effect.Threshold,
            Damage = effect.Damage,
            Percentage = effect.Percentage
        };
        
        // Apply relic boosts
        if (effect.Type == "SHIELD" && _currentRun.RelicIds.Contains("iron_plate"))
        {
            boostedEffect.Value += 2;
        }
        
        if (effect.Type == "HEAL" && _currentRun.RelicIds.Contains("healing_pendant"))
        {
            boostedEffect.Value += 2;
        }
        
        if (effect.Type == "DAMAGE" && _currentRun.RelicIds.Contains("burning_amulet"))
        {
            boostedEffect.Value += 2;
        }
        
        return boostedEffect;
    }

    public GameState ProcessAction(GameState state, GameAction action)
    {
        if (state.IsGameOver) return state;
        
        // Validate action
        if (!ValidationEngine.ValidateAction(state, action)) return state;
        
        // Validate game state
        if (!ValidationEngine.ValidateGameState(state)) 
        {
            Console.WriteLine("WARNING: Invalid game state detected!");
            return state;
        }

        return action.Type switch
        {
            "DRAW_CARD" => HandleDraw(state, action),
            "PLAY_CARD" => HandlePlayCard(state, action),
            "USE_ITEM" => HandleUseItem(state, action),
            "END_TURN"  => HandleEndTurn(state, action),
            _ => state
        };
    }

    private GameState HandleDraw(GameState state, GameAction action)
    {
        if (state.Phase != "DRAW") return state;
        var player = state.Players.First(p => p.PlayerId == action.PlayerId);
        var rng = new Random(state.RNGSeed + state.TurnNumber);
        
        // Process start-of-turn effects (poison, regen, burn)
        EffectEngine.ProcessStartOfTurnEffects(player);
        
        // Draw up to max hand size at start of combat (turn 1), 1 card per turn after
        var maxHandSize = player.MaxHandSize;
        var cardsToDraw = state.TurnNumber == 1 ? maxHandSize : 1;
        
        for (int i = 0; i < cardsToDraw && player.Hand.Count < maxHandSize; i++)
        {
            EffectEngine.DrawCard(player, rng);
        }
        
        state.Phase = "MAIN";
        return state;
    }

    private GameState HandlePlayCard(GameState state, GameAction action)
    {
        if (state.Phase != "MAIN") return state;
        if (action.CardInstanceId == null) return state;

        var player = state.Players.First(p => p.PlayerId == action.PlayerId);
        
        // Check for STUN (can't play any cards)
        if (player.StatusEffects.Any(s => s.Type == "STUN"))
            return state;
        
        var cardInst = player.Hand.FirstOrDefault(c => c.InstanceId == action.CardInstanceId);
        if (cardInst == null) return state;

        var def = _db.GetCard(cardInst.CardId);
        if (def == null) return state;
        
        // Check for SILENT (can't play magic cards)
        if (player.StatusEffects.Any(s => s.Type == "SILENT") && def.Tags.Contains("MAGIC"))
            return state;
        
        // Check for UNPLAYABLE curse
        var effects = cardInst.IsUpgraded && def.UpgradedEffects.Count > 0 
            ? def.UpgradedEffects 
            : def.Effects;
            
        if (effects.Any(e => e.Type == "UNPLAYABLE")) return state;

        // Calculate cost with modifiers and ENERGY_SURGE
        var cost = def.Cost + cardInst.Modifiers
            .Where(m => m.Type == "COST_REDUCTION")
            .Sum(m => -m.Value);
            
        var energySurge = player.StatusEffects.FirstOrDefault(s => s.Type == "ENERGY_SURGE");
        if (energySurge != null)
            cost = Math.Max(0, cost - energySurge.Value);
            
        cost = Math.Max(0, cost); // Prevent negative cost exploit

        if (player.Energy < cost) return state;

        player.Energy -= cost;
        player.Hand.Remove(cardInst);
        player.Discard.Add(cardInst);
        player.CardsPlayedThisCombat++;

        // Apply effects with boosts from relics/artifacts
        foreach (var effect in effects)
        {
            var boostedEffect = ApplyRelicBoosts(effect, player);
            state = EffectEngine.ApplyEffect(state, boostedEffect, action.PlayerId);
        }
        
        // Apply arcana passive effects
        ApplyArcanaPassive(state, player, def);
        
        // Process FEAR (discard 1 card after playing)
        var fear = player.StatusEffects.FirstOrDefault(s => s.Type == "FEAR");
        if (fear != null && player.Hand.Count > 0)
        {
            var rng = new Random(state.RNGSeed + state.TurnNumber);
            var discardIndex = rng.Next(player.Hand.Count);
            player.Discard.Add(player.Hand[discardIndex]);
            player.Hand.RemoveAt(discardIndex);
        }

        return state;
    }
    
    private GameState HandleUseItem(GameState state, GameAction action)
    {
        if (state.Phase != "MAIN") return state;
        if (action.ItemId == null) return state;

        var player = state.Players.First(p => p.PlayerId == action.PlayerId);
        
        // Check if player has the item
        if (!player.Inventory.Contains(action.ItemId)) return state;
        
        var itemDef = _db.GetItem(action.ItemId);
        if (itemDef == null) return state;
        
        // Using an item costs ALL energy
        if (player.Energy < player.MaxEnergy) return state;
        
        player.Energy = 0;
        player.Inventory.Remove(action.ItemId);
        
        // Apply item effects
        foreach (var effect in itemDef.Effects)
        {
            state = EffectEngine.ApplyEffect(state, effect, action.PlayerId);
        }
        
        return state;
    }

    private GameState HandleEndTurn(GameState state, GameAction action)
    {
        var player = state.Players.First(p => p.PlayerId == action.PlayerId);

        // Discard excess cards if hand exceeds max hand size
        var rng = new Random(state.RNGSeed + state.TurnNumber);
        while (player.Hand.Count > player.MaxHandSize)
        {
            var cardToDiscard = player.Hand[rng.Next(player.Hand.Count)];
            player.Hand.Remove(cardToDiscard);
            player.Discard.Add(cardToDiscard);
        }

        // Summons attack before turn ends
        if (player.MinionHP > 0)
        {
            var opponent = state.Players.First(p => p.PlayerId != player.PlayerId);
            EffectEngine.ApplyEffect(state, new EffectDefinition 
            { 
                Type = "DAMAGE", 
                Value = player.MinionHP, 
                Target = "ENEMY" 
            }, player.PlayerId);
        }

        // Tick down status effect durations at end of turn
        EffectEngine.TickDownStatusDurations(player);

        // Switch turn
        state.CurrentTurnPlayerId = state.Players.First(p => p.PlayerId != action.PlayerId).PlayerId;
        state.TurnNumber++;
        state.Phase = "DRAW";

        // Restore energy for next player
        var next = state.Players.First(p => p.PlayerId == state.CurrentTurnPlayerId);
        next.Energy = next.MaxEnergy;

        return state;
    }
    
    private void ApplyArcanaPassive(GameState state, PlayerState player, CardDefinition card)
    {
        var arcana = _db.GetArcana(player.ArcanaId);
        if (arcana == null) return;
        
        switch (arcana.PassiveEffect)
        {
            case "FIRE_MASTERY":
                if (card.Tags.Contains("FIRE"))
                {
                    // Already applied via relic system
                }
                break;
            case "DIVINE_BLESSING":
                if (card.Tags.Contains("HEAL"))
                {
                    EffectEngine.ApplyEffect(state, new EffectDefinition 
                    { 
                        Type = "SHIELD", 
                        Value = 2, 
                        Target = "SELF" 
                    }, player.PlayerId);
                }
                break;
            case "SWIFT_HANDS":
                // Draw extra card at start of turn (handled elsewhere)
                break;
            case "IRON_WALL":
                // Shield boost (handled via relic system)
                break;
        }
    }
    
    public GameState InitializeCombat(RunState run, MapNode node)
    {
        _currentRun = run; // Store for relic boost checks
        
        var enemyId = node.EnemyId!;
        var enemy = _db.GetEnemy(enemyId);
        if (enemy == null) throw new ArgumentException($"Invalid enemy: {enemyId}");
        
        var arcana = _db.GetArcana(run.ArcanaId);
        if (arcana == null) throw new ArgumentException($"Invalid arcana: {run.ArcanaId}");
        
        var rng = new Random(run.Seed + run.TotalCombats);
        
        // Calculate max energy with relics and blessings
        var maxEnergy = run.MaxEnergy;
        if (run.RelicIds.Contains("energy_crystal")) maxEnergy++;
        if (run.BlessingIds.Contains("wisdom_blessing")) maxEnergy++;
        
        var state = new GameState
        {
            RNGSeed = run.Seed + run.TotalCombats,
            TurnNumber = 1,
            Phase = "DRAW",
            CurrentTurnPlayerId = 1,
            Players = new List<PlayerState>
            {
                CreatePlayerState(run, maxEnergy, rng),
                CreateEnemyState(enemy, run.CurrentFloor, rng)
            }
        };
        
        // Apply combat start blessings and artifacts
        var player = state.Players.First(p => p.PlayerId == 1);
        var enemyState = state.Players.First(p => p.PlayerId == 2);
        
        // Player artifacts
        ApplyArtifactEffects(run.ArtifactIds, player, enemyState);
        
        // Enemy artifact
        if (!string.IsNullOrEmpty(node.ArtifactId))
        {
            ApplyArtifactEffects(new List<string> { node.ArtifactId }, enemyState, player);
        }
        
        // Boss blessings - increase with each boss (floor 15, 30, 45, etc.)
        if (node.Type == "BOSS")
        {
            var bossNumber = run.CurrentFloor / 15; // 1st boss = 1, 2nd boss = 2, etc.
            ApplyBossBlessings(enemyState, bossNumber);
        }
        
        if (run.BlessingIds.Contains("protection_blessing"))
        {
            player.StatusEffects.Add(new StatusEffect 
            { 
                Type = "SHIELD", 
                Value = 5, 
                Duration = 1,
                MaxStacks = 99
            });
        }
        
        if (run.BlessingIds.Contains("strength_blessing"))
        {
            player.StatusEffects.Add(new StatusEffect 
            { 
                Type = "STRENGTH", 
                Value = 2, 
                Duration = 99,
                MaxStacks = 99
            });
        }
        
        return state;
    }
    
    private void ApplyBossBlessings(PlayerState boss, int blessingCount)
    {
        var rng = new Random(boss.PlayerId + blessingCount);
        var availableBlessings = new List<string> 
        { 
            "STRENGTH", "SHIELD", "REGENERATION", "ENERGY",
            "BURN_RESISTANCE", "POISON_RESISTANCE", "PHYSICAL_RESISTANCE", "MAGIC_RESISTANCE"
        };
        
        // Apply random blessings to boss
        for (int i = 0; i < blessingCount; i++)
        {
            var blessing = availableBlessings[rng.Next(availableBlessings.Count)];
            
            switch (blessing)
            {
                case "STRENGTH":
                    boss.StatusEffects.Add(new StatusEffect 
                    { 
                        Type = "STRENGTH", 
                        Value = 3, 
                        Duration = 99, 
                        MaxStacks = 99 
                    });
                    break;
                case "SHIELD":
                    boss.StatusEffects.Add(new StatusEffect 
                    { 
                        Type = "SHIELD", 
                        Value = 15, 
                        Duration = 1, 
                        MaxStacks = 99 
                    });
                    break;
                case "REGENERATION":
                    boss.StatusEffects.Add(new StatusEffect 
                    { 
                        Type = "REGENERATION", 
                        Value = 5, 
                        Duration = 99, 
                        MaxStacks = 99 
                    });
                    break;
                case "ENERGY":
                    boss.Energy += 1;
                    boss.MaxEnergy += 1;
                    break;
                case "BURN_RESISTANCE":
                    boss.StatusEffects.Add(new StatusEffect 
                    { 
                        Type = "BURN_RESISTANCE", 
                        Value = 50, 
                        Duration = 99, 
                        MaxStacks = 99 
                    });
                    break;
                case "POISON_RESISTANCE":
                    boss.StatusEffects.Add(new StatusEffect 
                    { 
                        Type = "POISON_RESISTANCE", 
                        Value = 50, 
                        Duration = 99, 
                        MaxStacks = 99 
                    });
                    break;
                case "PHYSICAL_RESISTANCE":
                    boss.StatusEffects.Add(new StatusEffect 
                    { 
                        Type = "PHYSICAL_RESISTANCE", 
                        Value = 25, 
                        Duration = 99, 
                        MaxStacks = 99 
                    });
                    break;
                case "MAGIC_RESISTANCE":
                    boss.StatusEffects.Add(new StatusEffect 
                    { 
                        Type = "MAGIC_RESISTANCE", 
                        Value = 25, 
                        Duration = 99, 
                        MaxStacks = 99 
                    });
                    break;
            }
        }
    }
    
    private void ApplyArtifactEffects(List<string> artifactIds, PlayerState owner, PlayerState opponent)
    {
        foreach (var artifactId in artifactIds)
        {
            var artifact = _db.GetArtifact(artifactId);
            if (artifact == null) continue;
            
            switch (artifact.Effect)
            {
                case "START_STRENGTH":
                    owner.StatusEffects.Add(new StatusEffect { Type = "STRENGTH", Value = artifact.Value, Duration = 99, MaxStacks = 99 });
                    break;
                case "START_SHIELD":
                    owner.StatusEffects.Add(new StatusEffect { Type = "SHIELD", Value = artifact.Value, Duration = 1, MaxStacks = 99 });
                    break;
                case "START_REGENERATION":
                    owner.StatusEffects.Add(new StatusEffect { Type = "REGENERATION", Value = artifact.Value, Duration = 3, MaxStacks = 99 });
                    break;
                case "START_ENERGY":
                    owner.Energy += artifact.Value;
                    break;
                case "START_POISON_ENEMY":
                    opponent.StatusEffects.Add(new StatusEffect { Type = "POISON", Value = artifact.Value, Duration = 3, MaxStacks = 99 });
                    break;
                case "START_BURN_ENEMY":
                    opponent.StatusEffects.Add(new StatusEffect { Type = "BURN", Value = artifact.Value, Duration = 3, MaxStacks = 99 });
                    break;
                case "MAX_HP_BOOST":
                    owner.MaxHP += artifact.Value;
                    owner.HP += artifact.Value;
                    break;
                case "CURSED_STRENGTH":
                    owner.StatusEffects.Add(new StatusEffect { Type = "STRENGTH", Value = artifact.Value, Duration = 99, MaxStacks = 99 });
                    owner.HP = Math.Max(1, owner.HP - 5);
                    break;
                case "CURSED_ENERGY":
                    owner.Energy += artifact.Value;
                    owner.StatusEffects.Add(new StatusEffect { Type = "BURN", Value = 3, Duration = 99, MaxStacks = 99 });
                    break;
                case "CURSED_DRAW":
                    owner.MaxHandSize += artifact.Value;
                    owner.MaxHP = Math.Max(10, owner.MaxHP - 10);
                    owner.HP = Math.Min(owner.HP, owner.MaxHP);
                    break;
                case "BLESSED_VITALITY":
                    owner.MaxHP += artifact.Value;
                    owner.HP += artifact.Value;
                    owner.StatusEffects.Add(new StatusEffect { Type = "REGENERATION", Value = 2, Duration = 5, MaxStacks = 99 });
                    break;
                case "BLESSED_POWER":
                    owner.StatusEffects.Add(new StatusEffect { Type = "STRENGTH", Value = artifact.Value, Duration = 99, MaxStacks = 99 });
                    owner.StatusEffects.Add(new StatusEffect { Type = "SHIELD", Value = 10, Duration = 1, MaxStacks = 99 });
                    break;
                case "BLESSED_FORTUNE":
                    owner.Energy += artifact.Value;
                    owner.MaxHandSize += 1;
                    break;
            }
        }
    }
    
    private PlayerState CreatePlayerState(RunState run, int maxEnergy, Random rng)
    {
        var deck = run.Deck
            .Select(c => new CardInstance
            {
                CardId = c.CardId,
                OwnerPlayerId = 1,
                IsUpgraded = c.IsUpgraded,
                Modifiers = new List<Modifier>(c.Modifiers)
            })
            .OrderBy(_ => rng.Next())
            .ToList();
        
        return new PlayerState
        {
            PlayerId = 1,
            Name = "Player",
            HP = run.HP,
            MaxHP = run.MaxHP,
            Energy = maxEnergy,
            MaxEnergy = maxEnergy,
            MaxHandSize = run.MaxHandSize,
            Deck = deck,
            Inventory = new List<string>(run.Inventory),
            ArcanaId = run.ArcanaId
        };
    }
    
    private PlayerState CreateEnemyState(EnemyDefinition enemy, int floor, Random rng)
    {
        var deck = enemy.Deck
            .Select(cardId => new CardInstance
            {
                CardId = cardId,
                OwnerPlayerId = 2
            })
            .OrderBy(_ => rng.Next())
            .ToList();
        
        // Add 1 Rare and 2 Uncommon cards from enemy card pool
        var enemyCardPool = GetEnemyCardPool();
        
        // Add 1 Rare
        var rareCards = enemyCardPool.Where(c => c.Rarity == "RARE").ToList();
        if (rareCards.Count > 0)
        {
            var randomRare = rareCards[rng.Next(rareCards.Count)];
            deck.Add(new CardInstance 
            { 
                CardId = randomRare.Id, 
                OwnerPlayerId = 2 
            });
        }
        
        // Add 2 Uncommon
        var uncommonCards = enemyCardPool.Where(c => c.Rarity == "UNCOMMON").ToList();
        for (int i = 0; i < 2; i++)
        {
            if (uncommonCards.Count > 0)
            {
                var randomUncommon = uncommonCards[rng.Next(uncommonCards.Count)];
                deck.Add(new CardInstance 
                { 
                    CardId = randomUncommon.Id, 
                    OwnerPlayerId = 2 
                });
            }
        }
        
        // Safety check: if deck is empty, add basic monster cards
        if (deck.Count == 0)
        {
            Console.WriteLine($"WARNING: Enemy {enemy.Name} has empty deck! Adding default cards.");
            deck = new List<CardInstance>
            {
                new CardInstance { CardId = "monster_slash", OwnerPlayerId = 2 },
                new CardInstance { CardId = "monster_slash", OwnerPlayerId = 2 },
                new CardInstance { CardId = "monster_defend", OwnerPlayerId = 2 },
                new CardInstance { CardId = "monster_bite", OwnerPlayerId = 2 }
            };
        }
        
        // Shuffle deck
        deck = deck.OrderBy(_ => rng.Next()).ToList();
        
        // Scale enemy stats based on floor level
        var level = floor;
        var hpMultiplier = 1.0f + (level - 1) * 0.15f; // +15% HP per floor
        var scaledHP = (int)(enemy.HP * hpMultiplier);
        
        // Add extra cards to deck every 3 floors
        var extraCards = (level - 1) / 3;
        for (int i = 0; i < extraCards && deck.Count > 0; i++)
        {
            var cardToCopy = deck[rng.Next(deck.Count)];
            deck.Add(new CardInstance 
            { 
                CardId = cardToCopy.CardId, 
                OwnerPlayerId = 2 
            });
        }
        
        // Increase energy every 5 floors
        var maxEnergy = 3 + (level - 1) / 5;
        
        return new PlayerState
        {
            PlayerId = 2,
            Name = enemy.Name,
            HP = scaledHP,
            MaxHP = scaledHP,
            Energy = maxEnergy,
            MaxEnergy = maxEnergy,
            Deck = deck
        };
    }
    
    private List<CardDefinition> GetEnemyCardPool()
    {
        return new List<CardDefinition>
        {
            // Common
            new CardDefinition { Id = "monster_slash", Rarity = "COMMON" },
            new CardDefinition { Id = "monster_bite", Rarity = "COMMON" },
            new CardDefinition { Id = "monster_defend", Rarity = "COMMON" },
            new CardDefinition { Id = "monster_claw", Rarity = "COMMON" },
            // Uncommon
            new CardDefinition { Id = "monster_roar", Rarity = "UNCOMMON" },
            new CardDefinition { Id = "monster_poison_spit", Rarity = "UNCOMMON" },
            new CardDefinition { Id = "monster_fire_breath", Rarity = "UNCOMMON" },
            new CardDefinition { Id = "monster_regenerate", Rarity = "UNCOMMON" },
            new CardDefinition { Id = "monster_frost_nova", Rarity = "UNCOMMON" },
            new CardDefinition { Id = "monster_lightning_bolt", Rarity = "UNCOMMON" },
            new CardDefinition { Id = "monster_earth_spike", Rarity = "UNCOMMON" },
            new CardDefinition { Id = "monster_wind_slash", Rarity = "UNCOMMON" },
            new CardDefinition { Id = "monster_barrier", Rarity = "UNCOMMON" },
            new CardDefinition { Id = "monster_meditation", Rarity = "UNCOMMON" },
            new CardDefinition { Id = "monster_curse", Rarity = "UNCOMMON" },
            // Rare
            new CardDefinition { Id = "monster_heavy_strike", Rarity = "RARE" },
            new CardDefinition { Id = "monster_drain_life", Rarity = "RARE" },
            new CardDefinition { Id = "monster_fortify", Rarity = "RARE" },
            new CardDefinition { Id = "monster_rampage", Rarity = "RARE" },
            new CardDefinition { Id = "monster_berserk", Rarity = "RARE" },
            new CardDefinition { Id = "monster_soul_drain", Rarity = "RARE" },
            new CardDefinition { Id = "monster_stone_skin", Rarity = "RARE" },
            new CardDefinition { Id = "monster_plague", Rarity = "RARE" },
            new CardDefinition { Id = "monster_holy_light", Rarity = "RARE" },
            // Epic
            new CardDefinition { Id = "monster_arcane_bolt", Rarity = "EPIC" },
            new CardDefinition { Id = "monster_crushing_blow", Rarity = "EPIC" },
            new CardDefinition { Id = "monster_dark_ritual", Rarity = "EPIC" },
            new CardDefinition { Id = "monster_iron_skin", Rarity = "EPIC" },
            new CardDefinition { Id = "monster_mana_drain", Rarity = "EPIC" },
            new CardDefinition { Id = "monster_blood_pact", Rarity = "EPIC" },
            new CardDefinition { Id = "monster_venom_strike", Rarity = "EPIC" },
            new CardDefinition { Id = "monster_inferno", Rarity = "EPIC" },
            new CardDefinition { Id = "monster_battle_cry", Rarity = "EPIC" },
            new CardDefinition { Id = "monster_shadow_step", Rarity = "EPIC" },
            new CardDefinition { Id = "monster_chain_lightning", Rarity = "EPIC" },
            new CardDefinition { Id = "monster_earthquake", Rarity = "EPIC" },
            new CardDefinition { Id = "monster_void_blast", Rarity = "EPIC" },
            new CardDefinition { Id = "monster_titan_strength", Rarity = "EPIC" },
            new CardDefinition { Id = "monster_phoenix_rebirth", Rarity = "EPIC" },
            new CardDefinition { Id = "monster_absolute_zero", Rarity = "EPIC" },
            new CardDefinition { Id = "monster_arcane_mastery", Rarity = "EPIC" },
            new CardDefinition { Id = "monster_war_cry", Rarity = "EPIC" },
            // Legendary
            new CardDefinition { Id = "monster_meteor_strike", Rarity = "LEGENDARY" },
            new CardDefinition { Id = "monster_annihilate", Rarity = "LEGENDARY" },
            new CardDefinition { Id = "monster_divine_shield", Rarity = "LEGENDARY" },
            new CardDefinition { Id = "monster_life_steal", Rarity = "LEGENDARY" },
            new CardDefinition { Id = "monster_apocalypse", Rarity = "LEGENDARY" },
            new CardDefinition { Id = "monster_death_coil", Rarity = "LEGENDARY" },
            new CardDefinition { Id = "monster_time_stop", Rarity = "LEGENDARY" },
            new CardDefinition { Id = "monster_immortality", Rarity = "LEGENDARY" },
            new CardDefinition { Id = "monster_chaos_storm", Rarity = "LEGENDARY" },
            new CardDefinition { Id = "monster_god_hand", Rarity = "LEGENDARY" },
            new CardDefinition { Id = "monster_demon_pact", Rarity = "LEGENDARY" },
            new CardDefinition { Id = "monster_celestial_blessing", Rarity = "LEGENDARY" },
            new CardDefinition { Id = "monster_reality_warp", Rarity = "LEGENDARY" },
            new CardDefinition { Id = "monster_oblivion", Rarity = "LEGENDARY" },
            new CardDefinition { Id = "monster_eternal_fortress", Rarity = "LEGENDARY" }
        };
    }
    
    private string GetRandomEnemyCardRarity(Random rng)
    {
        var roll = rng.Next(100);
        if (roll < 45) return "COMMON";      // 45%
        if (roll < 70) return "UNCOMMON";    // 25%
        if (roll < 85) return "RARE";        // 15%
        if (roll < 95) return "EPIC";        // 10%
        return "LEGENDARY";                   // 5%
    }
}

