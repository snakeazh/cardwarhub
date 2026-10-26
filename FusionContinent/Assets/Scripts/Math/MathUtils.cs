namespace FusionContinent
{
    public static class MathUtils
    {
        public static int Max(int source,int minValue)
        {
            if (source< minValue)
            {
                source = minValue;
            }

            return source;
        }
    }
}