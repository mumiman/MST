using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace CCengine.Client
{

    public class HeroIconController : MonoBehaviour
    {
        // Data
        [SerializeField] private HeroData heroData;

        // UI components
        [SerializeField] private Image heroIconImage;
        [SerializeField] private Image hpBarFillImage;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private int maxHealth;
        private int currentHealth;

        // Movement
        public Vector3 prevPosition;
        public float movementSpeed = 2f; // Movement speed in units per second

        // initialize the visual representation
        public void Initialize(HeroData data)
        {
            heroData = data;
            if (heroData == null)
            {
                Debug.LogError("HeroData is not assigned to HeroIconController.");
                return;
            }
            maxHealth = heroData.maxHP;
            currentHealth = heroData.currentHP;

            UpdateHeroColor();

            // Subscribe to events
            // .OnMoved += OnMoved;
            // .OnDamaged += OnDamaged;
            // .OnHealthChanged += OnHealthChanged;
            // .OnDestroyed += OnDestroyed;
        }

        private void UpdateHeroColor()
        {
            if (spriteRenderer != null)
            {
                Color newColor;
                // Logic to determine the color based on who controlled
                if (heroData.team == Team.TeamA) { newColor = Color.blue; }
                else if (heroData.team == Team.TeamB) { newColor = Color.red; }
                else { newColor = Color.white; }

                // Set the alpha value
                // newColor.a = 0.7f;

                // Assign the modified color back to the sprite renderer
                spriteRenderer.color = newColor;
            }
        }

        public void UpdateData(HeroData newData){
            heroData = newData;
        }

        #region movement
        public void AnimateMove(Vector3 newPosition)
        {
            // Reset Trigger the attack animation
            // animator.ResetTrigger("AttackTrigger");

            // Start movement coroutine
            StopAllCoroutines();
            StartCoroutine(MoveToPosition(newPosition));
        }

        private IEnumerator MoveToPosition(Vector3 newPosition)
        {
            prevPosition = transform.position;

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

        #endregion

        #region On damage
        // update the HP bar
        public void UpdateHealthBar(int newHealth)
        {
            currentHealth = newHealth;
            if (hpBarFillImage != null)
            {
                float healthPercentage = (float)currentHealth / maxHealth;
                hpBarFillImage.fillAmount = healthPercentage;

                // Optional: Update color based on health percentage
                Color healthColor = Color.Lerp(Color.red, Color.green, healthPercentage);
                hpBarFillImage.color = healthColor;
            }
        }

        #endregion

        #region Destroyed
        // Ensure event unsubscription when the object is destroyed
        public void OnDestroyed(string id)
        {
            // Deactivate the icon or perform any cleanup
            HeroIconManager.Instance.DeactivateHeroIcon(id);

            // Unsubscribe from events to prevent memory leaks
            // heroData.OnHealthChanged -= OnHealthChanged;
            // heroData.OnDestroyed -= OnDestroyed;
        }

        #endregion
    }
}
