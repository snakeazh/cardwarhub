namespace FusionContinent
{
    /// <summary>
    /// 击杀后对侧刷新一次：已转移过的怪不再转移。
    /// </summary>
    public static class MonsterTransferOnDeathRule
    {
        public static void TryTransfer(MonsterCard monster)
        {
            if (monster == null)
            {
                return;
            }

            if (!BattleSession.IsPvpAi)
            {
                return;
            }

            if (monster.HasTransferred)
            {
                return;
            }

            if (GamePlayManager.Default == null || GameLand.Default == null)
            {
                return;
            }

            var targetSide = monster.BattleSide == BattleSide.Player
                ? BattleSide.Opponent
                : BattleSide.Player;

            var lane = GameLand.Default.GetLane(targetSide);
            if (lane == null || lane.OrdinaryMonster == null)
            {
                LogTool.LogError($"[MonsterTransferOnDeathRule] 对侧车道未就绪 targetSide={targetSide}");
                return;
            }

            GamePlayManager.Default.AddMonsterCard(
                monster._cardId,
                monster._level,
                default,
                1f,
                false,
                0.5f,
                targetSide,
                true);
        }
    }
}
