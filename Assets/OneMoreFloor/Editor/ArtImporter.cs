using UnityEditor;
using UnityEngine;

namespace OneMoreFloor.Editor
{
    // Applies the same sprite settings when the art is copied into an existing project.
    public sealed class ArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/OneMoreFloor/Resources/Art/")) return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;
            importer.mipmapEnabled=false; importer.alphaIsTransparency=true;
            importer.filterMode=FilterMode.Bilinear;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=2048;
            importer.spritePixelsPerUnit=100;
            importer.spriteBorder=assetPath.EndsWith("/panel.png") ? new Vector4(24,24,24,24) : Vector4.zero;
        }
    }
}
