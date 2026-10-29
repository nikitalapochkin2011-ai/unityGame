using UnityEditor;
using UnityEngine;

namespace OneMoreFloor.Editor
{
    // Applies the same sprite settings when the art is copied into an existing project.
    public sealed class ArtImporter : AssetPostprocessor
    {
        public override uint GetVersion() { return 2; }
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/OneMoreFloor/Resources/Art/")) return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;
            bool illustration=!(assetPath.EndsWith("/panel.png") || assetPath.EndsWith("/circle.png") || assetPath.EndsWith("/triangle.png") || assetPath.EndsWith("/gear.png"));
            importer.mipmapEnabled=illustration; importer.alphaIsTransparency=true;
            importer.filterMode=illustration ? FilterMode.Trilinear : FilterMode.Bilinear;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=2048;
            importer.spritePixelsPerUnit=200;
            importer.spriteBorder=assetPath.EndsWith("/panel.png") ? new Vector4(48,48,48,48) : Vector4.zero;
            foreach(string platform in new[]{"Standalone","Android","iPhone"})
            {
                var settings=importer.GetPlatformTextureSettings(platform);
                settings.overridden=true; settings.maxTextureSize=2048;
                settings.format=TextureImporterFormat.RGBA32;
                settings.textureCompression=TextureImporterCompression.Uncompressed;
                settings.crunchedCompression=false;
                importer.SetPlatformTextureSettings(settings);
            }
        }
    }
}
