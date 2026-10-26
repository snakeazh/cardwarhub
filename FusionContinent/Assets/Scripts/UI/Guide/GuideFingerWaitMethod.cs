using Config;
using FusionContinent.Model;
using QFramework;
using UnityEngine;


namespace FusionContinent.Guide
{
    public class GuideFingerWaitMethod : IGuideMethod
    {
        protected virtual GameObject GetInterObject(string path)
        {
            var target = GuideManager.Instance.FindGuideNode(_stepListStruct.Type);
            
            if (target != null)
            {
                return target;
            }
            return GameObject.Find(path);
        }
        public class HighLightMethod
        {
            public GameObject Owner;
            public RectTransform MaskedRectTransform;
            public RectTransform Arrow;
            public GuideArrowType mArrowType;
            public HighLightMethod(GameObject owner, RectTransform maskedRectTransform, RectTransform arrow,GuideArrowType arrowType)
            {
                Owner = owner;
                MaskedRectTransform = maskedRectTransform;
                Arrow = arrow;
                mArrowType =  arrowType;
            }
        }
        
        protected GuideContext _context;
        protected GuidesStepListStruct _stepListStruct;
        public void Init(GuideContext ctx, GuidesStepListStruct step)
        {
            _context = ctx;
            _stepListStruct = step;
        }

        protected EGuideType _guideType;
        protected HighLightMethod _interactionObject;

        
        protected HighLightMethod _interactionObject2;
        protected virtual string GetGuideUIPath()
        {
            return _stepListStruct.GuideUI;
        }
        public virtual void Enter()
        {
            LogTool.Log($"[GuideFingerWaitMethod] Enter - GuideUI: {_stepListStruct.GuideUI}, Position: ({_stepListStruct.Position.X}, {_stepListStruct.Position.Y})");
    
            _guideType = (_stepListStruct.TipType == GuideTipType.Finger || _stepListStruct.TipType == GuideTipType.WeakGuidance) ? EGuideType.Weak : EGuideType.Force;
            guideRoot = GetTargetRoot(this._context.Panel.Finger);
            this._context.Panel.Mask.enabled = this._stepListStruct.AddMask == 1;
            guideCanvas = guideRoot.GetComponent<Canvas>();
            if (!_stepListStruct.GuideUI.Equals(""))
            {

                _interactionObject  = new HighLightMethod(GetInterObject(GetGuideUIPath()),this._context.Panel.Maskabled
                ,this._context.Panel.Arrow1,_stepListStruct.ArrowOneType);

                // var targetRect = _interactionObject.Owner.GetComponent<RectTransform>();
                // LogTool.Log($"[GuideFingerWaitMethod] 目标UI: {_stepListStruct.GuideUI}, 世界坐标: {targetRect.position}, 锚点坐标: {targetRect.anchoredPosition}");
                Vector2 localPos = WorldPointToLocalPoint(_interactionObject.Owner.Position(),
                    this._context.Panel.GetComponent<RectTransform>());
                this._context.Panel.Finger.anchoredPosition= localPos;
                LogTool.Log($"[GuideFingerWaitMethod] 设置手指位置 - 世界坐标: {this._context.Panel.Finger.position}, 锚点坐标: {localPos}");
                
                this._context.Panel.Finger.anchoredPosition +=
                    new Vector2(_stepListStruct.Position.X, _stepListStruct.Position.Y);
                LogTool.Log($"[GuideFingerWaitMethod] 应用偏移后 - 锚点坐标: {this._context.Panel.Finger.anchoredPosition}");
            }
            else
            {
                _interactionObject  = new HighLightMethod(guideRoot.gameObject,this._context.Panel.Maskabled,this._context.Panel.Arrow1,_stepListStruct.ArrowOneType);
            }
            if (!_stepListStruct.GuideUITwo.Equals(""))
            {
                _interactionObject2  = new HighLightMethod(GameObject.Find(_stepListStruct.GuideUITwo),
                    this._context.Panel.Maskabled2,this._context.Panel.Arrow2,_stepListStruct.ArrowTwoType);

            }
            else
            {
                _interactionObject2  = new HighLightMethod(null,this._context.Panel.Maskabled2,
                    this._context.Panel.Arrow2,_stepListStruct.ArrowTwoType);
            }
      

            // highlight = this._context.Panel.Finger;
            HighlightTarget(_interactionObject,new Vector2(_stepListStruct.Position.X,_stepListStruct.Position.Y),_stepListStruct.ArrowOneType);
            HighlightTarget(_interactionObject2,new Vector2(_stepListStruct.Position.X,_stepListStruct.Position.Y),_stepListStruct.ArrowTwoType);
            if (this._stepListStruct.Type == GuideType.RefreshTip)
            {
                this._context.Panel.GuideTipsMessage1.gameObject.SetActive(true);
                this._context.Panel.GuideTipsMessage1.anchoredPosition =
                    this._context.Panel.Maskabled.anchoredPosition + new Vector2(0, -200);
            }
            if (this._stepListStruct.Type == GuideType.RefreshTip)
            {
                this._context.Panel.GuideTipsMessage2.gameObject.SetActive(true);
                this._context.Panel.GuideTipsMessage2.anchoredPosition =
                    this._context.Panel.Maskabled2.anchoredPosition + new Vector2(0, -200);
            }
            //动态设置引导对话
            var rect = UITransformUtil.GetLocalRectIn(guideRoot, guideCanvas, _interactionObject.Owner.GetComponent<RectTransform>());
            int rate = rect.center.y > 0 ? -1 : 1;
            float offsetY = rect.center.y > 0 ? -220 : 0;
            this._context.Panel.Dialoge.anchoredPosition = new Vector2(this._context.Panel.Dialoge.anchoredPosition.x,
                rect.center.y + (this._context.Panel.Dialoge.sizeDelta.y/2+_stepListStruct.HighLightSize.Y/2+offsetY)*rate); 
            
        
            this._context.Panel.Finger.gameObject.SetActive(true);
            // this._ctx.Panel.GetComponentInParent()
            SetFingerDepth(this._context.Panel.Finger);
            // Global.Publisher.Subscribe(MessagePublisher.CHANGE_GUIDE_INTERACTION+_guideType.ToString(),OnGuideChange);
        }

