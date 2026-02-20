# DDO Dat Reader API

An AI-powered tool for exploring [Dungeons & Dragons Online](https://www.ddo.com/) game data. Ask questions about items, spells, NPCs, quests, enhancement trees, loot tables, and more — and get answers pulled directly from the game's client files.

Under the hood, a .NET web API reads DDO's `.dat` files and exposes them over HTTP. An MCP (Model Context Protocol) bridge connects the API to Claude Code (or any MCP-compatible AI agent), so you can query game data conversationally instead of digging through raw files.

## What It Does

The API loads DDO's client dat files (gamelogic, general, sound, local_English, surfaces, animations, cells, maps, meshes, and highres textures) and exposes their contents over HTTP. It provides endpoints for:

- **DbProperties** — parsed property collections from `client_gamelogic.dat`, with name-based lookups, weenie type browsing, enhancement trees, and treasure tables
- **EntityDesc** — EntityDesc objects from `client_gamelogic.dat`
- **Images** — extract and serve PNG images from the dat files
- **Sounds** — sound metadata from `client_general.dat`
- **Raw dat access** — read raw binary objects from any dat file by ID
- **ID ranges** — list the known object type ID ranges
- **Cache management** — rebuild the gamelogic index, check cache status, or download the full index

## Prerequisites

- **DDO installed** — a local installation of Dungeons & Dragons Online (the API reads directly from the game's `.dat` files)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Git Bash or compatible shell** — `run.sh` uses bash and the Windows `reg` command to locate the DDO install path
- **Node.js** — for the OpenAPI-to-MCP bridge
- **Claude Code** — AI agent CLI
  ```
  npm install -g @anthropic-ai/claude-code
  ```

For Docker mode (`./run.sh --docker`), you also need **Docker Desktop**.

## Quick Start

Clone the repo and run the setup script:

```bash
git clone https://github.com/morrikan/ddo-dat-api.git
cd ddo-dat-api
./run.sh
```

### What `run.sh` Does

1. Queries the Windows registry to find your DDO installation directory
2. Builds and runs the API via `dotnet run` (or Docker with `--docker`)
3. Waits for the API to become available
4. Saves the OpenAPI schema and sets up the MCP server for Claude
5. Opens Swagger UI and launches Claude Code

If the registry lookup fails (e.g. non-standard install location), set `INSTALL_PATH` manually in `run.sh` or in `DatSource.cs`.

Once running, the API is available at `http://localhost:5138` and the Swagger UI at `http://localhost:5138/swagger`.

## Performance Note

> Index generation typically completes in **3-4 minutes** when running locally. **In Docker mode, it can take over 30 minutes** due to volume mount overhead.

The index is saved to disk as `indexcache.json` and loaded automatically on subsequent startups, so this cost is only paid once (or after a game patch). You can trigger a rebuild at any time via `POST /Cache/Rebuild`.

## API Endpoints

| Endpoint | Method | Description |
|---|---|---|
| `/DbProperties/{id}` | GET | Get a parsed property collection by ID |
| `/DbProperties/IdsForName?name=` | GET | Look up object IDs by name |
| `/DbProperties/WeenieTypeCounts` | GET | Get counts of each weenie type |
| `/DbProperties/ByWeenieType/{weenieType}` | GET | List objects of a given weenie type |
| `/DbProperties/EnhancementTrees` | GET | List all enhancement trees |
| `/DbProperties/TreasureTables` | GET | List all treasure table IDs |
| `/EntityDesc/{id}` | GET | Get an EntityDesc object by ID |
| `/Image/{id}` | GET | Get a PNG image by ID |
| `/Sound/{id}` | GET | Get sound metadata by ID |
| `/RawDat/{dat}/{id}` | GET | Read raw bytes from a specific dat file |
| `/RawDat/IdRanges` | GET | List known object type ID ranges |
| `/Cache/Rebuild` | POST | Trigger a full cache/index rebuild |
| `/Cache/Metadata` | GET | Get index timestamp, version, and size |
| `/Cache/Download` | GET | Download the full index |

IDs can be provided in hexadecimal (prefixed with `0x`) or as plain integers.

## Project Structure

```
ddo-dat-api/
├── run.sh                # Setup script (registry lookup, build, MCP setup, launch Claude)
├── claude.md             # AI agent instructions
├── .claude/skills/       # AI workflow skills (name lookup, property rendering, etc.)
└── src/DdoDatApi/
    ├── dockerfile        # Multi-stage .NET 10 build (for --docker mode)
    ├── Program.cs        # App entry point, Swagger config
    ├── DatSource.cs      # Loads all dat files via VoK.Sdk
    ├── Controllers/      # API endpoints
    ├── Caching/          # Index builder and cache loader
    ├── Converters/       # JSON property converters
    └── Models/           # DTOs
```

## License

This project is not affiliated with or endorsed by Standing Stone Games or Daybreak Game Company.
