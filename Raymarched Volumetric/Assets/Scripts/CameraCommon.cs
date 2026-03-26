using UnityEngine;

namespace CameraCommon
{
    public static class BB_Rendering
    {
        public static bool ShaderMaterialReady(Shader _shader, ref Material _material)
        {
            if (!_shader)
                return false;

            if (!_material)
            {
                _material = new Material(_shader);
                _material.hideFlags = HideFlags.HideAndDontSave;
            }

            return true;
        }

        public static bool IsKeywordEnabled(string _keyword, Material _material)
        {
            return _material.IsKeywordEnabled(_keyword);
        }
        public static Material BB_SetShaderKeyword(this Material _material, string _keyword, bool _state)
        {
            if (_state)
            {
                _material.EnableKeyword(_keyword);
            }
            else
            {
                _material.DisableKeyword(_keyword);
            }

            return _material;
        }
    }
}

