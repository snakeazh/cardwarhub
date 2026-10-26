using FusionContinent.Model;
using UnityEngine;

namespace FusionContinent.Manager
{
    public class StaminaManager : MonoBehaviour
    {
        private int _maxStamina;
        private float _recoverTime;
        private float _recoverTimeLimit;
            
        private void Awake()
        {
            
        }

        public void Init()
        {
            _maxStamina = Global.PlayerResource.GetMaxStamina();
            _recoverTimeLimit = DataConfigManager.Tables.TbPlayerConst.StaminaRecoveryTime;
        }

        private void Update()
        {
            _recoverTime += Time.deltaTime;
            if (_recoverTime >= _recoverTimeLimit && Global.PlayerResource.Stamina < _maxStamina)
            {
                _recoverTime = 0f;
                Global.PlayerResource.Stamina++;
                if (Global.PlayerResource.Stamina > _maxStamina)
                {
                    Global.PlayerResource.Stamina = _maxStamina;
                }
                
                Global.Publisher.Publish(MessagePublisher.CHANGE_STAMINA, Global.PlayerResource.Stamina);
                // LogTool.Log($">>>>>>> 体力恢复: {Global.Stamina.Value}, 每{_recoverTimeLimit}/秒恢复一点");
            }
        }
        
    }
}