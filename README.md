# DDO Dat Reader API

A web API for reading and querying data from [Dungeons & Dragons Online](https://www.ddo.com/) client `.dat` files. Built to be consumed by an MCP (Model Context Protocol) server, enabling AI assistants to look up game data such as items, spells, NPCs, quests, and images.

## What It Does

The API loads DDO's client dat files (gamelogic, general, sound, local_English, surfaces, animations, cells, maps, meshes, and highres textures) and exposes their contents over HTTP. It provides endpoints for:

- **Raw dat access** — read raw binary objects from any dat file by ID
- **DbProperties** — load parsed property collections from `client_gamelogic.dat`, with name-based lookups via a prebuilt index
- **EntityDesc** — load EntityDesc objects from `client_gamelogic.dat`
- **Images** — extract and serve PNG images from the dat files
- **ID ranges** — list the known object type ID ranges
- **Cache rebuild** — trigger a full re-index of the gamelogic dat

The included `compose.yaml` also runs an [OpenAPI-to-MCP bridge](https://github.com/ckanthony/openapi-mcp) alongside the API, so an MCP-compatible AI client can use the API's Swagger spec directly.

## Prerequisites

- **DDO installed** — a local installation of Dungeons & Dragons Online (the API reads directly from the game's `.dat` files)
- **Docker** — Docker Desktop (or Docker Engine + Compose) for the containerised setup
- **Git Bash or compatible shell** — `run.sh` uses bash and the Windows `reg` command to locate the DDO install path

If you want to build and run locally instead of using Docker:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Quick Start

Clone the repo and run the setup script:

```bash
git clone https://github.com/<your-username>/dat-api.git
cd dat-api
./run.sh
```

### What `run.sh` Does

1. Queries the Windows registry to find your DDO installation directory
2. Writes the path to a `.env` file (`DDO_INSTALL_PATH=...`)
3. Builds the Docker image from `src/DdoDatApi/`
4. Starts the containers via `docker compose up -d`

If the registry lookup fails (e.g. non-standard install location), open `run.sh` and set `INSTALL_PATH` manually.

Once running, the API is available at `http://localhost:8080` and the Swagger UI at `http://localhost:8080/swagger`.

## Performance Warning

> **Index generation can take over 30 minutes when running inside Docker**, due to the overhead of accessing the mounted dat files through a volume. When running the API locally (outside of Docker), the same process typically completes in **3–4 minutes**.

You can trigger a rebuild of the index at any time by sending a `POST` request to `/RawDat/RebuildCache`. The index is saved to disk as `indexcache.json` and will be loaded automatically on subsequent startups, so this cost is only paid once (or after a game patch).

## API Endpoints

| Endpoint | Method | Description |
|---|---|---|
| `/RawDat/{dat}/{id}` | GET | Read raw bytes from a specific dat file |
| `/RawDat/IdRanges` | GET | List known object type ID ranges |
| `/RawDat/RebuildCache` | POST | Trigger a full cache/index rebuild |
| `/DbProperties/{id}` | GET | Get a parsed property collection by ID |
| `/DbProperties/IdsForName?name=` | GET | Look up object IDs by name |
| `/EntityDesc/{id}` | GET | Get an EntityDesc object by ID |
| `/Image/{id}` | GET | Get a PNG image by ID |

IDs can be provided in hexadecimal (prefixed with `0x`) or as plain integers.

## Project Structure

```
dat-api/
├── run.sh              # Setup script (registry lookup, docker build & compose)
├── compose.yaml        # Docker Compose: API + MCP server
└── src/DdoDatApi/
    ├── dockerfile      # Multi-stage .NET 10 build
    ├── Program.cs       # App entry point, Swagger config
    ├── DatSource.cs     # Loads all dat files via VoK.Sdk
    ├── Controllers/     # API endpoints
    ├── Caching/         # Index builder and cache loader
    └── Models/          # DTOs
```

## License

This project is not affiliated with or endorsed by Standing Stone Games or Daybreak Game Company.
