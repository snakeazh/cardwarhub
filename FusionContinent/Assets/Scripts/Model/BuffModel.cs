using System;
using UnityEngine;

namespace FusionContinent.Model
{
    /**
     * BUFF卡类
     */
    public class BuffCard
    {
        private int _buffId;
        private float _buffEffect;
        private System.DateTime _expireTime;

        public void SetBuffEffect(int id, float effect, int second)
        {
            _buffId = id;
            _buffEffect += effect;

            // 计算过期时间
            _expireTime = DateTime.Now.AddSeconds(second);// 计算10秒之后的时间
            
            LogTool.Log(">>>> SetBuffEffect - _buffId: " + _buffId + ", _buffEffect: " + _buffEffect + ", _expireTime: " + _expireTime);
        }

        public float GetBuffEffect()
        {
            if (!CheckExpireTime())
            {
                return 0;// 已过期
            }

            return _buffEffect;
        }

        public bool CheckExpireTime()
        {
            DateTime currentTime = DateTime.Now;
            if (currentTime > _expireTime)
            {
                return false;// 已过期
            }

            return true;
        }
    }
}