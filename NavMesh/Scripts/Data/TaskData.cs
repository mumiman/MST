using UnityEngine;

namespace CCengine
{
    [CreateAssetMenu(fileName = "New Task", menuName = "Game/Task Data")]
    public class MissionData : ScriptableObject
    {
        public string taskId;
        public string taskName;
        [TextArea]
        public string description;
        public TaskType taskType;
        public int duration; // Duration of the task effect, if applicable
        // public Sprite icon;  // Optional: Icon representing the task

    }
}