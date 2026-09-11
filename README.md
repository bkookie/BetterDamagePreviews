# BetterDamagePreviews

Calculates a more accurate damage preview value, and displays it alongside the existing value in orange.

- Sums multi-hit attacks (including X cards)
- Considers effects like Slippery and Flutter stacks against your hit count
- Calculates certain card-specific effects like Tesla Coil & Fiend Fire
- Works with all base game cards, and many (simple) modded cards
- Doesn't require patching individual cards

Also provides an API to add additional support for modded cards and to define custom calculations. To use this, see the `BetterDamagePreviews.Preview.PreviewManager` class.