namespace Game.Models;

public class GameState
{
    public List<PlayerState> Players { get; set; } = new();

    public int CurrentTurnPlayerId { get; set; }

    public string Phase { get; set; } = "DRAW"; // DRAW, MAIN, END

    public int TurnNumber { get; set; }

    public int RNGSeed { get; set; }

    public bool IsGameOver { get; set; }
    public int? WinnerPlayerId { get; set; }
}
