using UnityEngine;
using UnityEngine.SceneManagement;

namespace CompleteProject
{
    /// <summary>
    /// Adds the gameplay feature components to any scene that contains a player, so they work without editing the scene.
    /// A component that is already in the scene (e.g. added by hand to tweak its values in the Inspector) is left alone.
    /// </summary>
    public static class GameplayBootstrap
    {
        [RuntimeInitializeOnLoadMethod (RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize ()
        {
            // Unsubscribe first in case domain reload is disabled in Enter Play Mode settings.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }


        static void OnSceneLoaded (Scene scene, LoadSceneMode mode)
        {
            PlayerHealth player = Object.FindAnyObjectByType <PlayerHealth> ();
            if(player == null)
                return;

            if(player.GetComponent <PlayerPowerUps> () == null)
                player.gameObject.AddComponent <PlayerPowerUps> ();
            // PlayerAmmo before PlayerUpgrades, which looks it up in Awake.
            if(player.GetComponent <PlayerAmmo> () == null)
                player.gameObject.AddComponent <PlayerAmmo> ();
            if(player.GetComponent <PlayerUpgrades> () == null)
                player.gameObject.AddComponent <PlayerUpgrades> ();

            GameObject host = null;
            Ensure <WaveManager> (ref host);
            Ensure <ComboManager> (ref host);
            Ensure <HighScoreManager> (ref host);
            Ensure <PickupManager> (ref host);
            Ensure <LevelManager> (ref host);
            Ensure <GameplayHUD> (ref host);
            Ensure <UpgradeMenu> (ref host);
            Ensure <ReloadButton> (ref host);
        }


        static void Ensure <T> (ref GameObject host) where T : Component
        {
            if(Object.FindAnyObjectByType <T> () != null)
                return;

            if(host == null)
                host = new GameObject ("Gameplay Features");

            host.AddComponent <T> ();
        }
    }
}
