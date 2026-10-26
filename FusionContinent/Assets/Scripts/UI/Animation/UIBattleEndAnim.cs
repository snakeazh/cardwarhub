using System;
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System.Collections;
using QFramework;
using UnityEngine.UI;

namespace FusionContinent.Helper
{
    public class UIBattleEndAnim : MonoBehaviour
    {
        [Header("--- 动画对象配置 ---")]
        [Tooltip("背景的CanvasGroup")]
        public CanvasGroup backgroundGroup;

        [Tooltip("统计框列表 (请按出现的先后顺序拖入)")]
        public List<CanvasGroup> contentGroups;

        [Header("--- 动画参数 ---")]
        [Tooltip("从下方多少像素处浮现")]
        public float moveOffset = 150f;

        [Tooltip("单个元素的动画耗时")]
        public float duration = 0.5f;

        [Tooltip("每个元素出现的间隔时间")]
        public float interval = 0.2f;

        [Tooltip("动画曲线")]
        public Ease easeType = Ease.OutQuad;

        private Vector2 _bgOriginalPos;
        private List<Vector2> _contentOriginalPos = new List<Vector2>();
        private bool _hasRecordedPos = false;
    
        
        private void Awake()
        {
            // 防止一开始还没播动画时，界面是乱的
            // 这里不设为0，因为如果动画脚本失效，至少还能看到界面
        }

        public Action OnAnimationEnd;
        
        public void PlayAnim()
        {
            // 启动协程，等待一帧再播放
            // 这是解决 "Alpha为0" 和 "布局冲突" 的关键
            StartCoroutine(PlayAnimCoroutine());
        }

        private IEnumerator PlayAnimCoroutine()
        {
            // 1. 强制杀掉旧动画
            DOTween.Kill(gameObject);

            // 2. 强制刷新UI布局 (非常重要，否则LayoutGroup下的物体坐标可能是错的)
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());

            // 3. 等待这一帧结束，让Unity把UI位置算好
            yield return null; 

            // 4. 记录正确的位置 (只记录一次)
            RecordPositions();

            // 5. 初始化状态 (变透明、下移)
            ResetState(backgroundGroup, _bgOriginalPos);
            for (int i = 0; i < contentGroups.Count; i++)
            {
                ResetState(contentGroups[i], _contentOriginalPos[i]);
            }

            // 6. 创建动画队列
            Sequence seq = DOTween.Sequence();
            seq.SetId(gameObject);
            seq.SetUpdate(true); // ★★★ 核心：忽略 Time.timeScale ★★★

            // 添加背景
            if (backgroundGroup != null)
            {
                seq.Append(CreateTween(backgroundGroup, _bgOriginalPos));
            }

            // 添加内容
            for (int i = 0; i < contentGroups.Count; i++)
            {
                if (interval > 0) seq.AppendInterval(interval);
                seq.Append(CreateTween(contentGroups[i], _contentOriginalPos[i],true));
            }

            seq.OnComplete(() =>
            {
                OnAnimationEnd?.Invoke();
            });
            seq.Play();
        }

        private void RecordPositions()
        {
            if (_hasRecordedPos) return;

            if (backgroundGroup != null)
                _bgOriginalPos = backgroundGroup.GetComponent<RectTransform>().anchoredPosition;

            _contentOriginalPos.Clear();
            foreach (var cg in contentGroups)
            {
                if (cg != null)
                {
                    var pos = cg.GetComponent<RectTransform>().anchoredPosition;
                    // pos.x = 0;
                    _contentOriginalPos.Add(pos);
                    
                }
            }
            _hasRecordedPos = true;
        }

        private void ResetState(CanvasGroup cg, Vector2 finalPos)
        {
            if (cg == null) return;
            cg.alpha = 0f; // 设为透明
            cg.interactable = false;
            
            // 下移位置
            RectTransform rt = cg.GetComponent<RectTransform>();
            rt.anchoredPosition = finalPos - new Vector2(0, moveOffset);
        }

        private Sequence CreateTween(CanvasGroup cg, Vector2 finalPos,bool forcePosX = false)
        {
            Sequence s = DOTween.Sequence();
            RectTransform rt = cg.GetComponent<RectTransform>();
            if (forcePosX)
            {
                s.JoinCallback(() => { rt.LocalPositionX(0f);});
                finalPos.x = 0;
            }
            
            // 移动
            s.Join(rt.DOAnchorPos(finalPos, duration)
                   .SetEase(easeType)
                   .SetUpdate(true)); // 确保每个Tween都忽略TimeScale

            // 透明度
            s.Join(cg.DOFade(1f, duration)
                   .SetEase(easeType)
                   .SetUpdate(true));

            s.OnComplete(() =>
            {
                cg.interactable = true;
            });
            return s;
        }

        // --- 安全兜底 ---
        // 如果界面被关闭、销毁，或者脚本被禁用，强制把所有东西恢复可见
        // 防止下一次打开时因为状态残留导致看不见
        private void OnDisable()
        {
            RecoverAll();
        }

        private void OnDestroy()
        {
            DOTween.Kill(gameObject);
            RecoverAll();
        }

        private void RecoverAll()
        {
            if (backgroundGroup != null) backgroundGroup.alpha = 1f;
            foreach (var cg in contentGroups)
            {
                if (cg != null) cg.alpha = 1f;
            }
        }
    }
}