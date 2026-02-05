using System;
using UnityEngine.SceneManagement;

namespace GameTranslator.Patches.Utils
{
    internal static class SceneManagerLoader
    {
        public static void EnableSceneLoadScanInternal(Action<int> sceneLoaded)
        {
            SceneManager.sceneLoaded += delegate (Scene arg1, LoadSceneMode arg2)
            {
                sceneLoaded(arg1.buildIndex);
            };
        }
    }
}
