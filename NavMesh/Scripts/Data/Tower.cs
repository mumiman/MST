using UnityEngine;

namespace CCengine
{
    [System.Serializable]
    public class Tower : Entity
    {
        public Tower(string name, Team team, Vector3 position, int maxHealth, int damage)
            : base(EntityType.Tower, name, team, position, maxHealth, damage) { }
    }
}