        protected virtual void SetFingerDepth(RectTransform finger)
        {
            finger.gameObject.SetActive(false);
            return;
            var psList = finger.GetComponentsInChildren<Renderer>();
            foreach (Renderer rn in psList)
            {
                rn.sortingLayerName = _guideType == EGuideType.Force ? "Guide" : "Default";
            }
        }
        
        
        protected bool _isDone = false;
        protected virtual void OnGuideChange(object obj)
        {
            var target = obj as GameObject;
            if (_interactionObject.Owner ==  target)
            {
                _isDone = true;
            }
        }

        private RectTransform GetTargetRoot(RectTransform target)
        {
            var current = target;
            while (current.parent!= null)
            {
                current = current.parent.GetComponent<RectTransform>();
            }

            return current;
        }
        
        private RectTransform guideRoot;   // 引导根（本地坐标的参考）
        protected Canvas guideCanvas;        // 引导 Canvas

        /// <summary>
        /// 将坐标点转换为屏幕点，再转换为局部坐标
        /// </summary>
        /// <param name="worldPoint">世界坐标点</param>
        /// <param name="targetRect">目标RectTransform（局部坐标的参考）</param>
        /// <returns>局部坐标点</returns>
        protected Vector2 WorldPointToLocalPoint(Vector3 worldPoint, RectTransform targetRect)
        {
            // 1. 世界坐标 -> 屏幕坐标
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPoint);
            LogTool.Log($"[GuideFingerWaitMethod] 世界坐标 {worldPoint} -> 屏幕坐标 {screenPoint}");
            
