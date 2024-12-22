using UnityEngine;


namespace CCengine
{
    [CreateAssetMenu(fileName = "NewCreepPath", menuName = "Game/Creep Path Data")]
    public class CreepPathData : ScriptableObject
    {
        public SLVector3[] waypoints;
        public Lane lane;
        public Team team;
    }
}