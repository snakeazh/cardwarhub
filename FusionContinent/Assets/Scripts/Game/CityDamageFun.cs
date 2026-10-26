using System;
using DG.Tweening;
using UnityEngine;
using QFramework;

namespace FusionContinent
{
	public partial class CityDamageFun : ViewController
	{
		private Tweener _moveTween;

		void Awake()
		{
			DamageText.richText = false;// 关闭 Rich Text（伤害跳字必做，避免Underline警告）
		}
		
		void OnEnable()
		{
			DamageText.richText = false;
		}

		private void OnDestroy()
		{
			KillMoveTween();
		}

		private void KillMoveTween()
		{
			if (_moveTween != null && _moveTween.IsActive())
			{
				_moveTween.Kill();
				_moveTween = null;
			}
		}

		private void PlayMoveTween()
		{
			KillMoveTween();

			_moveTween = transform.DOMoveY(transform.position.y + 0.5f, 0.5f)
				.SetEase(Ease.OutCubic)
				.SetTarget(gameObject)
				.SetId(GlobalConst.BattleTween)
				.OnComplete(() =>
				{
					_moveTween = null;
					if (this != null)
					{
						Destroy(gameObject);
					}
				});
		}

		public void SetData(int damage)
		{
			DamageText.fontSize = 80;
			if (damage > 0)
			{
				DamageText.text = $"+{damage}";
				DamageText.color = Color.green;
				// transform.LocalScale(Vector3.one);
			}
			else
			{
				DamageText.text = $"{damage}";
				DamageText.color = Color.red;
			}

			// 向上飘动并在结束后销毁
			PlayMoveTween();
		}
		
		public void SetData(string strVal, Color color = default)
		{
			DamageText.text = strVal;
			DamageText.fontSize = 80;
			if (color == default)
			{
				DamageText.fontSize = 35;
				DamageText.color = Color.yellow;
			}
			else
			{
				DamageText.color = color;
			}

			// 向上飘动并在结束后销毁
			PlayMoveTween();
		}
	}
}
