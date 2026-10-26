using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 分层远景滚动：云、植被等 RawImage 仅滚动 UVRect 的 X；
/// 另支持若干节点按 Z 轴持续旋转（如风车、装饰轮）。
/// 同一 UV 层可挂多张图，共享 SpeedX。
/// 贴图需开启 Wrap Mode = Repeat，才能无缝循环。
/// </summary>
public class DistantView : MonoBehaviour
{
	[Serializable]
	public class LayerImage
	{
		public RawImage RawImage;

		[HideInInspector]
		public float BaseY;

		[HideInInspector]
		public float Width = 1f;

		[HideInInspector]
		public float Height = 1f;
	}

	[Serializable]
	public class Layer
	{
		[Tooltip("同层多张图共用速度，例如多片云")]
		public List<LayerImage> Images = new List<LayerImage>();

		[Tooltip("UV X 每秒滚动速度；正数向左滚视差，负数相反")]
		public float SpeedX = 0.02f;
	}

	[Serializable]
	public class RotateNode
	{
		[Tooltip("需要旋转的节点（RectTransform / Transform）")]
		public Transform Target;

		[Tooltip("绕 Z 轴角速度（度/秒）；正数为逆时针")]
		public float SpeedZ = 30f;

		[HideInInspector]
		public Quaternion BaseRotation = Quaternion.identity;
	}

	[SerializeField]
	List<Layer> mLayers = new List<Layer>();

	[SerializeField]
	List<RotateNode> mRotateNodes = new List<RotateNode>();

	[SerializeField]
	bool mPlayOnEnable = true;

	[SerializeField]
	bool mPauseWhenDisabled = true;

	bool mPlaying;

	public bool IsPlaying => mPlaying;

	public IReadOnlyList<Layer> Layers => mLayers;

	public IReadOnlyList<RotateNode> RotateNodes => mRotateNodes;

	void OnEnable()
	{
		CacheUvBaselines();
		CacheRotateBaselines();
		if (mPlayOnEnable)
		{
			Play();
		}
	}

	void OnDisable()
	{
		if (mPauseWhenDisabled)
		{
			Pause();
		}
	}

	void LateUpdate()
	{
		if (!mPlaying)
		{
			return;
		}

		float dt = Time.deltaTime;
		UpdateUvLayers(dt);
		UpdateRotateNodes(dt);
	}

	public void Play()
	{
		mPlaying = true;
	}

	public void Pause()
	{
		mPlaying = false;
	}

	public void StopAndReset()
	{
		mPlaying = false;
		ResetUvX();
		ResetRotations();
	}

	public void ResetUvX()
	{
		if (mLayers == null)
		{
			return;
		}

		for (int i = 0; i < mLayers.Count; i++)
		{
			Layer layer = mLayers[i];
			if (layer == null || layer.Images == null)
			{
				continue;
			}

			for (int j = 0; j < layer.Images.Count; j++)
			{
				LayerImage image = layer.Images[j];
				if (image == null || image.RawImage == null)
				{
					continue;
				}

				SetUv(image, 0f);
			}
		}
	}

	public void ResetRotations()
	{
		if (mRotateNodes == null)
		{
			return;
		}

		for (int i = 0; i < mRotateNodes.Count; i++)
		{
			RotateNode node = mRotateNodes[i];
			if (node == null || node.Target == null)
			{
				continue;
			}

			node.Target.localRotation = node.BaseRotation;
		}
	}

	void UpdateUvLayers(float dt)
	{
		if (mLayers == null)
		{
			return;
		}

		for (int i = 0; i < mLayers.Count; i++)
		{
			Layer layer = mLayers[i];
			if (layer == null || layer.Images == null || layer.SpeedX == 0f)
			{
				continue;
			}

			float deltaX = layer.SpeedX/100f * dt;
			for (int j = 0; j < layer.Images.Count; j++)
			{
				ApplyUvDeltaX(layer.Images[j], deltaX);
			}
		}
	}

	void UpdateRotateNodes(float dt)
	{
		if (mRotateNodes == null)
		{
			return;
		}

		for (int i = 0; i < mRotateNodes.Count; i++)
		{
			RotateNode node = mRotateNodes[i];
			if (node == null || node.Target == null || node.SpeedZ == 0f)
			{
				continue;
			}

			node.Target.Rotate(0f, 0f, node.SpeedZ * dt, Space.Self);
		}
	}

	void CacheUvBaselines()
	{
		if (mLayers == null)
		{
			return;
		}

		for (int i = 0; i < mLayers.Count; i++)
		{
			Layer layer = mLayers[i];
			if (layer == null || layer.Images == null)
			{
				continue;
			}

			for (int j = 0; j < layer.Images.Count; j++)
			{
				LayerImage image = layer.Images[j];
				if (image == null || image.RawImage == null)
				{
					continue;
				}

				Rect uv = image.RawImage.uvRect;
				image.BaseY = uv.y;
				image.Width = uv.width > 0f ? uv.width : 1f;
				image.Height = uv.height > 0f ? uv.height : 1f;
			}
		}
	}

	void CacheRotateBaselines()
	{
		if (mRotateNodes == null)
		{
			return;
		}

		for (int i = 0; i < mRotateNodes.Count; i++)
		{
			RotateNode node = mRotateNodes[i];
			if (node == null || node.Target == null)
			{
				continue;
			}

			node.BaseRotation = node.Target.localRotation;
		}
	}

	static void ApplyUvDeltaX(LayerImage image, float deltaX)
	{
		if (image == null || image.RawImage == null)
		{
			return;
		}

		Rect uv = image.RawImage.uvRect;
		SetUv(image, Mathf.Repeat(uv.x + deltaX, 1f));
	}

	static void SetUv(LayerImage image, float x)
	{
		image.RawImage.uvRect = new Rect(x, image.BaseY, image.Width, image.Height);
	}
}
