using System;
using System.Collections.Generic;
using Config;
using UnityEngine;
using QFramework;

namespace FusionContinent
{
	public class AttackRangeDetector : MonoBehaviour
	{
		private List<CardType> targetTypes;

		// public void Initialize(GameCard gameCard, float range,List<CardType> targetTypes)
		// {
		// 	this.gameCard = gameCard;
		// 	this.targetTypes = targetTypes;
		//
		// 	// 将子物体的 Collider2D 设置为攻击范围
		// 	CircleCollider2D collider = GetComponent<CircleCollider2D>();
		// 	if (collider != null)
		// 	{
		// 		collider.radius = range;
		// 	}
		// }
		//
		// private void OnEnable()
		// {
		// 	GameCard.onDestroyed += OnTargetDestroyed;
		// }
		//
		// private void OnDisable()
		// {
		// 	GameCard.onDestroyed -= OnTargetDestroyed;
		// }
		//
		// private void OnTargetDestroyed(GameCard destroyedCard)
		// {
		// 	// 检查是否是当前锁定的目标
		// 	if (currentTarget == destroyedCard)
		// 	{
		// 		currentTarget = null;
		// 	}
		// }
		//
		// private void OnTriggerEnter2D(Collider2D other)
		// {
		// 	// if (other.name != "HitBox")
		// 	// {
		// 	// 	return;
		// 	// }
		// 	// GameCard potentialTarget = other.transform.parent.GetComponent<GameCard>();
		// 	// if (potentialTarget != null && targetTypes!= null && targetTypes.Contains(potentialTarget.selfType) && potentialTarget.hp > 0)
		// 	// {
		// 	// 	gameCard.SetClosestTarget(potentialTarget);
		// 	// 	if (gameCard.selfType == CardType.Monster)
		// 	// 	{
		// 	// 		Vector2 contactPoint = other.ClosestPoint(gameCard.transform.position); // 获取碰撞的接触点
		// 	// 		gameCard.enemyAtkPos = contactPoint; // 将碰撞的接触点赋值给 enemyAtkPos
		// 	// 	}
		// 	// }
		// }
		//
		// private void OnTriggerStay2D(Collider2D other)
		// {
		// 	if (currentTarget != null)
		// 	{
		// 		return;
		// 	}
		//
		// 	if (gameCard.isDrag)
		// 	{
		// 		return;
		// 	}
		// 	
		// 	if (other.name != "HitBox")
		// 	{
		// 		return;
		// 	}
		//
		// 	GameCard potentialTarget = other.transform.parent.GetComponent<GameCard>();
		// 	
		// 	// 检查目标是否符合条件（是可攻击的类型，且生命值大于0）
		// 	if (potentialTarget != null && targetTypes != null && targetTypes.Contains(potentialTarget.selfType) && potentialTarget.hp > 0)
		// 	{
		// 		// 如果还没有锁定目标，或者当前目标比之前找到的目标更近，则更新当前目标
		// 		if (currentTarget == null)
		// 		{
		// 			currentTarget = potentialTarget;
		// 		}
		// 		else
		// 		{
		// 			// 比较当前目标和潜在目标之间的距离，选择更近的
		// 			float currentDistance = Vector2.Distance(transform.position, currentTarget.transform.position);
		// 			float potentialDistance = Vector2.Distance(transform.position, potentialTarget.transform.position);
		//
		// 			if (potentialDistance < currentDistance)
		// 			{
		// 				currentTarget = potentialTarget;
		// 			}
		// 		}
		// 	}
		//
		// 	// 锁定最近的目标
		// 	if (currentTarget != null)
		// 	{
		// 		gameCard.SetClosestTarget(currentTarget);
		// 		if (gameCard.selfType == CardType.Monster)
		// 		{
		// 			Vector2 contactPoint = other.ClosestPoint(gameCard.transform.position); // 获取碰撞的接触点
		// 			gameCard.enemyAtkPos = contactPoint; // 将碰撞的接触点赋值给 enemyAtkPos
		// 		}
		// 	}
		// }
		//
		// private void OnTriggerExit2D(Collider2D other)
		// {
		// 	// 检查离开的碰撞体是否是当前锁定的目标
		// 	if (other.name == "HitBox" && currentTarget != null && other.transform.parent.GetComponent<GameCard>() == currentTarget)
		// 	{
		// 		// 解除锁定
		// 		currentTarget = null;
		// 		gameCard.SetClosestTarget(currentTarget);
		// 	}
		// }
	}

}
