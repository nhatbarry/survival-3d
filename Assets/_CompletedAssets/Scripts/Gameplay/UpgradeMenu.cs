using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CompleteProject
{
    /// <summary>
    /// Pauses the game on every level-up and lets the player pick one of a few random upgrades.
    /// The menu is built from code on its own overlay canvas.
    /// </summary>
    public class UpgradeMenu : MonoBehaviour
    {
        public int choiceCount = 3;                 // How many upgrades are offered each level.
        public float inputDelay = 0.4f;             // Real-time seconds before the cards accept taps, so a held joystick can't pick by accident.

        public static bool IsOpen { get; private set; }


        class Card
        {
            public RectTransform rect;
            public Button button;
            public Image accent;
            public Text title;
            public Text level;
            public Text description;
            public UpgradeType type;
        }


        LevelManager levelManager;
        PlayerUpgrades upgrades;
        PlayerHealth playerHealth;
        Font font;
        GameObject root;
        Text headerText;
        Card[] cards;
        float previousTimeScale = 1f;
        float openedAt;


        void Start ()
        {
            levelManager = LevelManager.TryGetInstance ();
            playerHealth = FindAnyObjectByType <PlayerHealth> ();
            upgrades = playerHealth != null ? playerHealth.GetComponent <PlayerUpgrades> () : null;

            ScoreManager scoreManager = FindAnyObjectByType <ScoreManager> ();
            font = scoreManager != null ? scoreManager.GetComponent <Text> ().font : null;

            if(levelManager == null || upgrades == null || font == null)
            {
                Debug.LogWarning ("UpgradeMenu: missing LevelManager, PlayerUpgrades or HUD font, the upgrade menu is disabled.");
                enabled = false;
                return;
            }

            Build ();
            root.SetActive (false);
        }


        void OnDestroy ()
        {
            // Never leave the game frozen if the menu goes away while open (e.g. scene reload).
            if(IsOpen)
            {
                IsOpen = false;
                Time.timeScale = previousTimeScale;
            }

            if(cards != null)
            {
                foreach(Card card in cards)
                    card.rect.DOKill ();
            }
        }


        void Update ()
        {
            if(IsOpen)
            {
                if(Time.unscaledTime - openedAt >= inputDelay)
                {
                    foreach(Card card in cards)
                        card.button.interactable = true;
                }
                return;
            }

            // Wait while the player is dead or the pause menu has stopped time.
            if(playerHealth.IsDead || Time.timeScale == 0f)
                return;

            if(levelManager.PendingLevelUps > 0)
            {
                Open ();
            }
        }


        void Open ()
        {
            levelManager.ConsumePendingLevelUp ();

            List<UpgradeType> choices = upgrades.GetRandomChoices (choiceCount);

            // Everything is maxed out, nothing to offer.
            if(choices.Count == 0)
                return;

            IsOpen = true;
            openedAt = Time.unscaledTime;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            headerText.text = "LEVEL " + (levelManager.Level - levelManager.PendingLevelUps) + "!";
            root.SetActive (true);

            for(int i = 0; i < cards.Length; i++)
            {
                Card card = cards[i];
                bool used = i < choices.Count;
                card.rect.gameObject.SetActive (used);
                if(!used)
                    continue;

                card.type = choices[i];
                UpgradeInfo info = upgrades.GetInfo (card.type);
                int currentLevel = upgrades.GetLevel (card.type);

                card.accent.color = info.colour;
                card.title.text = info.title;
                card.title.color = info.colour;
                card.level.text = currentLevel == 0 ? "NEW" : "LEVEL " + (currentLevel + 1) + " / " + info.maxLevel;
                card.description.text = info.description;
                card.button.interactable = false;

                // Pop the cards in one after another. SetUpdate(true) makes the tween ignore the paused time scale.
                card.rect.DOKill ();
                card.rect.localScale = Vector3.one * 0.6f;
                card.rect.DOScale (1f, 0.25f).SetEase (Ease.OutBack).SetDelay (i * 0.07f).SetUpdate (true);
            }

            // Keep the cards centred when fewer than choiceCount are offered.
            LayoutCards (choices.Count);
        }


        void Choose (int index)
        {
            if(!IsOpen || Time.unscaledTime - openedAt < inputDelay)
                return;

            upgrades.Apply (cards[index].type);
            Close ();
        }


        void Close ()
        {
            IsOpen = false;
            root.SetActive (false);
            Time.timeScale = previousTimeScale;
        }


        void Build ()
        {
            // Own canvas on top of everything, so the cards receive touches before the joysticks.
            root = new GameObject ("Upgrade Menu Canvas");
            root.transform.SetParent (transform, false);

            Canvas canvas = root.AddComponent <Canvas> ();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = root.AddComponent <CanvasScaler> ();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2 (960f, 540f);
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent <GraphicRaycaster> ();

            // Dark backdrop that also swallows touches meant for the game.
            Image backdrop = RuntimeUI.CreateImage ("Backdrop", root.transform, Vector2.zero, Vector2.zero, Vector2.zero,
                new Color (0f, 0f, 0f, 0.75f), true);
            RuntimeUI.Stretch (backdrop.rectTransform);

            Vector2 centre = new Vector2 (0.5f, 0.5f);
            headerText = RuntimeUI.CreateText ("Header", root.transform, font, centre, new Vector2 (0f, 205f), new Vector2 (600f, 70f),
                56, TextAnchor.MiddleCenter, new Color (1f, 0.85f, 0.2f));
            RuntimeUI.CreateText ("Subtitle", root.transform, font, centre, new Vector2 (0f, 158f), new Vector2 (600f, 30f),
                24, TextAnchor.MiddleCenter, new Color (1f, 1f, 1f, 0.8f)).text = "Choose an upgrade";

            cards = new Card[choiceCount];
            for(int i = 0; i < choiceCount; i++)
            {
                cards[i] = BuildCard (root.transform, i);
            }
        }


        Card BuildCard (Transform parent, int index)
        {
            Vector2 size = new Vector2 (250f, 290f);
            Vector2 centre = new Vector2 (0.5f, 0.5f);
            Vector2 top = new Vector2 (0.5f, 1f);

            Card card = new Card ();

            Image background = RuntimeUI.CreateImage ("Card" + index, parent, centre, new Vector2 (0f, -25f), size,
                new Color (0.1f, 0.1f, 0.14f, 0.95f), true);
            card.rect = background.rectTransform;

            card.button = background.gameObject.AddComponent <Button> ();
            card.button.targetGraphic = background;
            card.button.onClick.AddListener (() => Choose (index));

            card.accent = RuntimeUI.CreateImage ("Accent", card.rect, top, Vector2.zero, new Vector2 (size.x, 10f), Color.white);

            card.title = RuntimeUI.CreateText ("Title", card.rect, font, top, new Vector2 (0f, -25f), new Vector2 (230f, 44f),
                28, TextAnchor.MiddleCenter, Color.white);
            FitInside (card.title, 16);

            card.level = RuntimeUI.CreateText ("Level", card.rect, font, top, new Vector2 (0f, -72f), new Vector2 (230f, 24f),
                18, TextAnchor.MiddleCenter, new Color (1f, 1f, 1f, 0.6f));

            card.description = RuntimeUI.CreateText ("Description", card.rect, font, centre, new Vector2 (0f, -40f), new Vector2 (220f, 150f),
                24, TextAnchor.MiddleCenter, Color.white);
            FitInside (card.description, 14);

            return card;
        }


        void LayoutCards (int count)
        {
            const float spacing = 280f;

            for(int i = 0; i < count; i++)
            {
                float x = (i - (count - 1) * 0.5f) * spacing;
                cards[i].rect.anchoredPosition = new Vector2 (x, -25f);
            }
        }


        // Wraps the text and shrinks it if it doesn't fit its box.
        static void FitInside (Text text, int minSize)
        {
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minSize;
            text.resizeTextMaxSize = text.fontSize;
        }
    }
}
