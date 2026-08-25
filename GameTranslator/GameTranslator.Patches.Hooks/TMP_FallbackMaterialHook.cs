using GameTranslator.Patches.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch(typeof(TMP_MaterialManager), "GetFallbackMaterial", new Type[] { typeof(Material), typeof(Material) })]
    internal class TMP_FallbackMaterialHook
    {
        private static readonly Dictionary<int, DateTime> _lastLogTime = [];

        private static readonly TimeSpan _logCooldown = TimeSpan.FromSeconds(5);

        private static DateTime _lastCleanup = DateTime.Now;

        private static readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

        private static readonly HashSet<int> _fontAssetMaterialIds = [];

        private static void CleanupLogCache()
        {
            if (!TranslatePlugin.showOtherDebug.Value)
            {
                _lastLogTime.Clear();
                return;
            }
            var now = DateTime.Now;
            if (now - _lastCleanup < _cleanupInterval) return;
            _lastCleanup = now;
            var expired = new List<int>();
            foreach (var kv in _lastLogTime)
            {
                if (now - kv.Value > _cleanupInterval)
                    expired.Add(kv.Key);
            }
            foreach (var key in expired)
                _lastLogTime.Remove(key);
        }

        internal static void RegisterFontMaterial(TMP_FontAsset font)
        {
            if (font == null) return;
            var mat = FontHelper.GetFontMaterial(font);
            if (mat == null) return;
#if MANAGED
            _fontAssetMaterialIds.Add(mat.GetInstanceID());
#else
            _fontAssetMaterialIds.Add(mat.GetEntityId().GetHashCode());
#endif
        }

        internal static void RegisterAtlasMaterialIfSourceIsFontAsset(Material sourceMaterial, Material result)
        {
            if (sourceMaterial == null || result == null) return;
#if MANAGED
            if (_fontAssetMaterialIds.Contains(sourceMaterial.GetInstanceID()))
                _fontAssetMaterialIds.Add(result.GetInstanceID());
#else
            if (_fontAssetMaterialIds.Contains(sourceMaterial.GetEntityId().GetHashCode()))
                _fontAssetMaterialIds.Add(result.GetEntityId().GetHashCode());
#endif
        }

        [HarmonyPostfix]
        [HarmonyWrapSafe]
        public static void Postfix(Material sourceMaterial, Material targetMaterial, ref Material __result)
        {
            if (!TranslatePlugin.scaleFallbackEffects.Value) return;
            ApplyScale(sourceMaterial, __result, targetMaterial);
        }

        internal static void ApplyScale(Material sourceMaterial, Material result, Material targetMaterial)
        {
            if (!TranslatePlugin.scaleFallbackEffects.Value || result == null || sourceMaterial == null || targetMaterial == null)
                return;

#if MANAGED
            if (!_fontAssetMaterialIds.Contains(sourceMaterial.GetInstanceID()))
#else
            if (!_fontAssetMaterialIds.Contains(sourceMaterial.GetEntityId().GetHashCode()))
#endif
                return;

            ApplyCore(sourceMaterial, result, targetMaterial);
        }

        private static float Clamp(float value, float min, float max)
        {
            return Mathf.Clamp(value, min, max);
        }

        private static void ApplyCore(Material sourceMaterial, Material result, Material targetMaterial)
        {
            if (!sourceMaterial.HasProperty("_GradientScale") || !result.HasProperty("_GradientScale") || !targetMaterial.HasProperty("_GradientScale")) return;
            float srcOutline = sourceMaterial.GetFloat("_OutlineWidth");
            float srcOutlineSoftness = sourceMaterial.GetFloat("_OutlineSoftness");
            float srcUnderlay = sourceMaterial.GetFloat("_UnderlayDilate");
            float srcUnderlaySoftness = sourceMaterial.GetFloat("_UnderlaySoftness");
            float srcUnderlayOffsetX = sourceMaterial.GetFloat("_UnderlayOffsetX");
            float srcUnderlayOffsetY = sourceMaterial.GetFloat("_UnderlayOffsetY");
            float srcGlowInner = sourceMaterial.GetFloat("_GlowInner");
            float srcGlowOuter = sourceMaterial.GetFloat("_GlowOuter");
            float srcGlowOffset = sourceMaterial.GetFloat("_GlowOffset");
            float srcBevelWidth = sourceMaterial.GetFloat("_BevelWidth");
            float srcBevelOffset = sourceMaterial.GetFloat("_BevelOffset");
            float srcFaceDilate = sourceMaterial.GetFloat("_FaceDilate");
            float srcGS = sourceMaterial.GetFloat("_GradientScale");
            float targetGS = targetMaterial.GetFloat("_GradientScale");
            float scale = Clamp(TranslatePlugin.fallbackEffectScale.Value, -1f, 1f);

            result.SetFloat("_OutlineWidth", Mathf.Max(0f, srcOutline * scale));
            result.SetFloat("_OutlineSoftness", Mathf.Max(0f, srcOutlineSoftness * scale));
            result.SetFloat("_UnderlayDilate", srcUnderlay * scale);
            result.SetFloat("_UnderlaySoftness", Mathf.Max(0f, srcUnderlaySoftness * scale));
            result.SetFloat("_UnderlayOffsetX", srcUnderlayOffsetX * scale);
            result.SetFloat("_UnderlayOffsetY", srcUnderlayOffsetY * scale);
            result.SetFloat("_GlowInner", Mathf.Max(0f, srcGlowInner * scale));
            result.SetFloat("_GlowOuter", Mathf.Max(0f, srcGlowOuter * scale));
            result.SetFloat("_GlowOffset", srcGlowOffset * scale);
            result.SetFloat("_BevelWidth", Clamp(srcBevelWidth * scale, -0.5f, 0.5f));
            result.SetFloat("_BevelOffset", Clamp(srcBevelOffset * scale, -0.5f, 0.5f));
            result.SetFloat("_FaceDilate", srcFaceDilate * scale);

            if (TranslatePlugin.showOtherDebug.Value)
            {
#if MANAGED
                int key = sourceMaterial.GetInstanceID() ^ (targetMaterial.GetInstanceID() << 16);
#else
                int key = sourceMaterial.GetEntityId().GetHashCode() ^ (targetMaterial.GetEntityId().GetHashCode() << 16);
#endif
                var now = DateTime.Now;
                CleanupLogCache();
                if (!_lastLogTime.TryGetValue(key, out var last) || now - last >= _logCooldown)
                {
                    _lastLogTime[key] = now;
#if MANAGED
                    string tgtDesc = $"{targetMaterial.name}#{targetMaterial.GetInstanceID()}";
                    TranslatePlugin.logger.LogInfo(
                        $"[FallbackScale] srcMat={sourceMaterial.name}#{sourceMaterial.GetInstanceID()}, tgt={tgtDesc}, srcGS={srcGS}, tgtGS={targetGS}, scale={scale:F4}");
#else
                    string tgtDesc = $"{targetMaterial.name}#{targetMaterial.GetEntityId().GetHashCode()}";
                    TranslatePlugin.logger.LogInfo(
                        $"[FallbackScale] srcMat={sourceMaterial.name}#{sourceMaterial.GetEntityId().GetHashCode()}, tgt={tgtDesc}, srcGS={srcGS}, tgtGS={targetGS}, scale={scale:F4}");
#endif
                    TranslatePlugin.logger.LogInfo(
                        $"[FallbackScale] Outline: width={srcOutline}→{result.GetFloat("_OutlineWidth"):F4}, color={sourceMaterial.GetColor("_OutlineColor")}, keyword={sourceMaterial.IsKeywordEnabled("OUTLINE_ON")}");
                    TranslatePlugin.logger.LogInfo(
                        $"[FallbackScale] Underlay: dilate={srcUnderlay}→{result.GetFloat("_UnderlayDilate"):F4}, color={sourceMaterial.GetColor("_UnderlayColor")}, keyword={sourceMaterial.IsKeywordEnabled("UNDERLAY_ON")}");
                    TranslatePlugin.logger.LogInfo(
                        $"[FallbackScale] Glow: inner={srcGlowInner}→{result.GetFloat("_GlowInner"):F4}, outer={srcGlowOuter}→{result.GetFloat("_GlowOuter"):F4}, color={sourceMaterial.GetColor("_GlowColor")}, keyword={sourceMaterial.IsKeywordEnabled("GLOW_ON")}");
                    TranslatePlugin.logger.LogInfo(
                        $"[FallbackScale] Face: dilate={srcFaceDilate}, color={sourceMaterial.GetColor("_FaceColor")}");
                    TranslatePlugin.logger.LogInfo(
                        $"[FallbackScale] Bevel: width={srcBevelWidth}→{result.GetFloat("_BevelWidth"):F4}");
                }
            }
        }
    }

    [HarmonyPatch(typeof(TMP_MaterialManager), "GetFallbackMaterial", new Type[] { typeof(TMP_FontAsset), typeof(Material), typeof(int) })]
    internal class TMP_FallbackMaterialHook_AtlasIndexRegister
    {
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        public static void Postfix(Material sourceMaterial, ref Material __result)
        {
            if (!TranslatePlugin.scaleFallbackEffects.Value) return;
            TMP_FallbackMaterialHook.RegisterAtlasMaterialIfSourceIsFontAsset(sourceMaterial, __result);
        }
    }
}
