using Config;

namespace Assets.Scripts.Localization
{
    public interface ILocalizationProvider
    {
        string GetText(LocalizationModule module, string key, LanguageType language = default);
        void LoadLanguage(LanguageType language, LocalizationModule module = default);
        void HotUpdateModule(LocalizationModule module, string jsonContent);
    }
}