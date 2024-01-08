# Armor Affects Movement

## Author

redmoss, with thanks to Magicono43

## Description

This mod applies the burden of armor and weapons on the player by restricting their movement speed. The heavier your equipment loadout, the slower you become. Characters with high strength can offset this to a certain extent.

This is an RP-flavoured mod, essentially. The goal is to put a downside on wearing heavy armor, to make it not only more realistic, but to give an actual advantage for wearing light armor. The vanilla Daggerfall experience makes it a no-brainer to go full daedric plate on any character since there is no apparent downside; armor is effectively a dodge suit. Thus, rogue/assassin/burglar characters will benefit from full freedom of movement with this, allowing them to freely climb and leap around, while heavier armoured characters will have to stick to the ground, or hasten themselves with magical means, thus allowing better for the idea of a magic-infused walking tank class.

Horse and cart travel is not affected, all players should be able to get around the world at a good pace, otherwise it becomes tedious.

## Installation

Go into the appropriate folder for your OS, and place the `.dfmod` file there into `StreamingAssets/Mods` folder in your DFU installation.

## Formula

The current formula as follows, the player speed is modified as a float percentage (e.g. 75% of walk speed = 0.75)

```
totalWeight is the total weight of the player's equipped armor in kilograms.

overallEffect: 1 to 3 (1 strong, 3 weak). Default: 1.4
strengthEffect: 1000 to 10000 (1000 strong, 10000 weak). Default: 6000

weightModifier = (100f - (totalWeight / overallEffect)) / 100f
strengthModifier = (Strength * Strength / 5) / strengthEffect
strengthBonus = weightModifier * strengthModifier
modifier = Mathf.Clamp(weightModifier + strengthBonus, 0f, 1f)
```

Feedback is always appreciated, this mod needs testing for sure!

## TODO

- Restrict recalc code to only trigger on Inventory window, not every window
- Weight affects climbing (the player will slip more), with a "you slip!" message, perhaps
- Running uses more endurance as a function of weight
- Feedback and messages: "Your armor weighs you down" and "Your armor makes climbing harder"
- Revamp jump heights as they are quite underpowered anyway and this would emphasise low equip weight options
-- Option to enable/disable
