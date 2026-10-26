using System.Linq;

namespace FusionContinent.Utils
{
    public static class SensitiveCharacterTool
    {
        // 检测并替换敏感词
        public static string Filter(string input)
        {
            var sensitiveWords = DataConfigManager.Tables.TbSensitiveWordsConfig.DataList;
            foreach (var cfg in sensitiveWords)
            {
                var character = cfg.Character;
                if (input.Contains(character))
                {
                    string replacement = new string('*', character.Length);
                    input = input.Replace(character, replacement);
                }
            }
            
            return input;
        }
    }
}