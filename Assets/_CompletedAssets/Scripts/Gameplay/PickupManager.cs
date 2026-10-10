using System.Collections.Generic;
using CaptainPinkTurd.Core.DesignPattern.Singleton;
using CaptainPinkTurd.Core.Utilities;
using UnityEngine;

namespace CompleteProject
{
    /// <summary>
    /// Gives enemies a chance to drop a pickup when they die.
    /// The pickup visuals are built from primitives at runtime, so no prefabs are needed.
    /// </summary>
    public class PickupManager : Singleton<PickupManager>
    {
        [Range (0f, 1f)] public float dropChance = 0.2f;   // Chance that a killed enemy drops something.
        public int healAmount = 25;                         // Health restored by a Health pickup.
        public float pickupLifetime = 10f;                  // How long a pickup stays on the floor.
        public float spawnHeight = 0.6f;                    // Height above the floor that pickups float at.

        [Header ("Drop weights")]
        public float healthWeight = 0.45f;
        public float rapidFireWeight = 0.3f;
        public float damageBoostWeight = 0.25f;

        [Header ("Colours")]
        public Color healthColour = new Color (0.3f, 1f, 0.4f);
        public Color rapidFireColour = new Color (1f, 0.85f, 0.2f);
        public Color damageBoostColour = new Color (1f, 0.35f, 0.9f);


        readonly Dictionary<PickupType, GameObject> templates = new Dictionary<PickupType, GameObject> ();
        PlayerHealth playerHealth;


        protected override void Awake ()
        {
            base.Awake ();
            BuildTemplates ();
        }


        void Start ()
        {
            playerHealth = FindAnyObjectByType <PlayerHealth> ();
        }


        void OnEnable ()
        {
            EnemyHealth.Killed += OnEnemyKilled;
        }


        void OnDisable ()
        {
            EnemyHealth.Killed -= OnEnemyKilled;
        }


        void OnEnemyKilled (EnemyHealth enemy)
        {
            if(playerHealth != null && playerHealth.IsDead)
                return;

            if(Random.value > dropChance)
                return;

            Vector3 position = enemy.transform.position;
            position.y = spawnHeight;

            ObjectPoolManager.Instance.SpawnObject (templates[RollType ()], position, Quaternion.identity,
                ObjectPoolManager.PoolType.Collectable);
        }


        PickupType RollType ()
        {
            float roll = Random.value * (healthWeight + rapidFireWeight + damageBoostWeight);

            if(roll < healthWeight)
                return PickupType.Health;
            if(roll < healthWeight + rapidFireWeight)
                return PickupType.RapidFire;
            return PickupType.DamageBoost;
        }


        void BuildTemplates ()
        {
            // Templates live under an inactive parent so they never run themselves,
            // but their clones (which get no parent from Instantiate) start out active.
            GameObject templateRoot = new GameObject ("Pickup Templates");
            templateRoot.SetActive (false);
            templateRoot.transform.SetParent (transform);

            templates[PickupType.Health] = BuildTemplate (templateRoot.transform, PickupType.Health,
                PrimitiveType.Cube, new Vector3 (0.45f, 0.45f, 0.45f), healthColour);
            templates[PickupType.RapidFire] = BuildTemplate (templateRoot.transform, PickupType.RapidFire,
                PrimitiveType.Capsule, new Vector3 (0.3f, 0.3f, 0.3f), rapidFireColour);
            templates[PickupType.DamageBoost] = BuildTemplate (templateRoot.transform, PickupType.DamageBoost,
                PrimitiveType.Sphere, new Vector3 (0.5f, 0.5f, 0.5f), damageBoostColour);
        }


        GameObject BuildTemplate (Transform parent, PickupType type, PrimitiveType shape, Vector3 scale, Color colour)
        {
            GameObject go = GameObject.CreatePrimitive (shape);
            go.name = "Pickup_" + type;
            go.transform.SetParent (parent);
            go.transform.localScale = scale;

            go.GetComponent <Collider> ().isTrigger = true;

            Renderer rend = go.GetComponent <Renderer> ();
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.material.color = colour;

            // A small point light makes the pickup easy to spot in the dark level.
            GameObject glow = new GameObject ("Glow");
            glow.transform.SetParent (go.transform, false);
            Light light = glow.AddComponent <Light> ();
            light.type = LightType.Point;
            light.color = colour;
            light.range = 2.5f;
            light.intensity = 2f;

            Pickup pickup = go.AddComponent <Pickup> ();
            pickup.type = type;
            pickup.healAmount = healAmount;
            pickup.lifetime = pickupLifetime;

            return go;
        }
    }
}
