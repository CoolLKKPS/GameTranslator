using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameTranslator.Patches.Utils
{
    internal static class TextureEnhancement
    {
        private static readonly int BaseColorMapId = Shader.PropertyToID("_BaseColorMap");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly List<Renderer> RendererBuffer = [];
        private static readonly List<Material> MaterialBuffer = [];
        private static bool _isProcessing;
        private static bool _initialized;
        private static readonly ConcurrentQueue<bool> RefreshRequests = [];
        internal static bool IsEnabled => TranslatePlugin.textureEnhancement || TranslatePlugin.textureEnhancementDump;
        internal static bool CanTranslate => TranslatePlugin.changeTexture.Value && TranslatePlugin.textureEnhancement && TranslateConfig.cache != null && TranslateConfig.cache.HasRegisteredImages;
        internal static bool CanDump => TranslatePlugin.textureEnhancementDump;
        internal static bool CanProcess => IsEnabled && (CanTranslate || CanDump);
        internal static bool IsProcessing => _isProcessing;

        internal static bool ContainsRenderer(UnityEngine.Object source)
        {
            var gameObject = (source as GameObject) ?? (source as Component)?.gameObject;
            if (gameObject == null)
            {
                return false;
            }
            RendererBuffer.Clear();
            gameObject.GetComponentsInChildren(true, RendererBuffer);
            var hasContentRenderers = false;
            for (var i = 0; i < RendererBuffer.Count; i++)
            {
                if (RendererBuffer[i] is MeshRenderer or SkinnedMeshRenderer)
                {
                    hasContentRenderers = true;
                    break;
                }
            }
            RendererBuffer.Clear();
            return hasContentRenderers;
        }

        internal static void Initialize()
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
            GameTranslator.Patches.Translatons.TextureTranslationCache.Reloaded += OnCacheReloaded;
        }

        private static void OnCacheReloaded()
        {
            RefreshRequests.Enqueue(true);
        }

        internal static void ProcessRefreshRequests()
        {
            if (RefreshRequests.IsEmpty || _isProcessing)
            {
                return;
            }

            while (RefreshRequests.TryDequeue(out _))
            {
            }

            if (!CanProcess)
            {
                return;
            }

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    SweepScene(scene);
                }
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!CanProcess || _isProcessing)
            {
                return;
            }
            SweepScene(scene);
        }

        private static void SweepScene(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null)
                {
                    ProcessObject(roots[i]);
                }
            }
        }

        internal static void ProcessObject(UnityEngine.Object source)
        {
            if (!CanProcess || source == null || _isProcessing)
            {
                return;
            }

            var gameObject = (source as GameObject) ?? (source as Component)?.gameObject;
            if (gameObject == null)
            {
                return;
            }

            RendererBuffer.Clear();
            gameObject.GetComponentsInChildren(true, RendererBuffer);
            for (var i = 0; i < RendererBuffer.Count; i++)
            {
                ProcessRenderer(RendererBuffer[i]);
            }
            RendererBuffer.Clear();
        }

        private static void ProcessRenderer(Renderer renderer)
        {
            if (renderer is null or not (MeshRenderer or SkinnedMeshRenderer))
            {
                return;
            }

            renderer.GetSharedMaterials(MaterialBuffer);
            if (MaterialBuffer.Count == 0)
            {
                return;
            }

            for (var i = 0; i < MaterialBuffer.Count; i++)
            {
                ProcessMaterialTextures(MaterialBuffer[i]);
            }
        }

        private static void ProcessMaterialTextures(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty(BaseColorMapId))
            {
                ProcessProperty(material, BaseColorMapId);
            }
            if (material.HasProperty(MainTexId))
            {
                ProcessProperty(material, MainTexId);
            }
        }

        private static void ProcessProperty(Material material, int propertyId)
        {
            if (material.GetTexture(propertyId) is not Texture2D texture)
            {
                return;
            }

            var original = texture;
            try
            {
                _isProcessing = true;
                TextureTranslate.Instance.Hook_ImageChanged(ref texture, false, TranslatePlugin.SceneDumpPath);
            }
            catch
            {
            }
            finally
            {
                _isProcessing = false;
            }

            if (!ReferenceEquals(texture, original) && texture != null)
            {
                material.SetTexture(propertyId, texture);
            }
        }
    }
}
