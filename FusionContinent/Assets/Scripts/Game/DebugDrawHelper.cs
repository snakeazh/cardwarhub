using UnityEngine;

namespace FusionContinent.Skill
{
    /// <summary>
    /// 调试绘制辅助组件，运行时临时挂到场景显示碰撞区域
    /// </summary>
    public class DebugDrawHelper : MonoBehaviour
    {
        private float _lifetime = 3f;
        private System.Action<Vector3> _onDraw;
        private float _startTime;

        public static void DrawBox(Vector3 center, Vector2 size, float angleDeg, Color color, float lifetime = 3f)
        {
            var go = new GameObject("DebugBoxGizmo");
            go.transform.position = center;
            var drawer = go.AddComponent<DebugDrawHelper>();
            drawer._lifetime = lifetime;
            drawer._onDraw = (_) => DrawBoxGizmo(center, size, angleDeg, color);
            drawer._startTime = Time.time;
        }
        
        public static void DrawCircle(Vector3 center, float radius, Color color, float lifetime = 3f)
        {
            var go = new GameObject("DebugCircleGizmo");
            go.transform.position = center;
            var drawer = go.AddComponent<DebugDrawHelper>();
            drawer._lifetime = lifetime;
            drawer._onDraw = (_) => DrawCircleGizmo(center, radius, color);
            drawer._startTime = Time.time;
        }

        public static void DrawSector(Vector3 center, Vector2 forward, float radius, float angleDeg, Color color, float lifetime = 3f)
        {
            var go = new GameObject("DebugSectorGizmo");
            go.transform.position = center;
            var drawer = go.AddComponent<DebugDrawHelper>();
            drawer._lifetime = lifetime;
            drawer._onDraw = (_) => DrawSectorGizmo(center, forward, radius, angleDeg, color);
            drawer._startTime = Time.time;
        }

        void OnDrawGizmos()
        {
            if (_onDraw != null)
            {
                _onDraw(transform.position);
            }
        }

        void Update()
        {
            if (Time.time - _startTime > _lifetime)
            {
                Destroy(gameObject);
            }
        }

        private static void DrawBoxGizmo(Vector3 center, Vector2 size, float angleDeg, Color color, float fillAlpha = 0.1f)
        {
            float halfW = size.x * 0.5f;
            float halfH = size.y * 0.5f;
            float rad = angleDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            Vector3 Rotate(Vector2 v) => center + new Vector3(v.x * cos - v.y * sin, v.x * sin + v.y * cos, 0);

            Vector3 tl = Rotate(new Vector2(-halfW, halfH));
            Vector3 tr = Rotate(new Vector2(halfW, halfH));
            Vector3 br = Rotate(new Vector2(halfW, -halfH));
            Vector3 bl = Rotate(new Vector2(-halfW, -halfH));

            Gizmos.color = new Color(color.r, color.g, color.b, fillAlpha);
            Gizmos.DrawWireCube(center, size);
            Gizmos.color = color;
            Gizmos.DrawLine(tl, tr);
            Gizmos.DrawLine(tr, br);
            Gizmos.DrawLine(br, bl);
            Gizmos.DrawLine(bl, tl);
            Gizmos.DrawLine(tl, br);
            Gizmos.DrawLine(tr, bl);
        }
        
        private static void DrawCircleGizmo(Vector3 center, float radius, Color color, int segments = 32)
        {
            Gizmos.color = color;

            float angle = 0f;
            float delta = Mathf.PI * 2f / segments;

            Vector3 prev = center + new Vector3(Mathf.Cos(0), Mathf.Sin(0)) * radius;

            for (int i = 1; i <= segments; i++)
            {
                angle = i * delta;
                Vector3 next = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                Gizmos.DrawLine(prev, next);
                prev = next;
            }

            // 中心点
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, 0.15f);
        }

        private static void DrawSectorGizmo(Vector3 center, Vector2 forward, float radius, float angleDeg, Color color)
        {
            float halfAngle = angleDeg * 0.5f * Mathf.Deg2Rad;
            float forwardAngle = Mathf.Atan2(forward.y, forward.x);
            float startAngle = forwardAngle - halfAngle;
            float endAngle = forwardAngle + halfAngle;

            Vector3 p1 = center + new Vector3(Mathf.Cos(startAngle), Mathf.Sin(startAngle)) * radius;
            Vector3 p2 = center + new Vector3(Mathf.Cos(endAngle), Mathf.Sin(endAngle)) * radius;

            Gizmos.color = color;
            Gizmos.DrawLine(center, p1);
            Gizmos.DrawLine(center, p2);

            int segments = Mathf.CeilToInt(angleDeg / 5f);
            Vector3 prev = p1;
            for (int i = 1; i <= segments; i++)
            {
                float a = Mathf.Lerp(startAngle, endAngle, (float)i / segments);
                Vector3 cur = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                Gizmos.DrawLine(prev, cur);
                prev = cur;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, 0.2f);
        }
    }
}
