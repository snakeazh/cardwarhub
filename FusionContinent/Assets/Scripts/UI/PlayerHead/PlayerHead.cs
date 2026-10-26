using Config;
using FusionContinent;
using FusionContinent.Model;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHeadData
{
    public int Avatar;
    public int AvatarFrame;
}

public class PlayerHead : MonoBehaviour
{
    public System.Action<int> OnClickPlayer;
    public Image HeadImage;
    public Image HeadFrameImage;

    public void SetData(UserData userData)
    {
        var headData = new PlayerHeadData();
        headData.Avatar = userData.GetAvatarHeadConfigId();
        headData.AvatarFrame = userData.AvatarFrame;
        SetData(headData);
    }

    public void SetData(PlayerHeadData playerHeadData)
    {
        HeadImage.sprite = Global.PlayerHeadAtlas.GetSprite(ResolveAvatarSpriteName(playerHeadData.Avatar));

        if (HeadFrameImage == null) return;

        var frameSpriteName = ResolveAvatarFrameSpriteName(playerHeadData.AvatarFrame);
        if (string.IsNullOrEmpty(frameSpriteName))
        {
            HeadFrameImage.gameObject.SetActive(false);
            return;
        }

        HeadFrameImage.sprite = Global.PlayerHeadFrameAtlas.GetSprite(frameSpriteName);
        HeadFrameImage.gameObject.SetActive(true);
    }

    private static string ResolveAvatarSpriteName(int avatarHeadConfigId)
    {
        if (avatarHeadConfigId <= 0) return "Avatar1";

        var cfg = DataConfigManager.Tables.TbHeadConfig.GetOrDefault(avatarHeadConfigId);
        if (cfg != null && cfg.Type == HeadType.Avatar)
        {
            return "Avatar" + (cfg.Id - 10000);
        }

        if (avatarHeadConfigId >= 10001 && avatarHeadConfigId < 11000)
        {
            return "Avatar" + (avatarHeadConfigId - 10000);
        }

        return "Avatar" + avatarHeadConfigId;
    }

    private static string ResolveAvatarFrameSpriteName(int avatarFrameId)
    {
        if (avatarFrameId <= 0) return null;

        var cfg = DataConfigManager.Tables.TbHeadConfig.GetOrDefault(avatarFrameId);
        // if (cfg != null && cfg.Type == HeadType.AvatarFrame)
        {
            // return "AvatarFrame" + (cfg.Id - 11000);
        }

        return cfg.Icon;

        // return "AvatarFrame" + avatarFrameId;
    }
}
