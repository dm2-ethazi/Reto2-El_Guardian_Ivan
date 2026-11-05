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

        private Vector2 lastSize;
        private Rect lastSafeArea;

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

        public void Apply(float width, float height)
        {
            Apply(width, height, new Rect(0f, 0f, width, height));
        }

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
            // La columna completa mide 640 x 700; mantiene un margen incluso en ventanas pequeñas.
            float fit = Mathf.Min(1f, Mathf.Min(
                Mathf.Max(1f, safeArea.width * unitsPerPixel - 64f) / 640f,
                Mathf.Max(1f, safeArea.height * unitsPerPixel - 64f) / 700f));
            column.localScale = Vector3.one * fit;
        }
    }
}
