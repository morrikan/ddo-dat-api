---
name: ddo-search
description: Use when the user searches for game objects by keyword, partial name, or pattern — especially when the exact name is unknown or multiple words should match
---

# DDO Search

Search for game objects by keywords or regex patterns, ranked by match quality.

## When to Use

- User gives partial or approximate names ("fire shield spell", "vorpal sword")
- User wants to find objects matching multiple criteria words
- Exact name lookup (`ddo-name-lookup`) returned no results or user doesn't know the exact name

## Endpoint

`POST http://localhost:5138/DbProperties/Search`

**Request body** (JSON):
```json
{
  "words": ["fire", "shield"],
  "numResults": 10
}
```

- `words` (required, at least 1): each word is matched as a substring OR regex against cached object names (case-insensitive)
- `numResults` (optional, default 20): max results to return

**Response**: array of `{id, name, score}` ordered by score descending, then id ascending. Score is the fraction of input words that matched (0.0–1.0).

## Flow

1. **Search**: `curl -s -X POST http://localhost:5138/DbProperties/Search -H "Content-Type: application/json" -d '{"words":["term1","term2"]}'`
2. **Single high-score result**: Fetch full details with `ddo-describe`.
3. **Multiple results**: Show a ranked list with name, score, and hex ID. Let the user pick one, then fetch with `ddo-describe`.
4. **No results**: Tell the user nothing matched. Suggest broadening or changing search terms.

## Tips

- Use fewer, broader words for wider matches. Use more words to narrow results.
- Each word can be a regex: `"^Flame"` matches names starting with "Flame".
- A score of 1.0 means all words matched; 0.5 means half did.
- Returns 424 if the cache hasn't loaded — tell the user the cache needs to be rebuilt.
