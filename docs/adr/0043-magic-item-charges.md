# ADR 0043: Magic Item Charges & Active Abilities

## Status
Accepted

## Context
Magic items in D&D 5e often have charges that are consumed to use abilities (spells or other effects). These charges typically recharge at specific times (e.g., "at dawn") based on a die roll (e.g., "1d6 + 1"). We need a system to model this behavior and allow items to expose active abilities that consume these charges.

## Decision
We will implement a charge system on `IMagicItem` and introduce `IMagicItemAbility` to represent active uses of the item.

### 1. Recharge Frequency
We will introduce a `RechargeFrequency` enum to categorize when items recharge:
- `Dawn`, `Dusk`, `Midnight`
- `ShortRest`, `LongRest`
- `Never`, `Other`

### 2. IMagicItem Updates
`IMagicItem` will be updated to include:
- `RechargeFrequency RechargeFrequency { get; }`
- `string RechargeFormula { get; }` (e.g., "1d6+1")

### 3. Active Abilities
We will use the existing `IMagicItemAbility` interface but ensure it is fully integrated with the `UseMagicItemAction`.
- `MagicItemAbility` will wrap execution logic.
- For items importing spells (e.g., Wands), we will create `MagicItemAbility` instances that delegate to `CastSpellAction` or similar logic.

### 4. Import Logic
The `JsonMagicItemImporter` will be updated to:
- Parse recharge strings (e.g., "1d6+1 at dawn") into `Frequency` and `Formula`.
- (Future) Parse attached spells and create abilities for them.

## Consequences
- **Positive**: Standardized way to handle charges and recharging.
- **Positive**: Data-driven import of magic item behaviors.
- **Negative**: Parsing natural language recharge strings can be brittle; we will handle common cases and fallback to `Other`.

## Implementation Plan
1. Define `RechargeFrequency` enum.
2. Update `IMagicItem` and `MagicItem`.
3. Implement `MagicItemRecharger` service to handle the actual recharging logic.
4. Update Importer to parse recharge data.
