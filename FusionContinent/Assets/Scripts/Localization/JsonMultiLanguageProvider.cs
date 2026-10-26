using System;
using System.Collections.Generic;
using System.IO;
using Config;
using FusionContinent;
using QFramework;
using UnityEngine;
using SimpleJSON;

namespace Assets.Scripts.Localization
{
    public class JsonMultiLanguageProvider : ILocalizationProvider
    {
        private LanguageType _currentLanguage = default;
        
        private readonly Dictionary<LocalizationModule, Dictionary<string, Dictionary<LanguageType, string>>> _modules 
            = new Dictionary<LocalizationModule, Dictionary<string, Dictionary<LanguageType, string>>>();
        
        // 加载语言，可以按模块加载
        public void LoadLanguage(LanguageType language, LocalizationModule module = default)
        {
            _currentLanguage = language;

            if (module == default)
            {
                // 加载所有模块
                var loadModules = (LocalizationModule[])Enum.GetValues(typeof(LocalizationModule));
                foreach (var moduleName in loadModules)
                {
                    LoadModule(moduleName);
                }
            }
            else
            {
                // 加载单模块
                LoadModule(module);
            }
        }

        private void LoadModule(LocalizationModule moduleName)
        {
            var fileName = $"tb{moduleName.ToString().ToLower()}localization";
            string jsonFile = DataConfigManager.ReadConfigFile(fileName);
            if (jsonFile.IsNullOrEmpty())
            {
                LogTool.LogError("JSON file not found: " + fileName);
                return;
            }
            
            if (!_modules.ContainsKey(moduleName))
                _modules[moduleName] = new Dictionary<string, Dictionary<LanguageType, string>>();

            try
            {
                JSONNode node = JSON.Parse(jsonFile).AsArray;

                // 支持数组式结构
                if (node.IsArray)
                {
                    foreach (var item in node)
                    {
                        var data = item.Value;
                        string key = data["Key"];
                        if (string.IsNullOrEmpty(key))
                            continue;

                        if (!_modules[moduleName].ContainsKey(key))
                            _modules[moduleName][key] = new Dictionary<LanguageType, string>();

                        // 遍历字段
                        foreach (KeyValuePair<string, JSONNode> kv in item.Value)
                        {
                            string field = kv.Key;
                            if (field == "Key") continue;
                        
                            // string lang = field.ToUpperInvariant();
                            var lang = (LanguageType)Enum.Parse(typeof(LanguageType),field);
                            string value = kv.Value.Value;
                        
                            _modules[moduleName][key][lang] = value;
                        }
                    }
                }
                else
                {
                    LogTool.LogWarning($"[Localization] {moduleName} is not array-type JSON");
                }
            }
            catch (System.Exception e)
            {
                LogTool.LogError($"[Localization] Failed to parse {moduleName}: {e.Message}");
            }
        }

        // 获取文本
        public string GetText(LocalizationModule module, string key, LanguageType language = default)
        {
            if (language == default)
                language = _currentLanguage;

            if (_modules.TryGetValue(module, out var moduleDict))
            {
                if (moduleDict.TryGetValue(key, out var langDict))
                {
                    if (langDict.TryGetValue(language, out var value))
                        return value;
                }
            }

            return key; // 找不到返回 key
        }
        
        // 获取资源路径
        public string GetResourcePath(LocalizationModule module, string key, LanguageType language = default)
        {
            if (language == default)
                language = _currentLanguage;
            
            if (_modules.TryGetValue(module, out var moduleDict))
            {
                if (moduleDict.TryGetValue(key, out var langDict))
                {
                    // 优先使用当前语言的资源
                    if (langDict.TryGetValue(language, out var path))
                        return path;
                    
                    // 再使用 default
                    if (langDict.TryGetValue(LanguageType.Default, out var defaultPath))
                        return defaultPath;
                }
            }
            
            return null;
        }

        // 热更新单模块
        public void HotUpdateModule(LocalizationModule module, string jsonContent)
        {
            // string dir = Path.Combine(Application.streamingAssetsPath, "Localization", module);
            // if (!Directory.Exists(dir))
            //     Directory.CreateDirectory(dir);
            //
            // string path = Path.Combine(dir, "HotUpdate.json"); // 可以用 HotUpdate.json
            // File.WriteAllText(path, jsonContent);
            // LoadModule(module, path);
            // LogTool.Log("Module hot-updated: " + module);
        }

    }
}