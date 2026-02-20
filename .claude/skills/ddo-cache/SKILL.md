---
name: ddo-cache
description: Use when the user asks if game data is loaded, checking cache status, or when API calls return 424 errors
---

# DDO Cache Status

`curl -s "http://localhost:5138/Cache/Metadata"`

Returns timestamp, version, and size of the loaded index.

If the index is not loaded, most endpoints return HTTP 424. Tell the user the cache needs to be rebuilt.
