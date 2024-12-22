using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace CCengine.Client
{
    public class CreepWaveIconController : MonoBehaviour
    {
        [SerializeField] public CreepWaveData cwData;
        [SerializeField] private Animator animator;
        [SerializeField] private Image hpBarFillImage;

        // Health
        private int maxHealth;
        private int currentHealth;
        // movement
        public Vector3 prevPosition;
        public float movementSpeed = 2f; // Movement speed in units per second

        public float attackSpeed = 5f;
        public float attackRange = 0.2f;

        // Effect
        public GameObject collisionEffectPrefab;
        public AudioClip collisionSoundClip;

        //initialize the visual representation of the cell
        public void Initialize(CreepWaveData data)
        {
            cwData = data;
            currentHealth = cwData.currentHP;
            maxHealth = cwData.maxHP;
            UpdateCreepWaveIcon();
            // Set initial rotation based on team
            SetInitialRotation();
            InstantMove(cwData.position);

            // Subscribe to events
            // CreepWaveData.OnMoved += OnMoved;
            // CreepWaveData.OnDamaged += OnDamaged;
            // CreepWaveData.OnHealthChanged += OnHealthChanged;
            // CreepWaveData.OnDestroyed += OnDestroyed;

            // Debug.Log($"Initialized CreepWave ID: {CreepWaveData.Id}");
        }

        // Method to update the cell's visual representation
        public void UpdateCreepWaveIcon()
        {
            // the color based on team
            Color newColor;
            if (cwData.team == Team.TeamA) { newColor = Color.blue; }
            else if (cwData.team == Team.TeamB) { newColor = Color.red; }
            else { newColor = Color.white; }

            // Set the alpha value
            // newColor.a = 0.7f;

            hpBarFillImage.color = newColor;
        }

        #region Movement
        private void SetInitialRotation()
        {
            if (cwData.team == Team.TeamA)
            {
                // Face towards the right upper
                transform.rotation = Quaternion.Euler(0, 0, -45f);
            }
            else if (cwData.team == Team.TeamB)
            {
                // Face towards the left lower
                transform.rotation = Quaternion.Euler(0, 0, 135f);
            }
            // Counter-rotate the HP bar
            // hpBarTransform.rotation = Quaternion.identity;
        }

        public void AnimateMove(Vector3 newPosition)
        {
            // Reset Trigger the attack animation
            animator.ResetTrigger("AttackTrigger");
            // Start movement coroutine
            StopAllCoroutines();
            StartCoroutine(MoveToPosition(newPosition));

        }

        private IEnumerator MoveToPosition(Vector3 newPosition)
        {
            // Calculate start and target positions with team offsets
            prevPosition = transform.position;

            // Vector2 startCellLocalPosition = Helper.ToLocalPosition(startCellPosition);
            // Vector2 startOffset = CreepWaveIconManager.Instance.GetTeamOffset(CreepWaveData.Team);
            // Vector2 startPosition = startCellLocalPosition + startOffset;

            // Vector2 targetCellLocalPosition = Helper.ToLocalPosition(newPosition);
            // Vector2 targetOffset = CreepWaveIconManager.Instance.GetTeamOffset(CreepWaveData.Team);
            // Vector2 targetPosition = targetCellLocalPosition + targetOffset;

            // Update transform position to start position
            // transform.position = startPosition;

            float distance = Vector2.Distance(prevPosition, newPosition);
            float duration = distance / movementSpeed;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                transform.position = Vector2.Lerp(prevPosition, newPosition, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            transform.position = newPosition;
        }

        // Update position immediately without animation
        public void InstantMove(Vector3 newPosition)
        {
            // Vector2 newLocalPosition = Helper.ToLocalPosition(CreepWaveData.Position);
            // transform.position = new Vector3(newLocalPosition.x, newLocalPosition.y, transform.position.z);
            transform.position = newPosition;
        }
        #endregion

        #region On Damage

        public void OnDamaged(int damage)
        {
            // Trigger the attack animation
            animator.SetTrigger("AttackTrigger");
        }

        // Event handler for health changes
        public void UpdateHealthBar(int newHealth)
        {   
            currentHealth = newHealth;
            if (hpBarFillImage != null)
            {
                float healthPercentage = (float)currentHealth / maxHealth;
                hpBarFillImage.fillAmount = healthPercentage;

                // Change the color of the health bar based on health percentage
                // Color healthColor = Color.Lerp(Color.red, Color.green, healthPercentage);
                // hpBarFillImage.color = healthColor;
            }
            else
            {
                Debug.LogWarning("hpBarFillImage is not assigned in the inspector.");
            }
        }

        private void CreateCollisionEffect(Vector2 position)
        {
            // Instantiate collision effect
            if (collisionEffectPrefab != null)
            {
                GameObject collisionEffect = Instantiate(collisionEffectPrefab, position, Quaternion.identity);
                Destroy(collisionEffect, 1f);
            }

            // Play sound effect
            if (collisionSoundClip != null)
            {
                AudioSource.PlayClipAtPoint(collisionSoundClip, position);
            }
        }

        // Event handler for when the creep wave is destroyed


        #endregion

        #region Destroy
        // Ensure event unsubscription when the object is destroyed
        public void OnDestroyed(string id)
        {
            // Deactivate the icon or perform any cleanup
            CreepWaveIconManager.Instance.DeactivateCreepWaveIcon(id);
        }

        public void PlayDeathAnimation()
        {
            // Play death animation
            if (animator != null)
            {
                animator.SetTrigger("Die");
            }
        }

        public float GetDeathAnimationDuration()
        {
            // Return the length of the death animation
            if (animator != null)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                return stateInfo.length;
            }
            return 0f;
        }

        #endregion

    }
}