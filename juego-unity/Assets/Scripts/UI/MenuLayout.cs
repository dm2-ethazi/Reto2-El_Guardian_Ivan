using UnityEngine;
using UnityEngine.UI;

namespace PuertaCastillo.UI
{
    /// <summary>Centra el menú en el área segura y selecciona el fondo según la orientación.</summary>
    [ExecuteAlways]
    public class MenuLayout : MonoBehaviour
    {
        [SerializeField] private CanvasScaler scaler;
        [SerializeField] private RectTransform column;
        [SerializeField] private RectTransform image;
        [SerializeField] private Sprite landscapeBackground;
        [SerializeField] private Sprite portraitBackground;

        private const float ColumnHeight = 700f;
        private const float ColumnHeightWithContinue = 760f;

        private Vector2 lastSize;
        private Rect lastSafeArea;

        // Solo recoloca cuando cambia el tamaño de la pantalla o el área segura.
        private void Update()
        {
            Vector2 size = ScreenFit.CurrentSize;
            Rect safe = Application.isPlaying ? Screen.safeArea : new Rect(Vector2.zero, size);
            if (size != lastSize || safe != lastSafeArea)
            {
                lastSize = size;
                lastSafeArea = safe;
                Apply(size.x, size.y, safe);
            }
        }

        /// <summary>Recoloca el menú para una pantalla sin muescas (toda ella es área segura).</summary>
        public void Apply(float width, float height)
        {
            Apply(width, height, new Rect(0f, 0f, width, height));
        }

        /// <summary>Ajusta la escala del Canvas, cubre la pantalla con el fondo sin deformarlo y centra la columna de botones en el área segura.</summary>
        public void Apply(float width, float height, Rect safeArea)
        {
            if (width <= 0f || height <= 0f) return;
            ScreenFit.Configure(scaler, width, height);
            Vector2 logical = ScreenFit.LogicalSize(width, height);
            float unitsPerPixel = logical.x / width;
            image.anchorMin = image.anchorMax = image.pivot = new Vector2(0.5f, 0.5f);
            Image graphic = image.GetComponent<Image>();
            Sprite background = ScreenFit.IsPortrait(width, height) ? portraitBackground : landscapeBackground;
            if (background != null) graphic.sprite = background;
            float aspect = graphic.sprite != null ? graphic.sprite.rect.width / graphic.sprite.rect.height : 16f / 9f;
            float imageHeight = Mathf.Max(logical.y, logical.x / aspect);
            image.sizeDelta = new Vector2(imageHeight * aspect, imageHeight);
            image.anchoredPosition = Vector2.zero;

            column.anchorMin = column.anchorMax = column.pivot = new Vector2(0.5f, 0.5f);
            column.anchoredPosition = (safeArea.center - new Vector2(width, height) / 2f) * unitsPerPixel;
            bool hasContinue = PlaceContinueButton();
            float columnHeight = Application.isPlaying ? 960f : (hasContinue ? ColumnHeightWithContinue : ColumnHeight);
            // La columna completa mide 640 x 700 (760 con Continuar); mantiene un margen incluso en ventanas pequeñas.
            float fit = Mathf.Min(1f, Mathf.Min(
                Mathf.Max(1f, safeArea.width * unitsPerPixel - 64f) / 640f,
                Mathf.Max(1f, safeArea.height * unitsPerPixel - 64f) / columnHeight));
            column.localScale = Vector3.one * fit;
        }

        // Con partida guardada hay un botón más (Continuar, encima de Iniciar partida): los botones
        // se compactan un poco para que todo quepa bajo el título. Sin él, se queda como en la escena.
        private bool PlaceContinueButton()
        {
            var continueButton = column.Find("BotonContinuar") as RectTransform;
            if (continueButton == null || !continueButton.gameObject.activeSelf)
            {
                return false;
            }

            Place(continueButton, 40f, 120f);
            Place(column.Find("BotonIniciar"), -90f, 120f);
            Place(column.Find("BotonSalir"), -220f, 120f);
            Place(column.Find("BotonEs"), -345f, 110f);
            Place(column.Find("BotonEn"), -345f, 110f);
            Place(column.Find("BotonEu"), -345f, 110f);
            return true;
        }

        // Cambia solo la altura y la posición vertical de un elemento de la columna.
        private static void Place(Transform child, float y, float height)
        {
            if (child is RectTransform rect)
            {
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            }
        }
    }
}
