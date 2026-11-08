using UnityEngine;

namespace PuertaCastillo.Gameplay
{
    /// <summary>
    /// Mantiene como objetivo al enemigo más cercano a la puerta y comprueba las respuestas contra él.
    /// </summary>
    public class EnemyTargeting : MonoBehaviour
    {
        [SerializeField] private EnemySpawner spawner;
        [SerializeField] private Door door;

        public Enemy Target { get; private set; }

        // Recalcula el objetivo en cada frame y solo cambia el resaltado si es otro enemigo.
        private void Update()
        {
            Enemy closest = FindClosestToDoor();
            if (closest == Target)
            {
                return;
            }

            if (Target != null)
            {
                Target.SetTargeted(false);
            }

            Target = closest;

            if (Target != null)
            {
                Target.SetTargeted(true);
            }
        }

        /// <summary>
        /// Da por descifrado al objetivo si la respuesta coincide con su mensaje, sin espacios sobrantes y en mayúsculas.
        /// </summary>
        public bool TrySolve(string answer)
        {
            if (Target == null || answer.Trim().ToUpperInvariant() != Target.Message)
            {
                return false;
            }

            spawner.ResolveDecrypt(Target);
            return true;
        }

        // Recorre los enemigos activos y se queda con el más cercano a la puerta (el más peligroso).
        private Enemy FindClosestToDoor()
        {
            Enemy closest = null;
            float closestDistance = float.MaxValue;

            foreach (Enemy enemy in spawner.ActiveEnemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                float distance = Vector2.Distance(enemy.transform.position, door.transform.position);
                if (distance < closestDistance)
                {
                    closest = enemy;
                    closestDistance = distance;
                }
            }

            return closest;
        }
    }
}
