using UnityEngine;

namespace PuertaCastillo.Gameplay
{
    /// <summary>
    /// Enemigo que avanza hacia la puerta. Lo configura <see cref="EnemySpawner"/> al crearlo.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class Enemy : MonoBehaviour
    {
        private Rigidbody2D body;
        private Vector2 destination;
        private float speed;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        public void Init(Vector2 doorPosition, float moveSpeed)
        {
            destination = doorPosition;
            speed = moveSpeed;
        }

        private void FixedUpdate()
        {
            body.linearVelocity = (destination - body.position).normalized * speed;
        }
    }
}
