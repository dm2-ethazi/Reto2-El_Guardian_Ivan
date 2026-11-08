using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PuertaCastillo.UI
{
    /// <summary>Resultado de un gesto de deslizar.</summary>
    public enum Swipe
    {
        None,
        Right,
        Left
    }

    /// <summary>
    /// Gesto de pasar de página entre el menú y la partida, como si el menú estuviera a la izquierda
    /// y el nivel a la derecha: en el menú se desliza a la izquierda para entrar en el nivel y en la
    /// partida a la derecha para volver al menú. También funciona arrastrando con el ratón.
    /// Mientras se arrastra en la dirección válida, la pantalla sigue al dedo y por el borde entra un
    /// panel con el destino; al pasar el umbral se pone dorado. Antes de arrastrar no se ve nada.
    /// Al soltar pasado el umbral (o con un gesto rápido) termina de pasar la página; si no, vuelve.
    /// Lo crean <see cref="MainMenu"/> y <see cref="LevelHud"/>, que le dicen qué hacer y cuándo está permitido.
    /// </summary>
    public class SwipeNavigation : MonoBehaviour
    {
        /// <summary>Recorrido horizontal para que cuente al soltar, en proporción al ancho de la pantalla.</summary>
        public const float MinWidthFraction = 0.35f;

        /// <summary>Un gesto rápido cuenta con menos recorrido: al menos esta proporción del ancho...</summary>
        public const float FlickWidthFraction = 0.15f;

        /// <summary>...hecho en este tiempo como mucho, en segundos.</summary>
        public const float FlickMaxDuration = 0.3f;

        /// <summary>La pantalla se mueve menos que el dedo, para que se note que hay que empujar.</summary>
        public const float Resistance = 0.6f;

        private const float CompleteSeconds = 0.2f;
        private const float CancelSeconds = 0.15f;
        private const int OverlaySortingOrder = 1000;

        private static readonly Color NightBlue = new Color32(0x1B, 0x1B, 0x3A, 0xFF);
        private static readonly Color Parchment = new Color32(0xF2, 0xE8, 0xCF, 0xFF);
        private static readonly Color Gold = new Color32(0xD4, 0xA0, 0x17, 0xFF);

        /// <summary>Única dirección que hace algo en esta pantalla.</summary>
        public Swipe Direction;

        /// <summary>Se llama al completar el gesto, cuando la página ya ha salido.</summary>
        public Action Swiped;

        public Func<bool> IsAllowed;

        /// <summary>Texto del destino que se ve en el panel, como "Nivel 2" o "Menú".</summary>
        public Func<string> DestinationLabel;

        public TMP_FontAsset LabelFont;

        /// <summary>
        /// Si es true, solo bloquean el gesto los controles (botones, etc.) y no los fondos o textos.
        /// El menú lo usa porque su fondo ocupa toda la pantalla.
        /// </summary>
        public bool OnlyControlsBlock;

        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private readonly List<MovedRect> movedRects = new List<MovedRect>();
        private bool tracking;
        private bool animating;
        private bool contentCaptured;
        private Vector2 startPosition;
        private float startTime;

        private Camera movedCamera;
        private Vector3 cameraOriginal;
        private float worldPerPixel;

        private Canvas overlayCanvas;
        private CanvasGroup overlayGroup;
        private RectTransform band;
        private RectTransform arrow;
        private Image[] arrowBars;
        private TMP_Text label;

        private struct MovedRect
        {
            public RectTransform Rect;
            public Vector2 Original;
            public float ScaleFactor;
        }

        /// <summary>Píxeles que se ha desplazado el contenido (negativo hacia la izquierda).</summary>
        public float CurrentOffset { get; private set; }

        /// <summary>Progreso hacia el umbral: 0 sin arrastrar, 1 o más cuando al soltar se completaría.</summary>
        public float CurrentProgress { get; private set; }

        public bool IsOverlayVisible => overlayCanvas != null && overlayCanvas.gameObject.activeSelf;

        /// <summary>
        /// Decide si un gesto cuenta al soltar: más horizontal que vertical y, o bien largo (al menos
        /// el 35 % del ancho, sin importar lo lento), o bien un golpe rápido (15 % en 0,3 s).
        /// </summary>
        public static Swipe Classify(Vector2 delta, float duration, float screenWidth)
        {
            if (screenWidth <= 0f)
            {
                return Swipe.None;
            }

            float distance = Mathf.Abs(delta.x);
            if (distance <= Mathf.Abs(delta.y))
            {
                return Swipe.None;
            }

            bool farEnough = distance >= screenWidth * MinWidthFraction;
            bool flick = distance >= screenWidth * FlickWidthFraction && duration <= FlickMaxDuration;
            if (!farEnough && !flick)
            {
                return Swipe.None;
            }

            return delta.x > 0f ? Swipe.Right : Swipe.Left;
        }

        /// <summary>
        /// Cuánto se ha avanzado hacia el umbral arrastrando en <paramref name="direction"/>.
        /// En la dirección contraria, o si el arrastre es más vertical que horizontal, es 0.
        /// </summary>
        public static float Progress(Vector2 delta, Swipe direction, float screenWidth)
        {
            if (screenWidth <= 0f || direction == Swipe.None)
            {
                return 0f;
            }

            float along = direction == Swipe.Right ? delta.x : -delta.x;
            if (along <= 0f || along <= Mathf.Abs(delta.y))
            {
                return 0f;
            }

            return along / (screenWidth * MinWidthFraction);
        }

        private void Update()
        {
            if (animating)
            {
                return;
            }

            Pointer pointer = Pointer.current;
            if (pointer == null)
            {
                return;
            }

            Vector2 position = pointer.position.ReadValue();

            if (pointer.press.wasPressedThisFrame)
            {
                tracking = Direction != Swipe.None && (IsAllowed == null || IsAllowed()) && !IsOverInterface(position);
                startPosition = position;
                startTime = Time.unscaledTime;
                return;
            }

            if (!tracking)
            {
                return;
            }

            if (IsAllowed != null && !IsAllowed())
            {
                tracking = false;
                SetOffset(0f, 0f);
                return;
            }

            if (pointer.press.wasReleasedThisFrame)
            {
                tracking = false;
                Release(position - startPosition, Time.unscaledTime - startTime);
            }
            else if (pointer.press.isPressed)
            {
                Drag(position - startPosition);
            }
        }

        /// <summary>Sigue al dedo: mueve la pantalla y muestra el panel según el arrastre.</summary>
        public void Drag(Vector2 delta)
        {
            float progress = Progress(delta, Direction, Screen.width);
            float pixels = progress * Screen.width * MinWidthFraction * Resistance;
            SetOffset(Sign * pixels, progress);
        }

        /// <summary>Al soltar: completa el paso de página o lo devuelve a su sitio.</summary>
        public void Release(Vector2 delta, float duration)
        {
            if (Direction != Swipe.None && Classify(delta, duration, Screen.width) == Direction)
            {
                StartCoroutine(Complete());
            }
            else if (contentCaptured)
            {
                StartCoroutine(Cancel());
            }
        }

        private float Sign => Direction == Swipe.Right ? 1f : -1f;

        private IEnumerator Complete()
        {
            animating = true;
            float from = Mathf.Abs(CurrentOffset);
            float to = Screen.width;
            float progress = Mathf.Max(1f, CurrentProgress);
            for (float t = 0f; t < CompleteSeconds; t += Time.unscaledDeltaTime)
            {
                float k = t / CompleteSeconds;
                SetOffset(Sign * Mathf.Lerp(from, to, k * k), progress);
                yield return null;
            }
            SetOffset(Sign * to, progress);

            if (Swiped != null)
            {
                // Cambia de escena: no hace falta devolver nada a su sitio.
                Swiped();
            }
            else
            {
                SetOffset(0f, 0f);
                animating = false;
            }
        }

        private IEnumerator Cancel()
        {
            animating = true;
            float from = CurrentOffset;
            float progress = CurrentProgress;
            for (float t = 0f; t < CancelSeconds; t += Time.unscaledDeltaTime)
            {
                float k = 1f - (1f - t / CancelSeconds) * (1f - t / CancelSeconds);
                SetOffset(Mathf.Lerp(from, 0f, k), Mathf.Lerp(progress, 0f, k));
                yield return null;
            }
            SetOffset(0f, 0f);
            animating = false;
        }

        private void SetOffset(float pixels, float progress)
        {
            CurrentOffset = pixels;
            CurrentProgress = progress;

            if (Mathf.Approximately(pixels, 0f))
            {
                RestoreContent();
                if (overlayCanvas != null)
                {
                    overlayCanvas.gameObject.SetActive(false);
                }
                return;
            }

            if (!contentCaptured)
            {
                CaptureContent();
            }

            foreach (MovedRect moved in movedRects)
            {
                if (moved.Rect != null)
                {
                    moved.Rect.anchoredPosition = moved.Original + new Vector2(pixels / moved.ScaleFactor, 0f);
                }
            }

            if (movedCamera != null)
            {
                movedCamera.transform.position = cameraOriginal - movedCamera.transform.right * (pixels * worldPerPixel);
            }

            ShowOverlay(Mathf.Abs(pixels), progress);
        }

        // Guarda dónde estaba todo lo visible: los hijos de cada Canvas de pantalla y la cámara.
        private void CaptureContent()
        {
            movedRects.Clear();
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas || canvas == overlayCanvas || canvas.renderMode == RenderMode.WorldSpace)
                {
                    continue;
                }

                float scale = canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
                foreach (Transform child in canvas.transform)
                {
                    if (child is RectTransform rect)
                    {
                        movedRects.Add(new MovedRect { Rect = rect, Original = rect.anchoredPosition, ScaleFactor = scale });
                    }
                }
            }

            movedCamera = Camera.main;
            if (movedCamera != null && movedCamera.orthographic && Screen.height > 0)
            {
                cameraOriginal = movedCamera.transform.position;
                worldPerPixel = 2f * movedCamera.orthographicSize / Screen.height;
            }
            else
            {
                movedCamera = null;
            }

            contentCaptured = true;
        }

        private void RestoreContent()
        {
            if (!contentCaptured)
            {
                return;
            }

            foreach (MovedRect moved in movedRects)
            {
                if (moved.Rect != null)
                {
                    moved.Rect.anchoredPosition = moved.Original;
                }
            }
            movedRects.Clear();

            if (movedCamera != null)
            {
                movedCamera.transform.position = cameraOriginal;
                movedCamera = null;
            }

            contentCaptured = false;
        }

        // Panel azul noche que entra por el borde contrario al gesto, del ancho que deja libre la pantalla.
        private void ShowOverlay(float width, float progress)
        {
            if (overlayCanvas == null)
            {
                BuildOverlay();
            }

            overlayCanvas.gameObject.SetActive(true);
            bool fromRight = Direction == Swipe.Left;
            band.anchorMin = new Vector2(fromRight ? 1f : 0f, 0f);
            band.anchorMax = new Vector2(fromRight ? 1f : 0f, 1f);
            band.pivot = new Vector2(fromRight ? 1f : 0f, 0.5f);
            band.sizeDelta = new Vector2(width, 0f);

            overlayGroup.alpha = Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(progress));

            bool ready = progress >= 1f;
            Color color = ready ? Gold : Parchment;
            label.color = color;
            foreach (Image bar in arrowBars)
            {
                bar.color = color;
            }

            // La flecha apunta hacia donde va la página: a la izquierda al entrar por la derecha.
            arrow.localScale = new Vector3(fromRight ? 1f : -1f, 1f, 1f) * (ready ? 1.15f : 1f);
            label.text = DestinationLabel != null ? DestinationLabel() : string.Empty;
        }

        private void BuildOverlay()
        {
            // En la raíz de la escena: dentro de otro Canvas se movería con el contenido.
            var root = new GameObject("DeslizarPagina", typeof(RectTransform));
            overlayCanvas = root.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = OverlaySortingOrder;
            overlayGroup = root.AddComponent<CanvasGroup>();
            overlayGroup.blocksRaycasts = false;
            overlayGroup.interactable = false;

            band = new GameObject("Panel", typeof(RectTransform)).GetComponent<RectTransform>();
            band.SetParent(root.transform, false);
            band.gameObject.AddComponent<Image>().color = NightBlue;

            float unit = Mathf.Max(Screen.height, 1) / 100f;

            var content = new GameObject("Contenido", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(band, false);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
            content.sizeDelta = new Vector2(unit * 40f, unit * 20f);

            arrow = new GameObject("Flecha", typeof(RectTransform)).GetComponent<RectTransform>();
            arrow.SetParent(content, false);
            arrow.anchoredPosition = new Vector2(0f, unit * 4f);
            float length = unit * 6f;
            float thickness = unit * 1.2f;
            arrowBars = new[]
            {
                CreateBar(arrow, length, thickness, 45f, new Vector2(length * 0.35f, length * 0.35f)),
                CreateBar(arrow, length, thickness, -45f, new Vector2(length * 0.35f, -length * 0.35f)),
            };

            var labelObject = new GameObject("Destino", typeof(RectTransform));
            labelObject.transform.SetParent(content, false);
            label = labelObject.AddComponent<TextMeshProUGUI>();
            if (LabelFont != null)
            {
                label.font = LabelFont;
            }
            label.fontSize = unit * 5f;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(unit * 40f, unit * 8f);
            labelRect.anchoredPosition = new Vector2(0f, -unit * 6f);
        }

        // Una de las dos barras de la flecha "<": salen del vértice izquierdo hacia arriba y hacia abajo.
        private static Image CreateBar(RectTransform parent, float length, float thickness, float angle, Vector2 center)
        {
            var bar = new GameObject("Trazo", typeof(RectTransform)).GetComponent<RectTransform>();
            bar.SetParent(parent, false);
            bar.sizeDelta = new Vector2(length, thickness);
            bar.anchoredPosition = center - new Vector2(length * 0.35f, 0f);
            bar.localRotation = Quaternion.Euler(0f, 0f, angle);
            Image image = bar.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private void OnDisable()
        {
            RestoreContent();
        }

        private void OnDestroy()
        {
            if (overlayCanvas != null)
            {
                Destroy(overlayCanvas.gameObject);
            }
        }

        // Si el dedo empieza sobre el teclado, la regla o un botón, es para la interfaz, no un gesto.
        private bool IsOverInterface(Vector2 position)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            uiHits.Clear();
            eventSystem.RaycastAll(new PointerEventData(eventSystem) { position = position }, uiHits);
            if (!OnlyControlsBlock)
            {
                return uiHits.Count > 0;
            }

            foreach (RaycastResult hit in uiHits)
            {
                if (hit.gameObject.GetComponentInParent<Selectable>() != null)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
