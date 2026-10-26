using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using UnityEngine;
using UnityEngine.Serialization;

namespace FusionContinent
{
    public class SoldierHitBox : MonoBehaviour
    {
        // 当前融合的目标
        public SoldierCard currentFCard;
        private SoldierCard _soldierCard;

        // 融合目标的候选列表
        private List<SoldierCard> potentialFusionTargets = new List<SoldierCard>();

        public void Initialize(SoldierCard soldierCard)
        {
            this._soldierCard = soldierCard;
        }
        private void OnTriggerStay2D(Collider2D other)
        {
            // 士兵卡融合检测
            if (other.name == "HitBox" && _soldierCard.isDrag)
            {
                var potentialFusionCard = other.transform.parent.GetComponent<SoldierCard>();

                // 检查潜在融合目标是否有效且不是自身
                if (potentialFusionCard != null && potentialFusionCard != _soldierCard &&
                    !potentialFusionTargets.Contains(potentialFusionCard))
                {
                    potentialFusionTargets.Add(potentialFusionCard);
                }
                _soldierCard.SetFusionTarget(currentFCard);
                UpdateClosestFusionTarget();
            }
            
            // 检查离开的目标是否是出售卡
            if (other.name == "CardSellGold")
            {
                _soldierCard.isCell = true;
                // LogTool.Log("拖动到出售 OnTriggerStay2D - CardSellGold: ");
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            // 检查离开的目标是否是潜在的融合目标
            if (other.name == "HitBox")
            {
                Destroy(transform.parent.GetComponent<SoldierCard>().effectxuanwo);
                SoldierCard exitingCard = other.transform.parent.GetComponent<SoldierCard>();
                if (exitingCard != null && potentialFusionTargets.Contains(exitingCard))
                {
                    // 将目标从候选列表中移除
                    potentialFusionTargets.Remove(exitingCard);
                    currentFCard = null;
                    _soldierCard.SetFusionTarget(currentFCard);
                    // 将该目标的Frame设为false
                    exitingCard.Frame.gameObject.SetActive(false);
                }
            }
            
            // 检查离开的目标是否是出售卡
            if (other.name == "CardSellGold")
            {
                _soldierCard.isCell = false;
                // LogTool.Log("拖动到出售 OnTriggerExit2D - CardSellGold: ");
            }
        }

        private void UpdateClosestFusionTarget()
        {
            SoldierCard closestCard = null;
            float minDistance = float.MaxValue;

            foreach (SoldierCard target in potentialFusionTargets)
            {
                float distance = Vector2.Distance(transform.position, target.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestCard = target;
                }
            }

            // 更新最近融合目标
            if (closestCard != currentFCard)
            {
                // 如果之前有目标，将其Frame设为false
                if (currentFCard != null)
                {
                    currentFCard.Frame.gameObject.SetActive(false);
                }

                // 设置新的最近融合目标
                currentFCard = closestCard;

                // 如果有新的最近目标，将其Frame设为true
                if (currentFCard != null)
                {
                    _soldierCard.SetFusionTarget(currentFCard);
                    currentFCard.Frame.gameObject.SetActive(true);
                }
            }
        }
    }
}