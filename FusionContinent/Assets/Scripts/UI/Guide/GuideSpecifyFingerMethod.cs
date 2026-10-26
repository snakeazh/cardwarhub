using Config;
using FusionContinent.Model;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent.Guide
{
    /// <summary>
    /// 战斗内需要特殊处理的卡牌引导
    /// </summary>
    public class GuideSpecifyFingerMethod : IGuideMethod
    {
        private bool clicked = false;
        private Canvas _canvas;
        GraphicRaycaster m_raycaster;
        GuideButtonCompoent m_button;
        private GuideContext _selfContext;

        protected GuideContext Context
        {
            get
            {
                return _selfContext;
            }
            set
            {
                _selfContext = value;
            }
        }
        
        
        
        protected GuidesStepListStruct _stepListStruct;
        private int _cardId;
        private const string constURl = "GameLand/Canvas/FriendlySlot/SoldierCard(Clone)";
        public GuideSpecifyFingerMethod(int cardId)
        {
            _cardId = cardId;
        }
        public void Init(GuideContext ctx, GuidesStepListStruct step)
        {
            Context = ctx;
            _stepListStruct = step;
        }

        protected EGuideType _guideType;
        protected GameObject _interactionObject;
        
        public virtual void Enter()
        {
            string guideToUIPath = _stepListStruct.GuideUI + "/card_" + _cardId;
            if (_cardId == 400011)
            {
                guideToUIPath = constURl;
            }
            // LogTool.E(guideToUIPath);
            _interactionObject  = GameObject.Find(guideToUIPath);
            _guideType = _stepListStruct.TipType == GuideTipType.Finger ? EGuideType.Weak : EGuideType.Force;
            
            guideRoot = GetTargetRoot(this.Context.Panel.Finger);
            guideCanvas = guideRoot.GetComponent<Canvas>();
            highlight = this.Context.Panel.Finger;
   
            HighlightTarget(_interactionObject.GetComponent<RectTransform>(),new Vector2(_stepListStruct.Position.X,_stepListStruct.Position.Y));
            
            
            // this._ctx.Panel.Finger.anchoredPosition= obj.GetComponent<RectTransform>().anchoredPosition;
            this.Context.Panel.Finger.gameObject.SetActive(true);
            // this._ctx.Panel.GetComponentInParent()
            SetFingerDepth(this.Context.Panel.Finger);
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_GUIDE_PROGRESS_SPE,OnGuideChange);
            
            
            this.Context.Panel.Mask.gameObject.SetActive(true);
            
            InitDialog();
        }
        void InitDialog()
        {
            this.Context.Panel.Dialoge.gameObject.SetActive(true);
            this.Context.Panel.Text.text = _stepListStruct.TipText;
        }

        void SetFingerDepth(RectTransform finger)
        {
            var psList = finger.GetComponentsInChildren<Renderer>();
            foreach (Renderer rn in psList)
            {
                rn.sortingLayerName = _guideType == EGuideType.Force ? "Guide" : "Default";
            }
        }
        
        
        protected bool _isDone = false;
        protected virtual void OnGuideChange(object obj)
        {
            if (_cardId ==  (int)obj)
            {
                _isDone = true;
                
            }
        }
        
        void Clean()
        {
            if (_interactionObject)
            {
                foreach (var canvas in _interactionObject.GetComponentsInChildren<Canvas>())
                {
                    canvas.sortingLayerName = "Default";
                }
                
                Object.Destroy(m_raycaster);
                m_raycaster = null;
                Object.Destroy(_canvas);
                _canvas = null;
                
                _interactionObject = null;
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
        private Canvas guideCanvas;        // 引导 Canvas
        private RectTransform highlight;   // 遮罩或高亮框

        public void HighlightTarget(RectTransform target,Vector2 offest)
        {
            var rect = UITransformUtil.GetLocalRectIn(guideRoot, guideCanvas, target);
            // 设置位置和尺寸（局部坐标）
            highlight.anchoredPosition = rect.center + offest;
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
            // if ()
            {
                
            }
            // throw new System.NotImplementedException();
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_GUIDE_PROGRESS_SPE,OnGuideChange);
            Clean();
            
            Object.Destroy(m_button);
            this.Context = null;
        }
    }
}