using System.Collections;
using System.Collections.Generic;
using Config;
using UnityEngine;

namespace FusionContinent
{
    public class BuildingHitBox : MonoBehaviour
    {
        private BuildingCard buildingCard;
        
        public void Initialize(BuildingCard buildingCard)
        {
            this.buildingCard = buildingCard;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (GameLand.Default.CardSlot == null) return;
            if (!buildingCard.isDrag || other.name != "CardPos" ||
                other.transform.parent.name != GameLand.Default.CardSlot.name) return;
            var slotIndex = other.transform.GetSiblingIndex();
                
            buildingCard.targetPos = other.transform.position;
            buildingCard.targetIndex = slotIndex;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            
        }

        private void UpdateClosestFusionTarget()
        {
            
        }
    }
}