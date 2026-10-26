using System;
using Config;
using UnityEngine;
using QFramework;

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Random = UnityEngine.Random;

namespace FusionContinent
{
    public partial class DamageFun : ViewController
    {
        private static readonly Dictionary<Vector2Int, List<DamageFun>> s_ActiveStackMap = new Dictionary<Vector2Int, List<DamageFun>>();
        private const float StackStepY = 0.3f;
        private const float StackQuantize = 15f;
        public GameObject CritDamageEffect;
        private bool m_HasStackKey;
        private Vector2Int m_StackKey;
        private Coroutine m_ReturnCoroutine;
        private Coroutine m_ReleaseCoroutine;

        private Vector3 PrepareStackPosition(Vector3 basePos)
        {
            var key = new Vector2Int(Mathf.RoundToInt(basePos.x * StackQuantize), Mathf.RoundToInt(basePos.y * StackQuantize));
            if (!s_ActiveStackMap.TryGetValue(key, out var stackList))
            {
                stackList = new List<DamageFun>();
                s_ActiveStackMap[key] = stackList;
            }

            stackList.RemoveAll(item => item == null || !item.gameObject.activeInHierarchy);

            for (int i = 0; i < stackList.Count; i++)
            {
                var item = stackList[i];
                if (item == null || item == this)
                {
                    continue;
                }

                item.transform.position += new Vector3(0f, StackStepY, 0f);
            }

            // 栈规则：新的跳字永远在“最底层”（bottom）
            // 同时 old 的跳字被上推一格，所以新跳字不需要再上移。
            if (stackList.Contains(this))
            {
                stackList.Remove(this);
            }
            stackList.Insert(0, this);

            m_StackKey = key;
            m_HasStackKey = true;

            // 同步 sibling 排序：底层渲染在后/前取决于 Unity UI 规则，
            // 我们用“底 -> 上”重排，确保新跳字不会跑到视觉最上面。
            var parent = transform.parent;
            if (parent != null)
            {
                int minSiblingIndex = int.MaxValue;
                for (int i = 0; i < stackList.Count; i++)
                {
                    var item = stackList[i];
                    if (item == null || item.transform == null)
                    {
                        continue;
                    }

                    if (item.transform.parent != parent)
                    {
                        continue;
                    }

                    minSiblingIndex = Mathf.Min(minSiblingIndex, item.transform.GetSiblingIndex());
                }

                if (minSiblingIndex != int.MaxValue)
                {
                    for (int i = 0; i < stackList.Count; i++)
                    {
                        var item = stackList[i];
                        if (item == null || item.transform == null || item.transform.parent != parent)
                        {
                            continue;
                        }

                        item.transform.SetSiblingIndex(minSiblingIndex + i);
                    }
                }
            }

            return basePos;
        }

        private void ReleaseStackPosition()
        {
            if (!m_HasStackKey)
            {
                return;
            }

            if (s_ActiveStackMap.TryGetValue(m_StackKey, out var stackList))
            {
                stackList.Remove(this);
                if (stackList.Count == 0)
                {
                    s_ActiveStackMap.Remove(m_StackKey);
                }
            }

            m_HasStackKey = false;
        }

        private void StartAutoReturn(float delaySeconds)
        {
            if (m_ReturnCoroutine != null)
            {
                StopCoroutine(m_ReturnCoroutine);
                m_ReturnCoroutine = null;
            }

            m_ReturnCoroutine = StartCoroutine(AutoReturnAfterDelay(delaySeconds));
        }

        private void StartReleaseFromStack(float delaySeconds)
        {
            if (m_ReleaseCoroutine != null)
            {
                StopCoroutine(m_ReleaseCoroutine);
                m_ReleaseCoroutine = null;
            }

            m_ReleaseCoroutine = StartCoroutine(ReleaseFromStackAfterDelay(delaySeconds));
        }

        private IEnumerator ReleaseFromStackAfterDelay(float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            ReleaseStackPosition();
            m_ReleaseCoroutine = null;
        }

        private IEnumerator AutoReturnAfterDelay(float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            ReleaseStackPosition();
            
            DamageUIManager.Instance.Pool.Return(this.gameObject);
            m_ReturnCoroutine = null;
        }

        private static Transform EnsureDamageGroup(int siblingIndex = -1, Transform parentOverride = null)
        {
            Transform root = parentOverride != null ? parentOverride : GameLand.Default?.DamageParent;
            if (root == null)
            {
                return null;
            }

            var group = root.Find("TempGroup");
            if (group == null)
            {
                var temp = new GameObject("TempGroup");
                temp.transform.SetParent(root);
                temp.transform.localPosition = Vector3.zero;
                temp.transform.localScale = Vector3.one;
                group = temp.transform;
            }

            if (siblingIndex >= 0)
            {
                group.SetSiblingIndex(siblingIndex);
            }

            return group;
        }

