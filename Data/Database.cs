using System.Text.Json;
using Game.Definitions;

namespace Game.Data;

public class Database
{
    private static Database? _instance;
    private static readonly object _lock = new();
    
    public Dictionary<string, CardDefinition> Cards { get; private set; } = new();
    public Dictionary<string, ArcanaDefinition> Arcanas { get; private set; } = new();
    public Dictionary<string, EnemyDefinition> Enemies { get; private set; } = new();
    public Dictionary<string, RelicDefinition> Relics { get; private set; } = new();
    public Dictionary<string, RelicDefinition> Artifacts { get; private set; } = new();
    public Dictionary<string, EventDefinition> Events { get; private set; } = new();
    public Dictionary<string, NPCDefinition> NPCs { get; private set; } = new();
    public Dictionary<string, BlessingDefinition> Blessings { get; private set; } = new();
    public Dictionary<string, ItemDefinition> Items { get; private set; } = new();
    
    public bool IsLoaded { get; private set; }
    
    private Database() { }
    
    public static Database Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new Database();
                }
            }
            return _instance;
        }
    }
    
    public async Task LoadAllAsync(HttpClient http)
    {
        if (IsLoaded) return;
        
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        
        Cards = await LoadJsonAsync<CardDefinition>(http, "Data/cards.json", options);
        
        // Load enemy cards and merge into Cards dictionary
        var enemyCards = await LoadJsonAsync<CardDefinition>(http, "Data/enemy_cards.json", options);
        foreach (var kvp in enemyCards)
        {
            if (!Cards.ContainsKey(kvp.Key))
                Cards.Add(kvp.Key, kvp.Value);
        }
        
        Arcanas = await LoadJsonAsync<ArcanaDefinition>(http, "Data/arcanas.json", options);
        Enemies = await LoadJsonAsync<EnemyDefinition>(http, "Data/enemies.json", options);
        Relics = await LoadJsonAsync<RelicDefinition>(http, "Data/relics.json", options);
        Artifacts = await LoadJsonAsync<RelicDefinition>(http, "Data/artifacts.json", options);
        Events = await LoadJsonAsync<EventDefinition>(http, "Data/events.json", options);
        NPCs = await LoadJsonAsync<NPCDefinition>(http, "Data/npcs.json", options);
        Blessings = await LoadJsonAsync<BlessingDefinition>(http, "Data/blessings.json", options);
        Items = await LoadJsonAsync<ItemDefinition>(http, "Data/items.json", options);
        
        IsLoaded = true;
    }
    
    private async Task<Dictionary<string, T>> LoadJsonAsync<T>(HttpClient http, string path, JsonSerializerOptions options) where T : class
    {
        try
        {
            var json = await http.GetStringAsync(path);
            var list = JsonSerializer.Deserialize<List<T>>(json, options);
            
            if (list == null) return new Dictionary<string, T>();
            
            var idProp = typeof(T).GetProperty("Id");
            if (idProp == null) return new Dictionary<string, T>();
            
            return list.ToDictionary(item => idProp.GetValue(item)?.ToString() ?? "");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading {path}: {ex.Message}");
            return new Dictionary<string, T>();
        }
    }
    
    public bool IsValidCard(string cardId) => Cards.ContainsKey(cardId);
    public bool IsValidArcana(string arcanaId) => Arcanas.ContainsKey(arcanaId);
    public bool IsValidEnemy(string enemyId) => Enemies.ContainsKey(enemyId);
    public bool IsValidRelic(string relicId) => Relics.ContainsKey(relicId);
    public bool IsValidArtifact(string artifactId) => Artifacts.ContainsKey(artifactId);
    public bool IsValidEvent(string eventId) => Events.ContainsKey(eventId);
    public bool IsValidNPC(string npcId) => NPCs.ContainsKey(npcId);
    public bool IsValidBlessing(string blessingId) => Blessings.ContainsKey(blessingId);
    public bool IsValidItem(string itemId) => Items.ContainsKey(itemId);
    
    public CardDefinition? GetCard(string cardId) => Cards.GetValueOrDefault(cardId);
    public ArcanaDefinition? GetArcana(string arcanaId) => Arcanas.GetValueOrDefault(arcanaId);
    public EnemyDefinition? GetEnemy(string enemyId) => Enemies.GetValueOrDefault(enemyId);
    public RelicDefinition? GetRelic(string relicId) => Relics.GetValueOrDefault(relicId);
    public RelicDefinition? GetArtifact(string artifactId) => Artifacts.GetValueOrDefault(artifactId);
    public EventDefinition? GetEvent(string eventId) => Events.GetValueOrDefault(eventId);
    public NPCDefinition? GetNPC(string npcId) => NPCs.GetValueOrDefault(npcId);
    public BlessingDefinition? GetBlessing(string blessingId) => Blessings.GetValueOrDefault(blessingId);
    public ItemDefinition? GetItem(string itemId) => Items.GetValueOrDefault(itemId);
    
    public List<CardDefinition> GetAllCards() => Cards.Values.ToList();
}
