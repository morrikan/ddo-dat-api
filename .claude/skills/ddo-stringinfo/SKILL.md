---
name: ddo-stringinfo
description: Use when processing a StringInfo property to resolve its text, applying placeholder replacements and optional property context
---

# DDO StringInfo Processing

Resolve a StringInfo property into its final display text by looking up the string and applying replacements.

## When to Use

- You have a StringInfo property (with `key` and `table`) and need its resolved text
- The string may contain `{0}`, `{1}`, etc. placeholders that need replacement values substituted

## Inputs

- **StringInfo property**: must have `key` and `table` fields
- **Replacements** (optional): ordered values to substitute for `{0}`, `{1}`, etc.
- **Property context** (optional): a set of properties that may be needed to evaluate replacements

## Steps

1. **Look up the string**: invoke the **ddo-strings** skill with the `table` and `key` from the StringInfo property. This returns:
   ```json
   {
     "id": 228870261,
     "iteration": 1,
     "placeHolders": [84038849],
     "replacements": [],
     "value": "Open Lock +{0}  ()"
   }
   ```

2. **Substitute placeholders**: for each `{#}` token in `value`, replace it with the corresponding entry from the provided Replacements.

3. **Clean up**: strip any remaining `()` tokens and surrounding whitespace from the result.

4. **Return** the processed string.

## Example

Given:
- StringInfo: `key=0x0DA44875`, `table=0x2501F1F5`
- Replacements: `[15]`

1. ddo-strings returns: `"Open Lock +{0}  ()"`
2. Substitute `{0}` → `15`: `"Open Lock +15  ()"`
3. Strip `()` and extra whitespace: `"Open Lock +15"`
4. Result: **Open Lock +15**
