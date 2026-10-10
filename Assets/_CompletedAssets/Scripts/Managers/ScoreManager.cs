using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace CompleteProject
{
    public class ScoreManager : MonoBehaviour
    {
        public static int score;        // The player's score.


        Text text;                      // Reference to the Text component.


        void Awake ()
        {
            // Set up the reference.
            text = GetComponent <Text> ();

            // Reset the score.
            score = 0;
        }


        void Update ()
        {
            // With the level system running the score is shown as XP on the HUD's XP bar, so display the level here.
            if(LevelManager.HasInstance)
            {
                text.text = "LV " + LevelManager.Instance.Level;
            }
            else
            {
                // Set the displayed text to be the word "Score" followed by the score value.
                text.text = "Score: " + score;
            }
        }
    }
}