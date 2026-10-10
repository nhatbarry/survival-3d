using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

namespace CompleteProject
{
    public class PlayerHealth : MonoBehaviour
    {
        public int startingHealth = 100;                            // The amount of health the player starts the game with.
        public int currentHealth;                                   // The current health the player has.
        public Slider healthSlider;                                 // Reference to the UI's health bar.
        public Image damageImage;                                   // Reference to an image to flash on the screen on being hurt.
        public AudioClip deathClip;                                 // The audio clip to play when the player dies.
        public float flashSpeed = 5f;                               // The speed the damageImage will fade at.
        public Color flashColour = new Color(1f, 0f, 0f, 0.1f);     // The colour the damageImage is set to, to flash.
        public Color healFlashColour = new Color(0f, 1f, 0.3f, 0.1f); // The colour the damageImage is set to when the player is healed.

        public static event Action Died;                            // Raised when the player dies.


        Animator anim;                                              // Reference to the Animator component.
        AudioSource playerAudio;                                    // Reference to the AudioSource component.
        PlayerMovement playerMovement;                              // Reference to the player's movement.
        PlayerShooting playerShooting;                              // Reference to the PlayerShooting script.
        PlayerMovementMobile playerMovementMobile;                  // Reference to the player's mobile movement.
        PlayerShootingMobile playerShootingMobile;                  // Reference to the PlayerShootingMobile script.
        bool isDead;                                                // Whether the player is dead.
        bool damaged;                                               // True when the player gets damaged.
        bool healed;                                                // True when the player gets healed.

        public bool IsDead => isDead;


        void Awake ()
        {
            // Setting up the references.
            anim = GetComponent <Animator> ();
            playerAudio = GetComponent <AudioSource> ();
            playerMovement = GetComponent <PlayerMovement> ();
            playerShooting = GetComponentInChildren <PlayerShooting> ();
            playerMovementMobile = GetComponent <PlayerMovementMobile> ();
            playerShootingMobile = GetComponentInChildren <PlayerShootingMobile> ();

            // Set the initial health of the player.
            currentHealth = startingHealth;
        }


        void Update ()
        {
            // If the player has just been damaged...
            if(damaged)
            {
                // ... set the colour of the damageImage to the flash colour.
                damageImage.color = flashColour;
            }
            // If the player has just been healed...
            else if(healed)
            {
                // ... flash the heal colour instead.
                damageImage.color = healFlashColour;
            }
            // Otherwise...
            else
            {
                // ... transition the colour back to clear.
                damageImage.color = Color.Lerp (damageImage.color, Color.clear, flashSpeed * Time.deltaTime);
            }

            // Reset the damaged and healed flags.
            damaged = false;
            healed = false;
        }


        public void Heal (int amount, bool flash = true)
        {
            // Dead players can't be healed.
            if(isDead)
                return;

            // Set the healed flag so the screen will flash.
            if(flash)
                healed = true;

            // Restore health without going over the maximum.
            currentHealth = Mathf.Min (currentHealth + amount, startingHealth);

            // Set the health bar's value to the current health.
            healthSlider.value = currentHealth;
        }


        public void IncreaseMaxHealth (int amount)
        {
            if(isDead)
                return;

            // Raise the maximum, stretch the health bar to match and give the new health to the player.
            startingHealth += amount;
            healthSlider.maxValue = startingHealth;
            Heal (amount);
        }


        public void TakeDamage (int amount)
        {
            // Set the damaged flag so the screen will flash.
            damaged = true;

            // Reduce the current health by the damage amount.
            currentHealth -= amount;

            // Set the health bar's value to the current health.
            healthSlider.value = currentHealth;

            // Play the hurt sound effect.
            playerAudio.Play ();

            // If the player has lost all it's health and the death flag hasn't been set yet...
            if(currentHealth <= 0 && !isDead)
            {
                // ... it should die.
                Death ();
            }
        }


        void Death ()
        {
            // Set the death flag so this function won't be called again.
            isDead = true;

            // Turn off any remaining shooting effects.
            playerShooting.DisableEffects ();
            if(playerShootingMobile != null)
                playerShootingMobile.DisableEffects ();

            // Tell the animator that the player is dead.
            anim.SetTrigger ("Die");

            // Set the audiosource to play the death clip and play it (this will stop the hurt sound from playing).
            playerAudio.clip = deathClip;
            playerAudio.Play ();

            // Turn off the movement and shooting scripts.
            playerMovement.enabled = false;
            playerShooting.enabled = false;
            if(playerMovementMobile != null)
                playerMovementMobile.enabled = false;
            if(playerShootingMobile != null)
                playerShootingMobile.enabled = false;

            Died?.Invoke ();
        }


        public void RestartLevel ()
        {
            // Reload the level that is currently loaded.
            SceneManager.LoadScene (0);
        }
    }
}