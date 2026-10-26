using System.Collections.Generic;
using Config;
using DG.Tweening;
using QFramework;
using UnityEngine;

namespace FusionContinent.Skill
{
    /**
     * 瞬袭斩
     */
    public class SwiftAssaultSkill : SkillBase
    {
        private float _scale = 100f;// 物体缩放
        private Sequence _atkTween;
        
        protected override void OnCast(object obj)
        {
            // 物体大小
            _scale += Attributes.GetFinal(AttributeType.SkillRange);
            
            // 拖动到的释放位置
            var directionPos = (Vector3)obj;
            
            if (_atkTween != null) _atkTween.Kill();
            _atkTween = DOTween.Sequence();
            
            var assaultOffset = DataConfigManager.Tables.TbGameBattleConst.SkillSwiftAssaultOffset;

            // 技能连续伤害次数
            var continuousDamageNum = Mathf.Max(1, Attributes.GetFinal(AttributeType.SkillContinuousDamageNum));
            for (int i = 0; i < continuousDamageNum; i++)
            {
                var val = assaultOffset[i % 5];
                _atkTween.AppendCallback(() => OnCastSkillSwiftAssault(
                    new Vector2(directionPos.x + val.X, directionPos.y + val.Y),
                    val.Angle,
                    new Vector3(val.Reverse * _scale, _scale, _scale)
                )).AppendInterval(0.1f);
            }
           
            _atkTween.OnComplete(() =>
            {
                _atkTween = null;
            });
        }

        private void OnCastSkillSwiftAssault(Vector3 directionPos, float angle, Vector3 localScale)
        {
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(Config.ProjectileName);
            if (pool == null) return;

            GameObject bullet = pool.Get();
            if (bullet == null) return;

            // 子弹设置
            bullet.transform.SetParent(GameLand.Default.Bullet);
            bullet.transform.position = directionPos;
            bullet.transform.localScale = localScale;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
            DOTween.To(v => { }, 0, 0, 0.7f).onComplete += () =>
            {
                pool.Return(bullet);
            };

            var bulletBox = bullet.transform.Find("ani_xl_jn9_02/Box2");
            bulletBox.GetComponent<DrawSlashBullet>().SetStartAttack(this);
        }

    }

}