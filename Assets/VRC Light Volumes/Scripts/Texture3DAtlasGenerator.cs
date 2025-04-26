using System.Collections.Generic;
using UnityEngine;

public struct Atlas3D {
    public Texture3D Texture;
    public Vector3[] BoundsUvwMin;
    public Vector3[] BoundsUvwMax;
}

public static class Texture3DAtlasGenerator {
    public static Atlas3D CreateAtlas(Texture3D[] textures) {

        // Lianerizing SH
        Texture3D[] texs = new Texture3D[textures.Length];
        for (int i = 0; i < textures.Length / 3; i++) {
            Texture3D[] bundle = { textures[i * 3], textures[i * 3 + 1], textures[i * 3 + 2] };
            bundle = LinearizeSphericalHarmonics(bundle);
            texs[i * 3] = bundle[0];
            texs[i * 3 + 1] = bundle[1];
            texs[i * 3 + 2] = bundle[2];
        }

        int padding = 1;
        int count = texs.Length;

        var blocks = new List<(int index, int w, int h, int d)>();
        for (int i = 0; i < count; i++)
            blocks.Add((i, texs[i].width, texs[i].height, texs[i].depth));

        blocks.Sort((a, b) => (b.w * b.h * b.d).CompareTo(a.w * a.h * a.d));

        var placed = new List<(int x, int y, int z, int w, int h, int d, int index)>();
        int atlasW = 0, atlasH = 0, atlasD = 0;

        foreach (var block in blocks) {
            int w = block.w + padding * 2;
            int h = block.h + padding * 2;
            int d = block.d + padding * 2;

            Vector3Int bestPos = Vector3Int.zero;
            int bestVolume = int.MaxValue;

            List<int> xCandidates = new List<int> { 0 };
            List<int> yCandidates = new List<int> { 0 };
            List<int> zCandidates = new List<int> { 0 };

            foreach (var p in placed) {
                xCandidates.Add(p.x + p.w);
                yCandidates.Add(p.y + p.h);
                zCandidates.Add(p.z + p.d);
            }

            foreach (int x in xCandidates)
                foreach (int y in yCandidates)
                    foreach (int z in zCandidates) {
                        bool collides = false;
                        foreach (var p in placed) {
                            if (x < p.x + p.w && x + w > p.x &&
                                y < p.y + p.h && y + h > p.y &&
                                z < p.z + p.d && z + d > p.z) {
                                collides = true;
                                break;
                            }
                        }

                        if (collides) continue;

                        int newW = Mathf.Max(atlasW, x + w);
                        int newH = Mathf.Max(atlasH, y + h);
                        int newD = Mathf.Max(atlasD, z + d);
                        int volume = newW * newH * newD;

                        if (volume < bestVolume) {
                            bestVolume = volume;
                            bestPos = new Vector3Int(x, y, z);
                        }
                    }

            placed.Add((bestPos.x, bestPos.y, bestPos.z, w, h, d, block.index));
            atlasW = Mathf.Max(atlasW, bestPos.x + w);
            atlasH = Mathf.Max(atlasH, bestPos.y + h);
            atlasD = Mathf.Max(atlasD, bestPos.z + d);
        }

        var atlasPixels = new Color[atlasW * atlasH * atlasD];

        Vector3[] boundsMin = new Vector3[count];
        Vector3[] boundsMax = new Vector3[count];

        foreach (var p in placed) {
            Texture3D tex = texs[p.index];
            Color[] colors = tex.GetPixels();
            int w = tex.width;
            int h = tex.height;
            int d = tex.depth;

            for (int z = 0; z < d; z++) {
                for (int y = 0; y < h; y++) {
                    for (int x = 0; x < w; x++) {
                        int srcIndex = x + y * w + z * w * h;
                        int dstX = p.x + x + padding;
                        int dstY = p.y + y + padding;
                        int dstZ = p.z + z + padding;
                        int dstIndex = dstX + dstY * atlasW + dstZ * atlasW * atlasH;
                        atlasPixels[dstIndex] = colors[srcIndex];
                    }
                }
            }

            // Padding: duplicate edge pixels
            // X axis
            for (int z = 0; z < d; z++) {
                for (int y = 0; y < h; y++) {
                    atlasPixels[(p.x + 0) + (p.y + padding + y) * atlasW + (p.z + padding + z) * atlasW * atlasH] =
                        atlasPixels[(p.x + padding) + (p.y + padding + y) * atlasW + (p.z + padding + z) * atlasW * atlasH];

                    atlasPixels[(p.x + padding + w) + (p.y + padding + y) * atlasW + (p.z + padding + z) * atlasW * atlasH] =
                        atlasPixels[(p.x + padding + w - 1) + (p.y + padding + y) * atlasW + (p.z + padding + z) * atlasW * atlasH];
                }
            }
            // Y axis
            for (int z = 0; z < d; z++) {
                for (int x = 0; x < w; x++) {
                    atlasPixels[(p.x + padding + x) + (p.y + 0) * atlasW + (p.z + padding + z) * atlasW * atlasH] =
                        atlasPixels[(p.x + padding + x) + (p.y + padding) * atlasW + (p.z + padding + z) * atlasW * atlasH];

                    atlasPixels[(p.x + padding + x) + (p.y + padding + h) * atlasW + (p.z + padding + z) * atlasW * atlasH] =
                        atlasPixels[(p.x + padding + x) + (p.y + padding + h - 1) * atlasW + (p.z + padding + z) * atlasW * atlasH];
                }
            }
            // Z axis
            for (int y = 0; y < h; y++) {
                for (int x = 0; x < w; x++) {
                    atlasPixels[(p.x + padding + x) + (p.y + padding + y) * atlasW + (p.z + 0) * atlasW * atlasH] =
                        atlasPixels[(p.x + padding + x) + (p.y + padding + y) * atlasW + (p.z + padding) * atlasW * atlasH];

                    atlasPixels[(p.x + padding + x) + (p.y + padding + y) * atlasW + (p.z + padding + d) * atlasW * atlasH] =
                        atlasPixels[(p.x + padding + x) + (p.y + padding + y) * atlasW + (p.z + padding + d - 1) * atlasW * atlasH];
                }
            }

            boundsMin[p.index] = new Vector3(
                (float)(p.x + padding) / atlasW,
                (float)(p.y + padding) / atlasH,
                (float)(p.z + padding) / atlasD
            );
            boundsMax[p.index] = new Vector3(
                (float)(p.x + padding + w) / atlasW,
                (float)(p.y + padding + h) / atlasH,
                (float)(p.z + padding + d) / atlasD
            );
        }

        Texture3D atlasTexture = new Texture3D(atlasW, atlasH, atlasD, TextureFormat.RGBAHalf, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        LVUtils.Apply3DTextureData(atlasTexture, atlasPixels);
        return new Atlas3D { Texture = atlasTexture, BoundsUvwMin = boundsMin, BoundsUvwMax = boundsMax };

    }

    private static Texture3D[] LinearizeSphericalHarmonics(Texture3D[] texs) {

        int x = texs[0].width;
        int y = texs[0].height;
        int z = texs[0].depth;

        TextureFormat format = TextureFormat.RGBAHalf;
        Texture3D[] t = new Texture3D[3];
        t[0] = new Texture3D(x, y, z, format, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        t[1] = new Texture3D(x, y, z, format, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        t[2] = new Texture3D(x, y, z, format, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };

        Color[] colors0 = texs[0].GetPixels();
        Color[] colors1 = texs[1].GetPixels();
        Color[] colors2 = texs[2].GetPixels();

        for (int iz = 0; iz < z; iz++) {
            for (int iy = 0; iy < y; iy++) {
                for (int ix = 0; ix < x; ix++) {

                    int index = iz * (x * y) + iy * x + ix;

                    Color tex0 = colors0[index];
                    Color tex1 = colors1[index];
                    Color tex2 = colors2[index];

                    Vector3 L0 =  new Vector3(tex0.r, tex0.g, tex0.b);
                    Vector3 L1r = new Vector3(tex1.r, tex2.r, tex0.a);
                    Vector3 L1g = new Vector3(tex1.g, tex2.g, tex1.a);
                    Vector3 L1b = new Vector3(tex1.b, tex2.b, tex2.a);

                    L1r = LVUtils.LinearizeSingleSH(L0.x, L1r);
                    L1g = LVUtils.LinearizeSingleSH(L0.y, L1g);
                    L1b = LVUtils.LinearizeSingleSH(L0.z, L1b);

                    colors0[index] = new Color(L0.x, L0.y, L0.z, L1r.z);
                    colors1[index] = new Color(L1r.x, L1g.x, L1b.x, L1g.z);
                    colors2[index] = new Color(L1r.y, L1g.y, L1b.y, L1b.z);

                }
            }
        }

        LVUtils.Apply3DTextureData(t[0], colors0);
        LVUtils.Apply3DTextureData(t[1], colors1);
        LVUtils.Apply3DTextureData(t[2], colors2);

        return t;

    }

}