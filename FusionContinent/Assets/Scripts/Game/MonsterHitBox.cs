using System.Collections;
using System.Collections.Generic;
using Config;
using UnityEngine;

namespace FusionContinent
{
    public class MonsterHitBox : MonoBehaviour
    {
        private MonsterCard monsterCard;
        
        
        public void Initialize(MonsterCard monsterCard)
        {
            this.monsterCard = monsterCard;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            
        }

        private void UpdateClosestFusionTarget()
        {
            
        }
    }
}