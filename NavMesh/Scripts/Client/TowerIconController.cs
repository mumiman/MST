using UnityEngine;
using UnityEngine.UI;

namespace CCengine.Client
{
    public class TowerIconController : MonoBehaviour
    {
        public TowerData TowerData { get; private set; }
        private SpriteRenderer spriteRenderer;

        // UI elements
        [SerializeField] private Image hpBarFillImage;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Method to initialize the visual representation of the cell
        public void Initialize(TowerData towerData)
        {
            TowerData = towerData;

            // Initialize the health bar
            UpdateHealthBar(TowerData.currentHP);

            // Subscribe to events
            // TowerData.OnHealthChanged += OnHealthChanged;
        }

        // Method to update the health bar UI
        public void UpdateHealthBar(int newHP)
        {
            TowerData.currentHP = newHP;
            if (hpBarFillImage != null)
            {
                float healthPercentage = (float)TowerData.currentHP / TowerData.maxHP;
                hpBarFillImage.fillAmount = healthPercentage;

                // Optionally: Change the color of the health bar based on health percentage
                Color healthColor = Color.Lerp(Color.red, Color.green, healthPercentage);
                hpBarFillImage.color = healthColor;
            }
            else
            {
                Debug.LogWarning("hpBarFillImage is not assigned in the inspector.");
            }
        }

        // Ensure event unsubscription when the object is destroyed
        private void OnDestroy()
        {
            if (TowerData != null)
            {
                // TowerData.OnHealthChanged -= OnHealthChanged;
            }
        }
    }
}