        private static DamageFun SpawnDamageFun(Vector3 worldPos, int siblingIndex = -1, Transform parentOverride = null)
        {
            
            var pool = DamageUIManager.Instance.Pool;
            var damageObj = pool?.Get();
            if (damageObj == null)
            {
                return null;
            }

            var group = EnsureDamageGroup(siblingIndex, parentOverride);
            if (group == null)
            {
                pool.Return(damageObj);
                return null;
            }

            damageObj.SetActive(true);
            damageObj.transform.SetParent(group);
            if (siblingIndex >= 0)
            {
                damageObj.transform.SetSiblingIndex(siblingIndex);
            }
            damageObj.transform.localScale = Vector3.one * 0.05f;
            damageObj.transform.localPosition = Vector3.zero;
            damageObj.transform.position = worldPos;
            return damageObj.GetComponent<DamageFun>();
        }

        // 统一静态接口：外部只需要传 string（数字会在内部解析后走对应显示逻辑）
        public static void Show(
            string text,
            Vector3 worldPos,
            TakeDamageType damageType = TakeDamageType.General,
            bool isCriticalDamage = false,
            bool isToughness = false,
            Color color = default,
            int siblingIndex = -1,
            Transform parentOverride = null)
        {
            var damageFun = SpawnDamageFun(worldPos, siblingIndex, parentOverride);
            if (damageFun == null)
            {
                return;
            }

            if (damageType == TakeDamageType.Miss || (!isToughness && damageType == TakeDamageType.Miss))
            {
                damageFun.SetData(text, worldPos, color);
                return;
            }

            if (isToughness)
            {
                if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var toughnessValue))
                {
                    // 兼容可能的本地小数点格式（如 1,23）
                    float.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out toughnessValue);
                }

                if (toughnessValue <= 0f)
                {
                    return;
                }

