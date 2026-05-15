using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Game.Models;
using Game.Data;

namespace Game;

/// <summary>
/// Validates game state to prevent exploits and cheating
/// </summary>
public static class ValidationEngine
{
    private const int MAX_HAND_SIZE = 20;
    private const int MAX_DECK_SIZE = 100;
    private const int MAX_ENERGY = 20;
    private const int MAX_HP = 999;
    private const int MAX_GOLD = 9999;
    private const int MAX_STATUS_STACKS = 99;
    
    public static bool ValidateRunState(RunState run)
    {
        var db = Database.Instance;
        
        // Validate basic bounds
        if (run.Gold < 0 || run.Gold > MAX_GOLD)
        {
            Console.WriteLine($"Validation failed: Gold {run.Gold} out of bounds (0-{MAX_GOLD})");
            return false;
        }
        if (run.HP < 0 || run.HP > MAX_HP)
        {
            Console.WriteLine($"Validation failed: HP {run.HP} out of bounds (0-{MAX_HP})");
            return false;
        }
        if (run.MaxHP < 1 || run.MaxHP > MAX_HP)
        {
            Console.WriteLine($"Validation failed: MaxHP {run.MaxHP} out of bounds (1-{MAX_HP})");
            return false;
        }
        if (run.HP > run.MaxHP)
        {
            Console.WriteLine($"Validation failed: HP {run.HP} > MaxHP {run.MaxHP}");
            return false;
        }
        if (run.CurrentFloor < 1 || run.CurrentFloor > run.MaxFloor)
        {
            Console.WriteLine($"Validation failed: CurrentFloor {run.CurrentFloor} out of bounds (1-{run.MaxFloor})");
            return false;
        }
        
        // Validate deck
        if (run.Deck.Count > MAX_DECK_SIZE)
        {
            Console.WriteLine($"Validation failed: Deck size {run.Deck.Count} exceeds max {MAX_DECK_SIZE}");
            return false;
        }
        foreach (var card in run.Deck)
        {
            if (!db.IsValidCard(card.CardId))
            {
                Console.WriteLine($"Validation failed: Invalid card ID {card.CardId}");
                return false;
            }
            if (card.OwnerPlayerId != 1)
            {
                Console.WriteLine($"Validation failed: Card {card.CardId} has wrong owner {card.OwnerPlayerId}");
                return false;
            }
        }
        
        // Validate relics
        foreach (var relicId in run.RelicIds)
        {
            if (!db.IsValidRelic(relicId))
            {
                Console.WriteLine($"Validation failed: Invalid relic ID {relicId}");
                return false;
            }
        }
        
        // Validate arcana
        if (!db.IsValidArcana(run.ArcanaId))
        {
            Console.WriteLine($"Validation failed: Invalid arcana ID {run.ArcanaId}");
            return false;
        }
        
        // Check for duplicate card instances (exploit prevention)
        var instanceIds = run.Deck.Select(c => c.InstanceId).ToList();
        if (instanceIds.Count != instanceIds.Distinct().Count())
        {
            Console.WriteLine($"Validation failed: Duplicate card instances detected");
            return false;
        }
        
        Console.WriteLine("Run state validation passed");
        return true;
    }
    
    public static bool ValidateGameState(GameState state)
    {
        foreach (var player in state.Players)
        {
            // Validate energy
            if (player.Energy < 0 || player.Energy > MAX_ENERGY)
            {
                Console.WriteLine($"Invalid energy for player {player.PlayerId}: {player.Energy}");
                return false;
            }
            if (player.MaxEnergy < 1 || player.MaxEnergy > MAX_ENERGY)
            {
                Console.WriteLine($"Invalid max energy for player {player.PlayerId}: {player.MaxEnergy}");
                return false;
            }
            
            // Validate HP
            if (player.HP < 0 || player.HP > MAX_HP)
            {
                Console.WriteLine($"Invalid HP for player {player.PlayerId}: {player.HP}");
                return false;
            }
            if (player.MaxHP < 1 || player.MaxHP > MAX_HP)
            {
                Console.WriteLine($"Invalid max HP for player {player.PlayerId}: {player.MaxHP}");
                return false;
            }
            
            // Validate hand size
            if (player.Hand.Count > MAX_HAND_SIZE)
            {
                Console.WriteLine($"Invalid hand size for player {player.PlayerId}: {player.Hand.Count}");
                return false;
            }
            
            // Validate deck size
            var totalCards = player.Deck.Count + player.Hand.Count + player.Discard.Count + player.Exhaust.Count;
            if (totalCards > MAX_DECK_SIZE)
            {
                Console.WriteLine($"Invalid total cards for player {player.PlayerId}: {totalCards}");
                return false;
            }
            
            // Check for duplicate instances across all zones
            var allCards = player.Deck.Concat(player.Hand).Concat(player.Discard).Concat(player.Exhaust).ToList();
            var instanceIds = allCards.Select(c => c.InstanceId).ToList();
            if (instanceIds.Count != instanceIds.Distinct().Count())
            {
                Console.WriteLine($"Duplicate card instances found for player {player.PlayerId}");
                return false;
            }
            
            // Validate status effects
            foreach (var status in player.StatusEffects)
            {
                if (status.Value < 0 || status.Value > MAX_STATUS_STACKS)
                {
                    Console.WriteLine($"Invalid status value for player {player.PlayerId}: {status.Type}={status.Value}");
                    return false;
                }
                if (status.Duration < 0 || status.Duration > 999)
                {
                    Console.WriteLine($"Invalid status duration for player {player.PlayerId}: {status.Type} duration={status.Duration}");
                    return false;
                }
            }
        }
        
        return true;
    }
    
    public static bool ValidateAction(GameState state, GameAction action)
    {
        // Validate player ID
        if (action.PlayerId < 1 || action.PlayerId > state.Players.Count) return false;
        
        // Validate turn order
        if (action.PlayerId != state.CurrentTurnPlayerId) return false;
        
        // Validate action type
        var validActions = new[] { "PLAY_CARD", "END_TURN", "DRAW_CARD", "USE_ITEM" };
        if (!validActions.Contains(action.Type)) return false;
        
        return true;
    }
    
    public static string GenerateRunHash(RunState run)
    {
        // Create deterministic hash of run state to detect tampering
        var data = new
        {
            run.RunId,
            run.Seed,
            run.CurrentFloor,
            run.Gold,
            run.HP,
            run.MaxHP,
            DeckIds = run.Deck.Select(c => c.CardId).OrderBy(x => x).ToList(),
            RelicIds = run.RelicIds.OrderBy(x => x).ToList(),
            run.TotalCombats,
            run.TotalDamageDealt
        };
        
        var json = JsonSerializer.Serialize(data);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(bytes);
    }
    
    public static bool VerifyRunHash(RunState run)
    {
        var currentHash = GenerateRunHash(run);
        return currentHash == run.ValidationHash;
    }
    
    public static void UpdateRunHash(RunState run)
    {
        run.ValidationHash = GenerateRunHash(run);
    }
}
