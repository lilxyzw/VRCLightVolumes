using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using System;
using UnityEngine.Rendering;

namespace VRCLightVolumes {

    public static class TextureArrayGenerator {

        public static void CreateTexture2DArray(List<Texture> textures, int res, Action<Texture2DArray, int[]> onComplete) {

            if (textures == null || textures.Count == 0) {
                return;
            }

            int count = textures.Count;

            // Process every tex to rescale it or to convert cubemap into octahedtal cubemap format
            List<RenderTexture> processedTextures = new List<RenderTexture>();
            for (int i = 0; i < count; i++) {
                if (textures[i].GetType() == typeof(Cubemap)) {
                    var cube = (Cubemap)textures[i];
                    processedTextures.Add(CreateOctahedralCubemap(cube, res));
                } else if (textures[i].GetType() == typeof(Texture2D)) {
                    var tex = (Texture2D)textures[i];
                    processedTextures.Add(CreateTexture(tex, res));
                }
            }

            var requests = new List<AsyncGPUReadbackRequest>(count);
            var hashes = new Hash128[count];
            var texCache = new RenderTexture[count];

            int finished = 0;
            bool failed = false;

            for (int i = 0; i < count; i++) {
                int index = i;

                var request = AsyncGPUReadback.Request(processedTextures[i], 0, r => {

                    if (r.hasError) {
                        Debug.LogError($"Readback error at index {index}");
                        failed = true;
                        return;
                    }

                    var data = r.GetData<byte>();
                    HashUtilities.ComputeHash128(ref data, ref hashes[index]);
                    texCache[index] = processedTextures[index];

                    finished++;
                    if (finished == count && !failed) {
                        Finalize(hashes, texCache, onComplete);
                    }

                });

                requests.Add(request);
            }
        }

        private static void Finalize(Hash128[] hashes, RenderTexture[] texCache, Action<Texture2DArray, int[]> onComplete) {

            int count = texCache.Length;
            Dictionary<Hash128, int> hashToIndex = new Dictionary<Hash128, int>();
            List<RenderTexture> uniqueTexs = new List<RenderTexture>();
            int[] origToUnique = new int[count];

            for (int i = 0; i < count; i++) {
                if (hashToIndex.TryGetValue(hashes[i], out int existingIndex)) {
                    origToUnique[i] = existingIndex;
                } else {
                    int newIndex = uniqueTexs.Count;
                    hashToIndex[hashes[i]] = newIndex;
                    uniqueTexs.Add(texCache[i]);
                    origToUnique[i] = newIndex;
                }
            }

            int width = texCache[0].width;
            int height = texCache[0].height;
            var format = GraphicsFormat.R32G32B32A32_SFloat;
            var texArray = new Texture2DArray(width, height, uniqueTexs.Count, format, TextureCreationFlags.None);
            texArray.wrapMode = TextureWrapMode.Clamp;
            texArray.filterMode = FilterMode.Trilinear;
            texArray.anisoLevel = 0;

            for (int slice = 0; slice < uniqueTexs.Count; slice++) {
                var rt = uniqueTexs[slice];
                var temp = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true);
                RenderTexture.active = rt;
                temp.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                temp.Apply();
                Graphics.CopyTexture(temp, 0, 0, texArray, slice, 0);
                UnityEngine.Object.DestroyImmediate(temp);
            }

            RenderTexture.active = null;
            onComplete?.Invoke(texArray, origToUnique);

        }

        public static RenderTexture CreateOctahedralCubemap(Cubemap cubemap, int res, int padding = 1) {
            Material mat = new Material(Shader.Find("Hidden/CubeToOct"));
            mat.SetFloat("_Padding", padding);
            mat.SetFloat("_TextureSize", res);
            mat.SetTexture("_CubeTex", cubemap);
            RenderTexture rt = new RenderTexture(res, res, 0, RenderTextureFormat.ARGBFloat, 0);
            rt.Create();
            Graphics.Blit(null, rt, mat);
            return rt;
        }

        public static RenderTexture CreateTexture(Texture2D tex, int res) {
            RenderTexture rt = new RenderTexture(res, res, 0, RenderTextureFormat.ARGBFloat, 0);
            rt.Create();
            Graphics.Blit(tex, rt);
            return rt;
        }

    }

}