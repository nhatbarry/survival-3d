using System.Collections.Generic;
using UnityEngine;

namespace CompleteProject
{
    public enum UpgradeType
    {
        MultiShot,
        Pierce,
        Damage,
        FireRate,
        MoveSpeed,
        MaxHealth,
        Regeneration,
        Lifesteal,
        Luck,
        ComboTime
    }

    public struct UpgradeInfo
    {
        public string title;
        public string description;
        public int maxLevel;
        public Color colour;

        public UpgradeInfo (string title, string description, int maxLevel, Color colour)
        {
            this.title = title;
            this.description = description;
            this.maxLevel = maxLevel;
            this.colour = colour;
        }
    }

    /// <summary>
    /// Permanent upgrades the player picks from the level-up menu.
    /// </summary>
    public class PlayerUpgrades : MonoBehaviour
    {
        [Header ("Bonus per upgrade level")]
        public float damagePerLevel = 0.25f;            // +25% damage.
        public float fireRatePerLevel = 0.15f;          // +15% shots per second.
        public float moveSpeedPerLevel = 0.12f;         // +12% move speed.
        public int maxHealthPerLevel = 25;
        public float regenPerLevel = 1f;                // Health per second.
        public int lifestealPerLevel = 2;               // Health per kill.
        public float dropChancePerLevel = 0.08f;
        public float powerUpDurationPerLevel = 2f;
        public float comboWindowPerLevel = 1f;


        readonly Dictionary<UpgradeType, int> levels = new Dictionary<UpgradeType, int> ();
        PlayerHealth playerHealth;
        PlayerPowerUps powerUps;
        PlayerMovement movement;
        PlayerMovementMobile movementMobile;
        float baseSpeed, baseSpeedMobile;
        float regenBuffer;                              // Fractional health waiting to be regenerated.

        public int BeamCount => 1 + GetLevel (UpgradeType.MultiShot);
        public int PierceCount => GetLevel (UpgradeType.Pierce);
        public float DamageMultiplier => 1f + damagePerLevel * GetLevel (UpgradeType.Damage);
        public float FireIntervalMultiplier => 1f / (1f + fireRatePerLevel * GetLevel (UpgradeType.FireRate));
        public float MoveSpeedMultiplier => 1f + moveSpeedPerLevel * GetLevel (UpgradeType.MoveSpeed);


        void Awake ()
        {
            playerHealth = GetComponent <PlayerHealth> ();
            movement = GetComponent <PlayerMovement> ();
            movementMobile = GetComponent <PlayerMovementMobile> ();

            if(movement != null)
                baseSpeed = movement.speed;
            if(movementMobile != null)
                baseSpeedMobile = movementMobile.speed;
        }


        void OnEnable ()
        {
            EnemyHealth.Killed += OnEnemyKilled;
        }


        void OnDisable ()
        {
            EnemyHealth.Killed -= OnEnemyKilled;
        }


        void Update ()
        {
            int regenLevel = GetLevel (UpgradeType.Regeneration);
            if(regenLevel == 0 || playerHealth.IsDead)
                return;

            // Only regenerate while hurt so the buffer can't bank health.
            if(playerHealth.currentHealth >= playerHealth.startingHealth)
            {
                regenBuffer = 0f;
                return;
            }

            regenBuffer += regenPerLevel * regenLevel * Time.deltaTime;
            int whole = Mathf.FloorToInt (regenBuffer);
            if(whole > 0)
            {
                regenBuffer -= whole;
                playerHealth.Heal (whole, false);
            }
        }


        void OnEnemyKilled (EnemyHealth enemy)
        {
            int lifestealLevel = GetLevel (UpgradeType.Lifesteal);
            if(lifestealLevel > 0)
            {
                playerHealth.Heal (lifestealPerLevel * lifestealLevel, false);
            }
        }


        public int GetLevel (UpgradeType type)
        {
            return levels.TryGetValue (type, out int level) ? level : 0;
        }


        public bool CanUpgrade (UpgradeType type)
        {
            if(GetLevel (type) >= GetInfo (type).maxLevel)
                return false;

            // Some upgrades only make sense when their system is running.
            switch(type)
            {
                case UpgradeType.Luck:
                    return PickupManager.HasInstance;
                case UpgradeType.ComboTime:
                    return ComboManager.HasInstance;
                default:
                    return true;
            }
        }


