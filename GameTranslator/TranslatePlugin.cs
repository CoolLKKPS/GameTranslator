using BepInEx;
#if CORECLR
using BepInEx.Configuration;
using System.IO;
using UnityEngine;
#endif

namespace GameTranslator
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
#if CORECLR
    public class TranslatePlugin : MonoBehaviour
#else
    public class TranslatePlugin : BaseUnityPlugin
#endif
    {
        private void Awake()
        {
#if CORECLR
            var configPath = Path.Combine(Application.dataPath, "..", "BepInEx", "config", "GameTranslator.cfg");
            var metadata = new BepInEx.BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION);
            var config = new ConfigFile(configPath, false, metadata);
            var logger = BepInEx.Logging.Logger.CreateLogSource(PLUGIN_NAME);
#else
            var config = base.Config;
            var logger = base.Logger;
#endif
            GameTranslatorCore.Bootstrap(config, logger);
        }

        private const string PLUGIN_GUID = "GameTranslator";

        internal const string PLUGIN_NAME = "GameTranslator";

        internal const string PLUGIN_VERSION = "2.4.1";

        internal const string PLUGIN_VERSION_FULL = PLUGIN_VERSION + ".0";
    }
}
