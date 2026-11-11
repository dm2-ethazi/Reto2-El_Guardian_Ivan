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

        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private GameObject targetFrame;
        [SerializeField] private Color labelColor = new Color32(0xF2, 0xE8, 0xCF, 0xFF);
        [SerializeField] private Color targetLabelColor = new Color32(0xD4, 0xA0, 0x17, 0xFF);

        [Header("Tipo de enemigo")]
        [Tooltip("Mensajes que hay que descifrar para derrotarlo. 1 en los enemigos normales; el jefe usa más.")]
        [SerializeField, Min(1)] private int decryptsToKill = 1;
        [Tooltip("Relleno de la barra de vida. Solo lo usa el jefe.")]
        [SerializeField] private Transform healthFill;

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

        // El jefe es el único con barra de vida: su golpe es más largo y contundente.
        private bool IsBoss => healthFill != null;
        private float StrikeOut => IsBoss ? 0.08f : 0.06f;
        private float StrikeBack => IsBoss ? 0.26f : 0.18f;
        private float StrikeDistance => IsBoss ? 0.6f : 0.35f;
        private float StrikeSquash => IsBoss ? 0.4f : 0.28f;

        /// <summary>Transform del dibujo, el único que se mueve al golpear.</summary>
        public Transform Visual => visual;

        public bool IsAttacking { get; private set; }
        public bool IsTargeted { get; private set; }
        public string Message { get; private set; }
        public string EncryptedMessage => messageLabel.text;
        public int RemainingDecrypts { get; private set; }

        /// <summary>Fila del mensaje, para que no se solape con el de otros enemigos.</summary>
        public int LabelRow { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            RemainingDecrypts = decryptsToKill;

            visualRenderer = SpriteVisual.MoveToChild(gameObject);
            if (visualRenderer != null)
            {
                visual = visualRenderer.transform;
                visualColor = visualRenderer.color;
                if (TryGetComponent(out SpriteAnimator animator))
                {
                    animator.SetTarget(visualRenderer);
                }
            }

            if (healthFill != null)
            {
                healthFillWidth = healthFill.localScale.x;
            }
        }

        /// <summary>
        /// Prepara al enemigo recién creado con la velocidad y el daño que su tipo tiene en el nivel.
        /// Guarda el mensaje original para comprobar la respuesta y muestra en pantalla solo el cifrado.
        /// </summary>
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
        public void SetMessage(string message)
        {
            Message = message;
            messageLabel.text = CaesarCipher.Encrypt(message, caesarShift);
        }

        /// <summary>Resta un descifrado. Devuelve true si el enemigo ya no puede seguir.</summary>
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

        // Embestida: ida rápida hacia la puerta estirándose, aplastado contra ella en el impacto y
        // vuelta más lenta, con un destello rojo que se apaga. Fuera del golpe, el visual se queda
        // en su sitio y el SpriteAnimator sigue con su animación de andar o flotar.
        private void LateUpdate()
        {
            if (visual == null || strikeTimer < 0f)
            {
                return;
            }

            strikeTimer += Time.deltaTime;
            float reach;
            Vector3 scale;
            float flash;

            if (strikeTimer < StrikeOut)
            {
                // Ida: se lanza y se estira en la dirección del golpe.
                float t = strikeTimer / StrikeOut;
                reach = t * t;
                scale = new Vector3(1f - StrikeSquash * 0.4f * t, 1f + StrikeSquash * 0.6f * t, 1f);
                flash = t;
            }
            else if (strikeTimer < StrikeOut + StrikeBack)
            {
                // Vuelta: parte aplastado contra la puerta y recupera su forma.
                float t = (strikeTimer - StrikeOut) / StrikeBack;
                float ease = 1f - Mathf.SmoothStep(0f, 1f, t);
                reach = ease;
                float squash = (1f - t) * (1f - t);
                scale = new Vector3(1f + StrikeSquash * squash, 1f - StrikeSquash * 0.7f * squash, 1f);
                flash = 1f - t;
            }
            else
            {
                strikeTimer = -1f;
                visual.localPosition = Vector3.zero;
                visual.localScale = Vector3.one;
                visualRenderer.color = visualColor;
                return;
            }

            // La raíz tiene escala, así que el desplazamiento en mundo se pasa a espacio local.
            Vector3 worldOffset = (Vector3)(strikeDirection * (StrikeDistance * reach));
            visual.localPosition = transform.InverseTransformVector(worldOffset);
            visual.localScale = scale;
            visualRenderer.color = Color.Lerp(visualColor, StrikeFlashColor, flash);
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
