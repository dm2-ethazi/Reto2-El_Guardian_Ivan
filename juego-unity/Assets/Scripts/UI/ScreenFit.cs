using UnityEngine;
using UnityEngine.UI;

namespace PuertaCastillo.UI
{
    /// <summary>
    /// Calcula el tamaño lógico de la interfaz para cualquier pantalla. Siempre cabe la resolución
    /// de referencia, de modo que un botón ocupa lo mismo en un móvil en vertical o en horizontal.
    /// </summary>
    public static class ScreenFit
    {
        private static readonly Vector2 PortraitReference = new Vector2(1080f, 1920f);
        private static readonly Vector2 LandscapeReference = new Vector2(1920f, 1080f);

        /// <summary>Tamaño de la pantalla; en el editor, sin jugar, el de la pestaña Game.</summary>
        public static Vector2 CurrentSize
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    return UnityEditor.Handles.GetMainGameViewSize();
                }
#endif
                return new Vector2(Screen.width, Screen.height);
            }
        }

        public static bool IsPortrait(float width, float height)
        {
            return height > width;
        }

        public static Vector2 Reference(float width, float height)
        {
            return IsPortrait(width, height) ? PortraitReference : LandscapeReference;
        }

        /// <summary>Tamaño de la interfaz en unidades lógicas para una pantalla de esos píxeles.</summary>
        public static Vector2 LogicalSize(float width, float height)
        {
            Vector2 reference = Reference(width, height);
            float scale = Mathf.Min(width / reference.x, height / reference.y);
            return new Vector2(width, height) / scale;
        }

        public static void Configure(CanvasScaler scaler, float width, float height)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referenceResolution = Reference(width, height);
        }
    }
}
