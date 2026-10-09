using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace FightAgainstIsrael.Editor
{
    // Runtime models use just opaque, emissive, and alpha-blended Standard materials.
    // Keep their variants while excluding features this project never uses.
    public sealed class CombatShaderVariants : IPreprocessShaders
    {
        public int callbackOrder => 0;
        private static readonly string[] UnusedKeywords =
        {
            "_NORMALMAP", "_METALLICGLOSSMAP", "_DETAIL_MULX2", "_PARALLAXMAP",
            "_SPECGLOSSMAP", "_ALPHATEST_ON", "_ALPHAPREMULTIPLY_ON",
            "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A", "LIGHTMAP_ON", "DYNAMICLIGHTMAP_ON",
            "DIRLIGHTMAP_COMBINED", "LIGHTMAP_SHADOW_MIXING", "SHADOWS_SHADOWMASK",
            "FOG_LINEAR", "FOG_EXP", "FOG_EXP2", "VERTEXLIGHT_ON"
        };

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (shader.name != "Standard") return;
            if (snippet.passName == "FORWARD_DELTA" || snippet.passName == "DEFERRED")
            {
                data.Clear();
                return;
            }
            for (int index = data.Count - 1; index >= 0; index--)
            {
                foreach (string name in UnusedKeywords)
                {
                    var keyword = new ShaderKeyword(shader, name);
                    if (!data[index].shaderKeywordSet.IsEnabled(keyword)) continue;
                    data.RemoveAt(index);
                    break;
                }
            }
        }
    }
}
