using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using FusionContinent.Skill;
using UnityEngine;
using QFramework;

namespace FusionContinent
{
    public partial class MagicianAttack : MonoBehaviour
    {
        private float attackDuration = 2f; // 攻击持续时间
        [SerializeField] private float detectionRadius = 10f; // 检测敌人的范围

        private Tween pTween;

        private MonsterCard currentTarget;
        private float currentAttackTime;
        private bool isAttacking;

        private int damage;
        private int baseAtk;

        private HashSet<MonsterCard> hitTargets = new HashSet<MonsterCard>();

        private Vector3 splitDirection;

        private SoldierCard parentCard;
        private ISkill _skill;
        private GameObject splitProjectile;
        private GameObject raiseHand;

        private void Awake()
        {

        }


        private void Update()
        {
            if (parentCard.IsAtkLock())
            {
                StopAttack();
                return;
            }

            if (isAttacking)
            {
                currentAttackTime += Time.deltaTime;
                if (currentAttackTime >= attackDuration)
                {
                    // LogTool.Log(">>>>>  update - currentAttackTime: "+currentAttackTime + ", attackDuration: "+ attackDuration);
                    StopAttack();
                    return;
                }

                if (currentTarget == null)
                {
                    FindNextTarget();
                }

                UpdateLaser();
            }
        }

        public void StartAttack(Vector3 direction, SoldierCard parent, int atkDamage, ISkill skill)
        {
            parentCard = parent;
            splitDirection = direction;
            _skill = skill;

            baseAtk = atkDamage;
            // damage = Mathf.CeilToInt(atkDamage + baseAtk * parent.DamageModifiers + baseAtk * Global.GemDamage); // 应用伤害修正

            attackDuration = parent.attackDuration;

            if (!isAttacking)
            {
                isAttacking = true;
                currentAttackTime = 0f;
                FindNextTarget();
            }
            // Vector3 targetPosition = transform.position + direction * 100f; // 100f是一个远距离，确保超出屏幕范围
            // transform.DOMove(targetPosition, 800).SetEase(Ease.Linear).SetSpeedBased();
        }

        private void StopAttack()
        {
            isAttacking = false;
            currentTarget = null;
            Destroy(gameObject); // 如果超出屏幕范围，销毁子弹
            GameObject.Destroy(splitProjectile); // 销毁对象
            GameObject.Destroy(raiseHand); // 销毁抬手对象

            hitTargets.ForEach(monster =>
            {
                monster.SetMoveSpeed(parentCard.MonsterMoveSpeed);// 回复减速
            });
            hitTargets = null;
        }

        private void FindNextTarget()
        {
            currentTarget = parentCard.currentTarget;

            // Collider[] colliders = Physics.OverlapSphere(splitProjectile.transform.position, detectionRadius);
            // float nearestDistance = float.MaxValue;
            //
            // foreach (Collider col in colliders)
            // {
            //     LogTool.Log(">> collision.name: " + col.gameObject.name);
            //     if (col.TryGetComponent<MonsterCard>(out MonsterCard enemy) )
            //     {
            //         LogTool.Log(">> OverlapSphere - enemy: ");
            //         float distance = Vector3.Distance(transform.position, col.transform.position);
            //     }
            // }
        }

        private void UpdateLaser()
        {
            if (raiseHand == null)
            {
                // 抬手
                raiseHand = GamePlayManager.Default._resLoader
                    .Get<GameObject>("fashi_weapen01").Instantiate();
                raiseHand.transform.position = new Vector3(parentCard.transform.position.x, parentCard.transform.position.y + 1, 0);
                raiseHand.transform.SetParent(GameLand.Default.Bullet);
                raiseHand.LocalScale(Vector3.one);
            }

            // 计算激光距离&角度
            DOTween.Sequence().AppendCallback(() =>
            {
                if (currentTarget != null)
                {
                    Vector3 direction = (currentTarget.transform.position - parentCard.transform.position).normalized;
                    Vector3 finalDirection = Quaternion.Euler(0, 0, 0f) * direction;
                    // 计算子弹的旋转角度，使其朝向目标方向
                    float angle = Mathf.Atan2(finalDirection.y, finalDirection.x) * Mathf.Rad2Deg;
                    float angleY = angle - 90f;
                    splitProjectile.transform.rotation = Quaternion.Euler(0, 0, angleY);

                    // 设置Y轴（垂直）的缩放因子
                    float pointAbs = Mathf.Abs(parentCard.transform.position.y - currentTarget.transform.position.y);
                    float angleAbs = Mathf.Abs(angleY - pointAbs);

                    Vector3 scale = parentCard.transform.localScale;
                    scale.x = 1;
                    // scale.y = pointAbs * 4.5f;
                    scale.y = pointAbs * 5f;

                    scale.z = 1;

                    // 旋转角修正
                    if (scale.y <= 16 && angleY > 40)
                    {
                        scale.y += 5f;
                    }
                    if (scale.y <= 16 && angleY < -40)
                    {
                        scale.y += 5f;
                    }

                    // LogTool.Log(">>>> angleY: "+angleY + ", angleAbs: "+angleAbs);
                    // splitProjectile.transform.localScale = scale;
                    pTween = transform.DOScale(scale, 1);
                }
            }).AppendInterval(0.1f); // 每次间隔
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.name != "HitBox")
            {
                return;
            }

            MonsterCard hitTarget = other.transform.parent.GetComponent<MonsterCard>();
            if (hitTarget != null && hitTarget == currentTarget&& hitTarget.enabled != false)
            {
                if (pTween != null)
                {
                    // LogTool.Log(">>>>> 激光中敌人 - 停止");
                    pTween.Kill(); // 停止移动
                }

                DOTween.Sequence().AppendCallback(() =>
                {
                    if (hitTarget != null )
                    {
                        try
                        {
                            // hitTarget.TakeDamage(damage, baseAtk);// 命中造成伤害
                        }
                        catch
                        {

                        }

                        ShowBombSkill();// 爆炸效果
                        if (parentCard.MonsterMoveSpeed != 0)
                        {
                            hitTarget.SetMoveSpeed(-parentCard.MonsterMoveSpeed);// 减速效果
                            hitTargets.Add(hitTarget);// 记录被减速的怪物
                        }
                    }
                }).AppendInterval(0.1f); // 每次伤害间隔

            }

        }


        private void OnTriggerEnter2D(Collider2D collision)
        {

        }

        private void ShowBombSkill()
        {
            // if (parentCard.CheckSpecialCardExistByType(SpecialEffectType.Baozha)) return;
            //
            // GameObject bombProjectile = GamePlayManager.Default._resLoader
            //     .LoadSync<GameObject>("MagicianBombSkill").Instantiate();
            // bombProjectile.transform.position = new Vector3(currentTarget.transform.position.x, currentTarget.transform.position.y + 1, 0);
            // bombProjectile.transform.SetParent(GameLand.Default.Bullet);
            // bombProjectile.LocalScale(1);
            // DOTween.Sequence().AppendInterval(0.5f) // 延迟 0.1 秒
            // .AppendCallback(() =>
            // {
            //     if (bombProjectile != null)
            //     {
            //         GameObject.Destroy(bombProjectile); // 销毁对象
            //     }
            // });
            //
            // var magicianBombScript = bombProjectile.transform.Find("eff_fs_sj01").transform.Find("box").GetComponent<MagicianBombAttack>();
            // magicianBombScript.StartAttack(damage, baseAtk, _skill);
        }

        private void OnDestroy()
        {
            // 确保子弹销毁时，停止所有与其相关的 DOTween 动画
            DOTween.Kill(transform, true);
        }

    }
}
