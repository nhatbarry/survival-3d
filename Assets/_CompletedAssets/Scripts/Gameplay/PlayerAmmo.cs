using UnityEngine;
using UnityEngine.InputSystem;

namespace CompleteProject
{
    /// <summary>
    /// Magazine and reloading. Each trigger pull uses one round no matter how many beams it fires.
    /// The gun reloads automatically when the magazine runs dry, or manually with the reload button / R key.
    /// </summary>
    public class PlayerAmmo : MonoBehaviour
    {
        public int baseMagazineSize = 20;           // Rounds in a magazine before any upgrades.
        public float reloadTime = 1.4f;             // Seconds a reload takes.
        public float reloadTimeMultiplier = 1f;     // Lowered by the Fast Hands upgrade.


        PlayerHealth playerHealth;
        bool isReloading;
        float reloadElapsed;

        public int CurrentAmmo { get; private set; }
        public int MagazineSize { get; private set; }
        public bool IsReloading => isReloading;
        public float ReloadDuration => reloadTime * reloadTimeMultiplier;
        public float ReloadProgress => isReloading ? Mathf.Clamp01 (reloadElapsed / ReloadDuration) : 0f;


        void Awake ()
        {
            playerHealth = GetComponent <PlayerHealth> ();
            MagazineSize = baseMagazineSize;
            CurrentAmmo = MagazineSize;
        }


        void Update ()
        {
            if(playerHealth != null && playerHealth.IsDead)
            {
                isReloading = false;
                return;
            }

            if(Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                StartReload ();
            }

            if(isReloading)
            {
                reloadElapsed += Time.deltaTime;

                if(reloadElapsed >= ReloadDuration)
                {
                    CurrentAmmo = MagazineSize;
                    isReloading = false;
                }
            }
        }


        /// <summary>
        /// Uses one round. Returns false if the gun can't fire right now (empty or reloading).
        /// </summary>
        public bool TryConsume ()
        {
            if(isReloading)
                return false;

            if(CurrentAmmo <= 0)
            {
                StartReload ();
                return false;
            }

            CurrentAmmo--;

            // Reload straight away on the last round so the player doesn't have to.
            if(CurrentAmmo == 0)
                StartReload ();

            return true;
        }


        public void StartReload ()
        {
            if(isReloading || CurrentAmmo >= MagazineSize)
                return;
            if(playerHealth != null && playerHealth.IsDead)
                return;

            isReloading = true;
            reloadElapsed = 0f;
        }


        public void AddMagazineCapacity (int amount)
        {
            MagazineSize += amount;

            // The extra rounds are loaded right away, unless a reload will refill the magazine anyway.
            if(!isReloading)
                CurrentAmmo += amount;
        }
    }
}
