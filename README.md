# Player Stats Leaderboard - Rust Oxide Plugin

A comprehensive Rust server plugin that tracks detailed player statistics and provides leaderboard functionality. Monitor combat performance, resource gathering, explosive usage, and travel distance in real-time.

## Features

### Combat Statistics
- **Kills & Deaths**: Track player vs player eliminations
- **K/D Ratio**: Automatic calculation and tracking
- **Headshot Count**: Monitor accuracy and skill
- **One-Shot Kills**: Track devastating single-hit eliminations
- **Furthest Kill Distance**: Record longest-range eliminations

### Explosive Tracking
- **Rockets Used**: Monitor rocket launcher usage
- **C4 Explosives**: Track timed explosive deployment
- **Satchel Charges**: Monitor satchel usage
- **Beancan Grenades**: Track improvised explosive usage

### Resource Gathering
- **Wood Collection**: Track lumber harvesting
- **Stone Mining**: Monitor stone gathering
- **Metal Ore**: Track metal resource collection
- **Sulfur Mining**: Monitor sulfur extraction
- **High-Quality Metal**: Track rare resource gathering

### Travel & Movement
- **Total Distance Traveled**: Real-time movement tracking
- **Position Monitoring**: Continuous location updates

## Installation

1. **Download Files**: Copy all plugin files to your server
2. **Install Plugin**: Place `PlayerStatsLeaderboard.cs` in `oxide/plugins/`
3. **Configuration**: Copy `config/` folder to `oxide/config/`
4. **Language Files**: Copy `lang/` folder to `oxide/lang/`
5. **Restart Server**: Reload or restart your Rust server

## Configuration

Edit `oxide/config/PlayerStatsConfig.json` to customize:

```json
{
  "Enable Chat Commands": true,
  "Auto Save Interval (minutes)": 5,
  "Leaderboard Top Count": 10,
  "Track PVP Only": false,
  "Reset Stats on Wipe": false,
  "Enable Distance Tracking": true,
  "Enable Farming Stats": true
}
```

### Configuration Options
- **Enable Chat Commands**: Allow players to use chat commands
- **Auto Save Interval**: How often to save data (in minutes)
- **Leaderboard Top Count**: Number of top players to display
- **Track PVP Only**: Only track player vs player combat
- **Reset Stats on Wipe**: Automatically reset all stats on server wipe
- **Enable Distance Tracking**: Monitor player movement and travel
- **Enable Farming Stats**: Track resource gathering activities

## Commands

### Player Commands
- `/stats` - View your personal statistics
- `/stats [playername]` - View another player's stats (admin only)
- `/leaderboard` - Show kills leaderboard
- `/leaderboard [category]` - Show specific category leaderboard

### Leaderboard Categories
- `kills` - Most player eliminations
- `kd` - Highest kill/death ratio
- `headshots` - Most headshot kills
- `distance` - Furthest kill distance
- `farming` - Most resources gathered

### Admin Console Commands
- `playerstats.reset` - Reset all player statistics
- `playerstats.reset [playerid]` - Reset specific player's stats
- `playerstats.save` - Manually save all statistics

## Permissions

Grant these permissions to control access:

- `playerstats.view` - View personal and leaderboard statistics
- `playerstats.admin` - Access admin commands and view other player stats

## Data Storage

Statistics are automatically saved to:
- `oxide/data/PlayerStats.json` - All player statistics data
- Auto-save occurs every 5 minutes (configurable)
- Manual save available via console command

## Tracked Statistics

### Combat Metrics
- Total kills and deaths
- Kill/death ratio calculation
- Headshot accuracy count
- One-shot elimination count
- Maximum kill distance achieved

### Resource Collection
- Wood harvesting totals
- Stone mining amounts
- Metal ore collection
- Sulfur extraction totals
- High-quality metal gathering

### Explosive Usage
- Rocket launcher deployments
- C4 timed explosive usage
- Satchel charge deployments
- Beancan grenade usage

### Movement Data
- Total distance traveled
- Real-time position tracking
- Movement-based statistics

## Requirements

- **Rust Dedicated Server**
- **Oxide Mod Framework**
- **Newtonsoft.Json** (automatically included)

## Support

This plugin automatically handles:
- Player connection/disconnection events
- Server restarts and data persistence
- Performance optimization for large player counts
- Multi-language support through language files

## Version

**Version**: 1.0.0  
**Compatible**: Rust with Oxide Framework  
**Language**: C# (.NET)

## License

This plugin is provided as-is for Rust server administrators. Modify and distribute according to your server's needs.