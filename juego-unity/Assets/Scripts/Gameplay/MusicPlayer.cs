using System.Collections;
using UnityEngine;
using UnityEngine.UI;
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
        /// <summary>Pista del menú y del mapa, en Assets/Resources.</summary>
        public const string MenuTrack = "Musica/menu_musica";
        /// <summary>Pista de la partida, en Assets/Resources.</summary>
        public const string BattleTrack = "Musica/batalla_musica";
        private const float Volume = 0.6f;

        private static MusicPlayer instance;

        private AudioSource source;
        private AudioListener ownListener;
        private AudioSource effects;
        private AudioSource doorImpact;
        private AudioClip[] effectClips;
        private Coroutine fade;
        private string requestedTrack;
        private float fadeGain = 1f;

        public static MusicPlayer Audio
        {
            get
            {
                if (instance == null)
                {
                    var host = new GameObject("Audio");
                    DontDestroyOnLoad(host);
                    instance = host.AddComponent<MusicPlayer>();
                }
                return instance;
            }
        }

        public float Master { get; private set; }
        public float Music { get; private set; }
        public float Effects { get; private set; }
        public bool Muted { get; private set; }

        public void SetMaster(float value) { Master = Mathf.Clamp01(value); SaveMix(); }
        public void SetMusic(float value) { Music = Mathf.Clamp01(value); SaveMix(); }
        public void SetEffects(float value) { Effects = Mathf.Clamp01(value); SaveMix(); }
        public void SetMuted(bool value) { Muted = value; SaveMix(); }

        private void SaveMix()
        {
            PlayerPrefs.SetFloat("audio.master", Master);
            PlayerPrefs.SetFloat("audio.music", Music);
            PlayerPrefs.SetFloat("audio.effects", Effects);
            PlayerPrefs.SetInt("audio.muted", Muted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyMix();
        }

        private void ApplyMix()
        {
            float gain = Muted ? 0f : Master;
            source.volume = Volume * Music * gain * fadeGain;
            effects.volume = doorImpact.volume = Effects * gain;
        }

        public static void PlayEffect(SoundCue cue)
        {
            var audio = Audio;
            int index = (int)cue;
            var clip = audio.effectClips[index];
            if (clip != null) audio.effects.PlayOneShot(clip);
        }

        public void PlayDoorImpact(AudioClip clip, float pitch, float volume)
        {
            if (clip == null) return;
            doorImpact.pitch = pitch;
            doorImpact.PlayOneShot(clip, volume);
        }

        private AudioSource CreateSource()
        {
            var channel = gameObject.AddComponent<AudioSource>();
            channel.playOnAwake = false;
            channel.spatialBlend = 0f;
            channel.ignoreListenerPause = true;
            return channel;
        }

        private static void PlayButton() => PlayEffect(SoundCue.Button);

        private IEnumerator BindButtons(Scene scene)
        {
            // Wait for Start so the generated keyboard and other runtime controls exist.
            yield return null;
            foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (button.gameObject.scene != scene) continue;
                button.onClick.RemoveListener(PlayButton);
                button.onClick.AddListener(PlayButton);
            }
        }

        /// <summary>Pista que está sonando, o null si no suena ninguna.</summary>
        public static AudioClip CurrentClip => instance != null ? instance.source.clip : null;

        /// <summary>True si está sonando la música.</summary>
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

        // Al cargar una escena elige su pista; crea el reproductor la primera vez y lo mantiene entre escenas.
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

            var audio = Audio;
            audio.UpdateListener();
            audio.Play(track);
            audio.StartCoroutine(audio.BindButtons(scene));
        }

        /// <summary>Pista de cada escena: el menú y el mapa comparten pista, y el juego tiene la suya. Otras escenas no cambian la música.</summary>
        /// <param name="sceneName">Nombre de la escena cargada.</param>
        /// <returns>Ruta de la pista en Resources, o null si la escena no cambia la música.</returns>
        public static string TrackFor(string sceneName)
        {
            switch (sceneName)
            {
                case "MenuPrincipal": return MenuTrack;
                case "MapaNiveles": return MenuTrack;
                case "PuertaDelCastillo": return BattleTrack;
                default: return null;
            }
        }

        // Si el nombre no sirve, se reconoce la escena por lo que contiene.
        private static string TrackByContent()
        {
            if (FindAnyObjectByType<PuertaCastillo.UI.MainMenu>() != null ||
                FindAnyObjectByType<PuertaCastillo.UI.LevelMap>() != null)
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
            source = CreateSource();
            source.loop = true;
            effects = CreateSource();
            doorImpact = CreateSource();

            Master = Mathf.Clamp01(PlayerPrefs.GetFloat("audio.master", 1f));
            Music = Mathf.Clamp01(PlayerPrefs.GetFloat("audio.music", 1f));
            Effects = Mathf.Clamp01(PlayerPrefs.GetFloat("audio.effects", 1f));
            Muted = PlayerPrefs.GetInt("audio.muted", 0) != 0;
            ApplyMix();

            var cues = System.Enum.GetValues(typeof(SoundCue));
            effectClips = new AudioClip[cues.Length];
            foreach (SoundCue cue in cues)
                effectClips[(int)cue] = Resources.Load<AudioClip>("Sonidos/Eventos/" + cue);

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

        // Carga la pista de Resources y la pone en bucle. Si ya suena esa misma, no la corta.
        private void Play(string track)
        {
            if (requestedTrack == track) return;
            requestedTrack = track;
            AudioClip clip = Resources.Load<AudioClip>(track);
            if (clip == null)
            {
                Debug.LogWarning($"MusicPlayer: no encuentro la pista Resources/{track}.");
            }

            if (fade != null) StopCoroutine(fade);
            if (source.clip == null && clip != null)
            {
                source.clip = clip;
                source.Play();
            }
            if (source.clip != clip || fadeGain < 1f)
                fade = StartCoroutine(ChangeTrack(clip));
        }

        private IEnumerator ChangeTrack(AudioClip clip)
        {
            if (source.clip != clip)
            {
                while (fadeGain > 0f)
                {
                    fadeGain = Mathf.MoveTowards(fadeGain, 0f, Time.unscaledDeltaTime * 3f);
                    ApplyMix();
                    yield return null;
                }

                source.Stop();
                source.clip = clip;
                if (clip != null) source.Play();
            }

            while (clip != null && fadeGain < 1f)
            {
                fadeGain = Mathf.MoveTowards(fadeGain, 1f, Time.unscaledDeltaTime * 3f);
                ApplyMix();
                yield return null;
            }
            fade = null;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
    public enum SoundCue
    {
        Button, Correct, Incorrect, EnemyDefeated, EnemyAttack, Victory, Defeat, BossSpawn
    }
}
