using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Core.App.Localization;
using PCL.Core.Wpf;

namespace PCL.Core.Test;

[TestClass]
public class LocalizationTest
{
    private static readonly string[] LanguageFiles = LocalizationService.SupportedLanguages
        .Select(language => language.Code)
        .ToArray();

    [STATestMethod]
    public void LanguageResourcesUseTranslationsAndFallBackToBaseKeys()
    {
        var app = new Application();
        try
        {
            var backend = new WpfLocalizationBackend();
            var baseLanguage = LocalizationService.DefaultLanguageCode;
            backend.ApplyLanguageResources(baseLanguage, null);
            var baseValues = LoadKeys(baseLanguage).ToDictionary(key => key, backend.TryFindResource);
            foreach (var language in LanguageFiles.Where(language => language != baseLanguage))
            {
                backend.ApplyLanguageResources(baseLanguage, language);
                Assert.AreEqual(language, backend.TryFindResource("Meta.Code"));
                var translated = LoadKeys(language);
                foreach (var (key, value) in baseValues)
                {
                    Assert.IsNotNull(value, key);
                    var actual = backend.TryFindResource(key);
                    Assert.IsNotNull(actual, $"{language}: {key}");
                    if (!translated.Contains(key)) Assert.AreEqual(value, actual, $"{language}: {key}");
                }
            }
        }
        finally { app.Shutdown(); }
    }


    [TestMethod]
    public void LanguageDictionariesShouldNotContainDuplicateKeys()
    {
        foreach (var language in LanguageFiles)
        {
            var keys = LoadKeyList(language);
            var duplicated = keys
                .GroupBy(key => key)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();

            Assert.IsEmpty(duplicated, $"{language} 存在重复语言键：{string.Join(", ", duplicated)}");
        }
    }

    [TestMethod]
    public void LanguageKeysShouldUseDotNaming()
    {
        foreach (var language in LanguageFiles)
        foreach (var key in LoadKeys(language))
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(key), $"{language} 存在空语言键");
            Assert.IsTrue(key.Contains('.'), $"{language} 的语言键缺少分组分隔符：{key}");
            Assert.IsFalse(key.Contains(' '), $"{language} 的语言键不应包含空格：{key}");
        }
    }


    [TestMethod]
    public void SupportedLanguagesShouldHaveValidCultureAndResourceDictionary()
    {
        foreach (var language in LocalizationService.SupportedLanguages)
        {
            CultureInfo.GetCultureInfo(language.CultureName);

            var filePath = Path.Combine(GetRepositoryRoot(), "PCL.Core.Wpf", "App", "Localization", "Languages",
                language.Code + ".xaml");
            Assert.IsTrue(File.Exists(filePath), $"{language.Code} 缺少语言资源文件");
        }
    }


    [TestMethod]
    public void FontProfileShouldFollowCultureGlyphStandard()
    {
        // WPF normally registers this parser during Application startup.
        _ = System.IO.Packaging.PackUriHelper.UriSchemePack;
        Assert.AreEqual(LocalizationFontProfile.SimplifiedChinese,
            LocalizationFontService.ResolveProfileFromCultureName("zh-CN"));
        Assert.AreEqual(LocalizationFontProfile.SimplifiedChinese,
            LocalizationFontService.ResolveProfileFromCultureName("zh-Hans"));
        Assert.AreEqual(LocalizationFontProfile.TraditionalChinese,
            LocalizationFontService.ResolveProfileFromCultureName("zh-TW"));
        Assert.AreEqual(LocalizationFontProfile.TraditionalChinese,
            LocalizationFontService.ResolveProfileFromCultureName("zh_HK"));
        Assert.AreEqual(LocalizationFontProfile.TraditionalChinese,
            LocalizationFontService.ResolveProfileFromCultureName("zh-Hant-HK"));
        Assert.AreEqual(LocalizationFontProfile.Japanese,
            LocalizationFontService.ResolveProfileFromCultureName("ja-JP"));
        Assert.AreEqual(LocalizationFontProfile.Korean,
            LocalizationFontService.ResolveProfileFromCultureName("ko-KR"));
        Assert.AreEqual(LocalizationFontProfile.English,
            LocalizationFontService.ResolveProfileFromCultureName("en-US"));
        Assert.AreEqual(LocalizationFontProfile.English,
            LocalizationFontService.ResolveProfileFromCultureName("en-GB"));
        Assert.AreEqual(LocalizationFontProfile.Other,
            LocalizationFontService.ResolveProfileFromCultureName("fr-FR"));
        Assert.AreEqual(LocalizationFontProfile.Other,
            LocalizationFontService.ResolveProfileFromCultureName("es-ES"));
        Assert.AreEqual(LocalizationFontProfile.Other,
            LocalizationFontService.ResolveProfileFromCultureName("pt-BR"));
    }

    [TestMethod]
    public void FontProfileAliasesShouldNotMakeLanguageResourceSupported()
    {
        Assert.IsFalse(LocalizationService.IsLanguageSupported("zh-HK"));
        Assert.AreEqual(LocalizationService.DefaultLanguageCode, LocalizationService.ResolveLanguage("zh-HK").Code);
    }

    private static HashSet<string> LoadKeys(string language)
    {
        return LoadKeyList(language).ToHashSet();
    }

    private static string[] LoadKeyList(string language)
    {
        var filePath = Path.Combine(GetRepositoryRoot(), "PCL.Core.Wpf", "App", "Localization", "Languages",
            language + ".xaml");
        var document = XDocument.Load(filePath);
        var keyAttributeName = XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml");
        return document.Descendants()
            .Select(element => element.Attribute(keyAttributeName)?.Value)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key!)
            .ToArray();
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "PCL.Core"))) return directory.FullName;
            directory = directory.Parent;
        }

        Assert.Fail("无法定位仓库根目录");
        return string.Empty;
    }
}
