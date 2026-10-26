namespace FusionContinent.LocalServer
{
    /// <summary>
    /// 本地随机源。权重抽取语义对照 Go math/rand（rand.Intn(total) + 前缀扫描）。
    /// </summary>
    public class LocalRng
    {
        private readonly System.Random _random = new System.Random();

        /// <summary>返回 [0, maxExclusive)，同 Go rand.Intn。</summary>
        public int Next(int maxExclusive)
        {
            return _random.Next(maxExclusive);
        }

        /// <summary>返回 [0,1)，同 Go rand.Float64（融合石吞噬概率/进度浮动、装备特效触发用）。</summary>
        public double NextDouble()
        {
            return _random.NextDouble();
        }
    }
}
