using System.Collections.Generic;
using UnityEngine;

namespace CCengine
{
    public static class TaskManager
    {
        private static Dictionary<string, MissionData> taskDataDictionary;

        // Load all TaskData assets at runtime
        static TaskManager()
        {
            LoadAllTasks();
        }

        private static void LoadAllTasks()
        {
            taskDataDictionary = new Dictionary<string, MissionData>();
            MissionData[] tasks = Resources.LoadAll<MissionData>("TaskData");

            foreach (MissionData task in tasks)
            {
                if (!taskDataDictionary.ContainsKey(task.taskId))
                {
                    taskDataDictionary.Add(task.taskId, task);
                }
                else
                {
                    Debug.LogWarning($"Duplicate task ID detected: {task.taskId}");
                }
            }
        }

        // Retrieve TaskData by taskId
        public static MissionData GetTaskData(string taskId)
        {
            if (taskDataDictionary.TryGetValue(taskId, out MissionData taskData))
            {
                return taskData;
            }
            else
            {
                Debug.LogError($"TaskData with ID {taskId} not found.");
                return null;
            }
        }
    }
}
