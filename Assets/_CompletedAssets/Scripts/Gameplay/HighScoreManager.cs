using CaptainPinkTurd.Core.DesignPattern.Singleton;
using UnityEngine;

namespace CompleteProject
{
    /// <summary>
    /// Tracks the best score across sessions using PlayerPrefs.
    /// </summary>
    public class HighScoreManager : Singleton<HighScoreManager>
    {
        const string HighScoreKey = "HighScore";


        int previousBest;                           // The best score before this run started.

        public int BestScore { get; private set; }
        public bool IsNewBest => BestScore > previousBest;


        protected override void Awake ()
        {
            base.Awake ();

            previousBest = PlayerPrefs.GetInt (HighScoreKey, 0);
            BestScore = previousBest;
        }


        void OnEnable ()
        {
            PlayerHealth.Died += Save;
        }


        void OnDisable ()
        {
            PlayerHealth.Died -= Save;
            Save ();
        }


        void Update ()
        {
            if(ScoreManager.score > BestScore)
            {
                BestScore = ScoreManager.score;
            }
        }


        // Mobile apps are often killed in the background without OnDisable being called.
        void OnApplicationPause (bool paused)
        {
            if(paused)
            {
                Save ();
            }
        }


        public void Save ()
        {
            if(BestScore > PlayerPrefs.GetInt (HighScoreKey, 0))
            {
                PlayerPrefs.SetInt (HighScoreKey, BestScore);
                PlayerPrefs.Save ();
            }
        }
    }
}
