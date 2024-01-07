using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Utility;
using System;

namespace ArmorAffectsMovementMod
{
    public class ArmorAffectsMovement : MonoBehaviour
    {
        private static Mod mod;
        PlayerEntity player;
        PlayerSpeedChanger speedChanger;
        string walkSpeedId;
        string runSpeedId;

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
            speedChanger = GameManager.Instance.SpeedChanger;
            player = GameManager.Instance.PlayerEntity;

            SaveLoadManager.OnLoad += InitArmorSpeeds;
            StartGameBehaviour.OnStartGame += InitArmorSpeeds;
            DaggerfallUI.UIManager.OnWindowChange += RecalcArmorSpeeds;

            Debug.Log("ArmorAffectsMovement | Mod started.");
        }

        public void InitArmorSpeeds(object sender, EventArgs e)
        {
            modifyMovementFromArmor();
        }

        public void InitArmorSpeeds(SaveData_v1 saveData)
        {
            modifyMovementFromArmor();
        }

        public void RecalcArmorSpeeds(object sender, EventArgs e)
        {
            // Clear the previous modifiers before recalculating.
            Debug.Log("ArmorAffectsMovement | Recalculating...");

            if (walkSpeedId != null)
                speedChanger.RemoveSpeedMod(walkSpeedId, false);
            if (runSpeedId != null)
                speedChanger.RemoveSpeedMod(runSpeedId, true);

            modifyMovementFromArmor();
        }

        private void modifyMovementFromArmor()
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

            var armorPenalty = calculateArmorMovementPenalty(totalWeight);

            speedChanger.AddWalkSpeedMod(out string walkSpeedUID, armorPenalty);
            speedChanger.AddRunSpeedMod(out string runSpeedUID, armorPenalty);

            // Cache the ids of the modifiers so we can clear them for recalc.
            walkSpeedId = walkSpeedUID;
            runSpeedId = runSpeedUID;
        }

        // How much to modify speed (e.g. 75% of normal speed: 0.75, No change: 1)
        private float calculateArmorMovementPenalty(float totalWeight)
        {
            // Penalty is a portion of the total weight as a percentage reduction, with a bonus from strength.
            float weightModifier = (100f - (totalWeight / 1.5f)) / 100f;
            float strengthModifier = player.Stats.LiveStrength / 200f;
            float strengthBonus = weightModifier * strengthModifier;
            float modifier = Mathf.Clamp(weightModifier + strengthBonus, 0f, 1f);

            Debug.Log("ArmorAffectsMovement | Total Weight: " + totalWeight);
            Debug.Log("ArmorAffectsMovement | Weight modifier: " + weightModifier);
            Debug.Log("ArmorAffectsMovement | Strength bonus: " + strengthBonus);
            Debug.Log("ArmorAffectsMovement | Armor modifier: " + modifier);

            return modifier;
        }
    }
}
}
