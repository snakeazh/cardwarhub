using UnityEngine;

namespace FusionContinent
{
    public static class UIUtils
    {
        public static Vector3 ConvertToLocal(Camera sourceCamera, Transform sourceTransform,Camera targetCamera,Transform targetParentTransform)
        {
            // RectTransformUtility.ScreenPointToLocalPointInRectangle()
            // var worldV3 = targetCamera.ScreenToWorldPoint(sourceScreenV3);
            
            // RectTransformUtility.ScreenPointToLocalPointInRectangle()
            
            // 1. 【Source -> 屏幕】
            // 将 sourceTransform 的世界坐标转换为 A相机的屏幕坐标
            // 注意：resultScreenPos.z 等于物体距离 A相机的深度
            Vector3 screenPos = sourceCamera.WorldToScreenPoint(sourceTransform.position);
            // ---------------------------------------------------------
            // 【关键点：Z轴处理】
            // 如果你希望转换后的点在 B相机前方固定的距离，需要在这里修改 z
            // 如果保持不变，则物体在B相机里的深度 = 物体在A相机里的深度
            // screenPos.z = 10f; // 示例：强制设定距离B相机10米
            // ---------------------------------------------------------
            // 2. 【屏幕 -> 世界 (基于B相机)】
            // 将屏幕坐标转换为 B相机视角的 世界坐标
            // 这里的“相对”通常指通过B的视口投影出的世界位置
            Vector3 worldPosFromB = targetCamera.ScreenToWorldPoint(screenPos);
            // 3. 【世界 -> Target本地】
            // 将计算出的世界坐标转换为 targetTransform 的本地坐标 (Relative Coordinate)
            Vector3 finalLocalPos = targetParentTransform.InverseTransformPoint(worldPosFromB);
            // 输出结果
            LogTool.Log($"最终相对于Target的坐标: {finalLocalPos}");
            // 测试：如果你想把一个物体放在这个位置验证
            // testObject.transform.localPosition = finalLocalPos; // 需设置父物体为targetTransform
            return finalLocalPos;
        }
    }
}