            // 2. 屏幕坐标 -> 局部坐标
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, screenPoint, guideCanvas.worldCamera, out localPoint);
            LogTool.Log($"[GuideFingerWaitMethod] 屏幕坐标 {screenPoint} -> 局部坐标 {localPoint} (目标: {targetRect.name})");
            
            return localPoint;
        }
        
        /// <summary>
        /// 将屏幕点转换为局部坐标
        /// </summary>
        /// <param name="screenPoint">屏幕坐标点</param>
        /// <param name="targetRect">目标RectTransform（局部坐标的参考）</param>
        /// <returns>局部坐标点</returns>
        protected Vector2 ScreenPointToLocalPoint(Vector2 screenPoint, RectTransform targetRect)
        {
            Vector2 localPoint;
            bool result = RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, screenPoint, guideCanvas.worldCamera, out localPoint);
            LogTool.Log($"[GuideFingerWaitMethod] 屏幕坐标 {screenPoint} -> 局部坐标 {localPoint} (目标: {targetRect.name}, 结果: {result})");
            
            return localPoint;
        }

        public virtual void HighlightTarget(HighLightMethod highLightMethod,Vector2 offest,GuideArrowType arrowType)
        {
            LogTool.Log($"[GuideFingerWaitMethod] HighlightTarget - Owner: {highLightMethod.Owner?.name}, Offset: {offest}, ArrowType: {arrowType}");
            
            highLightMethod.Arrow.gameObject.SetActive(arrowType != GuideArrowType.NoArrow);
            highLightMethod.MaskedRectTransform.gameObject.SetActive(true);
            if (highLightMethod.Owner == null)
            {
                LogTool.LogWarning("[GuideFingerWaitMethod] HighlightTarget - Owner is null, reset position and size");
                highLightMethod.MaskedRectTransform.anchoredPosition = new Vector2(-100000f,0f);
                highLightMethod.MaskedRectTransform.sizeDelta = Vector2.zero;
                
                return;
            }
            var rect = UITransformUtil.GetLocalRectIn(guideRoot, guideCanvas, highLightMethod.Owner.GetComponent<RectTransform>());
            LogTool.Log($"[GuideFingerWaitMethod] 计算的局部矩形 - Center: {rect.center}, Size: {rect.size}");
            
            // 设置位置和尺寸（局部坐标）
            var pos = rect.center + offest;
            LogTool.Log($"[GuideFingerWaitMethod] 最终位置 (含偏移) - {pos}");
            highLightMethod.MaskedRectTransform.anchoredPosition = pos;
            var size = new Vector2(this._stepListStruct.HighLightSize.X,
                this._stepListStruct.HighLightSize.Y);
            highLightMethod.MaskedRectTransform.sizeDelta = size;
            
            switch (arrowType)
            {
                case GuideArrowType.Down:
                    highLightMethod.Arrow.anchoredPosition = pos + new  Vector2(0, size.y/2);
                    highLightMethod.Arrow.rotation = Quaternion.Euler(new Vector3(0, 0, 270));
                    break;        
                case GuideArrowType.Up:
                    highLightMethod.Arrow.anchoredPosition = pos - new  Vector2(0, size.y/2);
                    highLightMethod.Arrow.rotation = Quaternion.Euler(new Vector3(0, 0, 90));
                    break;        
                case GuideArrowType.Right:
                    highLightMethod.Arrow.anchoredPosition = pos - new  Vector2(size.x/2,0);
                    highLightMethod.Arrow.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
                    break;        
                case GuideArrowType.Left:
                    highLightMethod.Arrow.anchoredPosition = pos + new  Vector2(size.x/2,0);
                    highLightMethod.Arrow.rotation = Quaternion.Euler(new Vector3(0, 0, 180));
                    break;        
            }
            // highLightMethod.Arrow.
            
        }
        
        public virtual void Tick()
        {
        }

        public virtual bool IsDone()
        {
            return _isDone;
        }

        public virtual void Exit()
        {
            // Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_GUIDE_INTERACTION+_guideType.ToString(),OnGuideChange);
            // this._context.Panel?.Finger.gameObject.SetActive(false);
        }
    }
}