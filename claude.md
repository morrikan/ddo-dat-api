# DDO Dat API

This project serves game data from Dungeons and Dragons Online. Users ask about items, monsters, spells, NPCs, enhancement trees, loot tables, and other game entities. The `ddo-dat-api` MCP server and WebApp provide read-only access to this data.

When this file is loaded, tell the user "Loaded claude.md from ddodatapi"

## Presentation

- Non-enum integers `0x10000000` and above: show in hex with `0x` prefix, all 8 digits, color `#FFF5C6`.
- Non-enum integers below `0x10000000`: show as regular base-10 numbers.
- Never show a word in more than one color.

## API

Base URL: `http://localhost:5138`. All endpoints are GET. Use `curl -s` to call them.

## Behavior

Act immediately on user queries — don't describe steps, present plans, or ask permission to proceed.

If you don't know something about DDO game mechanics, say so rather than guessing.

## Common Patterns

- **Name to details**: Search by name, get IDs, fetch properties. This is the most common flow.
- **424 errors**: The index hasn't loaded. Tell the user the cache needs to be rebuilt.
- **ID format**: Use `0x` prefix for hex IDs. The API accepts both `0x`-prefixed hex and plain integers.
- **`0x70` IDs**: `/DbProperties/{id}` automatically converts `0x70______` to `0x79______`.

## Skills

Project-specific skills in `.claude/skills/` handle the main workflows:

| Skill | When to use |
|-------|-------------|
| `ddo-name-lookup` | User asks about a named thing |
| `ddo-object-types` | Identifying what kind of game object something is |
| `ddo-property-rendering` | Displaying object property details |
| `ddo-browsing` | Browsing categories, enhancement trees, treasure tables |
| `ddo-media` | Images, sounds |
| `ddo-cache` | Checking if data is loaded |
