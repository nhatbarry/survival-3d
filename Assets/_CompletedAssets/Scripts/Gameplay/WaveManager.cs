using CaptainPinkTurd.Core.DesignPattern.Singleton;
using UnityEngine;

namespace CompleteProject
{
    /// <summary>
    /// Advances the wave every waveDuration seconds. Each wave makes enemies spawn faster and gives them more health.
    /// </summary>
    public class WaveManager : Singleton<WaveManager>
    {
        public float waveDuration = 30f;                    // How long each wave lasts.
        public float spawnIntervalReductionPerWave = 0.1f;  // How much shorter the spawn interval gets each wave (0.1 = 10%).
        public float minSpawnIntervalMultiplier = 0.35f;    // The spawn interval never drops below this fraction of the original.
        public float healthIncreasePerWave = 0.15f;         // How much extra health enemies get each wave (0.15 = 15%).


        float timer;                                        // Time spent in the current wave.
        bool stopped;                                       // Waves stop advancing once the player is dead.

        public int CurrentWave { get; private set; } = 1;
        public float WaveTimeRemaining => Mathf.Max (0f, waveDuration - timer);
        public float SpawnIntervalMultiplier => Mathf.Max (minSpawnIntervalMultiplier, 1f - spawnIntervalReductionPerWave * (CurrentWave - 1));
        public float EnemyHealthMultiplier => 1f + healthIncreasePerWave * (CurrentWave - 1);

        // Safe to call even when there is no WaveManager in the scene.
        public static float CurrentSpawnIntervalMultiplier => HasInstance ? instance.SpawnIntervalMultiplier : 1f;
        public static float CurrentEnemyHealthMultiplier => HasInstance ? instance.EnemyHealthMultiplier : 1f;


        void OnEnable ()
        {
            PlayerHealth.Died += OnPlayerDied;
        }


        void OnDisable ()
        {
            PlayerHealth.Died -= OnPlayerDied;
        }


        void Update ()
        {
            if(stopped)
                return;

            timer += Time.deltaTime;

            if(timer >= waveDuration)
            {
                timer -= waveDuration;
                CurrentWave++;
            }
        }


        void OnPlayerDied ()
        {
            stopped = true;
        }
    }
}
