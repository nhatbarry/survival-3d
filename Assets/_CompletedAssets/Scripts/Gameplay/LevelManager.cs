using CaptainPinkTurd.Core.DesignPattern.Singleton;
using UnityEngine;

namespace CompleteProject
{
    /// <summary>
    /// Turns score into levels. Each level needs more score than the last, and every level-up is queued
    /// so the UpgradeMenu can offer one upgrade per level.
    /// </summary>
    public class LevelManager : Singleton<LevelManager>
    {
        public int baseScoreToLevel = 50;           // Score needed to go from level 1 to level 2.
        public int extraScorePerLevel = 35;         // How much more score each following level needs.


        int levelStartScore;                        // The total score at which the current level started.

        public int Level { get; private set; } = 1;
        public int PendingLevelUps { get; private set; }
        public int ScoreIntoLevel => Mathf.Max (0, ScoreManager.score - levelStartScore);
        public int ScoreForNextLevel => ScoreRequired (Level);
        public float Progress => Mathf.Clamp01 ((float) ScoreIntoLevel / ScoreForNextLevel);


        public int ScoreRequired (int level)
        {
            return baseScoreToLevel + extraScorePerLevel * (level - 1);
        }


        void Update ()
        {
            // A big combo kill can be worth more than one level.
            while(ScoreManager.score - levelStartScore >= ScoreForNextLevel)
            {
                levelStartScore += ScoreForNextLevel;
                Level++;
                PendingLevelUps++;
            }
        }


        public bool ConsumePendingLevelUp ()
        {
            if(PendingLevelUps == 0)
                return false;

            PendingLevelUps--;
            return true;
        }
    }
}
