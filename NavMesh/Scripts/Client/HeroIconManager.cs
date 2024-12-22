using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace CCengine.Client
{
    public class HeroIconManager : MonoBehaviour
    {
        public static HeroIconManager Instance;

        [SerializeField] private GameObject heroIconPrefab;
        public Dictionary<string, HeroIconController> heroIcons = new Dictionary<string, HeroIconController>();
        private float heroOffset = 0.2f; // Hero position offset in cell for each team 

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }

        }

        public void InstantiateHeroIcon(HeroData hero)
        {
            GameObject heroGO = Instantiate(heroIconPrefab, hero.position, Quaternion.identity);
            heroGO.name = $"{hero.name}";
            heroGO.transform.SetParent(this.transform);

            // Initialize HeroIconController
            HeroIconController heroIC = heroGO.GetComponent<HeroIconController>();
            if (heroIC != null)
            {
                heroIC.Initialize(hero);
                heroIcons[hero.heroId] = heroIC;
            }
            else
            {
                Debug.LogError("HeroIconController component not found on hero marker prefab.");
            }
        }

        public void ActivateHeroIcon(string heroId)
        {
            if (heroIcons.TryGetValue(heroId, out HeroIconController heroController))
            {
                heroController.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogError("HeroData not found on heroIcons dictionary.");
            }
        }

        // public Vector2 GetTeamOffset(Team team)
        // {
        //     if (team == Team.TeamA)
        //     {
        //         return new Vector2(-heroOffset, -heroOffset);
        //     }
        //     else if (team == Team.TeamB)
        //     {
        //         return new Vector2(heroOffset, heroOffset);
        //     }
        //     else
        //     {
        //         return Vector2.zero;
        //     }
        // }

        // Function to remove the hero icon
        public void DeactivateHeroIcon(string heroId)
        {
            if (heroIcons.TryGetValue(heroId, out HeroIconController heroController))
            {
                // Deactivate the icon
                heroController.gameObject.SetActive(false);

                // Optionally, remove it from the dictionary if you won't reuse it
                // heroIcons.Remove(hero.Id);

                // Optionally, return it to a pool if using object pooling
                // HeroIconPool.Instance.ReturnHeroIcon(heroIcon);
            }
            else
            {
                Debug.LogWarning($"Hero icon for hero ID {heroId} not found.");
            }
        }

    }
}
