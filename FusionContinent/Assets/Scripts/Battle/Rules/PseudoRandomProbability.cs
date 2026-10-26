using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 战斗伪随机概率：
    /// <list type="bullet">
    /// <item>未触发：连续失败时按当前概率多掷一次（1 次 → 2 次 → …），触发后失败次数归 0。</item>
    /// <item>已触发：连续触发衰减 base → base/2 → base*(7/30) → 最低 5%；未触发则衰减档位归 0。</item>
    /// </list>
    /// 基准 30% 时档位为 30% → 15% → 7% → 5%。
    /// </summary>
    public sealed class PseudoRandomProbability
    {
        public const float MinRate = 0.05f;
        public const int MaxSuccessTier = 3;

        private int mSuccessTier;
        private int mFailCount;

        public int SuccessTier => mSuccessTier;
        public int FailCount => mFailCount;

        /// <summary>
        /// 按当前伪随机状态掷一次（含失败叠加的多次独立判定）。
        /// </summary>
        /// <param name="baseRate">配置基准概率（0~1），对应首次/衰减归零后的概率。</param>
        /// <param name="ownerId">规则所属 Owner / 英灵 cardId，仅用于日志。</param>
        /// <param name="ruleId">规则 Id，仅用于日志；暴击检测可传 0。</param>
        /// <param name="logTag">日志标签，如 Crit / BattleRule。</param>
        public bool Roll(float baseRate, int ownerId = 0, int ruleId = 0, string logTag = null)
        {
            string tag = string.IsNullOrEmpty(logTag) ? "Rule" : logTag;
            string idPart = ruleId > 0 ? $" Rule={ruleId}" : string.Empty;

            if (baseRate <= 0f)
            {
                LogTool.Log(
                    $"[PseudoRandom][{tag}] Owner={ownerId}{idPart} baseRate={baseRate:P1} → 跳过(<=0)");
                return false;
            }

            if (baseRate >= 1f)
            {
                int tierBefore = mSuccessTier;
                int failBefore = mFailCount;
                OnSuccess();
                LogTool.Log(
                    $"[PseudoRandom][{tag}] Owner={ownerId}{idPart} baseRate={baseRate:P1} → 必过 " +
                    $"tier {tierBefore}→{mSuccessTier} fail {failBefore}→{mFailCount}");
                return true;
            }

            float rate = GetCurrentRate(baseRate);
            int rollTimes = mFailCount + 1;
            int tierBeforeRoll = mSuccessTier;
            int failBeforeRoll = mFailCount;
            bool hit = false;
            float lastRoll = -1f;
            int hitIndex = -1;

            for (int i = 0; i < rollTimes; i++)
            {
                lastRoll = UnityEngine.Random.value;
                LogTool.Log(
                    $"[PseudoRandom][{tag}] Owner={ownerId}{idPart} " +
                    $"检测[{i + 1}/{rollTimes}] rate={rate:P1} random={lastRoll:F4} " +
                    $"base={baseRate:P1} tier={tierBeforeRoll} failCount={failBeforeRoll} " +
                    $"→ {(lastRoll <= rate ? "通过" : "未通过")}");

                if (lastRoll <= rate)
                {
                    hit = true;
                    hitIndex = i + 1;
                    break;
                }
            }

            if (hit)
            {
                OnSuccess();
            }
            else
            {
                OnFail();
            }

            LogTool.Log(
                $"[PseudoRandom][{tag}] Owner={ownerId}{idPart} 结果={(hit ? "触发" : "未触发")} " +
                $"hitIndex={hitIndex} lastRandom={lastRoll:F4} rate={rate:P1} " +
                $"tier {tierBeforeRoll}→{mSuccessTier} fail {failBeforeRoll}→{mFailCount}");

            return hit;
        }

        public float GetCurrentRate(float baseRate)
        {
            float clampedBase = Mathf.Clamp01(baseRate);
            switch (mSuccessTier)
            {
                case 0:
                    return Mathf.Max(MinRate, clampedBase);
                case 1:
                    return Mathf.Max(MinRate, clampedBase * 0.5f);
                case 2:
                    return Mathf.Max(MinRate, clampedBase * (7f / 30f));
                default:
                    return MinRate;
            }
        }

        public void Reset()
        {
            mSuccessTier = 0;
            mFailCount = 0;
        }

        private void OnSuccess()
        {
            mFailCount = 0;
            if (mSuccessTier < MaxSuccessTier)
            {
                mSuccessTier++;
            }
        }

        private void OnFail()
        {
            mFailCount++;
            mSuccessTier = 0;
        }
    }
}
