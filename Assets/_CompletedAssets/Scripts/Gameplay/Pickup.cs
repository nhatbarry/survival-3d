using System;
using CaptainPinkTurd.Core.Utilities;
using UnityEngine;

namespace CompleteProject
{
    public enum PickupType
    {
        Health,
        RapidFire,
        DamageBoost
    }

    /// <summary>
    /// A collectable dropped by enemies. Spins and bobs in place, blinks before it expires and is returned to the pool.
    /// </summary>
    public class Pickup : MonoBehaviour
    {
        public PickupType type;
        public int healAmount = 25;             // Health restored by a Health pickup.
        public float lifetime = 10f;            // How long the pickup stays on the floor.
        public float blinkTime = 3f;            // The pickup blinks for this many seconds before it disappears.
        public float rotateSpeed = 120f;        // Degrees per second.
        public float bobHeight = 0.2f;
        public float bobSpeed = 3f;

        public static event Action<Pickup> Collected;   // Raised when the player picks something up.


        Renderer rend;
        float age;
        Vector3 basePosition;
        bool despawned;


        void Awake ()
        {
            rend = GetComponent <Renderer> ();
        }


        void OnEnable ()
        {
            // Reset state, since pooled pickups get reused.
            age = 0f;
            despawned = false;
            basePosition = transform.position;
            rend.enabled = true;
        }


        void Update ()
        {
            age += Time.deltaTime;

            if(age >= lifetime)
            {
                Despawn ();
                return;
            }

            transform.Rotate (Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
            transform.position = basePosition + Vector3.up * (Mathf.Sin (age * bobSpeed) * bobHeight);

            // Blink to warn the player that the pickup is about to disappear.
            if(lifetime - age < blinkTime)
            {
                rend.enabled = Mathf.FloorToInt (age * 8f) % 2 == 0;
            }
        }


        void OnTriggerEnter (Collider other)
        {
            if(despawned || !other.CompareTag ("Player"))
                return;

            PlayerHealth playerHealth = other.GetComponent <PlayerHealth> ();
            if(playerHealth == null || playerHealth.IsDead)
                return;

            if(type == PickupType.Health)
            {
                playerHealth.Heal (healAmount);
            }
            else
            {
                PlayerPowerUps powerUps = other.GetComponent <PlayerPowerUps> ();
                if(powerUps != null)
                {
                    powerUps.Activate (type);
                }
            }

            Collected?.Invoke (this);
            Despawn ();
        }


        void Despawn ()
        {
            if(despawned)
                return;

            despawned = true;
            ObjectPoolManager.Instance.ReturnObjectToPool (gameObject);
        }
    }
}
