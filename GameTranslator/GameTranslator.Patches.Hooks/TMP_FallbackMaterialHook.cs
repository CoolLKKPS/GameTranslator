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
            if (!TranslatePlugin.scaleFallbackEffects.Value || __result == null || sourceMaterial == null || targetMaterial == null)
                return;

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
            float targetGS = targetMaterial.GetFloat("_GradientScale");
            float scale = TranslatePlugin.fallbackEffectScale.Value;
            __result.SetFloat("_OutlineWidth", srcOutline * scale);
            __result.SetFloat("_OutlineSoftness", srcOutlineSoftness * scale);
            __result.SetFloat("_UnderlayDilate", srcUnderlay * scale);
            __result.SetFloat("_UnderlaySoftness", srcUnderlaySoftness * scale);
            __result.SetFloat("_UnderlayOffsetX", srcUnderlayOffsetX * scale);
            __result.SetFloat("_UnderlayOffsetY", srcUnderlayOffsetY * scale);
            __result.SetFloat("_GlowInner", srcGlowInner * scale);
            __result.SetFloat("_GlowOuter", srcGlowOuter * scale);
            __result.SetFloat("_GlowOffset", srcGlowOffset * scale);
            __result.SetFloat("_GlowPower", srcGlowPower * scale);
            __result.SetFloat("_Bevel", srcBevel * scale);
            __result.SetFloat("_BevelWidth", srcBevelWidth * scale);
            __result.SetFloat("_BevelOffset", srcBevelOffset * scale);
            __result.SetFloat("_BevelClamp", srcBevelClamp * scale);
            __result.SetFloat("_BevelRoundness", srcBevelRoundness * scale);
            __result.SetFloat("_FaceDilate", srcFaceDilate * scale);
            if (srcFaceSoftness != 0f) __result.SetFloat("_FaceSoftness", srcFaceSoftness * scale);

            if (TranslatePlugin.showOtherDebug.Value)
            {
                int key = sourceMaterial.GetInstanceID() ^ (targetMaterial.GetInstanceID() << 16);
                var now = DateTime.Now;
                CleanupLogCache();
                if (!_lastLogTime.TryGetValue(key, out var last) || now - last >= _logCooldown)
                {
                    _lastLogTime[key] = now;
                    TranslatePlugin.logger.LogInfo(
                        $"[FallbackScale] srcMat={sourceMaterial.name}#{sourceMaterial.GetInstanceID()}, tgtMat={targetMaterial.name}#{targetMaterial.GetInstanceID()}, srcGS={srcGS}, tgtGS={targetGS}, scale={scale:F4}");
                    TranslatePlugin.logger.LogInfo(
                        $"[FallbackScale] Outline: width={srcOutline}→{__result.GetFloat("_OutlineWidth"):F4}, color={sourceMaterial.GetColor("_OutlineColor")}, keyword={sourceMaterial.IsKeywordEnabled("OUTLINE_ON")}");
                    TranslatePlugin.logger.LogInfo(
                        $"[FallbackScale] Underlay: dilate={srcUnderlay}→{__result.GetFloat("_UnderlayDilate"):F4}, color={sourceMaterial.GetColor("_UnderlayColor")}, keyword={sourceMaterial.IsKeywordEnabled("UNDERLAY_ON")}");
                    TranslatePlugin.logger.LogInfo(
                        $"[FallbackScale] Glow: inner={srcGlowInner}→{__result.GetFloat("_GlowInner"):F4}, outer={srcGlowOuter}→{__result.GetFloat("_GlowOuter"):F4}, color={sourceMaterial.GetColor("_GlowColor")}, keyword={sourceMaterial.IsKeywordEnabled("GLOW_ON")}");
                    TranslatePlugin.logger.LogInfo(
                        $"[FallbackScale] Face: dilate={sourceMaterial.GetFloat("_FaceDilate")}, color={sourceMaterial.GetColor("_FaceColor")}");
                    TranslatePlugin.logger.LogInfo(
                        $"[FallbackScale] Bevel: width={srcBevelWidth}→{__result.GetFloat("_BevelWidth"):F4}");
                }
            }
        }
    }
}
