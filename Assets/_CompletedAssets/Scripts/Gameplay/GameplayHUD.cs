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
        public Color bigComboColour = new Color (1f, 0.62f, 0f);   // Deeper gold once the combo gets long.
        public int bigComboCount = 10;                              // Combo count that switches to bigComboColour.
        public Color powerUpColour = new Color (0.6f, 0.9f, 1f);
        public Color xpColour = new Color (0.35f, 0.75f, 1f);

        [Header ("Combo animation")]
        public float comboMaxScale = 1.2f;          // Scale right after a kill, when the combo window is full.
        public float comboMinScale = 0.55f;         // Scale just before the combo runs out.
        public float comboPopScale = 1.5f;          // Peak scale of the pop on each kill.
        public float comboPopUpTime = 0.08f;
        public float comboPopSettleTime = 0.15f;

        [Header ("Wave animation")]
        public float waveHoldTime = 1.2f;           // How long the big wave title stays in the middle.
        public float waveMoveTime = 0.6f;           // How long it takes to shrink up to the top edge.
        public float waveDockedScale = 0.5f;        // Scale of the wave label once docked at the top.


        static readonly Vector2 WaveTitleAnchor = new Vector2 (0.5f, 0.5f);
        static readonly Vector2 WaveTitlePosition = new Vector2 (0f, 120f);
        static readonly Vector2 WaveDockedAnchor = new Vector2 (0.5f, 1f);
        static readonly Vector2 WaveDockedPosition = new Vector2 (0f, -25f);


        RectTransform hudRoot;
        Font font;
        Text bestText, waveText, comboText, powerUpText, toastText, gameOverText, xpText;
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
        int shownComboCount;
        float comboPopElapsed;                      // Time since the last combo kill, drives the pop.
        float comboPopStartScale;
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
            // Starts as a big title above the player, then shrinks and docks at the top edge (see ShowWave).
            waveText = CreateText ("WaveText", WaveDockedAnchor, WaveDockedPosition, new Vector2 (500f, 80f),
                64, TextAnchor.MiddleCenter, waveColour);
            waveText.rectTransform.pivot = new Vector2 (0.5f, 0.5f);
            waveText.rectTransform.localScale = Vector3.one * waveDockedScale;
            // Bottom-centre, between the joysticks. Pivot in the middle so it scales around its centre.
            comboText = CreateText ("ComboText", new Vector2 (0.5f, 0f), new Vector2 (0f, 80f), new Vector2 (400f, 60f),
                44, TextAnchor.MiddleCenter, comboColour);
            comboText.rectTransform.pivot = new Vector2 (0.5f, 0.5f);
            // Top-right.
            powerUpText = CreateText ("PowerUpText", new Vector2 (1f, 1f), new Vector2 (-15f, -60f), new Vector2 (300f, 60f),
                22, TextAnchor.UpperRight, powerUpColour);
            toastText = CreateText ("ToastText", new Vector2 (0.5f, 0.5f), new Vector2 (0f, -90f), new Vector2 (500f, 50f),
                32, TextAnchor.MiddleCenter, Color.white);

            comboText.gameObject.SetActive (false);
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
            if(waveText != null) waveText.rectTransform.DOKill ();
            if(toastText != null) toastText.DOKill ();
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
            ShowWave ("WAVE " + shownWave);
        }


        // Pops the wave title in above the player, holds it, then shrinks it into the top-centre of the screen.
        void ShowWave (string message)
        {
            RectTransform rect = waveText.rectTransform;
            rect.DOKill ();

            waveText.text = message;
            rect.anchorMin = WaveTitleAnchor;
            rect.anchorMax = WaveTitleAnchor;
            rect.anchoredPosition = WaveTitlePosition;
            rect.localScale = Vector3.one * 0.6f;

            // Tweening the anchors and the position together slides it smoothly from the middle to the top edge.
            DOTween.Sequence ()
                .SetTarget (rect)
                .Append (rect.DOScale (1f, 0.3f).SetEase (Ease.OutBack))
                .AppendInterval (waveHoldTime)
                .Append (rect.DOAnchorMin (WaveDockedAnchor, waveMoveTime).SetEase (Ease.InOutCubic))
                .Join (rect.DOAnchorMax (WaveDockedAnchor, waveMoveTime).SetEase (Ease.InOutCubic))
                .Join (rect.DOAnchorPos (WaveDockedPosition, waveMoveTime).SetEase (Ease.InOutCubic))
                .Join (rect.DOScale (waveDockedScale, waveMoveTime).SetEase (Ease.InOutCubic));
        }


        void UpdateCombo ()
        {
            if(comboManager == null)
                return;

            int count = comboManager.ComboCount;

            // A combo starts with the second kill in a row.
            if(count < 2)
            {
                comboText.gameObject.SetActive (false);
                shownComboCount = count;
                return;
            }

            comboText.gameObject.SetActive (true);
            RectTransform rect = comboText.rectTransform;

            // Every new kill restarts the pop from whatever size the text has shrunk to.
            if(count != shownComboCount)
            {
                shownComboCount = count;
                comboText.text = "x" + count + " COMBO";
                comboText.color = count >= bigComboCount ? bigComboColour : comboColour;
                comboPopElapsed = 0f;
                // A brand new combo pops in from small.
                comboPopStartScale = count > 2 ? rect.localScale.x : 0.3f;
            }

            comboPopElapsed += Time.deltaTime;

            // Resting size shrinks as the combo window runs out.
            float restScale = Mathf.Lerp (comboMinScale, comboMaxScale, comboManager.TimeRemainingNormalized);
            float scale;

            if(comboPopElapsed < comboPopUpTime)
            {
                // Grow quickly up to the peak...
                scale = Mathf.Lerp (comboPopStartScale, comboPopScale, EaseOut (comboPopElapsed / comboPopUpTime));
            }
            else if(comboPopElapsed < comboPopUpTime + comboPopSettleTime)
            {
                // ... then settle back down to the resting size.
                scale = Mathf.Lerp (comboPopScale, restScale, EaseOut ((comboPopElapsed - comboPopUpTime) / comboPopSettleTime));
            }
            else
            {
                scale = restScale;
            }

            rect.localScale = Vector3.one * scale;
        }


        static float EaseOut (float t)
        {
            t = 1f - Mathf.Clamp01 (t);
            return 1f - t * t * t;
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
