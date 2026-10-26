using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:54afa1c0-d2b5-408a-9aee-0769fb250887
	public partial class eqipshopitem
	{
		public const string Name = "eqipshopitem";
		
		[SerializeField]
		public UnityEngine.UI.Image shopitembg;
		[SerializeField]
		public TMPro.TextMeshProUGUI buylimitnum;
		[SerializeField]
		public TMPro.TextMeshProUGUI shopnametxt;
		[SerializeField]
		public UnityEngine.UI.Image shopicon;
		[SerializeField]
		public UnityEngine.UI.Button consumebtn;
		[SerializeField]
		public UnityEngine.UI.Image consumeicon;
		[SerializeField]
		public TMPro.TextMeshProUGUI consumenum;
		[SerializeField]
		public UnityEngine.UI.Image shopitembgmask;
		[SerializeField]
		public UnityEngine.UI.Button shopiconbtn;
		[SerializeField]
		public UnityEngine.UI.Toggle Toggle;
		[SerializeField]
		public Transform effect1;
		[SerializeField]
		public Transform effect2;
		
		private eqipshopitemData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			shopitembg = null;
			buylimitnum = null;
			shopnametxt = null;
			shopicon = null;
			consumebtn = null;
			consumeicon = null;
			consumenum = null;
			shopitembgmask = null;
			shopiconbtn = null;
			Toggle = null;
			effect1 = null;
			effect2 = null;
			
			mData = null;
		}
		
		public eqipshopitemData Data
		{
			get
			{
				return mData;
			}
		}
		
		eqipshopitemData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new eqipshopitemData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
