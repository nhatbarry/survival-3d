using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CompleteProject
{
    /// <summary>
    /// Builds the extra HUD elements (XP bar, best score, wave, combo, power-ups, announcements) on the existing HUD canvas.
    /// </summary>
    public class GameplayHUD : MonoBehaviour
    {
        public Color waveColour = new Color (1f, 0.55f, 0.2f);
        public Color comboColour = new Color (1f, 0.85f, 0.2f);
        public Color powerUpColour = new Color (0.6f, 0.9f, 1f);
        public Color xpColour = new Color (0.35f, 0.75f, 1f);


        RectTransform hudRoot;
        Font font;
        Text bestText, waveText, comboText, powerUpText, announceText, toastText, gameOverText, xpText;
        RectTransform xpFill;

        PlayerHealth playerHealth;
        PlayerPowerUps powerUps;
        WaveManager waveManager;
        ComboManager comboManager;
        HighScoreManager highScoreManager;
        LevelManager levelManager;

        int shownLevel = 1;
        int shownScoreIntoLevel = -1;
        float shownProgress;
        int shownWave;
        int shownMultiplier = 1;
        int shownBest = -1;
        int shownRapidFire = -1, shownDamageBoost = -1;
        bool gameOverShown;


        void Start ()
        {
            ScoreManager scoreManager = FindAnyObjectByType <ScoreManager> ();
            if(scoreManager == null)
            {
                Debug.LogWarning ("GameplayHUD: no ScoreManager found, the extra HUD is disabled.");
                enabled = false;
                return;
            }

            Text scoreText = scoreManager.GetComponent <Text> ();
            hudRoot = scoreText.canvas.transform as RectTransform;
            font = scoreText.font;

            playerHealth = FindAnyObjectByType <PlayerHealth> ();
            powerUps = playerHealth != null ? playerHealth.GetComponent <PlayerPowerUps> () : null;
            waveManager = WaveManager.TryGetInstance ();
            comboManager = ComboManager.TryGetInstance ();
            highScoreManager = HighScoreManager.TryGetInstance ();
            levelManager = LevelManager.TryGetInstance ();

            foreach(Text text in hudRoot.GetComponentsInChildren <Text> (true))
            {
                if(text.name == "GameOverText")
                    gameOverText = text;
            }

            // Top-left, under the level: the XP bar, then the best score.
            if(levelManager != null)
            {
                Image xpBackground = RuntimeUI.CreateImage ("XPBar", hudRoot, new Vector2 (0f, 1f), new Vector2 (15f, -68f),
                    new Vector2 (190f, 20f), new Color (0f, 0f, 0f, 0.55f));
                xpFill = RuntimeUI.CreateImage ("Fill", xpBackground.transform, Vector2.zero, Vector2.zero, Vector2.zero, xpColour).rectTransform;
                RuntimeUI.Stretch (xpFill);
                xpFill.anchorMax = new Vector2 (0f, 1f);
                xpText = RuntimeUI.CreateText ("XPText", xpBackground.transform, font, new Vector2 (0.5f, 0.5f), Vector2.zero,
                    new Vector2 (190f, 20f), 15, TextAnchor.MiddleCenter, Color.white);
            }
            bestText = CreateText ("BestText", new Vector2 (0f, 1f), new Vector2 (10f, -92f), new Vector2 (200f, 30f),
                22, TextAnchor.MiddleCenter, new Color (1f, 1f, 1f, 0.7f));
            // Top-centre.
            waveText = CreateText ("WaveText", new Vector2 (0.5f, 1f), new Vector2 (0f, -10f), new Vector2 (300f, 40f),
                30, TextAnchor.MiddleCenter, waveColour);
            // Top-right.
            comboText = CreateText ("ComboText", new Vector2 (1f, 1f), new Vector2 (-15f, -10f), new Vector2 (250f, 45f),
                36, TextAnchor.MiddleRight, comboColour);
            powerUpText = CreateText ("PowerUpText", new Vector2 (1f, 1f), new Vector2 (-15f, -60f), new Vector2 (300f, 60f),
                22, TextAnchor.UpperRight, powerUpColour);
            // Centre of the screen, faded out until something happens.
            announceText = CreateText ("AnnounceText", new Vector2 (0.5f, 0.5f), new Vector2 (0f, 120f), new Vector2 (500f, 80f),
                64, TextAnchor.MiddleCenter, waveColour);
            toastText = CreateText ("ToastText", new Vector2 (0.5f, 0.5f), new Vector2 (0f, -90f), new Vector2 (500f, 50f),
                32, TextAnchor.MiddleCenter, Color.white);

            comboText.gameObject.SetActive (false);
            announceText.color = Color.clear;
            toastText.color = Color.clear;
        }


        void OnEnable ()
        {
            Pickup.Collected += OnPickupCollected;
        }


        void OnDisable ()
        {
            Pickup.Collected -= OnPickupCollected;
        }


        void OnDestroy ()
        {
            if(announceText != null) announceText.DOKill ();
            if(toastText != null) toastText.DOKill ();
            if(comboText != null) comboText.rectTransform.DOKill ();
        }


        void Update ()
        {
            if(playerHealth != null && playerHealth.IsDead)
            {
                ShowGameOver ();
                return;
            }

            UpdateXP ();
            UpdateBest ();
            UpdateWave ();
            UpdateCombo ();
            UpdatePowerUps ();
        }


        void UpdateXP ()
        {
            if(levelManager == null)
                return;

            // Empty the bar straight away on level-up instead of sliding it backwards.
            if(levelManager.Level != shownLevel)
            {
                shownLevel = levelManager.Level;
                shownProgress = 0f;
            }

            shownProgress = Mathf.MoveTowards (shownProgress, levelManager.Progress, 2f * Time.unscaledDeltaTime);
            xpFill.anchorMax = new Vector2 (shownProgress, 1f);

            if(levelManager.ScoreIntoLevel != shownScoreIntoLevel)
            {
                shownScoreIntoLevel = levelManager.ScoreIntoLevel;
                xpText.text = shownScoreIntoLevel + " / " + levelManager.ScoreForNextLevel;
            }
        }


        void UpdateBest ()
        {
            if(highScoreManager == null || highScoreManager.BestScore == shownBest)
                return;

            shownBest = highScoreManager.BestScore;
            bestText.text = "Best: " + shownBest;
        }


        void UpdateWave ()
        {
            if(waveManager == null || waveManager.CurrentWave == shownWave)
                return;

            shownWave = waveManager.CurrentWave;
            waveText.text = "WAVE " + shownWave;
            Flash (announceText, "WAVE " + shownWave, waveColour, 1.5f);
        }


        void UpdateCombo ()
        {
            if(comboManager == null)
                return;

            int multiplier = comboManager.Multiplier;

            if(multiplier > 1)
            {
                comboText.gameObject.SetActive (true);

                // Fade the text out as the combo window runs out.
                Color colour = comboColour;
                colour.a = Mathf.Lerp (0.35f, 1f, comboManager.TimeRemainingNormalized);
                comboText.color = colour;

                if(multiplier != shownMultiplier)
                {
                    comboText.text = "x" + multiplier + " COMBO";

                    if(multiplier > shownMultiplier)
                    {
                        comboText.rectTransform.DOKill (true);
                        comboText.rectTransform.DOPunchScale (Vector3.one * 0.4f, 0.3f, 6);
                    }
                }
            }
            else
            {
                comboText.gameObject.SetActive (false);
            }

            shownMultiplier = multiplier;
        }


        void UpdatePowerUps ()
        {
            if(powerUps == null)
                return;

            int rapidFire = Mathf.CeilToInt (powerUps.RapidFireTimeRemaining);
            int damageBoost = Mathf.CeilToInt (powerUps.DamageBoostTimeRemaining);

            // Only rebuild the string when the displayed seconds change.
            if(rapidFire == shownRapidFire && damageBoost == shownDamageBoost)
                return;

            shownRapidFire = rapidFire;
            shownDamageBoost = damageBoost;

            string text = "";
            if(rapidFire > 0)
                text += "RAPID FIRE  " + rapidFire + "s\n";
            if(damageBoost > 0)
                text += "DAMAGE x" + powerUps.damageMultiplier + "  " + damageBoost + "s";

            powerUpText.text = text;
        }


        void ShowGameOver ()
        {
            if(gameOverShown)
                return;

            gameOverShown = true;

            bestText.gameObject.SetActive (false);
            waveText.gameObject.SetActive (false);
            comboText.gameObject.SetActive (false);
            powerUpText.gameObject.SetActive (false);
            announceText.gameObject.SetActive (false);
            toastText.gameObject.SetActive (false);
            if(xpFill != null)
                xpFill.parent.gameObject.SetActive (false);

            if(gameOverText == null || highScoreManager == null)
                return;

            highScoreManager.Save ();

            string summary = highScoreManager.IsNewBest
                ? "<color=#FFD633>NEW BEST: " + highScoreManager.BestScore + "</color>"
                : "Score: " + ScoreManager.score + "   Best: " + highScoreManager.BestScore;

            string reached = "";
            if(levelManager != null)
                reached += "Level " + levelManager.Level + "   ";
            if(waveManager != null)
                reached += "Wave " + waveManager.CurrentWave;
            if(reached.Length > 0)
                summary += "\nReached " + reached.Trim ();

            gameOverText.text = "Game Over!\n<size=28>" + summary + "</size>";
        }


        void OnPickupCollected (Pickup pickup)
        {
            if(toastText == null)
                return;

            switch(pickup.type)
            {
                case PickupType.Health:
                    Flash (toastText, "+" + pickup.healAmount + " HP", new Color (0.3f, 1f, 0.4f), 1f);
                    break;
                case PickupType.RapidFire:
                    Flash (toastText, "RAPID FIRE!", new Color (1f, 0.85f, 0.2f), 1f);
                    break;
                case PickupType.DamageBoost:
                    string multiplier = powerUps != null ? powerUps.damageMultiplier.ToString () : "2";
                    Flash (toastText, "DAMAGE x" + multiplier + "!", new Color (1f, 0.35f, 0.9f), 1f);
                    break;
            }
        }


        // Shows the text, pops its scale and fades it out after holdTime seconds.
        void Flash (Text target, string message, Color colour, float holdTime)
        {
            target.DOKill ();
            target.rectTransform.DOKill ();

            target.text = message;
            target.color = colour;
            target.rectTransform.localScale = Vector3.one * 1.3f;
            target.rectTransform.DOScale (1f, 0.25f).SetEase (Ease.OutBack);
            target.DOFade (0f, 0.6f).SetDelay (holdTime);
        }


        Text CreateText (string objectName, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment, Color colour)
        {
            return RuntimeUI.CreateText (objectName, hudRoot, font, anchor, position, size, fontSize, alignment, colour);
        }
    }
}
