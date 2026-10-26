using QFramework;

namespace FusionContinent
{
	public class RealmRoomData : UIPanelData
	{
	}

	/// <summary>
	/// 旧面板组件壳，仅保留 Designer 绑定的 UI 字段供 RealmRoomNew 复用。
	/// </summary>
	public partial class RealmRoom : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as RealmRoomData ?? new RealmRoomData();
		}

		protected override void OnClose()
		{
			
		}
	}
}
