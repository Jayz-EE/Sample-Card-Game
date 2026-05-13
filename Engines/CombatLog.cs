namespace Game;

public class CombatLog
{
    public List<string> Entries { get; } = new();
    
    public void Add(string message) => Entries.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
    
    public void Clear() => Entries.Clear();
    
    public void Display(int count = 5)
    {
        var recent = Entries.TakeLast(count);
        foreach (var entry in recent)
            Console.WriteLine($"  {entry}");
    }
}
