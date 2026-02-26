---
name: ddo-strings
description: Use when the user asks about string table entries, needs to look up text by key/table IDs, or when resolving StringInfo property values from client_localEnglish.dat
---

# DDO Strings

Look up localized string entries from client_localEnglish.dat by key and table ID.

## When to Use

- User wants to resolve a StringInfo property's `key` and `table` to its text
- User asks about string table entries or localized text
- You need to look up text that isn't included inline in a DbProperties response

## Endpoint

`GET http://localhost:5138/Strings/{table}/{key}`

- `table`: string table data ID, range `0x25000000`–`0x26FFFFFF`. Hex with `0x` prefix or plain integer.
- `key`: string key data ID. Hex with `0x` prefix or plain integer.

## Example

Given a StringInfo property:
```json
{
  "propertyName": "Item_Description",
  "key": "0x033D632E",
  "table": "0x25003A9A"
}
```

Fetch the text:
```
curl -s "http://localhost:5138/Strings/0x25003A9A/0x033D632E"
```

## Error Handling

- **400**: Could not parse the key or table ID
- **404**: Entry not found in the dat file
