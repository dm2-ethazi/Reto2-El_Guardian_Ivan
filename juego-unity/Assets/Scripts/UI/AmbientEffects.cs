using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PuertaCastillo.UI
{
    /// <summary>
    /// Da vida al fondo: velas que flotan, llamas de antorcha y niebla que se desplaza.
    /// Sirve para el escenario de la partida y para el menú: si está en un objeto de interfaz crea
    /// imágenes y, si no, sprites. Son solo decorado, así que no afectan al juego.
    /// </summary>
    public class AmbientEffects : MonoBehaviour
    {
        [SerializeField] private Sprite candle;
        [SerializeField] private Sprite mist;
        [SerializeField] private Sprite[] flameFrames;
        [SerializeField, Min(0)] private int candleCount = 0;
        [SerializeField, Min(0)] private int mistCount = 0;
        [SerializeField] private Vector2[] torchPositions;
        [SerializeField] private Vector2 areaCenter;
        [SerializeField] private Vector2 areaSize = Vector2.one;
        [Tooltip("Escala de cada pieza.")]
        [SerializeField, Min(0.01f)] private float pieceScale = 1f;
        [Tooltip("Cuánto equivale una unidad de movimiento: 1 en el mundo, unos 60 en la interfaz.")]
        [SerializeField, Min(0.01f)] private float motionScale = 1f;
        [SerializeField, Min(0.1f)] private float flameFramesPerSecond = 9f;

        private readonly List<Transform> candles = new List<Transform>();
        private readonly List<Vector3> candleOrigins = new List<Vector3>();
        private readonly List<Transform> mists = new List<Transform>();
        private readonly List<SpriteSetter> flames = new List<SpriteSetter>();
        private int nextSibling = 1;

        // Cambia el sprite de una llama, tanto si es una Image (menú) como un SpriteRenderer (partida).
        private delegate void SpriteSetter(Sprite sprite);

        private void Start()
        {
            for (int i = 0; i < mistCount; i++)
            {
                mists.Add(Create("Niebla", mist, RandomPoint(), new Color(1f, 1f, 1f, 0.22f), -6, out _));
            }

            for (int i = 0; i < candleCount; i++)
            {
                Vector3 origin = RandomPoint();
                candles.Add(Create("Vela", candle, origin, Color.white, -4, out _));
                candleOrigins.Add(origin);
            }

            foreach (Vector2 position in torchPositions)
            {
                Create("Llama", flameFrames[0], position, Color.white, 1, out SpriteSetter setter);
                flames.Add(setter);
            }
        }

        // Las velas suben y bajan, la niebla se desliza a la derecha y vuelve a entrar, y las llamas cambian de fotograma.
        private void Update()
        {
            for (int i = 0; i < candles.Count; i++)
            {
                float height = Mathf.Sin(Time.time * 0.9f + i * 1.7f) * 0.14f;
                float sway = Mathf.Sin(Time.time * 0.5f + i * 2.3f) * 0.06f;
                candles[i].localPosition = candleOrigins[i] + new Vector3(sway, height, 0f) * motionScale;
            }

            float left = areaCenter.x - areaSize.x / 2f;
            foreach (Transform piece in mists)
            {
                Vector3 position = piece.localPosition;
                position.x += 0.22f * motionScale * Time.deltaTime;
                position.x = left + Mathf.Repeat(position.x - left, areaSize.x);
                piece.localPosition = position;
            }

            Sprite flame = flameFrames[(int)(Time.time * flameFramesPerSecond) % flameFrames.Length];
            foreach (SpriteSetter setter in flames)
            {
                setter(flame);
            }
        }

        // Punto al azar dentro del área configurada.
        private Vector3 RandomPoint()
        {
            return areaCenter + new Vector2(Random.Range(-0.5f, 0.5f) * areaSize.x, Random.Range(-0.5f, 0.5f) * areaSize.y);
        }

        // En el menú las piezas van justo encima del fondo y por debajo del título y los botones.
        private Transform Create(string pieceName, Sprite sprite, Vector3 position, Color color, int order, out SpriteSetter setter)
        {
            GameObject piece = new GameObject(pieceName);

            if (transform is RectTransform)
            {
                piece.transform.SetParent(transform, false);
                piece.transform.SetSiblingIndex(nextSibling++);
                Image image = piece.AddComponent<Image>();
                image.sprite = sprite;
                image.color = color;
                image.raycastTarget = false;
                image.SetNativeSize();
                setter = value => image.sprite = value;
            }
            else
            {
                piece.transform.SetParent(transform, false);
                SpriteRenderer spriteRenderer = piece.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = sprite;
                spriteRenderer.color = color;
                spriteRenderer.sortingOrder = order;
                setter = value => spriteRenderer.sprite = value;
            }

            piece.transform.localPosition = position;
            piece.transform.localScale = Vector3.one * pieceScale;
            return piece.transform;
        }
    }
}
