# Dawid Bogocz -- Portfolio

Minimalist developer portfolio built with **Blazor WebAssembly (.NET 9)**. Single-page layout with a dark terminal aesthetic, theme switching, and a procedural platformer game powered by a .NET Web API.

## Sections

- **Hero** -- name, tagline, social/github links
- **About** -- code-block bio with syntax highlighting
- **Experience** -- timeline-based work history
- **Projects** -- project cards with tech tags
- **Games** -- playable Procedural Platformer  
- **Contact** -- email, GitHub, LinkedIn

## Tech Stack

- **Frontend**: Blazor WASM (.NET 9), vanilla CSS, HTML
- **Backend**: .NET 9 Web API (Docker), EF Core + SQLite
- **Deployment**: Caddy reverse proxy (agentserver), Docker
- **Auth**: None (public portfolio)

## Platformer Game

A procedurally generated side-scrolling platformer integrated into the portfolio. Features:

- **Server-side level generation** -- deterministic by seed, chunk-based with themes (starter, normal, challenge, rest)
- **Gameplay**: WASD movement, double jump (press W/Space again in air), stomp enemies, avoid spikes, collect coins
- **Leaderboard** -- SQLite-backed, all-time top scores with time bonus (faster finish = higher score)
- **Enemies** -- patrol AI, killable by stomping (+25 points), visual squish death animation
- **Traps** -- platform spikes and ground-level spike gauntlets with clear visual indicators
- **Double jump** -- key-down edge detection, no hold-to-waste
- **Timer** -- displayed on HUD, time bonus at level completion

### API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/game/level?seed=N` | Generate a level (random seed if omitted) |
| POST | `/api/game/score` | Submit a score |
| GET | `/api/game/leaderboard?top=N` | Get top N scores |

### Running Locally

```bash
# Backend
cd src/Platformer.Api
dotnet run    # Creates data/game.db automatically

# Frontend
cd src/Portfolio.Web
dotnet run
```

### Docker

```bash
cd src/Platformer.Api
docker build -t portfolio-platformer-api .
docker run -d -p 5001:5001 -v $(pwd)/data:/app/data portfolio-platformer-api
```

## Themes

Four editor-inspired themes accessible from the navbar:

- **VSDark** (default) -- blue accent
- **Godot** -- orange accent
- **Monokai** -- green accent
- **Light** -- light mode, blue accent

## License

MIT