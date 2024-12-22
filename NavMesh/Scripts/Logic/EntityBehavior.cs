using UnityEngine;
using UnityEngine.AI;

namespace CCengine
{
    [System.Serializable]
    public class EntityBehavior : MonoBehaviour
    {
        public Entity Data { get; private set; }
        private Vector3 targetPosition;
        private Vector3 previousPosition;
        private float interpolationTimer = 0f;
        private float interpolationDuration = 0.5f; // Match the turn duration

        public void Initialize(Entity data)
        {
            Data = data;
            transform.position = data.Position;
            targetPosition = data.Position;
            previousPosition = data.Position;
        }

        public void SetTargetPosition(Vector3 newPosition)
        {
            previousPosition = transform.position;
            targetPosition = newPosition;
            interpolationTimer = 0f;
        }

        private void Update()
        {
            interpolationTimer += Time.deltaTime;
            float t = interpolationTimer / interpolationDuration;
            t = Mathf.Clamp01(t);

            // Interpolate position
            transform.position = Vector3.Lerp(previousPosition, targetPosition, t);
        }

        // private void OnTriggerEnter(Collider other)
        // {
        //     EntityBehavior otherEntity = other.GetComponent<EntityBehavior>();
        //     if (otherEntity != null && IsEnemy(otherEntity))
        //     {
        //         OnCombatTriggered(otherEntity);
        //     }
        // }

        public void DetectNearbyEnemies(float radius)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
            foreach (Collider collider in colliders)
            {
                EntityBehavior nearbyEntity = collider.GetComponent<EntityBehavior>();
                if (nearbyEntity != null && IsEnemy(nearbyEntity))
                {
                    OnCombatTriggered(nearbyEntity);
                }
            }
        }

        private bool IsEnemy(EntityBehavior other)
        {
            // Define logic for determining enemies
            return Data.EntityType != other.Data.EntityType;
        }

        private void OnCombatTriggered(EntityBehavior enemy)
        {
            Debug.Log($"{Data.Name} triggered combat with {enemy.Data.Name}");
            // Notify server or handle combat logic
        }
    }
}