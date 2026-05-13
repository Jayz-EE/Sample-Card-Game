# Roguelike Card Game - Browser Version

This is a Blazor WebAssembly port of the C# console roguelike card game, now playable directly in your browser.

## What Changed

### Architecture
- **Blazor WebAssembly**: Runs C# code directly in the browser via WebAssembly
- **Component-based UI**: Replaced console I/O with interactive Razor components
- **Async data loading**: JSON files loaded via HttpClient instead of File.ReadAllText
- **CSS styling**: ANSI color codes replaced with modern CSS

### Core Game Logic (Unchanged)
All game engines remain intact:
- GameEngine (combat system)
- AIEngine (enemy AI)
- EventEngine (events and shops)
- EffectEngine (card effects)
- RunManager (run state management)
- MapGenerator (procedural map generation)

### UI Components
- **ArcanaSelection**: Choose your starting character
- **MapScreen**: Navigate between nodes
- **CombatScreen**: Turn-based card combat with real-time updates
- **EventScreen**: Story events and choices
- **ShopScreen**: Buy cards and relics
- **RestScreen**: Heal or upgrade cards
- **GameOver**: Run statistics and score

## Running the Game

### Development
```bash
cd GameBlazor
dotnet run
```

Then open your browser to `https://localhost:5001` (or the URL shown in the terminal).

### Build for Production
```bash
cd GameBlazor
dotnet publish -c Release
```

The output will be in `bin/Release/net10.0/publish/wwwroot/`. Deploy these files to any static web host.

## Browser Requirements
- Modern browser with WebAssembly support (Chrome, Firefox, Edge, Safari)
- JavaScript enabled

## Key Differences from Console Version
1. **No Thread.Sleep**: Replaced with `Task.Delay` for smooth async UI updates
2. **Click-based input**: Mouse/touch instead of keyboard input
3. **Visual feedback**: Hover effects, color-coded stats, emoji icons
4. **Responsive layout**: Works on desktop and mobile browsers

## Project Structure
```
GameBlazor/
├── Components/          # Razor UI components
├── Pages/              # Routable pages
├── Engines/            # Game logic (from original)
├── Models/             # Data models (from original)
├── Definitions/        # Game definitions (from original)
├── Data/               # JSON data files (from original)
└── wwwroot/
    ├── css/            # Styling
    └── Data/           # JSON files served to browser
```

## Future Enhancements
- Save/load runs to browser localStorage
- Animations for card plays and damage
- Sound effects
- Mobile-optimized touch controls
- Multiplayer via SignalR
