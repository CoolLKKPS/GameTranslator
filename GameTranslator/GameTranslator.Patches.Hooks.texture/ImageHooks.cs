using System;

namespace GameTranslator.Patches.Hooks.texture
{
    internal static class ImageHooks
    {
        public static readonly Type[] All = new Type[]
        {
            typeof(MaskableGraphic_OnEnable_Hook),
            typeof(Image_sprite_Hook),
            typeof(Image_overrideSprite_Hook),
            typeof(Image_material_Hook),
            typeof(RawImage_texture_Hook),
            typeof(Cursor_SetCursor_Hook),
            typeof(Material_mainTexture_Hook),
            typeof(CubismRenderer_MainTexture_Hook),
            typeof(CubismRenderer_TryInitialize_Hook),
            typeof(UIAtlas_spriteMaterial_Hook),
            typeof(UISprite_OnInit_Hook),
            typeof(UISprite_material_Hook),
            typeof(UISprite_atlas_Hook),
            typeof(UI2DSprite_sprite2D_Hook),
            typeof(UI2DSprite_material_Hook),
            typeof(UITexture_mainTexture_Hook),
            typeof(UITexture_material_Hook),
            typeof(UIPanel_clipTexture_Hook),
            typeof(UIRect_OnInit_Hook),
            typeof(DicingTextures_GetTexture_Hook)
        };

        public static readonly Type[] Sprite = new Type[] { typeof(Sprite_texture_Hook) };

        public static readonly Type[] SpriteRenderer = new Type[] { typeof(SpriteRenderer_sprite_Hook) };
    }
}
