using UnityEngine;

namespace CompleteProject
{
    /// <summary>
    /// Timed power-ups that modify the player's gun. Works with both the desktop and the mobile shooting scripts.
    /// </summary>
    public class PlayerPowerUps : MonoBehaviour
    {
        public float duration = 8f;                         // How long each power-up lasts.
        public float rapidFireIntervalMultiplier = 0.5f;    // Rapid fire multiplies the time between bullets by this.
        public int damageMultiplier = 2;                    // Damage boost multiplies the damage per shot by this.


        PlayerShooting shooting;                            // Reference to the desktop shooting script.
        PlayerShootingMobile shootingMobile;                // Reference to the mobile shooting script.
        PlayerUpgrades upgrades;                            // Reference to the player's permanent upgrades.
        float baseInterval, baseIntervalMobile;             // The original time between bullets.
        int baseDamage, baseDamageMobile;                   // The original damage per shot.
        float rapidFireTimer;                               // Time left on rapid fire.
        float damageBoostTimer;                             // Time left on the damage boost.

        public float RapidFireTimeRemaining => rapidFireTimer;
        public float DamageBoostTimeRemaining => damageBoostTimer;


        void Awake ()
        {
            shooting = GetComponentInChildren <PlayerShooting> (true);
            shootingMobile = GetComponentInChildren <PlayerShootingMobile> (true);

            if(shooting != null)
            {
                baseInterval = shooting.timeBetweenBullets;
                baseDamage = shooting.damagePerShot;
            }

            if(shootingMobile != null)
            {
                baseIntervalMobile = shootingMobile.timeBetweenBullets;
                baseDamageMobile = shootingMobile.damagePerShot;
            }
        }


        void Update ()
        {
            if(rapidFireTimer <= 0f && damageBoostTimer <= 0f)
                return;

            rapidFireTimer = Mathf.Max (0f, rapidFireTimer - Time.deltaTime);
            damageBoostTimer = Mathf.Max (0f, damageBoostTimer - Time.deltaTime);

            // Reapplying every frame while active is cheap and handles expiry without extra bookkeeping.
            ApplyStats ();
        }


        public void Activate (PickupType type)
        {
            switch(type)
            {
                case PickupType.RapidFire:
                    rapidFireTimer = duration;
                    break;
                case PickupType.DamageBoost:
                    damageBoostTimer = duration;
                    break;
            }

            ApplyStats ();
        }


        /// <summary>
        /// Recalculates the gun stats from the base values, the active power-ups and the permanent upgrades.
        /// </summary>
        public void ApplyStats ()
        {
            // The upgrades are added at runtime, so look them up lazily.
            if(upgrades == null)
                upgrades = GetComponent <PlayerUpgrades> ();

            float intervalMultiplier = rapidFireTimer > 0f ? rapidFireIntervalMultiplier : 1f;
            float damageMult = damageBoostTimer > 0f ? damageMultiplier : 1f;

            if(upgrades != null)
            {
                intervalMultiplier *= upgrades.FireIntervalMultiplier;
                damageMult *= upgrades.DamageMultiplier;
            }

            if(shooting != null)
            {
                shooting.timeBetweenBullets = baseInterval * intervalMultiplier;
                shooting.damagePerShot = Mathf.RoundToInt (baseDamage * damageMult);
            }

            if(shootingMobile != null)
            {
                shootingMobile.timeBetweenBullets = baseIntervalMobile * intervalMultiplier;
                shootingMobile.damagePerShot = Mathf.RoundToInt (baseDamageMobile * damageMult);
            }
        }
    }
}
