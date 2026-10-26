using System;

namespace FusionContinent.Utils
{
    public static class ObjectTool
    {

        public static int ObjectToInt(object obj)
        {
            int value;
            if (obj is int i)
            {
                value = i;
            }
            else if (obj is long l)
            {
                value = (int)l; // 小心溢出
            }
            else if (obj is float f)
            {
                value = (int)f;
            }
            else if (obj is double d)
            {
                value = (int)d;
            }
            else if (obj is string s && int.TryParse(s, out int parsed))
            {
                value = parsed;
            }
            else
            {
                // 类型不支持或无法转换
                throw new Exception($"ObjectToInt -> Unsupported type: {obj?.GetType().Name}");
            }

            return value;
        }

    }
}