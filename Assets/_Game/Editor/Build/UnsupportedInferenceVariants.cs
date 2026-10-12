using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProtocoloZero.Build
{
    // ConvGeneric declares the LT_ANYVARIANTS transposed-output combination
    // unsupported. Keep ungrouped and MINDIV_64 variants and every other shader.
    public sealed class UnsupportedInferenceVariants : IPreprocessComputeShaders
    {
        public int callbackOrder => -100;

        public void OnProcessComputeShader(ComputeShader shader, string kernelName,
            IList<ShaderCompilerData> data)
        {
            if (AssetDatabase.GetAssetPath(shader) !=
                "Packages/com.unity.ai.inference/Runtime/Core/Resources/Sentis/ComputeShaders/ConvGeneric.compute")
                return;
            switch (kernelName)
            {
                case "Conv1D_OutTransposed_4eoc_4esp":
                case "Conv1DComplex_OutTransposed_4eoc_4esp":
                case "Conv1D_Scaled_OutTransposed_4eoc_4esp":
                case "Conv1D_Scaled_Complex_OutTransposed_4eoc_4esp":
                case "Conv1D_OutTransposedDouble_4eoc_4esp":
                    break;
                default:
                    return;
            }
            var unsupported = new ShaderKeyword(shader, "GROUP_OC_PER_GROUP_LT_ANYVARIANTS");
            int removed = 0;
            for (int i = data.Count - 1; i >= 0; i--)
            {
                if (!data[i].shaderKeywordSet.IsEnabled(unsupported)) continue;
                data.RemoveAt(i);
                removed++;
            }
            if (removed > 0)
                Debug.Log($"[PA4-SHADER] {kernelName}: stripped {removed} unsupported LT_ANYVARIANTS; retained {data.Count}.");
        }
    }
}
