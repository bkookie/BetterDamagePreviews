# BetterDamagePreviews

Calculates a more accurate damage preview value, and displays it alongside the existing value in orange.

- Sums multi-hit attacks (including X cards)
- Considers effects like Slippery and Flutter stacks against your hit count
- Certain card-specific effects like Tesla Coil
	
Works with all base game cards, and any (simple) modded cards that follow base game conventions (doesn't require patching individual cards).

Also provides an API to add additional support for modded cards and to define custom calculations. To use this, see the `BetterDamagePreviews.Preview.PreviewManager` class.