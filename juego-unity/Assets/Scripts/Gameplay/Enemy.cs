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
        private const float LabelRowHeight = 0.5f;

        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private GameObject targetFrame;
        [SerializeField] private Color labelColor = new Color32(0xF2, 0xE8, 0xCF, 0xFF);
        [SerializeField] private Color targetLabelColor = new Color32(0xD4, 0xA0, 0x17, 0xFF);

        [Header("Tipo de enemigo")]
        [SerializeField, Min(0.1f)] private float speedMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float damageMultiplier = 1f;
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

        public bool IsAttacking { get; private set; }
        public bool IsTargeted { get; private set; }
        public string Message { get; private set; }
        public string EncryptedMessage => messageLabel.text;
        public float SpeedMultiplier => speedMultiplier;
        public int RemainingDecrypts { get; private set; }

        /// <summary>Fila del mensaje mientras golpea, para que no se solape con el de otros enemigos.</summary>
        public int LabelRow { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            RemainingDecrypts = decryptsToKill;

            if (healthFill != null)
            {
                healthFillWidth = healthFill.localScale.x;
            }
        }

        /// <summary>
        /// Prepara al enemigo recién creado con los valores del nivel, ajustados por los
        /// multiplicadores de su tipo. Guarda el mensaje original para comprobar la respuesta
        /// y muestra en pantalla solo el cifrado.
        /// </summary>
        public void Init(Vector2 doorPosition, LevelConfig config, string message, int row)
        {
            LabelRow = row;
            destination = doorPosition;
            speed = config.enemySpeed * speedMultiplier;
            damagePerHit = Mathf.RoundToInt(config.damagePerHit * damageMultiplier);
            secondsBetweenHits = config.secondsBetweenHits;
            caesarShift = config.caesarShift;
            SetMessage(message);
        }

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
                IsAttacking = true;
                hitTimer = 0f;
                body.linearVelocity = Vector2.zero;
                MoveLabel(LabelRow * LabelRowHeight);
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
            }
        }

        // Si dejara de tocar la puerta, vuelve a avanzar.
        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.collider.TryGetComponent<Door>(out _))
            {
                IsAttacking = false;
                MoveLabel(-LabelRow * LabelRowHeight);
            }
        }

        private void MoveLabel(float height)
        {
            messageLabel.rectTransform.anchoredPosition += Vector2.up * height;
        }
    }
}
