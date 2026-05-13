namespace Game.Models;

public class GameAction
{
    public string Type { get; set; } = ""; // PLAY_CARD, END_TURN, DRAW_CARD, USE_ITEM

    public int PlayerId { get; set; }

    public string? CardInstanceId { get; set; }
    
    public string? ItemId { get; set; }

    public int? TargetPlayerId { get; set; }
}
