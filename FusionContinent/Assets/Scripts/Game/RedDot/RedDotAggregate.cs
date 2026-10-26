using System.Collections.Generic;

namespace Game.RedDot
{
    internal static class RedDotAggregate
    {
        public static int Compute(RedDotAggregateMode mode, int localCount, IReadOnlyList<RedDotNode> children)
        {
            if (children.Count == 0)
            {
                return localCount;
            }

            switch (mode)
            {
                case RedDotAggregateMode.Any:
                {
                    if (localCount > 0)
                    {
                        return 1;
                    }

                    for (int i = 0; i < children.Count; i++)
                    {
                        if (children[i].Count > 0)
                        {
                            return 1;
                        }
                    }

                    return 0;
                }
                case RedDotAggregateMode.Max:
                {
                    int max = localCount;
                    for (int i = 0; i < children.Count; i++)
                    {
                        if (children[i].Count > max)
                        {
                            max = children[i].Count;
                        }
                    }

                    return max;
                }
                default:
                {
                    int sum = localCount;
                    for (int i = 0; i < children.Count; i++)
                    {
                        sum += children[i].Count;
                    }

                    return sum;
                }
            }
        }
    }
}
