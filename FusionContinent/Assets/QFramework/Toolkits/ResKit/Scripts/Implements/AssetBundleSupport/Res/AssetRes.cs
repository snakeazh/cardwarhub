/****************************************************************************
 * Copyright (c) 2017 snowcold
 * Copyright (c) 2017 liangxie
 * 
 * http://qframework.io
 * https://github.com/liangxiegame/QFramework
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 ****************************************************************************/


using System;

namespace QFramework
{
	using UnityEngine;
	using System.Collections;

	public class AssetRes : Res
	{
		protected string[]           mAssetBundleArray;
		protected AssetBundleRequest mAssetBundleRequest;
		private IAssetHandle mProviderHandle;
		protected string                 mOwnerBundleName;

		public override string OwnerBundleName
		{
			get { return mOwnerBundleName; }
			set { mOwnerBundleName = value; }
		}
		
		public static AssetRes Allocate(string name, string onwerBundleName, Type assetTypde)
		{
			var res = SafeObjectPool<AssetRes>.Instance.Allocate();
			if (res != null)
			{
				res.AssetName = name;
				res.mOwnerBundleName = onwerBundleName;
				res.AssetType = assetTypde;
				res.InitAssetBundleName();
			}

			return res;
		}

		public string AssetBundleName
		{
			get { return mAssetBundleArray == null ? null : mAssetBundleArray[0]; }
		}

		public AssetRes(string assetName) : base(assetName)
		{

		}

		public AssetRes()
		{

		}

		public override bool LoadSync()
		{
			if (!CheckLoadAble())
			{
				return false;
			}

			State = ResState.Loading;

			HoldDependRes();

			Object obj = null;
			var provider = ResKit.Get.Container.Get<IAssetProvider>();
			if (provider == null)
			{
				UnHoldDependRes();
				OnResLoadFaild();
				return false;
			}
			mProviderHandle = provider.LoadAssetSync(mAssetName, AssetType);
			obj = mProviderHandle.AssetObject;

			UnHoldDependRes();

			if (obj == null)
			{
				OnResLoadFaild();
				return false;
			}

			mAsset = obj;
			State = ResState.Ready;
			return true;
		}

		public override void LoadAsync()
		{
			if (!CheckLoadAble())
			{
				return;
			}

			State = ResState.Loading;

			ResMgr.Instance.PushIEnumeratorTask(this);
		}

		public override IEnumerator DoLoadAsync(System.Action finishCallback)
		{
			if (RefCount <= 0)
			{
				OnResLoadFaild();
				finishCallback();
				yield break;
			}

			State = ResState.Loading;
			HoldDependRes();

			var provider = ResKit.Get.Container.Get<IAssetProvider>();
			if (provider == null)
			{
				UnHoldDependRes();
				OnResLoadFaild();
				finishCallback();
				yield break;
			}
			mProviderHandle = provider.LoadAssetAsync(mAssetName, AssetType);
			yield return mProviderHandle.Wait();

			UnHoldDependRes();

			if (mProviderHandle.AssetObject == null)
			{
				OnResLoadFaild();
				finishCallback();
				yield break;
			}

			mAsset = mProviderHandle.AssetObject;
			State = ResState.Ready;
			finishCallback();
		}

		public override string[] GetDependResList()
		{
			return mAssetBundleArray;
		}

		public override void OnRecycled()
		{
			mAssetBundleArray = null;
			mProviderHandle = null;
		}

		public override void Recycle2Cache()
		{
			SafeObjectPool<AssetRes>.Instance.Recycle(this);
		}

		protected override float CalculateProgress()
		{
			if (mProviderHandle == null)
			{
				return 0;
			}
			return mProviderHandle.Progress;
		}

		protected override void OnReleaseRes()
		{
			if (mProviderHandle != null)
			{
				mProviderHandle.Release();
				mProviderHandle = null;
			}
			mAsset = null;
		}

		protected void InitAssetBundleName()
		{
			mAssetBundleArray = null;

			var resSearchKeys = ResSearchKeys.Allocate(mAssetName,mOwnerBundleName,AssetType);

			var config =  AssetBundleSettings.AssetBundleConfigFile.GetAssetData(resSearchKeys);

			resSearchKeys.Recycle2Cache();

			if (config == null)
			{
				Debug.LogError("Not Find AssetData For Asset:" + mAssetName);
				return;
			}

			var assetBundleName = config.OwnerBundleName;

			if (string.IsNullOrEmpty(assetBundleName))
			{
				Debug.LogError("Not Find AssetBundle In Config:" + config.AssetBundleIndex + mOwnerBundleName);
				return;
			}

			mAssetBundleArray = new string[1];
			mAssetBundleArray[0] = assetBundleName;
		}

		public override string ToString()
		{
			return string.Format("Type:Asset\t {0}\t FromAssetBundle:{1}", base.ToString(), AssetBundleName);
		}
	}
}
