using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace MyRobotJP
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInProcess("Robot.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "myrobot.japanesefont";
        public const string Name = "MyRobot Japanese Font";
        public const string Version = "1.2.0";

        internal static ManualLogSource Log;
        internal static TMP_FontAsset JpFont;

        internal static ConfigEntry<string> FontFile;
        internal static ConfigEntry<string> ExtraFallbackFonts;
        internal static readonly List<TMP_FontAsset> ExtraFonts = new List<TMP_FontAsset>();
        internal static ConfigEntry<int> SamplingPointSize;
        internal static ConfigEntry<int> AtlasSize;
        internal static ConfigEntry<bool> ReplaceAllTexts;
        internal static ConfigEntry<bool> AddAsFallback;
        internal static ConfigEntry<bool> DebugLog;

        private static string GetPath(Transform t)
        {
            var path = t.name;
            while ((t = t.parent) != null) path = t.name + "/" + path;
            return path;
        }

        // Original font of each text we changed, so we can restore it when leaving Japanese.
        private static readonly Dictionary<int, TMP_FontAsset> OriginalFonts = new Dictionary<int, TMP_FontAsset>();
        private static readonly Dictionary<int, Material> OriginalMaterials = new Dictionary<int, Material>();
        private static readonly HashSet<int> PatchedFallbackOwners = new HashSet<int>();

        private void Awake()
        {
            Log = Logger;
            FontFile = Config.Bind("Font", "FontFile", "Corporate-Logo-Rounded-Bold-ver3.otf",
                "fonts フォルダ内のフォントファイル名 (.ttf / .otf)");
            ExtraFallbackFonts = Config.Bind("Font", "ExtraFallbackFonts", "NotoSansJP-Bold.otf",
                "メインのフォントに無い文字(JIS第3・第4水準の漢字など)を表示する予備フォント。fonts フォルダ内のファイル名をカンマ区切りで指定。空欄で無効");
            SamplingPointSize = Config.Bind("Font", "SamplingPointSize", 90,
                "フォントアトラスに描き込むときのサイズ。大きいほどきれいだがメモリを使う");
            AtlasSize = Config.Bind("Font", "AtlasSize", 2048, "フォントアトラスの幅と高さ (1024 / 2048 / 4096)");
            ReplaceAllTexts = Config.Bind("Behavior", "ReplaceAllTextsInJapanese", true,
                "日本語のとき、ゲーム内のすべての文字をこのフォントに置き換える");
            AddAsFallback = Config.Bind("Behavior", "AddAsFallback", true,
                "どの言語でも、元のフォントに無い文字(日本語など)をこのフォントで表示する");

            DebugLog = Config.Bind("Debug", "DebugLog", false,
                "フォントを置き換えた文字の情報をログに出す(不具合調査用)");

            if (!LoadFont()) return;

            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            SceneManager.sceneLoaded += (_, __) => ApplyAll();
            Log.LogInfo($"{Name} {Version} loaded");
        }

        private string FontsDir => Path.Combine(Path.GetDirectoryName(Info.Location) ?? Paths.PluginPath, "fonts");

        private static TMP_FontAsset CreateFont(string path)
        {
            if (!File.Exists(path))
            {
                Log.LogError("Font file not found: " + path);
                return null;
            }
            TMP_FontAsset fa = null;
            try
            {
                int size = Mathf.Clamp(AtlasSize.Value, 512, 8192);
                fa = TMP_FontAsset.CreateFontAsset(path, 0, SamplingPointSize.Value, 9,
                    GlyphRenderMode.SDFAA, size, size);
            }
            catch (Exception e)
            {
                Log.LogError("Failed to create font asset from " + path + ": " + e);
                return null;
            }
            if (fa == null)
            {
                Log.LogError("TMP_FontAsset.CreateFontAsset returned null for " + path);
                return null;
            }
            fa.name = "MyRobotJP " + Path.GetFileNameWithoutExtension(path);
            fa.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            fa.isMultiAtlasTexturesEnabled = true;
            DontDestroyOnLoad(fa);
            Log.LogInfo("Loaded font: " + path);
            return fa;
        }

        private bool LoadFont()
        {
            JpFont = CreateFont(Path.Combine(FontsDir, FontFile.Value));
            if (JpFont == null) return false;

            var table = JpFont.fallbackFontAssetTable ?? (JpFont.fallbackFontAssetTable = new List<TMP_FontAsset>());
            foreach (var raw in (ExtraFallbackFonts.Value ?? "").Split(','))
            {
                var file = raw.Trim();
                if (file.Length == 0) continue;
                var extra = CreateFont(Path.Combine(FontsDir, file));
                if (extra == null) continue;
                ExtraFonts.Add(extra);
                table.Add(extra);
            }
            return true;
        }

        private static bool materialAdopted;

        // The game's own Japanese font asset uses a material whose shader works on every canvas
        // (including the in-game monitors). Our runtime font gets a default material that doesn't,
        // so copy the game's material onto our atlas once.
        internal static void AdoptMaterialFrom(TMP_FontAsset gameFont)
        {
            if (materialAdopted || JpFont == null || gameFont == null || gameFont == JpFont) return;
            var src = gameFont.material;
            if (src == null) return;
            materialAdopted = true;
            try
            {
                var mat = TMP_MaterialManager.GetFallbackMaterial(src, JpFont.material);
                if (mat == null) return;
                mat.name = JpFont.name + " Material";
                JpFont.material = mat;
                foreach (var extra in ExtraFonts)
                {
                    var em = TMP_MaterialManager.GetFallbackMaterial(src, extra.material);
                    if (em != null) extra.material = em;
                }
                Log.LogInfo($"Using shader '{src.shader.name}' from game font '{gameFont.name}'");
            }
            catch (Exception e) { Log.LogError("AdoptMaterialFrom failed: " + e); }
        }

        internal static void EnsureMaterialAdopted()
        {
            if (materialAdopted) return;
            var lex = Manager<Lexicon>.main;
            if (lex != null) lex.GetFontForSystemLanguage(SystemLanguage.Japanese); // triggers the postfix
        }

        internal static bool IsJapanese()
        {
            var lex = Manager<Lexicon>.main;
            return lex != null && lex.currentSystemLanguage == SystemLanguage.Japanese;
        }

        internal static void ApplyFallbacks()
        {
            if (!AddAsFallback.Value || JpFont == null) return;
            try
            {
                var global = TMP_Settings.fallbackFontAssets;
                if (global != null)
                {
                    if (!global.Contains(JpFont)) global.Add(JpFont);
                    foreach (var extra in ExtraFonts)
                        if (!global.Contains(extra)) global.Add(extra);
                }
            }
            catch (Exception e) { Log.LogDebug("TMP_Settings unavailable: " + e.Message); }

            foreach (var fa in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            {
                if (fa == null || fa == JpFont || ExtraFonts.Contains(fa)) continue;
                if (!PatchedFallbackOwners.Add(fa.GetInstanceID())) continue;
                var table = fa.fallbackFontAssetTable ?? (fa.fallbackFontAssetTable = new List<TMP_FontAsset>());
                if (!table.Contains(JpFont)) table.Add(JpFont);
                foreach (var extra in ExtraFonts)
                    if (!table.Contains(extra)) table.Add(extra);
            }
        }

        internal static void ApplyTo(TMP_Text text)
        {
            if (text == null || JpFont == null || !ReplaceAllTexts.Value) return;
            int id = text.GetInstanceID();
            if (IsJapanese())
            {
                EnsureMaterialAdopted();
                var current = text.font;
                if (current == JpFont) return;
                if (current == null) return;
                var originalMat = text.fontSharedMaterial;
                if (!OriginalFonts.ContainsKey(id)) { OriginalFonts[id] = current; OriginalMaterials[id] = originalMat; }
                text.font = JpFont;
                // Keep the game's own shader and settings (outline, glow, screen effects...) by
                // building a material from the original one with our atlas, like TMP does for fallbacks.
                if (originalMat != null && JpFont.material != null && originalMat.shader != JpFont.material.shader)
                {
                    try { text.fontSharedMaterial = TMP_MaterialManager.GetFallbackMaterial(originalMat, JpFont.material); }
                    catch (Exception e) { Log.LogDebug("Material copy failed for " + text.name + ": " + e.Message); }
                }
                if (DebugLog.Value)
                    Log.LogInfo($"[Replace] {GetPath(text.transform)} font={current.name} shader={(originalMat ? originalMat.shader.name : "null")} overflow={text.overflowMode} size={text.fontSize} auto={text.enableAutoSizing} rect={text.rectTransform.rect.size}");
            }
            else if (OriginalFonts.TryGetValue(id, out var original))
            {
                OriginalFonts.Remove(id);
                OriginalMaterials.TryGetValue(id, out var originalMat);
                OriginalMaterials.Remove(id);
                if (text.font == JpFont && original != null)
                {
                    text.font = original;
                    if (originalMat != null) text.fontSharedMaterial = originalMat;
                }
            }
        }

        internal static void ApplyAll()
        {
            EnsureMaterialAdopted();
            ApplyFallbacks();
            foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                if (t == null || !t.gameObject.scene.IsValid()) continue; // skip prefabs/assets
                ApplyTo(t);
            }
        }
    }

    // The game asks Lexicon which font to use for the current language. Answer with ours for Japanese.
    [HarmonyPatch(typeof(Lexicon), nameof(Lexicon.GetFontForSystemLanguage))]
    internal static class GetFontPatch
    {
        private static void Postfix(SystemLanguage systemLanguage, ref TMP_FontAsset __result)
        {
            if (systemLanguage == SystemLanguage.Japanese && Plugin.JpFont != null)
            {
                Plugin.AdoptMaterialFrom(__result);
                __result = Plugin.JpFont;
            }
        }
    }

    [HarmonyPatch(typeof(Lexicon), "AssignLanguage")]
    internal static class AssignLanguagePatch
    {
        private static void Postfix()
        {
            try { Plugin.ApplyAll(); } catch (Exception e) { Plugin.Log.LogError(e); }
        }
    }

    [HarmonyPatch(typeof(TextMeshProUGUI), "OnEnable")]
    internal static class UGUIEnablePatch
    {
        private static void Postfix(TextMeshProUGUI __instance) => Plugin.ApplyTo(__instance);
    }

    [HarmonyPatch(typeof(TextMeshPro), "OnEnable")]
    internal static class WorldEnablePatch
    {
        private static void Postfix(TextMeshPro __instance) => Plugin.ApplyTo(__instance);
    }
}
