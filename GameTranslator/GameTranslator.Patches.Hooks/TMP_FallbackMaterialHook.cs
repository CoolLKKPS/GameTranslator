using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch]
    internal class TMP_FallbackMaterialHook
    {
        private static readonly Dictionary<int, DateTime> _lastLogTime = new Dictionary<int, DateTime>();

        private static readonly TimeSpan _logCooldown = TimeSpan.FromSeconds(5);

        private static DateTime _lastCleanup = DateTime.Now;

        private static readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

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

        static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("TMPro.TMP_MaterialManager"), "GetFallbackMaterial", new[] { typeof(Material), typeof(Material) });
        }

        [HarmonyPostfix]
        [HarmonyWrapSafe]
        public static void Postfix(Material sourceMaterial, Material targetMaterial, ref Material __result)
        {
            ApplyScale(sourceMaterial, __result, targetMaterial);
        }

        internal static void ApplyScale(Material sourceMaterial, Material result, Material targetMaterial)
        {
            if (!TranslatePlugin.scaleFallbackEffects.Value || result == null || sourceMaterial == null || targetMaterial == null)
                return;

            ApplyCore(sourceMaterial, result, targetMaterial);
        }

        internal static void ApplyScaleWithoutTarget(Material sourceMaterial, Material result)
        {
            if (!TranslatePlugin.scaleFallbackEffects.Value || result == null || sourceMaterial == null)
                return;

            ApplyCore(sourceMaterial, result, null);
        }

        private static void ApplyCore(Material sourceMaterial, Material result, Material targetMaterial)
        {
            float srcOutline = sourceMaterial.GetFloat("_OutlineWidth");
            float srcOutlineSoftness = sourceMaterial.GetFloat("_OutlineSoftness");
            float srcUnderlay = sourceMaterial.GetFloat("_UnderlayDilate");
            float srcUnderlaySoftness = sourceMaterial.GetFloat("_UnderlaySoftness");
            float srcUnderlayOffsetX = sourceMaterial.GetFloat("_UnderlayOffsetX");
            float srcUnderlayOffsetY = sourceMaterial.GetFloat("_UnderlayOffsetY");
            float srcGlowInner = sourceMaterial.GetFloat("_GlowInner");
            float srcGlowOuter = sourceMaterial.GetFloat("_GlowOuter");
            float srcGlowOffset = sourceMaterial.GetFloat("_GlowOffset");
            float srcGlowPower = sourceMaterial.GetFloat("_GlowPower");
            float srcBevel = sourceMaterial.GetFloat("_Bevel");
            float srcBevelWidth = sourceMaterial.GetFloat("_BevelWidth");
            float srcBevelOffset = sourceMaterial.GetFloat("_BevelOffset");
            float srcBevelClamp = sourceMaterial.GetFloat("_BevelClamp");
            float srcBevelRoundness = sourceMaterial.GetFloat("_BevelRoundness");
            float srcFaceDilate = sourceMaterial.GetFloat("_FaceDilate");
            float srcFaceSoftness = sourceMaterial.HasProperty("_FaceSoftness") ? sourceMaterial.GetFloat("_FaceSoftness") : 0f;
            float srcGS = sourceMaterial.GetFloat("_GradientScale");
            float targetGS = targetMaterial != null ? targetMaterial.GetFloat("_GradientScale") : 0f;
            float scale = TranslatePlugin.fallbackEffectScale.Value;

            result.SetFloat("_OutlineWidth", srcOutline * scale);
            result.SetFloat("_OutlineSoftness", srcOutlineSoftness * scale);
            result.SetFloat("_UnderlayDilate", srcUnderlay * scale);
            result.SetFloat("_UnderlaySoftness", srcUnderlaySoftness * scale);
            result.SetFloat("_UnderlayOffsetX", srcUnderlayOffsetX * scale);
            result.SetFloat("_UnderlayOffsetY", srcUnderlayOffsetY * scale);
            result.SetFloat("_GlowInner", srcGlowInner * scale);
            result.SetFloat("_GlowOuter", srcGlowOuter * scale);
            result.SetFloat("_GlowOffset", srcGlowOffset * scale);
            result.SetFloat("_GlowPower", srcGlowPower * scale);
            result.SetFloat("_Bevel", srcBevel * scale);
            result.SetFloat("_BevelWidth", srcBevelWidth * scale);
            result.SetFloat("_BevelOffset", srcBevelOffset * scale);
            result.SetFloat("_BevelClamp", srcBevelClamp * scale);
            result.SetFloat("_BevelRoundness", srcBevelRoundness * scale);
            result.SetFloat("_FaceDilate", srcFaceDilate * scale);
            if (srcFaceSoftness != 0f) result.SetFloat("_FaceSoftness", srcFaceSoftness * scale);

            if (TranslatePlugin.showOtherDebug.Value)
            {
                int key = targetMaterial != null ? (sourceMaterial.GetInstanceID() ^ (targetMaterial.GetInstanceID() << 16)) : sourceMaterial.GetInstanceID();
                var now = DateTime.Now;
                CleanupLogCache();
                if (!_lastLogTime.TryGetValue(key, out var last) || now - last >= _logCooldown)
                {
                    _lastLogTime[key] = now;
                    string tgtDesc = targetMaterial != null ? $"{targetMaterial.name}#{targetMaterial.GetInstanceID()}" : "(atlasIndex)";
                    TranslatePlugin.logger.LogInfo(
                        $"[FallbackScale] srcMat={sourceMaterial.name}#{sourceMaterial.GetInstanceID()}, tgt={tgtDesc}, srcGS={srcGS}, tgtGS={targetGS}, scale={scale:F4}");
                    TranslatePlugin.logger.LogInfo(
                        $"[FallbackScale] Outline: width={srcOutline}→{result.GetFloat("_OutlineWidth"):F4}, color={sourceMaterial.GetColor("_OutlineColor")}, keyword={sourceMaterial.IsKeywordEnabled("OUTLINE_ON")}");
                    TranslatePlugin.logger.LogInfo(
                        $"[FallbackScale] Underlay: dilate={srcUnderlay}→{result.GetFloat("_UnderlayDilate"):F4}, color={sourceMaterial.GetColor("_UnderlayColor")}, keyword={sourceMaterial.IsKeywordEnabled("UNDERLAY_ON")}");
                    TranslatePlugin.logger.LogInfo(
                        $"[FallbackScale] Glow: inner={srcGlowInner}→{result.GetFloat("_GlowInner"):F4}, outer={srcGlowOuter}→{result.GetFloat("_GlowOuter"):F4}, color={sourceMaterial.GetColor("_GlowColor")}, keyword={sourceMaterial.IsKeywordEnabled("GLOW_ON")}");
                    TranslatePlugin.logger.LogInfo(
                        $"[FallbackScale] Face: dilate={sourceMaterial.GetFloat("_FaceDilate")}, color={sourceMaterial.GetColor("_FaceColor")}");
                    TranslatePlugin.logger.LogInfo(
                        $"[FallbackScale] Bevel: width={srcBevelWidth}→{result.GetFloat("_BevelWidth"):F4}");
                }
            }
        }
    }

    [HarmonyPatch]
    internal class TMP_FallbackMaterialHook_AtlasIndex
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("TMPro.TMP_MaterialManager"), "GetFallbackMaterial", new[] { AccessTools.TypeByName("TMPro.TMP_FontAsset"), typeof(Material), typeof(int) });
        }

        [HarmonyPostfix]
        [HarmonyWrapSafe]
        public static void Postfix(Material sourceMaterial, ref Material __result)
        {
            TMP_FallbackMaterialHook.ApplyScaleWithoutTarget(sourceMaterial, __result);
        }
    }
}
