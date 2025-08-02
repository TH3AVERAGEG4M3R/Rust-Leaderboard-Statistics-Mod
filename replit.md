# Overview

This is a Rust player statistics tracking plugin that monitors and records player performance metrics in real-time. The plugin tracks various gameplay statistics including kills, deaths, K/D ratios, headshots, distance tracking, and farming activities. It provides leaderboard functionality and chat commands for players to view their statistics.

# User Preferences

Preferred communication style: Simple, everyday language.

# System Architecture

## Plugin Architecture
The system follows a modular plugin architecture designed for the Rust game server environment. The core components are organized into separate concerns:

- **Configuration Management**: JSON-based configuration system allowing server administrators to customize tracking behavior, auto-save intervals, and feature toggles
- **Data Persistence**: JSON file-based storage for player statistics with configurable auto-save functionality
- **Localization System**: Multi-language support through JSON language files for user-facing messages and formatting

## Data Storage Strategy
The plugin uses a file-based storage approach with JSON serialization:
- Player statistics are stored in a single JSON file (`PlayerStats.json`) 
- Configuration settings are externalized to `PlayerStatsConfig.json` for easy server administration
- Data persistence includes automatic saving at configurable intervals to prevent data loss

## Feature Toggle System
The architecture implements a comprehensive feature toggle system allowing administrators to:
- Enable/disable specific tracking categories (PVP, distance, farming)
- Control chat command availability
- Configure leaderboard display settings
- Set automatic data reset policies for server wipes

## Statistics Tracking Categories
The system tracks multiple performance categories:
- Combat statistics (kills, deaths, K/D ratios, headshots, one-shot kills)
- Distance-based metrics (furthest kill distance)
- Weapon usage tracking (rockets, C4 explosives)
- Resource gathering and farming activities

## Command Interface
Chat-based command system providing players with:
- Personal statistics lookup functionality
- Category-specific leaderboard access
- Real-time data querying capabilities

# External Dependencies

## Game Server Integration
- **Rust Game Server**: Direct integration with Rust's plugin system for real-time event monitoring and player interaction
- **Server Admin Tools**: Configuration interface for server administrators to customize tracking behavior

## File System Dependencies
- **JSON Configuration Files**: External configuration management for runtime behavior modification
- **Local File Storage**: Direct file system access for persistent data storage without external database requirements

## Localization Framework
- **Multi-language Support**: JSON-based language file system supporting internationalization for player-facing messages and formatting