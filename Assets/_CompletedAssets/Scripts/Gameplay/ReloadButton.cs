using DG.Tweening;
using GeekGame.Input;
using UnityEngine;
using UnityEngine.UI;

namespace CompleteProject
{
    /// <summary>
    /// A reload button styled like the on-screen joysticks (same ring and handle sprites), sitting above-left of the shoot stick.
    /// The handle shows the rounds left, and the ring fills up while reloading.
    /// </summary>
    public class ReloadButton : MonoBehaviour
    {
        public Color knobColour = new Color (1f, 0.82f, 0.45f);
        public Color lowAmmoColour = new Color (1f, 0.35f, 0.3f);
        public Color progressColour = new Color (1f, 0.82f, 0.45f);
        [Range (0f, 1f)] public float lowAmmoFraction = 0.25f;     // Pulse the handle when the magazine is this empty.
        public Vector2 position = new Vector2 (-235f, 235f);        // Centre of the button, measured from the bottom-right corner.
        public float size = 100f;


        PlayerAmmo ammo;
        PlayerHealth playerHealth;
        RectTransform root;
        Image progress, knob;
        Text ammoText;
        int shownAmmo = -1, shownMagazine = -1;


        void Start ()
        {
            playerHealth = FindAnyObjectByType <PlayerHealth> ();
            ammo = playerHealth != null ? playerHealth.GetComponent <PlayerAmmo> () : null;

            ScoreManager scoreManager = FindAnyObjectByType <ScoreManager> ();
            if(ammo == null || scoreManager == null)
            {
                Debug.LogWarning ("ReloadButton: missing PlayerAmmo or HUD, the reload button is disabled.");
                enabled = false;
                return;
            }

            Text scoreText = scoreManager.GetComponent <Text> ();

            // Borrow the look and the canvas from the shoot joystick (handle = ShootButton, ring = its parent ShootImage).
            Image handleImage = null, ringImage = null;
            if(JoystickFire.instance != null)
            {
                handleImage = JoystickFire.instance.GetComponent <Image> ();
                if(JoystickFire.instance.transform.parent != null)
                    ringImage = JoystickFire.instance.transform.parent.GetComponent <Image> ();
            }

            Transform parent = ringImage != null ? ringImage.transform.parent : scoreText.canvas.transform;
            Build (parent, scoreText.font, ringImage != null ? ringImage.sprite : null, handleImage != null ? handleImage.sprite : null);
        }


        void OnDestroy ()
        {
            if(knob != null)
                knob.rectTransform.DOKill ();
        }


        void Update ()
        {
            if(playerHealth.IsDead)
            {
                root.gameObject.SetActive (false);
                return;
            }

            progress.enabled = ammo.IsReloading;
            progress.fillAmount = ammo.ReloadProgress;

            if(ammo.CurrentAmmo != shownAmmo || ammo.MagazineSize != shownMagazine)
            {
                shownAmmo = ammo.CurrentAmmo;
                shownMagazine = ammo.MagazineSize;
                ammoText.text = shownAmmo + "<size=13>/" + shownMagazine + "</size>";
            }

            if(ammo.IsReloading)
            {
                // Dim the handle while the magazine is out.
                knob.color = Color.Lerp (knobColour, Color.gray, 0.5f);
            }
            else if(ammo.CurrentAmmo <= ammo.MagazineSize * lowAmmoFraction)
            {
                // Pulse red to nudge the player to reload.
                knob.color = Color.Lerp (knobColour, lowAmmoColour, Mathf.PingPong (Time.unscaledTime * 4f, 1f));
            }
            else
            {
                knob.color = knobColour;
            }
        }


        void OnPressed ()
        {
            ammo.StartReload ();

            RectTransform knobRect = knob.rectTransform;
            knobRect.DOKill (true);
            knobRect.DOPunchScale (Vector3.one * -0.2f, 0.2f, 4);
        }


        void Build (Transform parent, Font font, Sprite ringSprite, Sprite handleSprite)
        {
            Vector2 centre = new Vector2 (0.5f, 0.5f);

            // The ring is also the touch area.
            Image ring = RuntimeUI.CreateImage ("ReloadButton", parent, new Vector2 (1f, 0f), position, Vector2.one * size,
                Color.white, true);
            ring.sprite = ringSprite;
            root = ring.rectTransform;
            root.pivot = centre;
            root.anchoredPosition = position;
            root.gameObject.AddComponent <PointerDownRelay> ().Pressed += OnPressed;

            // Same ring, filled clockwise from the top while reloading.
            progress = RuntimeUI.CreateImage ("Progress", root, centre, Vector2.zero, Vector2.one * size, progressColour);
            progress.sprite = ringSprite;
            progress.type = Image.Type.Filled;
            progress.fillMethod = Image.FillMethod.Radial360;
            progress.fillOrigin = (int) Image.Origin360.Top;
            progress.fillClockwise = true;
            progress.enabled = false;

            knob = RuntimeUI.CreateImage ("Knob", root, centre, Vector2.zero, Vector2.one * size * 0.5f, knobColour);
            knob.sprite = handleSprite;
            knob.rectTransform.pivot = centre;

            ammoText = RuntimeUI.CreateText ("Ammo", knob.transform, font, centre, Vector2.zero, new Vector2 (size, 40f),
                22, TextAnchor.MiddleCenter, Color.white);
            ammoText.rectTransform.pivot = centre;
            ammoText.rectTransform.anchoredPosition = Vector2.zero;

            Text label = RuntimeUI.CreateText ("Label", root, font, new Vector2 (0.5f, 1f), new Vector2 (0f, 2f), new Vector2 (size, 22f),
                16, TextAnchor.MiddleCenter, new Color (1f, 1f, 1f, 0.85f));
            label.rectTransform.pivot = new Vector2 (0.5f, 0f);
            label.rectTransform.anchoredPosition = new Vector2 (0f, 2f);
            label.text = "RELOAD";
        }
    }
}
