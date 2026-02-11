using GameTranslator.Patches.Translatons;
using System;
using UnityEngine;

namespace GameTranslator
{
    public class GameTranslatorManager : MonoBehaviour
    {
        private static GameTranslatorManager _instance;

        public static GameTranslatorManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    CreateInstance();
                }
                return _instance;
            }
        }

        public static void CreateGameObject()
        {
            if (_instance == null)
            {
                CreateInstance();
            }
        }

        private static void CreateInstance()
        {
            var gameObject = new GameObject("GameTranslatorManager");
            _instance = gameObject.AddComponent<GameTranslatorManager>();
            DontDestroyOnLoad(gameObject);
            gameObject.hideFlags = HideFlags.HideAndDontSave;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            TranslatePlugin.logger?.LogInfo("GameTranslatorManager initialized");
        }

        private void Update()
        {
            try
            {
                AsyncTranslationManager.Instance.ProcessMainThreadActions();
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger?.LogError("Error in GameTranslatorManager Update: " + ex.Message);
            }
        }

        private void OnDestroy()
        {
            try
            {
                AsyncTranslationManager.Instance.Stop();
                _instance = null;
                TranslatePlugin.logger?.LogInfo("GameTranslatorManager destroyed");
            }
            catch (Exception ex)
            {
                TranslatePlugin.logger?.LogError("Error in GameTranslatorManager OnDestroy: " + ex.Message);
            }
        }
    }
}