                damageFun.SetToughnessDamage(toughnessValue, worldPos, color);
                return;
            }

            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var damageValue))
            {
                damageFun.SetData(damageValue, worldPos, damageType, isCriticalDamage);
                return;
            }

            // 解析失败则回退到普通文本显示（不保证 damageType 样式）
            damageFun.SetData(text, worldPos, color);
        }

        void Awake()
        {
            // DamageText.richText = false;// 关闭 Rich Text（伤害跳字必做，避免Underline警告）
            // DamageText.font = DamageText.font; // 强制触发字体初始化
        }
		
        void OnEnable()
        {
            // DamageText.richText = false;
        }

        private void OnDisable()
        {
            if (m_ReturnCoroutine != null)
            {
                StopCoroutine(m_ReturnCoroutine);
                m_ReturnCoroutine = null;
            }

            if (m_ReleaseCoroutine != null)
            {
                StopCoroutine(m_ReleaseCoroutine);
                m_ReleaseCoroutine = null;
            }

            ReleaseStackPosition();
        }

        static StringBuilder builder = new StringBuilder();
        public string DamageConvert(int damage,TakeDamageType damageType,bool IsCriticalDamage)
        {
            builder.Clear();
            int damageTypeValue =  -1;
            if (IsCriticalDamage)
            {
                if (damageType == TakeDamageType.PhysicalDamage || damageType == TakeDamageType.MagicDamage)
                {
                    damageTypeValue = 0;
                }
                else if (damageType == TakeDamageType.RealDamage)
                {
                    damageTypeValue = 3;
                }
            }
            else
            {
                if (damageType == TakeDamageType.PhysicalDamage || damageType == TakeDamageType.SchoolDamage )
                {
                    damageTypeValue = 1;
                }
                else if (damageType == TakeDamageType.MagicDamage)
                {
                    damageTypeValue = 2;
                }
                else if (damageType == TakeDamageType.RealDamage)
                {
                    damageTypeValue = 3;
                }
            }
            
            
            // var damageTypeValue = (int)(IsCriticalDamage? TakeDamageType.CriticalDamage : TakeDamageType.PhysicalDamage) ;
            int absDamage = Mathf.Abs(damage);
            string damageText = absDamage.ToString(CultureInfo.InvariantCulture);

            for (int i = 0; i < damageText.Length; i++)
            {
                if (damageTypeValue>= 0)
                {
                    builder.Append("<sprite=");

                    string spriteName = $"{damageTypeValue+1}_{damageText[i]}";
                    var index = Global.DamageAsset.GetSpriteIndexFromName(spriteName);
                    builder.Append(index);
                    builder.Append(">");
                }
                else
                {
                    builder.Append(damageText[i]);
                }
            }

            return builder.ToString();
        }

        public void SetData(int damage, Vector3 pos, TakeDamageType damageType,bool IsCriticalDamage)
        {
            gameObject.SetActive(true);
            transform.localScale = Vector3.one;
            transform.position = PrepareStackPosition(pos);
            var rectt = transform.GetComponent<RectTransform>();
            float offestX = Random.Range(-150f, 0);
            rectt.anchoredPosition = new Vector2(rectt.anchoredPosition.x+offestX,rectt.anchoredPosition.y);
            var color = DamageText.color;
            DamageText.color = new Color(color.r, color.g, color.b, 1f);
            // DamageText.transform.localScale = Vector3.one * 0.4f;
            DamageText.transform.Find("SchoolDamage").localScale = Vector3.zero;
            // DamageText.outlineColor = new Color32(0,0,0,255);
            // 设置颜色和样式
            DamageText.transform.Find("CriticalDamage").localScale = Vector3.zero;
            switch (damageType)
            {
                case TakeDamageType.PhysicalDamage:
                    DamageText.color = Color.white;
                    break;
                case TakeDamageType.MagicDamage:
                    DamageText.color = new Color32(129, 194, 255, 255);
                    break;
                case TakeDamageType.RealDamage:
                    DamageText.color = new Color32(255, 255, 255, 255);
                    DamageText.transform.Find("SchoolDamage").localScale = Vector3.one;
                    break;
                case TakeDamageType.SchoolDamage:
                    DamageText.color = Color.white;
                    break;
            }

            bool showIsCriticalDamage = false;
            // 暴击伤害（排除真实伤害和流派伤害）
            if (IsCriticalDamage && damageType != TakeDamageType.SchoolDamage && damageType != TakeDamageType.RealDamage)
            {
                showIsCriticalDamage = true;
                DamageText.transform.localScale = Vector3.one;
                // DamageText.transform.Find("CriticalDamage").localScale = Vector3.one;
                DamageText.color = Color.red;
            }

            if (CritDamageEffect)
            {
                CritDamageEffect.SetActive(showIsCriticalDamage);
            }
            DamageText.text = DamageConvert(damage,damageType,showIsCriticalDamage);
            UpdateLayout();
            StartReleaseFromStack(0.2f);
            StartAutoReturn(1.1f);
        }

        public void UpdateLayout()
        {
            DamageText.rectTransform.sizeDelta = new Vector2(DamageText.preferredWidth, DamageText.preferredHeight);
            DamageText.rectTransform.parent.GetComponent<RectTransform>().sizeDelta = DamageText.rectTransform.sizeDelta;
        }
        
        public void SetData(string strVal, Vector3 pos, Color color = default)
        {
            gameObject.SetActive(true);
            transform.localScale = Vector3.one;
            transform.position = PrepareStackPosition(pos);

            DamageText.text = strVal;
            DamageText.transform.localScale = Vector3.one;
            DamageText.transform.Find("CriticalDamage").localScale = Vector3.zero;
            UpdateLayout();
            
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;

            if (color == default)
            {
                DamageText.color = Color.yellow;
            }
            else
            {
                DamageText.color = color;
            }
            StartReleaseFromStack(0.2f);
            StartAutoReturn(1.1f);
        }
        
        

        /// <summary>
        /// 显示韧性伤害飘字
        /// </summary>
        /// <param name="damage">削韧数值</param>
        /// <param name="pos">飘字世界坐标</param>
        /// <param name="color">飘字颜色，默认紫色</param>
        public void SetToughnessDamage(float damage, Vector3 pos, Color color = default)
        {
            gameObject.SetActive(true);
            transform.localScale = Vector3.one;
            transform.position = PrepareStackPosition(pos);

            DamageText.text = $"{(int)damage}";
            UpdateLayout();
            
            DamageText.transform.localScale = Vector3.one;
            DamageText.transform.Find("CriticalDamage").localScale = Vector3.zero;
            DamageText.transform.Find("SchoolDamage").localScale = Vector3.zero;
            DamageText.outlineColor = new Color32(0, 0, 0, 255);

            DamageText.color = (color == default)
                ? new Color32(201, 132, 255, 255)
                : color;

            // 上白下红垂直渐变
            var gradient = new TMPro.VertexGradient(
                new Color32(255, 255, 255, 255),  // top-left      白色
                new Color32(255, 255, 255, 255),  // top-right     白色
                new Color32(251, 19, 14, 255),    // bottom-left   #FB130E
                new Color32(251, 19, 14, 255)     // bottom-right  #FB130E
            );
            DamageText.enableVertexGradient = true;
            DamageText.colorGradient = gradient;

            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            StartReleaseFromStack(0.2f);
            StartAutoReturn(1f);
        }


    }
}