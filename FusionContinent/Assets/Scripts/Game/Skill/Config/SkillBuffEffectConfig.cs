using System;
using System.Collections.Generic;
using System.Globalization;
using Config;
using FusionContinent.Buff;
using ValueType = Config.ValueType;

namespace FusionContinent.Skill.Config
{
    [Serializable]
    public class SkillBuffEffectData
    {
        public int BuffTypeId;
        public int BuffId;

        /// <summary>
        /// 多效果数据列表（当 MultiEffectMode 启用时使用）
        /// </summary>
        public List<BuffEffectData> Effects;

        /// <summary>
        /// 多效果模式：true 表示使用 Effects 列表，false 兼容旧版单效果
        /// </summary>
        public bool MultiEffectMode = false;

        // 旧版字段（当 MultiEffectMode = false 时使用）
        public string AttrTypeName = "";
        public ValueType AttrValueType = ValueType.Number;
        public string PercentBaseAttrName = "";
        public int Stack = 1;
        public string StackAttrName = "";
        public float Duration = -1f;
        public string DurationAttrName = "";
        public float EffectValue;
        public string EffectValueAttrName = "";
        public float PercentEffectValue;
        public string PercentEffectValueAttrName = "";
        public string PercentValueSourceAttrName = "";
        public string BuffValueSource = "Caster";
        public int DamageType;
        public string DamageTypeAttrName = "";
        public float Probability = 1f;
        public string ProbabilityAttrName = "";
        public float TickInterval;
        public string TickIntervalAttrName = "";
        public string BuffVfx = "";
        public string ShieldVfx = "EliaSkillMoonshadowWard01";
        public string BreakVfx = "EliaSkillMoonshadowWard02";
        public bool UseTargetAsValueSource = false;
    }

    public static class SkillBuffEffectSerializer
    {
        public static string Serialize(List<SkillBuffEffectData> effects)
        {
            if (effects == null || effects.Count == 0) return string.Empty;
            var rows = new List<string>();
            foreach (var e in effects)
            {
                if (e.MultiEffectMode && e.Effects != null && e.Effects.Count > 0)
                {
                    // 多效果模式：序列化 Effects 列表
                    foreach (var effect in e.Effects)
                    {
                        var parts = new List<string>
                        {
                            Pair("bt", e.BuffTypeId),
                            Pair("bi", e.BuffId),
                            Pair("mem", true), // MultiEffectMode flag
                            Pair("et", (int)effect.EffectType),
                            Pair("at", effect.AttributeType),
                            Pair("ev", effect.Value),
                            Pair("evt", (int)effect.ValueType),
                            Pair("pv", effect.PercentValue),
                            Pair("st", e.Stack),
                            Pair("du", e.Duration),
                            Pair("ti", e.TickInterval),
                            Pair("fx", e.BuffVfx)
                        };
                        rows.Add(string.Join("&", parts));
                    }
                }
                else
                {
                    // 旧版单效果模式
                    var parts = new List<string>
                    {
                        Pair("bt", e.BuffTypeId),
                        Pair("bi", e.BuffId),
                        Pair("mem", false),
                        Pair("at", e.AttrTypeName),
                        Pair("avt", (int)e.AttrValueType),
                        Pair("pba", e.PercentBaseAttrName),
                        Pair("st", e.Stack),
                        Pair("sa", e.StackAttrName),
                        Pair("du", e.Duration),
                        Pair("da", e.DurationAttrName),
                        Pair("ev", e.EffectValue),
                        Pair("ea", e.EffectValueAttrName),
                        Pair("pv", e.PercentEffectValue),
                        Pair("pa", e.PercentEffectValueAttrName),
                        Pair("ps", e.PercentValueSourceAttrName),
                        Pair("bv", e.BuffValueSource),
                        Pair("dt", e.DamageType),
                        Pair("dta", e.DamageTypeAttrName),
                        Pair("pr", e.Probability),
                        Pair("pra", e.ProbabilityAttrName),
                        Pair("ti", e.TickInterval),
                        Pair("tia", e.TickIntervalAttrName),
                        Pair("fx", e.BuffVfx),
                        Pair("sv", e.ShieldVfx),
                        Pair("bvfx", e.BreakVfx),
                        Pair("uts", e.UseTargetAsValueSource)
                    };
                    rows.Add(string.Join("&", parts));
                }
            }
            return string.Join(";", rows);
        }

        public static List<SkillBuffEffectData> Deserialize(string raw)
        {
            var result = new List<SkillBuffEffectData>();
            if (string.IsNullOrEmpty(raw)) return result;
            var rows = raw.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var row in rows)
            {
                var e = new SkillBuffEffectData();
                var parts = row.Split('&', StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    var kv = part.Split('=', 2);
                    if (kv.Length != 2) continue;
                    var key = kv[0];
                    var value = Uri.UnescapeDataString(kv[1]);
                    switch (key)
                    {
                        case "bt": e.BuffTypeId = ToInt(value); break;
                        case "bi": e.BuffId = ToInt(value); break;
                        case "mem": e.MultiEffectMode = ToBool(value, false); break;
                        // 多效果字段
                        case "et": break; // BuffEffectType 枚举
                        case "at":
                            if (Enum.TryParse<AttributeType>(value, out var attrType))
                                e.Effects ??= new List<BuffEffectData>();
                                // 将在后续处理
                            break;
                        case "evt": break; // effect.ValueType
                        // 旧版字段
                        case "avt": e.AttrValueType = (ValueType)ToInt(value, (int)ValueType.Number); break;
                        case "pba": e.PercentBaseAttrName = value; break;
                        case "st": e.Stack = ToInt(value, 1); break;
                        case "sa": e.StackAttrName = value; break;
                        case "du": e.Duration = ToFloat(value, -1f); break;
                        case "da": e.DurationAttrName = value; break;
                        case "ev": e.EffectValue = ToFloat(value); break;
                        case "ea": e.EffectValueAttrName = value; break;
                        case "pv": e.PercentEffectValue = ToFloat(value); break;
                        case "pa": e.PercentEffectValueAttrName = value; break;
                        case "ps": e.PercentValueSourceAttrName = value; break;
                        case "bv": e.BuffValueSource = value; break;
                        case "dt": e.DamageType = ToInt(value); break;
                        case "dta": e.DamageTypeAttrName = value; break;
                        case "pr": e.Probability = ToFloat(value, 1f); break;
                        case "pra": e.ProbabilityAttrName = value; break;
                        case "ti": e.TickInterval = ToFloat(value); break;
                        case "tia": e.TickIntervalAttrName = value; break;
                        case "fx": e.BuffVfx = value; break;
                        case "sv": e.ShieldVfx = value; break;
                        case "bvfx": e.BreakVfx = value; break;
                        case "uts": e.UseTargetAsValueSource = ToBool(value, false); break;
                    }
                }
                result.Add(e);
            }
            return result;
        }

        private static string Pair(string k, object v) => $"{k}={Uri.EscapeDataString(v?.ToString() ?? string.Empty)}";
        private static int ToInt(string s, int d = 0) => int.TryParse(s, out var v) ? v : d;
        private static float ToFloat(string s, float d = 0f) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : d;
        private static bool ToBool(string s, bool d = false) => bool.TryParse(s, out var v) ? v : d;
    }
}
