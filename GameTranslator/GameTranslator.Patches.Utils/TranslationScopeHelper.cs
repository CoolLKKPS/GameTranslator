using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using XUnity.Common.Constants;
using XUnity.Common.Logging;

namespace GameTranslator.Patches.Utils
{
    internal static class TranslationScopeHelper
    {
        public static int GetScope(object ui)
        {
            if (TranslationScopeHelper.EnableTranslationScoping)
            {
                try
                {
                    Component component = ui as Component;
                    if (component != null && component)
                    {
                        return TranslationScopeHelper.GetScopeFromComponent(component);
                    }
                    if (ui is GUIContent)
                    {
                        return -1;
                    }
                    return TranslationScopeHelper.GetActiveSceneId();
                }
                catch (MissingMemberException ex)
                {
                    XuaLogger.AutoTranslator.Error(ex, "A 'missing member' error occurred while retriving translation scope. Disabling translation scopes.");
                    TranslationScopeHelper.EnableTranslationScoping = false;
                }
                return -1;
            }
            return -1;
        }

        private static int GetScopeFromComponent(Component component)
        {
            return component.gameObject.scene.buildIndex;
        }

        public static int GetActiveSceneId()
        {
            return TranslationScopeHelper.GetActiveSceneIdBySceneManager();
        }

        private static int GetActiveSceneIdBySceneManager()
        {
            return SceneManager.GetActiveScene().buildIndex;
        }

        public static void RegisterSceneLoadCallback(Action<int> sceneLoaded)
        {
            UnityTypes.SceneManager_Methods.add_sceneLoaded(delegate (Scene scene, LoadSceneMode mode)
            {
                sceneLoaded(scene.buildIndex);
            });
            SceneManagerLoader.EnableSceneLoadScanInternal(sceneLoaded);
        }

        public static bool EnableTranslationScoping = true;

        internal static class TranslationScopes
        {
            public const int None = -1;
        }
    }
}
