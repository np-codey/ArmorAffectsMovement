# Armor Affects Movement

## Author

redmoss, with huge thanks to Magicono43

## Description

This mod applies the burden of armor and weapons on the player by restricting their movement speed. The heavier your equipment loadout, the slower you become; but characters with high strength can offset this to a certain extent.

This is an RP-flavoured mod, essentially. The goal is to put a downside on wearing heavy armor, to make it not only more realistic, but to give an actual advantage for wearing light armor. The vanilla Daggerfall experience makes it a no-brainer to go full plate on every character since there is no downside; armor is effectively a dodge suit. Thus, Rogue/assassin/burglar characters will benefit from full freedom of movement with this, allowing them to freely climb and leap around, while heavier armoured characters will have to stick to the ground.

## Installation

Place the `.dfmod` file into `StreamingAssets/Mods` folder in your DFU installation.

## Formula

The current formula should be:
```
totalWeight is the total weight of the player's equipped armor in kilograms.

weightModifier = (100f - (totalWeight / 1.5f)) / 100f
strengthModifier = Strength / 500f
strengthBonus = weightModifier * strengthModifier
modifier = Mathf.Clamp(weightModifier + strengthBonus, 0f, 1f)
```

Feedback is always appreciated, this mod needs testing for sure!

## TODO

- Restrict recalc code to only trigger on Inventory window, not every window
- Weight affects climbing (the player will slip more)
- Weight affects jump height
- Ensure horse and cart speeds are not affected (for ease of gameplay)
- Options, such as ignoring weapon weight, a global modifier for user taste
