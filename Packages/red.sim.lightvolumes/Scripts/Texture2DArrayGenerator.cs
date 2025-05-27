using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using Unity.Collections;

namespace VRCLightVolumes {

    public static class Texture2DArrayGenerator {
        
        public static Texture2DArray CreateTexture2DArray(List<Texture2D> textures, int width, int height, out int[] ids) {
            
            if (textures == null || textures.Count == 0) {
                Debug.LogError("[Texture2D Array Generator] No textures provided!");
                ids = new int[0];
                return null;
            }

            int count = textures.Count;


            // Deduplication of same textures 2D
            var keyToUnique = new Dictionary<string, int>();
            var uniqueTexs = new List<Texture2D>();
            var origToUnique = new int[count];

            for (int i = 0; i < count; ++i) {
                Texture2D t = textures[i];
                NativeArray<byte> raw = t.GetPixelData<byte>(0);
                Hash128 hash = Hash128.Compute(raw);
                string key = "" + hash;

                if (!keyToUnique.TryGetValue(key, out int uIdx)) {
                    uIdx = uniqueTexs.Count;
                    uniqueTexs.Add(t);
                    keyToUnique.Add(key, uIdx);
                }
                origToUnique[i] = uIdx;
            }

            ids = origToUnique;
            int uniqueCount = uniqueTexs.Count;

            var format = GraphicsFormat.R32G32B32_SFloat;
            var texArray = new Texture2DArray(width, height, uniqueCount, format, TextureCreationFlags.None) {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear
            };

            // Temporary RT for GPU-side resampling and conversion
            RenderTexture rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat) {
                enableRandomWrite = false,
                useMipMap = false,
                autoGenerateMips = false,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear
            };
            rt.Create();

            // Intermediate readable texture
            Texture2D tempTex = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true);

            for (int i = 0; i < uniqueCount; i++) {
                var src = uniqueTexs[i];
                Graphics.Blit(src, rt);

                RenderTexture.active = rt;
                tempTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tempTex.Apply();

                Color[] pixels = tempTex.GetPixels();
                texArray.SetPixels(pixels, i);
            }

            RenderTexture.active = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tempTex);

            texArray.Apply(false, false);

            return texArray;
        }
    }

}