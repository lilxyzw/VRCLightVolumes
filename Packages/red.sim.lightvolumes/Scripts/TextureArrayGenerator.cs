using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using System.Collections;

namespace VRCLightVolumes {

    public static class TextureArrayGenerator {

        // Creates a Texture2DArray from a Texture2D or a Cubemaps list. Deduplicates everything if the assets are the same and also returns an array of uniique IDs in the texture array for each of the source textures
        // Note that uniqueIDs length will not be the same as Texture2DArray lenght if there are Cubemaps represented in the source list. One cubemap uses 6 Texture2DArray slots.
        public static IEnumerator CreateTexture2DArrayAsync(List<Texture> textures, int res, System.Action<Texture2DArray, int[]> onComplete) {

            int count = textures.Count;
            if (textures == null || count == 0) {
                yield break;
            }

            // Deduplication
            var uniqueIDs = new int[count]; // IDs in unique textures list for each source texture element 
            List<Texture> uniqueTextures = new List<Texture>(); // Deduplicated textures list
            for (int i  = 0; i < count; i++) {
                int id = uniqueTextures.IndexOf(textures[i]);
                if(id == -1) { // It's a new unique texture
                    uniqueTextures.Add(textures[i]);
                    uniqueIDs[i] = uniqueTextures.Count - 1;
                } else { // This is a duplicated texture
                    uniqueIDs[i] = id;
                }
                yield return null;
            }
            int uniqueCount = uniqueTextures.Count;

            // Rescaling and bliting textures into render textures
            List<RenderTexture> processedTextures = new List<RenderTexture>();
            for (int i = 0; i < uniqueCount; i++) {
                if(uniqueTextures[i].GetType() == typeof(Cubemap)) {
                    Cubemap cube = (Cubemap)uniqueTextures[i];
                    processedTextures.AddRange(CreateCubemap(cube, res));
                } else if (uniqueTextures[i].GetType() == typeof(Texture2D)) {
                    Texture2D tex = (Texture2D)uniqueTextures[i];
                    processedTextures.Add(CreateTexture(tex, res));
                }
                yield return null;
            }
            int processedCount = processedTextures.Count;

            // Copying data from render textures to Texture2DArray
            Texture2DArray array = new Texture2DArray(res, res, processedCount, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None, 0);
            array.anisoLevel = 0;
            array.filterMode = FilterMode.Trilinear;
            array.wrapMode = TextureWrapMode.Clamp;
            Texture2D temp = new Texture2D(res, res, TextureFormat.RGBAFloat, false, true); // Temp texture to read pixels from RT to
            for (int i = 0; i < processedCount; i++) {
                RenderTexture.active = processedTextures[i];
                temp.ReadPixels(new Rect(0, 0, res, res), 0, 0);
                temp.Apply(false);
                Graphics.CopyTexture(temp, 0, 0, array, i, 0);
                yield return null;
            }
            array.Apply(false);

            // Release and destroy the textures
            RenderTexture.active = null;
            Object.DestroyImmediate(temp);
            for (int i = 0; i < processedCount; i++) {
                processedTextures[i].Release();
                Object.DestroyImmediate(processedTextures[i]);
            }

            onComplete?.Invoke(array, uniqueIDs);

        }

        // Blits all cubemap faces into a render texture array with specified resolution
        public static RenderTexture[] CreateCubemap(Cubemap cubemap, int res) {
            RenderTexture[] rt = new RenderTexture[6];
            Material mat = new Material(Shader.Find("Hidden/CubeFace"));
            mat.SetTexture("_MainTex", cubemap);
            for (int i = 0; i < 6; i++) {
                mat.SetInt("_FaceIndex", i);
                rt[i] = new RenderTexture(res, res, 0);
                rt[i].graphicsFormat = GraphicsFormat.R32G32B32A32_SFloat;
                rt[i].enableRandomWrite = false;
                rt[i].Create();
                Graphics.Blit(null, rt[i], mat);
            }
            Graphics.SetRenderTarget(null);
            Object.DestroyImmediate(mat);
            return rt;
        }

        // Blits texture into a render texture with specified resolution
        public static RenderTexture CreateTexture(Texture2D tex, int res) {
            RenderTexture rt = new RenderTexture(res, res, 0);
            rt.graphicsFormat = GraphicsFormat.R32G32B32A32_SFloat;
            rt.enableRandomWrite = false;
            rt.Create();
            Graphics.Blit(tex, rt);
            Graphics.SetRenderTarget(null);
            return rt;
        }

    }

}