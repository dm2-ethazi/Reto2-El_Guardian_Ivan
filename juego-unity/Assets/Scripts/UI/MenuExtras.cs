using System;
using PuertaCastillo.Crypto;
using PuertaCastillo.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PuertaCastillo.UI
{
    // Built from the button and font already serialized in MenuPrincipal.
    internal sealed class MenuExtras
    {
        private static readonly Color Ink = new Color(0.106f, 0.106f, 0.227f);
        private static readonly Color Gold = new Color(0.94f, 0.88f, 0.72f);
        private readonly Button template;
        private readonly Transform column;
        private readonly CanvasGroup menuGroup;
        private readonly RectTransform panel;
        private readonly Button helpButton;
        private readonly Button audioButton;
        private readonly GameObject helpPage;
        private readonly GameObject audioPage;
        private readonly TMP_Text[] helpText;
        private readonly TMP_Text audioTitle;
        private readonly TMP_Text[] volumeLabels = new TMP_Text[3];
        private readonly Slider[] sliders = new Slider[3];
        private readonly Button mute;
        private readonly Button back;
        private Button returnFocus;

        public bool IsOpen => panel.gameObject.activeSelf;

        public MenuExtras(Button start)
        {
            template = start;
            column = start.transform.parent;
            menuGroup = column.gameObject.AddComponent<CanvasGroup>();
            helpButton = MakeButton("BotonAyuda", column, -160, -443, 300, 72, () => Open(true));
            audioButton = MakeButton("BotonSonido", column, 160, -443, 300, 72, () => Open(false));
            panel = Box("PanelAyudaYSonido", column, 0, 0, 640, 960);
            Image background = panel.gameObject.AddComponent<Image>();
            background.color = Ink;
            panel.gameObject.AddComponent<CanvasGroup>().ignoreParentGroups = true;

            var help = Box("Ayuda", panel, 0, 0, 640, 960);
            helpPage = help.gameObject;
            helpText = new[]
            {
                Text(help, 400, 72, 48, TextAlignmentOptions.Center),
                Text(help, 228, 242, 35, TextAlignmentOptions.TopLeft),
                Text(help, -12, 206, 35, TextAlignmentOptions.TopLeft),
                Text(help, -246, 218, 35, TextAlignmentOptions.TopLeft)
            };

            var audio = Box("Sonido", panel, 0, 0, 640, 960);
            audioPage = audio.gameObject;
            audioTitle = Text(audio, 400, 72, 48, TextAlignmentOptions.Center);
            for (int i = 0; i < sliders.Length; i++)
            {
                int index = i;
                float y = 260 - 162 * i;
                volumeLabels[i] = Text(audio, y, 60, 34, TextAlignmentOptions.Left);
                sliders[i] = MakeSlider(audio, y - 56, value => ChangeVolume(index, value));
            }

            mute = MakeButton("Silenciar", audio, 0, -253, 520, 80, ToggleMute);
            back = MakeButton("Volver", panel, 0, -411, 520, 85, Close);
            panel.gameObject.SetActive(false);
            RefreshLanguage();
        }

        public void RefreshLanguage()
        {
            Label(helpButton, Pick("Ayuda", "Help", "Laguntza"));
            Label(audioButton, Pick("Sonido", "Audio", "Soinua"));
            Label(back, Pick("Volver al menú", "Back to menu", "Itzuli menura"));
            helpText[0].text = Pick("Cómo jugar", "How to play", "Nola jokatu");
            helpText[1].text = Pick(
                "<b>Defiende la puerta</b>\nLos enemigos avanzan hacia el castillo. Descifra el mensaje del enemigo más cercano, marcado en dorado. Escribe la solución y pulsa Descifrar o Intro. Una respuesta correcta elimina a un enemigo normal.",
                "<b>Defend the gate</b>\nEnemies move toward the castle. Decode the message from the nearest enemy, highlighted in gold. Enter the solution and press Decrypt or Enter. A correct answer defeats a regular enemy.",
                "<b>Babestu atea</b>\nEtsaiak gaztelurantz doaz. Deszifratu urre-kolorez markatutako etsairik hurbilenaren mezua. Idatzi erantzuna eta sakatu Deszifratu edo Enter. Erantzun zuzenak etsai arrunta garaitzen du.");
            string cipher = CaesarCipher.Encrypt("HECHIZO", 3);
            helpText[2].text = Pick(
                $"<b>Cifrado César</b>\nPara descifrar, usa la pista para deducir cuántas letras debes retroceder. Usa la regla del juego con la Ñ. En el nivel 1, retrocede 3 posiciones: {cipher} = HECHIZO.",
                $"<b>Caesar cipher</b>\nTo decode, use the hint to work out how many letters to move back. The in-game ruler includes Ñ. In level 1, move back 3 places: {cipher} = HECHIZO.",
                $"<b>Zesar zifratzea</b>\nDeszifratzeko, erabili pista zenbat letra atzera egin behar duzun jakiteko. Erabili Ñ duen jokoko erregela. 1. mailan, egin 3 toki atzera: {cipher} = HECHIZO.");
            helpText[3].text = Pick(
                "<b>Cinco niveles</b>\nLos enemigos golpean la puerta al llegar. Si su vida baja a cero, pierdes. Resiste hasta que acabe el tiempo para avanzar. Hay distintos enemigos y un jefe final: necesita varios aciertos. Derrotarlo también da la victoria final.",
                "<b>Five levels</b>\nEnemies attack when they reach the gate. If its health reaches zero, you lose. Survive until time runs out to advance. There are different enemies. The final boss takes several correct answers. Defeating it also wins the game.",
                "<b>Bost maila</b>\nEtsaiek atea jotzen dute bertara heltzean. Atearen bizitza zerora iristen bada, galduko duzu. Eutsi denbora amaitu arte. Hainbat etsai mota daude. Azken buruzagiak hainbat erantzun zuzen behar ditu. Hura garaituz gero ere irabaziko duzu.");
            audioTitle.text = Pick("Ajustes de sonido", "Audio settings", "Soinu-ezarpenak");
            if (IsOpen && audioPage.activeSelf) RefreshAudioLabels();
        }

        private void Open(bool showHelp)
        {
            returnFocus = showHelp ? helpButton : audioButton;
            menuGroup.interactable = false;
            menuGroup.blocksRaycasts = false;
            helpPage.SetActive(showHelp);
            audioPage.SetActive(!showHelp);
            if (!showHelp)
            {
                MusicPlayer audio = MusicPlayer.Audio;
                sliders[0].SetValueWithoutNotify(audio.Master);
                sliders[1].SetValueWithoutNotify(audio.Music);
                sliders[2].SetValueWithoutNotify(audio.Effects);
                RefreshAudioLabels();
            }
            panel.gameObject.SetActive(true);
            if (showHelp) back.Select();
            else sliders[0].Select();
        }

        private void Close()
        {
            panel.gameObject.SetActive(false);
            menuGroup.interactable = true;
            menuGroup.blocksRaycasts = true;
            returnFocus.Select();
        }

        private void ChangeVolume(int index, float value)
        {
            MusicPlayer audio = MusicPlayer.Audio;
            switch (index)
            {
                case 0: audio.SetMaster(value); break;
                case 1: audio.SetMusic(value); break;
                case 2: audio.SetEffects(value); break;
            }
            RefreshAudioLabels();
        }

        private void ToggleMute()
        {
            MusicPlayer audio = MusicPlayer.Audio;
            audio.SetMuted(!audio.Muted);
            RefreshAudioLabels();
        }

        private void RefreshAudioLabels()
        {
            volumeLabels[0].text = WithPercent(Pick("Volumen general", "Master volume", "Bolumen orokorra"), sliders[0]);
            volumeLabels[1].text = WithPercent(Pick("Música", "Music", "Musika"), sliders[1]);
            volumeLabels[2].text = WithPercent(Pick("Efectos", "Sound effects", "Soinu-efektuak"), sliders[2]);
            Label(mute, MusicPlayer.Audio.Muted
                ? Pick("Reactivar sonido", "Unmute", "Aktibatu soinua")
                : Pick("Silenciar", "Mute", "Isilarazi"));
        }

        private static string WithPercent(string label, Slider slider) =>
            $"{label}  {Mathf.RoundToInt(slider.value * 100f)} %";

        private Button MakeButton(string name, Transform parent, float x, float y, float width, float height, Action click)
        {
            Button button = UnityEngine.Object.Instantiate(template, parent, false);
            button.name = name;
            foreach (LocalizedText localized in button.GetComponentsInChildren<LocalizedText>(true))
                UnityEngine.Object.DestroyImmediate(localized);

            Place((RectTransform)button.transform, x, y, width, height);
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            text.enableAutoSizing = true;
            text.fontSizeMin = 20;
            text.fontSizeMax = 34;
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => click());
            return button;
        }

        private Slider MakeSlider(Transform parent, float y, UnityEngine.Events.UnityAction<float> changed)
        {
            RectTransform root = Box("Volumen", parent, 0, y, 552, 56);
            root.gameObject.AddComponent<Image>().color = new Color(0.035f, 0.06f, 0.095f);
            RectTransform track = Box("Pista", root, 0, 0, 504, 14);
            track.gameObject.AddComponent<Image>().color = new Color(0.35f, 0.29f, 0.21f);
            RectTransform area = Box("Recorrido", root, 0, 0, 504, 56);
            RectTransform handle = Box("Control", area, 0, 0, 26, 46);
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = Gold;
            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.onValueChanged.AddListener(changed);
            return slider;
        }

        private TMP_Text Text(Transform parent, float y, float height, float max, TextAlignmentOptions align)
        {
            var text = Box("Texto", parent, 0, y, 552, height).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = template.GetComponentInChildren<TMP_Text>(true).font;
            text.color = Gold;
            text.alignment = align;
            text.enableAutoSizing = true;
            text.fontSizeMin = 20;
            text.fontSizeMax = max;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform Box(string name, Transform parent, float x, float y, float w, float h)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            Place(rect, x, y, w, h);
            return rect;
        }

        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
        }

        private static void Label(Button button, string value) =>
            button.GetComponentInChildren<TMP_Text>(true).text = value;

        private static string Pick(string spanish, string english, string basque) => Localization.Current switch
        {
            Language.English => english,
            Language.Basque => basque,
            _ => spanish
        };
    }
}

