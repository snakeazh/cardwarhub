using Config;
using DG.Tweening;
using QFramework;
using UnityEngine;

namespace FusionContinent.Skill
{
    /**
     * 圆舞斩
     */
    public class CircularDanceSkill : SkillBase
    {
        [SerializeField] private float scale = 150f;// 物体缩放
        
        protected override void OnCast(object obj)
        {
            // 物体大小
            scale += Attributes.GetFinal(AttributeType.SkillRange);
            
            var direction = (Vector3)obj;
            OnCastCircularDance01(direction);
            
            // 刀刃
            int continuousAtkNum = (int)Attributes.GetFinal(AttributeType.SkillContinuousAtkNum) + 1;
            float continuousAtkSpeed = Attributes.GetFinal(AttributeType.SkillContinuousAtkSpeed);
            for (int i = 0; i < continuousAtkNum; i++)
            {
                // 每轮延迟 interval * j，然后同时生成这一轮的所有刀刃
                DOVirtual.DelayedCall(continuousAtkSpeed * i, () =>
                {
                    OnCastCircularDance02(direction);
                });
            }
        }

        private void OnCastCircularDance01(Vector3 direction)
        {
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate($"{Config.ProjectileName}01");
            if (pool == null) return;

            GameObject circularDance = pool.Get();
            if (circularDance == null) return;

            // 子弹设置
            Vector3 dancePos = Parent.transform.position;
            circularDance.transform.SetParent(GameLand.Default.Bullet);
            circularDance.transform.position = new Vector3(dancePos.x, dancePos.y + 3f, dancePos.z);
            circularDance.transform.localScale = Vector3.one * scale;
            circularDance.GetComponent<DrawSlashBullet>().SetStartAttack(this, 0.5f);

            // 让子弹朝向正确方向（与传入的 direction 一致）
            circularDance.transform.up = direction;

            DOTween.To(v => { }, 0, 0, 1.5f).onComplete += () =>
            {
                pool.Return(circularDance);
            };
        }
        
        private void OnCastCircularDance02(Vector3 direction)
        {
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate($"{Config.ProjectileName}02");
            if (pool == null) return;

            GameObject circularDance = pool.Get();
            if (circularDance == null) return;

            // 子弹设置
            circularDance.transform.SetParent(GameLand.Default.Bullet);
            circularDance.transform.position = Parent.transform.position;
            circularDance.transform.localScale = Vector3.one * scale;
            circularDance.GetComponent<DrawSlashBullet>().SetStartAttack(this);
            
            // 计算目标位置并开始移动
            Vector3 targetPosition = circularDance.transform.position + direction * 100f;
            circularDance.transform.DOMove(targetPosition, GetLaunchSpeed()).SetEase(Ease.Linear).SetSpeedBased();

            // 让子弹朝向正确方向（与传入的 direction 一致）
            circularDance.transform.up = direction;

            DOTween.To(v => { }, 0, 0, 1f).onComplete += () =>
            {
                pool.Return(circularDance);
            };
        }


    }
}