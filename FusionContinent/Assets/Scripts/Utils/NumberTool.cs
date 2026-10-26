using System;

namespace FusionContinent.Utils
{
    public static class NumberTool
    {
        public static string FormatNumber(long number)
        {
            return number.ToString();
        }

        public static string FormatNumber(double number)
        {
            return number.ToString("0.#");
        }

        public static string FormatNumber<T>(T value)
        {
            return FormatNumber(Convert.ToDouble(value));
        }
    }
}
