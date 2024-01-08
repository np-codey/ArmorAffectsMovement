using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Utility;
using System;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using static Mono.CSharp.Parameter;

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
            debugMode = true;
            settings = mod.GetSettings();

            speedChanger = GameManager.Instance.SpeedChanger;
            player = GameManager.Instance.PlayerEntity;
            acrobatMotor = GameManager.Instance.AcrobatMotor;
            overallMovementEffect = settings.GetValue<float>("General", "overallMovementEffect");
            overallJumpMultiplier = settings.GetValue<float>("General", "overallJumpMultiplier");

            SaveLoadManager.OnLoad += InitMovement;
            StartGameBehaviour.OnStartGame += InitMovement;
            DaggerfallUI.UIManager.OnWindowChange += RecalculateMovement;
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
            // Clear the previous modifiers before recalculating, otherwise the modifiers will compound.
            if (walkSpeedId != null)
                speedChanger.RemoveSpeedMod(walkSpeedId, false);

            if (runSpeedId != null)
                speedChanger.RemoveSpeedMod(runSpeedId, true);

            modifyMovement();
        }

        void modifyMovement()
        {
            var equipment = player.ItemEquipTable.EquipTable;
            float totalWeight = 0f;

            // Iterate through the equipment slots and calculate weight total of all equipped items.
            for (int i = 0; i < equipment.Length; i++)
            {
                if (equipment[i] == null)
                    continue;

                totalWeight += equipment[i].weightInKg;
            }

            var armorPenalty = calculateArmorWalkRunPenalty(totalWeight);

            speedChanger.AddWalkSpeedMod(out string walkSpeedUID, armorPenalty);
            speedChanger.AddRunSpeedMod(out string runSpeedUID, armorPenalty);
            acrobatMotor.jumpSpeed = AcrobatMotor.defaultJumpSpeed * calculateJumpSpeedPenalty(totalWeight);

            // Cache the uids of the modifiers so we can clear them on recalculation.
            walkSpeedId = walkSpeedUID;
            runSpeedId = runSpeedUID;
        }

        // How much to modify speed (e.g. 75% of normal speed: 0.75, No change: 1)
        float calculateArmorWalkRunPenalty(float totalWeight)
        {
            float strength = player.Stats.LiveStrength;

            // Power of the effect of weight, from 1 to 3. 1 is severe, 3 is weak. Default is 1.4.
            float overallEffect = overallMovementEffect;

            // Impact that strength has, from 1000 to 7000. 1500 is strong, 10000 is weak.
            float strengthEffect = 6000f;

            float weightModifier = (100f - (totalWeight / overallEffect)) / 100f;
            float strengthBonus = weightModifier * ((strength * (strength / 5f)) / strengthEffect);

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

        float calculateJumpSpeedPenalty(float totalWeight)
        {
            float modifier = 1f;
            modifier *= overallJumpMultiplier;

            if (debugMode)
            {
                Debug.Log("ArmorAffectsMovement | Jump modifier: " + modifier);
            }

            return modifier;
        }
    }
}
