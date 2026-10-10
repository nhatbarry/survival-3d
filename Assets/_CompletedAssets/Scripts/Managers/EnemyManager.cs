using UnityEngine;

namespace CompleteProject
{
    public class EnemyManager : MonoBehaviour
    {
        public PlayerHealth playerHealth;       // Reference to the player's heatlh.
        public GameObject enemy;                // The enemy prefab to be spawned.
        public float spawnTime = 3f;            // How long between each spawn.
        public Transform[] spawnPoints;         // An array of the spawn points this enemy can spawn from.


        float timer;                            // Time since the last spawn.


        void Update ()
        {
            timer += Time.deltaTime;

            // The spawn interval shrinks as the waves go on.
            if(timer >= spawnTime * WaveManager.CurrentSpawnIntervalMultiplier)
            {
                timer = 0f;
                Spawn ();
            }
        }


        void Spawn ()
        {
            // If the player has no health left...
            if(playerHealth.currentHealth <= 0f)
            {
                // ... exit the function.
                return;
            }

            // Find a random index between zero and one less than the number of spawn points.
            int spawnPointIndex = Random.Range (0, spawnPoints.Length);

            // Create an instance of the enemy prefab at the randomly selected spawn point's position and rotation.
            GameObject spawned = Instantiate (enemy, spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);

            // Enemies get tougher in later waves.
            EnemyHealth enemyHealth = spawned.GetComponent <EnemyHealth> ();
            if(enemyHealth != null)
            {
                enemyHealth.ApplyHealthMultiplier (WaveManager.CurrentEnemyHealthMultiplier);
            }
        }
    }
}