using PuertaCastillo.Crypto;
using TMPro;
using UnityEngine;

namespace PuertaCastillo.Gameplay
{
    /// <summary>
    /// Enemigo que avanza hacia la puerta y la golpea mientras está en contacto con ella.
    /// Lo configura <see cref="EnemySpawner"/> al crearlo. Los tipos de enemigo son variantes del prefab.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class Enemy : MonoBehaviour
    {
        private const float LabelRowHeight = 0.4f;

        /// <summary>Texto hijo que muestra el mensaje cifrado encima del enemigo.</summary>
        [SerializeField] private TMP_Text messageLabel;
        /// <summary>Marco dorado que se enseña cuando el enemigo es el objetivo.</summary>
        [SerializeField] private GameObject targetFrame;
        /// <summary>Color del mensaje cuando el enemigo no es el objetivo (pergamino).</summary>
        [SerializeField] private Color labelColor = new Color32(0xF2, 0xE8, 0xCF, 0xFF);
        /// <summary>Color del mensaje cuando el enemigo es el objetivo (dorado).</summary>
        [SerializeField] private Color targetLabelColor = new Color32(0xD4, 0xA0, 0x17, 0xFF);

        /// <summary>Aciertos necesarios para derrotarlo: 1 en los normales y 4 en el jefe.</summary>
        [Header("Tipo de enemigo")]
        [Tooltip("Mensajes que hay que descifrar para derrotarlo. 1 en los enemigos normales; el jefe usa más.")]
        [SerializeField, Min(1)] private int decryptsToKill = 1;
        /// <summary>Relleno de la barra de vida. Solo lo tiene el jefe, y por eso sirve para reconocerlo.</summary>
        [Tooltip("Relleno de la barra de vida. Solo lo usa el jefe.")]
        [SerializeField] private Transform healthFill;

        [Header("Perspectiva")]
        [Tooltip("Altura del horizonte del fondo, en el mundo. Cuanto más cerca de ella, más pequeño se ve el enemigo.")]
        [SerializeField] private float horizonY = 3.29f;
        [Tooltip("Tamaño en el horizonte (1 = tamaño normal). Desde ahí crece poco a poco hasta 1 al llegar a la muralla.")]
        [SerializeField, Range(0.05f, 1f)] private float minPerspectiveScale = 0.7f;
        [Tooltip("Tamaño del dibujo respecto al sprite (1 = como el sprite). Solo visual: no cambia el collider.")]
        [SerializeField, Min(0.1f)] private float visualScale = 1.15f;

        private Rigidbody2D body;
        private Vector2 destination;
        private float speed;
        private int damagePerHit;
        private float secondsBetweenHits;
        private int caesarShift;
        private float hitTimer;
        private float healthFillWidth;

        // Animación de golpe: solo se mueve el hijo visual, nunca el collider ni el Rigidbody2D.
        private Transform visual;
        private SpriteRenderer visualRenderer;
        private Color visualColor = Color.white;
        private float strikeTimer = -1f;
        private Vector2 strikeDirection = Vector2.down;

        private static readonly Color StrikeFlashColor = new Color(1f, 0.35f, 0.3f, 1f);

        // Perspectiva: altura a la que el enemigo toca la muralla (allí se ve a tamaño normal), mitad
        // del alto del dibujo para mantener los pies en el suelo al encoger, y la barra de vida del jefe,
        // que acompaña a la cabeza. Sin muralla (wallY NaN) el enemigo se ve siempre a tamaño normal.
        private float wallY = float.NaN;
        private float visualHalfHeight;
        private Transform[] healthBarParts = new Transform[0];
        private float[] healthBarHeights = new float[0];

        // El jefe es el único con barra de vida: su golpe es más largo y contundente.
        private bool IsBoss => healthFill != null;
        private float StrikeOut => IsBoss ? 0.08f : 0.06f;
        private float StrikeBack => IsBoss ? 0.26f : 0.18f;
        private float StrikeDistance => IsBoss ? 0.35f : 0.2f;
        private float StrikeSquash => IsBoss ? 0.4f : 0.28f;

        /// <summary>Transform del dibujo, el único que se mueve al golpear.</summary>
        public Transform Visual => visual;

        /// <summary>True mientras está en contacto con la puerta y la golpea.</summary>
        public bool IsAttacking { get; private set; }
        /// <summary>True si es el objetivo actual del campo de respuesta.</summary>
        public bool IsTargeted { get; private set; }
        /// <summary>Mensaje original (sin cifrar), con el que se compara la respuesta.</summary>
        public string Message { get; private set; }
        /// <summary>Mensaje cifrado tal como se ve en pantalla.</summary>
        public string EncryptedMessage => messageLabel.text;
        /// <summary>Aciertos que faltan para derrotarlo.</summary>
        public int RemainingDecrypts { get; private set; }

        /// <summary>Fila del mensaje, para que no se solape con el de otros enemigos.</summary>
        public int LabelRow { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            RemainingDecrypts = decryptsToKill;
            // El mensaje cifrado solo se muestra en la barra superior, no encima del enemigo.
            // El texto se sigue guardando en la etiqueta porque EncryptedMessage lo lee de ahí.
            messageLabel.enabled = false;

            visualRenderer = SpriteVisual.MoveToChild(gameObject);
            if (visualRenderer != null)
            {
                visual = visualRenderer.transform;
                visualColor = visualRenderer.color;
                visualHalfHeight = visualRenderer.sprite != null ? visualRenderer.sprite.bounds.extents.y : 0f;
                if (TryGetComponent(out SpriteAnimator animator))
                {
                    animator.SetTarget(visualRenderer);
                }
            }

            if (healthFill != null)
            {
                healthFillWidth = healthFill.localScale.x;
                // La barra son dos hijos del enemigo: el fondo (BarraVida) y el relleno.
                Transform background = transform.Find("BarraVida");
                healthBarParts = background != null ? new[] { background, healthFill } : new[] { healthFill };
                healthBarHeights = new float[healthBarParts.Length];
                for (int i = 0; i < healthBarParts.Length; i++)
                {
                    healthBarHeights[i] = healthBarParts[i].localPosition.y;
                }
            }
        }

        /// <summary>
        /// Prepara al enemigo recién creado con la velocidad y el daño que su tipo tiene en el nivel.
        /// Guarda el mensaje original para comprobar la respuesta y muestra en pantalla solo el cifrado.
        /// </summary>
        /// <param name="doorPosition">Posición de la puerta, hacia la que avanza.</param>
        /// <param name="stats">Prefab, velocidad y daño de su tipo en este nivel.</param>
        /// <param name="config">Nivel actual: intervalo de golpes y desplazamiento César.</param>
        /// <param name="message">Mensaje original que tendrá que descifrar el jugador.</param>
        /// <param name="row">Fila del texto, para que no se solape con el de otros enemigos.</param>
        public void Init(Vector2 doorPosition, EnemyStats stats, LevelConfig config, string message, int row)
        {
            LabelRow = row;
            PlaceLabel(row);
            destination = doorPosition;
            speed = stats.speed;
            damagePerHit = stats.damagePerHit;
            secondsBetweenHits = config.secondsBetweenHits;
            caesarShift = config.caesarShift;
            SetMessage(message);

            // El enemigo se para cuando su círculo toca el borde superior del collider de la puerta:
            // esa es la altura en la que debe verse a tamaño normal.
            Door door = FindAnyObjectByType<Door>();
            if (door != null && door.TryGetComponent(out Collider2D doorCollider))
            {
                float radius = GetComponent<CircleCollider2D>().radius * Mathf.Abs(transform.lossyScale.y);
                wallY = doorCollider.bounds.max.y + radius;
            }
        }

        /// <summary>
        /// Tamaño con el que se ve el enemigo según lo lejos que está: 1 al llegar a la muralla y menos
        /// cuanto más cerca del horizonte, donde vale <see cref="minPerspectiveScale"/>. Solo es visual.
        /// </summary>
        public float PerspectiveScale
        {
            get
            {
                if (float.IsNaN(wallY) || horizonY <= wallY)
                {
                    return 1f;
                }

                // 0 en el horizonte y 1 en la muralla; el tamaño crece en línea recta entre los dos extremos.
                float t = Mathf.Clamp01((horizonY - transform.position.y) / (horizonY - wallY));
                return Mathf.Lerp(minPerspectiveScale, 1f, t);
            }
        }

        // Las filas pares van encima del enemigo y las impares debajo, para no llegar a la barra superior.
        private void PlaceLabel(int row)
        {
            Vector2 position = messageLabel.rectTransform.anchoredPosition;
            float offset = position.y + row / 2 * LabelRowHeight;
            position.y = row % 2 == 0 ? offset : -offset;
            messageLabel.rectTransform.anchoredPosition = position;
        }

        /// <summary>Cambia el mensaje: guarda el original para comprobar la respuesta y muestra el cifrado.</summary>
        /// <param name="message">Mensaje original en mayúsculas.</param>
        public void SetMessage(string message)
        {
            Message = message;
            messageLabel.text = CaesarCipher.Encrypt(message, caesarShift);
        }

        /// <summary>Resta un descifrado. Devuelve true si el enemigo ya no puede seguir.</summary>
        /// <returns>True si ya no le quedan aciertos y hay que destruirlo.</returns>
        public bool TakeDecrypt()
        {
            RemainingDecrypts--;

            // La barra se encoge por la derecha: reduce el ancho y mueve el centro a la izquierda.
            if (healthFill != null)
            {
                float ratio = (float)RemainingDecrypts / decryptsToKill;
                healthFill.localScale = new Vector3(healthFillWidth * ratio, healthFill.localScale.y, 1f);
                healthFill.localPosition = new Vector3(-healthFillWidth * (1f - ratio) / 2f, healthFill.localPosition.y, 0f);
            }

            return RemainingDecrypts <= 0;
        }

        /// <summary>Marca o desmarca al enemigo como objetivo: enseña el marco y pinta su mensaje en dorado.</summary>
        /// <param name="targeted">True para marcarlo como objetivo.</param>
        public void SetTargeted(bool targeted)
        {
            IsTargeted = targeted;
            targetFrame.SetActive(targeted);
            messageLabel.color = targeted ? targetLabelColor : labelColor;
        }

        // El movimiento va en FixedUpdate porque usa física. Se recalcula la dirección en cada paso
        // y deja de aplicarse cuando el enemigo ya está golpeando la puerta.
        private void FixedUpdate()
        {
            if (!IsAttacking)
            {
                body.linearVelocity = (destination - body.position).normalized * speed;
            }
        }

        // Primer contacto con la puerta: el enemigo se detiene y empieza a contar para golpear.
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.TryGetComponent<Door>(out _))
            {
                if (!IsAttacking) MusicPlayer.PlayEffect(SoundCue.EnemyAttack);
                IsAttacking = true;
                hitTimer = 0f;
                body.linearVelocity = Vector2.zero;
            }
        }

        // Se ejecuta en cada paso de física mientras siga el contacto: acumula tiempo y golpea al
        // llegar al intervalo del nivel. Por eso el daño no depende de que el enemigo se separe.
        // El Rigidbody2D del prefab no duerme: dormido dejaría de recibir este evento y la puerta de perder vida.
        private void OnCollisionStay2D(Collision2D collision)
        {
            if (!collision.collider.TryGetComponent(out Door door))
            {
                return;
            }

            hitTimer += Time.fixedDeltaTime;
            if (hitTimer >= secondsBetweenHits)
            {
                hitTimer -= secondsBetweenHits;
                door.TakeDamage(damagePerHit);

                // La animación sale del mismo punto que el daño, así el golpe visual coincide con él.
                StartStrike(door.transform.position);
                door.PlayHitFeedback(IsBoss ? 2f : 1f);
            }
        }

        // Prepara la embestida hacia la puerta; LateUpdate la anima en los fotogramas siguientes.
        private void StartStrike(Vector2 doorPosition)
        {
            Vector2 toDoor = doorPosition - body.position;
            // La puerta es ancha: el enemigo embiste sobre todo hacia abajo, hacia ella.
            strikeDirection = new Vector2(toDoor.x * 0.25f, toDoor.y).normalized;
            if (strikeDirection == Vector2.zero)
            {
                strikeDirection = Vector2.down;
            }
            strikeTimer = 0f;
        }

        // Cada fotograma: tamaño por perspectiva y, si está golpeando, la embestida (ida rápida hacia la
        // puerta estirándose, aplastado contra ella en el impacto y vuelta más lenta, con un destello
        // rojo que se apaga). Solo cambia el hijo visual; el SpriteAnimator sigue con sus fotogramas.
        private void LateUpdate()
        {
            if (visual == null)
            {
                return;
            }

            Vector3 strikeScale = Vector3.one;
            float reach = 0f;
            float flash = 0f;

            if (strikeTimer >= 0f)
            {
                strikeTimer += Time.deltaTime;
                if (strikeTimer < StrikeOut)
                {
                    // Ida: se lanza y se estira en la dirección del golpe.
                    float t = strikeTimer / StrikeOut;
                    reach = t * t;
                    strikeScale = new Vector3(1f - StrikeSquash * 0.4f * t, 1f + StrikeSquash * 0.6f * t, 1f);
                    flash = t;
                }
                else if (strikeTimer < StrikeOut + StrikeBack)
                {
                    // Vuelta: parte aplastado contra la puerta y recupera su forma.
                    float t = (strikeTimer - StrikeOut) / StrikeBack;
                    reach = 1f - Mathf.SmoothStep(0f, 1f, t);
                    float squash = (1f - t) * (1f - t);
                    strikeScale = new Vector3(1f + StrikeSquash * squash, 1f - StrikeSquash * 0.7f * squash, 1f);
                    flash = 1f - t;
                }
                else
                {
                    strikeTimer = -1f;
                }
            }

            // Al cambiar de tamaño (distancia y visualScale) se mueve el dibujo para que los pies sigan en el suelo.
            float perspective = PerspectiveScale * visualScale;
            Vector3 feet = new Vector3(0f, -(1f - perspective) * visualHalfHeight, 0f);

            // La raíz tiene escala, así que el desplazamiento de la embestida (en mundo) se pasa a local.
            Vector3 worldOffset = (Vector3)(strikeDirection * (StrikeDistance * reach));
            visual.localPosition = feet + transform.InverseTransformVector(worldOffset);
            // Al embestir se acerca un poco a la cámara, así que crece ligeramente.
            visual.localScale = strikeScale * (perspective * (1f + 0.12f * reach));
            visualRenderer.color = Color.Lerp(visualColor, StrikeFlashColor, flash);

            // El marco del objetivo rodea al dibujo y la barra del jefe sigue a su cabeza.
            targetFrame.transform.localPosition = feet;
            targetFrame.transform.localScale = Vector3.one * perspective;
            // La barra solo sigue a la distancia (no a visualScale), para no montarse sobre el mensaje.
            float distance = PerspectiveScale;
            float barFeet = -(1f - distance) * visualHalfHeight;
            for (int i = 0; i < healthBarParts.Length; i++)
            {
                Vector3 position = healthBarParts[i].localPosition;
                healthBarParts[i].localPosition = new Vector3(position.x, barFeet + healthBarHeights[i] * distance, position.z);
            }
        }

        // Si dejara de tocar la puerta, vuelve a avanzar.
        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.collider.TryGetComponent<Door>(out _))
            {
                IsAttacking = false;
            }
        }
    }
}
