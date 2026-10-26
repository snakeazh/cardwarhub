using System.Collections;
using UnityEngine;
using UnityEngine.U2D;

namespace Manager
{
    public class AtlasPreloader : MonoBehaviour
    {
        [Header("需要预加载的图集")]
        public SpriteAtlas[] atlases;

        IEnumerator Start()
        {
            foreach (var atlas in atlases)
            {
                if (atlas == null) continue;

                int count = atlas.spriteCount;
                Sprite[] sprites = new Sprite[count];
                atlas.GetSprites(sprites);

                // 可选：强制让 GPU 上传贴图
                foreach (var s in sprites)
                {
                    if (s != null)
                        s.texture.GetNativeTexturePtr();
                }
            }

            yield return null; // 等待一帧，确保图集上传完成
            LogTool.Log("[AtlasPreloader] 所有图集预加载完成");
        }
    }
}