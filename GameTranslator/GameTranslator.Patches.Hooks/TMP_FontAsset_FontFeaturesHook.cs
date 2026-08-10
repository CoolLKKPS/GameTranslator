using HarmonyLib;
using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace GameTranslator.Patches.Hooks
{
    [HarmonyPatch]
    internal class TMP_FontAsset_FontFeaturesHook
    {
        private static readonly MethodInfo _loadFontFaceMethod;
        private static readonly MethodInfo _fontEngineLoadFace;

        private static readonly PropertyInfo _sourceFontFileProp;
        private static readonly PropertyInfo _faceInfoProp;
        private static PropertyInfo _pointSizeProp;

        static TMP_FontAsset_FontFeaturesHook()
        {
            _loadFontFaceMethod = Enum.IsDefined(typeof(AtlasPopulationMode), "DynamicOS") ? AccessTools.Method(typeof(TMP_FontAsset), "LoadFontFace", Type.EmptyTypes, null) : null;

            if (_loadFontFaceMethod != null)
                return;

            _faceInfoProp = AccessTools.Property(typeof(TMP_FontAsset), "faceInfo") ?? AccessTools.Property(typeof(TMP_Asset), "faceInfo");

            _sourceFontFileProp = AccessTools.Property(typeof(TMP_FontAsset), "sourceFontFile");

            _fontEngineLoadFace = Enum.IsDefined(typeof(AtlasPopulationMode), "DynamicOS")
            ? AccessTools.Method(typeof(FontEngine), "LoadFontFace", new[] { typeof(Font), typeof(float) })
            : AccessTools.Method(typeof(FontEngine), "LoadFontFace", new[] { typeof(Font), typeof(int) });

            if (_faceInfoProp != null)
            {
                var fiType = _faceInfoProp.PropertyType;
                _pointSizeProp = AccessTools.Property(fiType, "pointSize");
            }
        }

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(TMP_FontAsset), "UpdateGlyphAdjustmentRecords", Type.EmptyTypes, null);
        }

        [HarmonyPrefix]
        [HarmonyWrapSafe]
        public static bool EnsureFontFaceLoaded(TMP_FontAsset __instance)
        {
            if (__instance == null)
                return true;

            if (__instance.atlasPopulationMode == AtlasPopulationMode.Static)
                return true;

            try
            {
                if (_loadFontFaceMethod != null)
                {
                    object result = _loadFontFaceMethod.Invoke(__instance, null);
                    return (int)result == 0;
                }

                if (_fontEngineLoadFace != null && _sourceFontFileProp != null)
                {
                    var sourceFont = _sourceFontFileProp.GetValue(__instance) as Font;
                    if (sourceFont != null && _faceInfoProp != null)
                    {
                        var faceInfo = _faceInfoProp.GetValue(__instance);
                        if (faceInfo != null && _pointSizeProp != null)
                        {
                            var pointSize = _pointSizeProp.GetValue(faceInfo);
                            if (pointSize != null)
                            {
                                object result = _fontEngineLoadFace.Invoke(null, new[] { sourceFont, pointSize });
                                return (int)result == 0;
                            }
                        }
                    }
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
