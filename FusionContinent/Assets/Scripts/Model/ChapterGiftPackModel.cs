namespace FusionContinent.Model
{
    /// <summary>
    /// 关卡礼包玩家数据（对应 proto ChapterGiftPack）
    /// </summary>
    public class ChapterGiftPack
    {
        public int ChapterId { get; set; }
        public bool IsPurchase { get; set; }

        public ChapterGiftPack()
        {
        }

        public ChapterGiftPack(ChapterGiftPack other)
        {
            ChapterId = other.ChapterId;
            IsPurchase = other.IsPurchase;
        }
    }
}
