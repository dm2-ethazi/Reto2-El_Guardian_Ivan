using UnityEngine;
using UnityEngine.SceneManagement;

namespace PuertaCastillo.Gameplay
{
    /// <summary>
    /// Música de fondo en bucle. Se crea sola al arrancar el juego (no depende de objetos de escena)
    /// y elige la pista según la escena cargada: menu_musica en el menú y batalla_musica en el juego.
    /// Las pistas están en Assets/Resources/Musica. Si la escena nueva usa la misma pista
    /// (reiniciar el nivel), la música sigue sin cortarse.
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        public const string MenuTrack = "Musica/menu_musica";
        public const string BattleTrack = "Musica/batalla_musica";
        private const float Volume = 0.6f;

        private static MusicPlayer instance;

        private AudioSource source;
        private AudioListener ownListener;

        /// <summary>Pista que está sonando, o null si no suena ninguna.</summary>
        public static AudioClip CurrentClip => instance != null ? instance.source.clip : null;

        public static bool IsPlaying => instance != null && instance.source.isPlaying;

        // Con "Enter Play Mode Options" sin recarga de dominio, los estáticos sobreviven entre partidas.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // Al pulsar Play en el editor la primera escena llega como copia de seguridad
        // (Temp/__Backupscenes), con otro nombre: se vuelve a comprobar cuando ya está cargada.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterFirstScene()
        {
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single)
            {
                return;
            }

            string track = TrackFor(scene.name) ?? TrackByContent();
            if (track == null)
            {
                return;
            }

            if (instance == null)
            {
                var go = new GameObject("Musica");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<MusicPlayer>();
            }

            instance.UpdateListener();
            instance.Play(track);
        }

        /// <summary>Pista de cada escena: el menú y el juego. Otras escenas no cambian la música.</summary>
        public static string TrackFor(string sceneName)
        {
            switch (sceneName)
            {
                case "MenuPrincipal": return MenuTrack;
                case "PuertaDelCastillo": return BattleTrack;
                default: return null;
            }
        }

        // Si el nombre no sirve, se reconoce la escena por lo que contiene.
        private static string TrackByContent()
        {
            if (FindAnyObjectByType<PuertaCastillo.UI.MainMenu>() != null)
            {
                return MenuTrack;
            }
            if (FindAnyObjectByType<LevelManager>() != null)
            {
                return BattleTrack;
            }
            return null;
        }

        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.loop = true;
            source.volume = Volume;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;

            ownListener = gameObject.AddComponent<AudioListener>();
            ownListener.enabled = false;
        }

        // Sin AudioListener no se oye nada: el menú no tiene ninguno en su cámara. Se usa el propio
        // solo si la escena no trae otro, para no tener dos a la vez (Unity avisaría).
        private void UpdateListener()
        {
            bool sceneHasListener = false;
            foreach (AudioListener listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            {
                if (listener != ownListener && listener.isActiveAndEnabled)
                {
                    sceneHasListener = true;
                    break;
                }
            }
            ownListener.enabled = !sceneHasListener;
        }

        private void Play(string track)
        {
            AudioClip clip = Resources.Load<AudioClip>(track);
            if (clip == null)
            {
                Debug.LogWarning($"MusicPlayer: no encuentro la pista Resources/{track}.");
                return;
            }

            if (source.clip == clip && source.isPlaying)
            {
                return;
            }

            source.clip = clip;
            source.Play();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
