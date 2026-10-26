using System.Collections;
using System.Collections.Generic;
using System.IO;
using Config;
using UnityEngine;
using QFramework;
using SimpleJSON;
using YooAsset;

namespace FusionContinent
{
	public class DataConfigManager
	{
		public static Tables Tables { get; private set; }


		// [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		public static void Initialize()
		{
		
			Tables = new Tables(LoadByteBuf);
		
		}

		// 全量配置的 JSON 原文。Tables 初始化逐表消费后即从字典移除（8.4MB 原文不再常驻），
		// 剩余条目仅为 tb*localization 多语言文件（JsonMultiLanguageProvider 按需取用，生命周期与字典一致）
		private static Dictionary<string, string> dictionary;
		public static IEnumerator LoadAllConfigs()
		{
			dictionary = new Dictionary<string, string>();
			var handle = YooAssets.LoadAllAssetsAsync<TextAsset>("Config_tbtextinfo");		
			yield return handle;
			foreach (var assetObj in handle.AllAssetObjects)
			{
				var configs = assetObj as TextAsset;
				if (configs == null)
				{
					continue;
				}
				dictionary.Add(configs.name, configs.text);
			}
			handle.Release();
		}

		public static string ReadConfigFile(string fileName)
		{
			string fileContent = string.Empty;
			if (dictionary.TryGetValue(fileName, out fileContent))
			{
				
			}

			return fileContent;
		}

		private static JSONNode LoadByteBuf(string file)
		{
			string jsonFile = ReadConfigFile(file);
			if (!jsonFile.IsNullOrEmpty())
			{
				// 配置表仅在 Tables 初始化时被消费一次，解析前即从字典移除，原文 string 交给 GC；
				// tb*localization 多语言表例外：JsonMultiLanguageProvider 之后还要按原文再读一次，保留
				if (!file.EndsWith("localization"))
				{
					dictionary.Remove(file);
				}
				return JSON.Parse(jsonFile);
			}
			else
			{
				LogTool.LogError("JSON file not found: " + file);
				return null;
			}
		}
	}
}
