using System.Collections.Generic;
using Config;

namespace FusionContinent.Model
{
    public static class TaskConfigMap
    {
        private static Dictionary<int, TaskListStruct> sSubTaskMap;
        private static Dictionary<int, TaskConfig> sGroupMap;

        public static void Build()
        {
            sSubTaskMap = new Dictionary<int, TaskListStruct>();
            sGroupMap = new Dictionary<int, TaskConfig>();

            var table = DataConfigManager.Tables?.TbTaskConfig?.DataList;
            if (table == null) return;

            foreach (var group in table)
            {
                foreach (var sub in group.TaskList)
                {
                    sSubTaskMap[sub.Id] = sub;
                    sGroupMap[sub.Id] = group;
                }
            }
        }

        public static bool TryGetSubTask(int subTaskId, out TaskListStruct subTask, out TaskConfig group)
        {
            if (sSubTaskMap == null)
            {
                Build();
            }

            if (sSubTaskMap != null && sSubTaskMap.TryGetValue(subTaskId, out subTask) && sGroupMap.TryGetValue(subTaskId, out group))
            {
                return true;
            }

            subTask = null;
            group = null;
            return false;
        }
    }
}
