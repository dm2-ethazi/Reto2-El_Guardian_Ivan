using PuertaCastillo.Crypto;
using TMPro;
using UnityEngine;

namespace PuertaCastillo.Gameplay
{
    /// <summary>
    /// Enemigo que avanza hacia la puerta y la golpea mientras está en contacto con ella.
    /// Lo configura <see cref="EnemySpawner"/> al crearlo.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class Enemy : MonoBehaviour
    {
        private const float SecondsBetweenHits = 1f;
        private const float LabelRowHeight = 0.5f;

        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private GameObject targetFrame;
        [SerializeField] private Color labelColor = new Color32(0xF2, 0xE8, 0xCF, 0xFF);
        [SerializeField] private Color targetLabelColor = new Color32(0xD4, 0xA0, 0x17, 0xFF);

        private Rigidbody2D body;
        private Vector2 destination;
        private float speed;
        private int damagePerHit;
        private float hitTimer;

        public bool IsAttacking { get; private set; }
        public bool IsTargeted { get; private set; }
        public string Message { get; private set; }
        public string EncryptedMessage => messageLabel.text;

        /// <summary>Fila del mensaje mientras golpea, para que no se solape con el de otros enemigos.</summary>
        public int LabelRow { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        public void Init(Vector2 doorPosition, LevelConfig config, string message, int row)
        {
            LabelRow = row;
            destination = doorPosition;
            speed = config.enemySpeed;
            damagePerHit = config.damagePerHit;
            Message = message;
            messageLabel.text = CaesarCipher.Encrypt(message, config.caesarShift);
        }

        public void SetTargeted(bool targeted)
        {
            IsTargeted = targeted;
            targetFrame.SetActive(targeted);
            messageLabel.color = targeted ? targetLabelColor : labelColor;
        }

        private void FixedUpdate()
        {
            if (!IsAttacking)
            {
                body.linearVelocity = (destination - body.position).normalized * speed;
            }
        }

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

        // El Rigidbody2D del prefab no duerme: dormido dejaría de recibir este evento y la puerta de perder vida.
        private void OnCollisionStay2D(Collision2D collision)
        {
            if (!collision.collider.TryGetComponent(out Door door))
            {
                return;
            }

            hitTimer += Time.fixedDeltaTime;
            if (hitTimer >= SecondsBetweenHits)
            {
                hitTimer -= SecondsBetweenHits;
                door.TakeDamage(damagePerHit);
            }
        }

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
