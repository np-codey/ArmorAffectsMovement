using System;
using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallConnect;
using DaggerfallWorkshop.Game.Items;

namespace ArmorAffectsMovementMod
{
    public class ArmorAffectsMovement : MonoBehaviour
    {
        private static Mod mod;
        string walkSpeedId;
        string runSpeedId;
        float overallMovementEffect;
        float overallJumpMultiplier;
        bool debugMode = false;
        ModSettings settings;
        PlayerEntity player;
        PlayerSpeedChanger speedChanger;
        AcrobatMotor acrobatMotor;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<ArmorAffectsMovement>();
            mod.IsReady = true;
        }

        void Start()
        {
            // debugMode = true;
            settings = mod.GetSettings();
            speedChanger = GameManager.Instance.SpeedChanger;
            player = GameManager.Instance.PlayerEntity;
            acrobatMotor = GameManager.Instance.AcrobatMotor;
            overallMovementEffect = settings.GetValue<float>("General", "overallMovementEffect");
            overallJumpMultiplier = settings.GetValue<float>("General", "overallJumpMultiplier");

            // Register the various modifications to movement.
            SaveLoadManager.OnLoad += InitMovement;
            StartGameBehaviour.OnStartGame += InitMovement;
            DaggerfallUI.UIManager.OnWindowChange += RecalculateMovement;
            FormulaHelper.RegisterOverride<Func<PlayerEntity, int, int>>(mod, "CalculateClimbingChance", CalculateClimbingChance);
        }

        public void InitMovement(object sender, EventArgs e)
        {
            modifyMovement();
        }

        public void InitMovement(SaveData_v1 saveData)
        {
            modifyMovement();
        }

        public void RecalculateMovement(object sender, EventArgs e)
        {
            // Clear the previous walk/run modifiers before recalc, otherwise the modifiers will compound.
            if (walkSpeedId != null)
                speedChanger.RemoveSpeedMod(walkSpeedId, false);

            if (runSpeedId != null)
                speedChanger.RemoveSpeedMod(runSpeedId, true);

            modifyMovement();
        }

        void modifyMovement()
        {
            var totalWeight = getEquipmentWeight();
            var armorPenalty = calculateArmorWalkRunPenalty(totalWeight);

            speedChanger.AddWalkSpeedMod(out string walkSpeedUID, armorPenalty);
            speedChanger.AddRunSpeedMod(out string runSpeedUID, armorPenalty);
            acrobatMotor.jumpSpeed = AcrobatMotor.defaultJumpSpeed * calculateJumpSpeedPenalty(totalWeight);

            // Cache the uids of the walk/run modifiers so we can clear them on recalculation.
            walkSpeedId = walkSpeedUID;
            runSpeedId = runSpeedUID;
        }

        float getEquipmentWeight()
        {
            float totalWeight = 0f;

            foreach (DaggerfallUnityItem item in player.ItemEquipTable.EquipTable)
            {
                if (item == null)
                    continue;

                totalWeight += item.weightInKg;
            }

            // Ensure weight is never ridiculously high as to break our calculations; protects against other mods too.
            return Mathf.Clamp(totalWeight, 0f, 100f);
        }

        // How much to modify speed (e.g. 75% of normal speed: 0.75, No change: 1)
        float calculateArmorWalkRunPenalty(float totalWeight)
        {
            float strength = player.Stats.LiveStrength;

            // Power of the effect of weight, see settings for range and default.
            float overallEffect = overallMovementEffect;

            // Impact that strength has, from 1000 to 10000. 1500 is strong, 10000 is weak.
            float strengthEffect = 4000f;

            float weightModifier = (110f - (totalWeight / overallEffect)) / 100f;
            float strengthBonus = weightModifier * (strength * (strength / 5f) / strengthEffect);

            // Ensure the speed modifier does not exceed 1 or cease movement entirely.
            float modifier = Mathf.Clamp(weightModifier + strengthBonus, 0.1f, 1f);

            if (debugMode)
            {
                Debug.Log("ArmorAffectsMovement | Overall effect: " + overallEffect);
                Debug.Log("ArmorAffectsMovement | Total Weight: " + totalWeight);
                Debug.Log("ArmorAffectsMovement | Weight modifier: " + weightModifier);
                Debug.Log("ArmorAffectsMovement | Strength bonus: " + strengthBonus);
                Debug.Log("ArmorAffectsMovement | Speed modifier: " + modifier);
            }

            return modifier;
        }

        // How much to modify jump height (e.g. 75% of normal height: 0.75, No change: 1)
        float calculateJumpSpeedPenalty(float totalWeight)
        {
            // If the equip weight is under 17kg, no jump penalty. This should allow for roughly full leather and a daedric one-hander.
            float modifier = totalWeight > 17f ? (110f - totalWeight) / 100f : 1f;
            modifier = Mathf.Clamp(modifier * overallJumpMultiplier, 0.1f, float.MaxValue);

            if (debugMode)
                Debug.Log("ArmorAffectsMovement | Jump modifier: " + modifier);

            return modifier;
        }

        // Overrides the base formula in Formulas/FormulaHelper.cs -> CalculateClimbingChance().
        // As such, if the original formula ever changes, this should also be updated.
        int CalculateClimbingChance(PlayerEntity player, int basePercentSuccess)
        {
            int skill = player.Skills.GetLiveSkillValue(DFCareer.Skills.Climbing);
            int luck = player.Stats.GetLiveStatValue(DFCareer.Stats.Luck);

            if (player.Race == Races.Khajiit)
                skill += 30;

            // Climbing effect states "target can climb twice as well" - doubling effective skill after racial applied
            if (player.IsEnhancedClimbing)
                skill *= 2;

            // Armor weight modifier
            float equipWeight = getEquipmentWeight();
            float weightModifier = (equipWeight >= 17f) ? (equipWeight * equipWeight) / 20f : 0f;
            skill -= (int)weightModifier;

            // Clamp skill range
            skill = Mathf.Clamp(skill, 5, 95);
            float luckFactor = Mathf.Lerp(0, 10, luck * 0.01f);

            // Skill Check
            int chance = (int)(Mathf.Lerp(basePercentSuccess, 100, skill * .01f) + luckFactor);

            if (debugMode)
            {
                Debug.Log("ArmorAffectsMovement | Weight malus: " + weightModifier);
                Debug.Log("ArmorAffectsMovement | Final climb skill: " + skill);
            }

            return chance;
        }
    }
}
