using UnityEngine;
using UnitySampleAssets.CrossPlatformInput;
using GeekGame.Input;

namespace CompleteProject
{
    public class PlayerShootingMobile : MonoBehaviour
    {
        public int damagePerShot = 20;                  // The damage inflicted by each bullet.
        public float timeBetweenBullets = 0.15f;        // The time between each shot.
        public float range = 100f;                      // The distance the gun can fire.
        
        float timer;                                    // A timer to determine when to fire.
        int shootableMask;                              // A layer mask so the raycast only hits things on the shootable layer.
        ParticleSystem gunParticles;                    // Reference to the particle system.
        LineRenderer gunLine;                           // Reference to the line renderer.
        AudioSource gunAudio;                           // Reference to the audio source.
        Light gunLight;                                 // Reference to the light component.
		public Light faceLight;								// Duh
        float effectsDisplayTime = 0.2f;                // The proportion of the timeBetweenBullets that the effects will display for.
        GunBeams gunBeams;                              // Fires and draws the beams (more than one with the Multi Shot upgrade).
        PlayerUpgrades upgrades;                        // Reference to the player's upgrades.


        void Awake ()
        {
#if UNITY_EDITOR
            enabled = false;
#else
            enabled = true;
#endif
            // Create a layer mask for the Shootable layer.
            shootableMask = LayerMask.GetMask ("Shootable");

            // Set up the references.
            gunParticles = GetComponent<ParticleSystem> ();
            gunLine = GetComponent <LineRenderer> ();
            gunAudio = GetComponent<AudioSource> ();
            gunLight = GetComponent<Light> ();
            gunBeams = new GunBeams (transform, gunLine, shootableMask);
			//faceLight = GetComponentInChildren<Light> ();
        }


        void Update ()
        {
            // Add the time since Update was last called to the timer.
            timer += Time.deltaTime;


            // If there is input on the shoot direction stick and it's time to fire...
			if ((JoystickFire.instance.Fire) && timer >= timeBetweenBullets)
            {
                // ... shoot the gun
                Shoot();
			}




            // If the timer has exceeded the proportion of timeBetweenBullets that the effects should be displayed for...
            if(timer >= timeBetweenBullets * effectsDisplayTime)
            {
                // ... disable the effects.
                DisableEffects ();
            }
        }


        public void DisableEffects ()
        {
            // Disable the line renderer and the light.
            gunBeams.Disable ();
			faceLight.enabled = false;
            gunLight.enabled = false;
        }


        void Shoot ()
        {
            // Reset the timer.
            timer = 0f;

            // Play the gun shot audioclip.
            gunAudio.Play ();

            // Enable the lights.
            gunLight.enabled = true;
			faceLight.enabled = true;

            // Stop the particles from playing if they were, then start the particles.
            gunParticles.Stop ();
            gunParticles.Play ();

            // The upgrades are added at runtime, so look them up lazily.
            if(upgrades == null)
                upgrades = GetComponentInParent <PlayerUpgrades> ();

            int beamCount = upgrades != null ? upgrades.BeamCount : 1;
            int pierceCount = upgrades != null ? upgrades.PierceCount : 0;

            // Fire the beams, damaging any enemies they hit, and draw the lines.
            gunBeams.Fire (range, damagePerShot, beamCount, pierceCount);
        }
    }
}