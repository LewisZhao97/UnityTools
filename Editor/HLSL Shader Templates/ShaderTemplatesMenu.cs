using UnityEditor;

namespace Lewiszhao.Unitytools.Editor
{
    internal static class ShaderTemplatesMenu
    {
        private const string k_OpaqueUnlitPlusShader =
            "Packages/com.lewiszhao.unitytools/Editor/HLSL Shader Templates/ShaderTemplates/Opaque Unlit+ Shader.shader.txt";

        [MenuItem("Assets/Create/Shader/Opaque Unlit+ Shader", false)]
        public static void CreateUrpUnlitPlusShader()
        {
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(
                k_OpaqueUnlitPlusShader,
                "Opaque Unlit+ Shader.shader"
            );
        }

        private const string k_TransparentUnlitShader =
            "Packages/com.lewiszhao.unitytools/Editor/HLSL Shader Templates/ShaderTemplates/Transparent Unlit Shader.shader.txt";

        [MenuItem("Assets/Create/Shader/Transparent Unlit Shader", false)]
        public static void CreateUrpTransparentUnlitShader()
        {
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(
                k_TransparentUnlitShader,
                "Transparent Unlit Shader.shader"
            );
        }

        private const string k_SimpleLitShader =
            "Packages/com.lewiszhao.unitytools/Editor/HLSL Shader Templates/ShaderTemplates/Simple Lit Shader.shader.txt";

        [MenuItem("Assets/Create/Shader/Simple Lit Shader", false)]
        public static void CreateUrpSimpleLitShader()
        {
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(
                k_SimpleLitShader,
                "Simple Lit Shader.shader");
        }

        private const string k_PbrLitShader =
            "Packages/com.lewiszhao.unitytools/Editor/HLSL Shader Templates/ShaderTemplates/PBR Lit Shader.shader.txt";

        private const string k_PbrInput =
            "Packages/com.lewiszhao.unitytools/Editor/HLSL Shader Templates/ShaderTemplates/PBRInput.hlsl.txt";

        private const string k_PbrSurface =
            "Packages/com.lewiszhao.unitytools/Editor/HLSL Shader Templates/ShaderTemplates/PBRSurface.hlsl.txt";

        [MenuItem("Assets/Create/Shader/PBR Lit Shader", false)]
        public static void CreateUrpPbrLitShader()
        {
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(
                k_PbrInput,
                "PBRInput.hlsl");
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(
                k_PbrSurface,
                "PBRSurface.hlsl");
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(
                k_PbrLitShader,
                "PBR Lit Shader.shader");
        }

        private const string k_DiffuseLitShader =
            "Packages/com.lewiszhao.unitytools/Editor/HLSL Shader Templates/ShaderTemplates/Diffuse Lit Shader.shader.txt";

        [MenuItem("Assets/Create/Shader/Diffuse Lit Shader", false)]
        public static void CreateUrpDiffuseLitShader()
        {
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(
                k_DiffuseLitShader,
                "Diffuse Lit Shader.shader");
        }
    }
}