using CaptainPinkTurd.Core.DesignPattern.Singleton;
using UnityEngine;

namespace CompleteProject
{
    /// <summary>
    /// Kills made in quick succession build a combo that multiplies the score of each kill.
    /// </summary>
    public class ComboManager : Singleton<ComboManager>
    {
        public float comboWindow = 2.5f;            // Seconds the player has to make the next kill before the combo resets.
        public int killsPerMultiplierStep = 2;      // How many kills it takes to raise the multiplier by one.
        public int maxMultiplier = 5;               // The highest the multiplier can go.


        int comboCount;                             // Number of kills in the current combo.
        float timer;                                // Time left before the combo resets.

        public int ComboCount => comboCount;
        public int Multiplier => comboCount == 0 ? 1 : Mathf.Min (maxMultiplier, 1 + (comboCount - 1) / killsPerMultiplierStep);
        public float TimeRemainingNormalized => comboCount == 0 ? 0f : timer / comboWindow;


        void Update ()
        {
            if(comboCount == 0)
                return;

            timer -= Time.deltaTime;

            if(timer <= 0f)
            {
                comboCount = 0;
            }
        }


        /// <summary>
        /// Adds a kill to the combo and returns the score multiplier for that kill.
        /// </summary>
        public int RegisterKill ()
        {
            comboCount++;
            timer = comboWindow;
            return Multiplier;
        }
    }
}