        /// <summary>
        /// Picks up to count different upgrades that can still be levelled up.
        /// </summary>
        public List<UpgradeType> GetRandomChoices (int count)
        {
            List<UpgradeType> available = new List<UpgradeType> ();
            foreach(UpgradeType type in System.Enum.GetValues (typeof (UpgradeType)))
            {
                if(CanUpgrade (type))
                    available.Add (type);
            }

            // Fisher-Yates shuffle, then keep the first few.
            for(int i = available.Count - 1; i > 0; i--)
            {
                int j = Random.Range (0, i + 1);
                (available[i], available[j]) = (available[j], available[i]);
            }

            if(available.Count > count)
                available.RemoveRange (count, available.Count - count);

            return available;
        }


        public void Apply (UpgradeType type)
        {
            levels[type] = GetLevel (type) + 1;

            if(powerUps == null)
                powerUps = GetComponent <PlayerPowerUps> ();

            switch(type)
            {
                case UpgradeType.Damage:
                case UpgradeType.FireRate:
                    if(powerUps != null)
                        powerUps.ApplyStats ();
                    break;

                case UpgradeType.MoveSpeed:
                    if(movement != null)
                        movement.speed = baseSpeed * MoveSpeedMultiplier;
                    if(movementMobile != null)
                        movementMobile.speed = baseSpeedMobile * MoveSpeedMultiplier;
                    break;

                case UpgradeType.MaxHealth:
                    playerHealth.IncreaseMaxHealth (maxHealthPerLevel);
                    break;

                case UpgradeType.Luck:
                    PickupManager.Instance.dropChance = Mathf.Min (1f, PickupManager.Instance.dropChance + dropChancePerLevel);
                    if(powerUps != null)
                        powerUps.duration += powerUpDurationPerLevel;
                    break;

                case UpgradeType.ComboTime:
                    ComboManager.Instance.comboWindow += comboWindowPerLevel;
                    break;
            }
        }


        public UpgradeInfo GetInfo (UpgradeType type)
        {
            switch(type)
            {
                case UpgradeType.MultiShot:
                    return new UpgradeInfo ("Multi Shot", "Fire " + (BeamCount + 1) + " beams at once",
                        3, new Color (1f, 0.55f, 0.2f));
                case UpgradeType.Pierce:
                    return new UpgradeInfo ("Piercing Rounds", "Beams pass through " + (PierceCount + 1) + " more " + (PierceCount == 0 ? "enemy" : "enemies"),
                        3, new Color (0.4f, 0.8f, 1f));
                case UpgradeType.Damage:
                    return new UpgradeInfo ("Heavy Rounds", "+" + Percent (damagePerLevel) + " damage",
                        5, new Color (1f, 0.3f, 0.3f));
                case UpgradeType.FireRate:
                    return new UpgradeInfo ("Trigger Happy", "+" + Percent (fireRatePerLevel) + " fire rate",
                        5, new Color (1f, 0.85f, 0.2f));
                case UpgradeType.MoveSpeed:
                    return new UpgradeInfo ("Swift Feet", "+" + Percent (moveSpeedPerLevel) + " move speed",
                        4, new Color (0.5f, 1f, 0.8f));
                case UpgradeType.MaxHealth:
                    return new UpgradeInfo ("Vitality", "+" + maxHealthPerLevel + " max health and heal " + maxHealthPerLevel,
                        4, new Color (0.3f, 1f, 0.4f));
                case UpgradeType.Regeneration:
                    return new UpgradeInfo ("Regeneration", "Recover " + regenPerLevel + More (type) + " HP every second",
                        3, new Color (0.6f, 1f, 0.3f));
                case UpgradeType.Lifesteal:
                    return new UpgradeInfo ("Vampirism", "Heal " + lifestealPerLevel + More (type) + " HP for every kill",
                        3, new Color (0.85f, 0.2f, 0.45f));
                case UpgradeType.Luck:
                    return new UpgradeInfo ("Lucky Charm", "+" + Percent (dropChancePerLevel) + " drop chance, power-ups last " + powerUpDurationPerLevel + "s longer",
                        3, new Color (1f, 0.35f, 0.9f));
                case UpgradeType.ComboTime:
                    return new UpgradeInfo ("Combo Master", "+" + comboWindowPerLevel + "s to keep your combo going",
                        2, new Color (1f, 0.75f, 0.4f));
                default:
                    return new UpgradeInfo (type.ToString (), "", 1, Color.white);
            }
        }


        // " more" once the player already has the upgrade, so stacking reads naturally.
        string More (UpgradeType type)
        {
            return GetLevel (type) > 0 ? " more" : "";
        }


        static string Percent (float value)
        {
            return Mathf.RoundToInt (value * 100f) + "%";
        }
    }
}
