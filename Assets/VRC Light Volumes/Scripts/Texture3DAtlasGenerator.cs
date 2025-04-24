using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class Texture3DAtlasGenerator {

    // Padding, hardcoded to be one pixel. It's pointless to make it wider without mip-mapping enabled
    public const int Padding = 1; 

    // Final 3D Atlas structure
    public struct Atlas3D {
        public Atlas3D(Texture3D texture, Vector3[] boundsUvwMin, Vector3[] boundsUvwMax) {
            Texture = texture;
            BoundsUvwMin = boundsUvwMin;
            BoundsUvwMax = boundsUvwMax;
        }
        public Texture3D Texture; // Final atlas Texture
        public Vector3[] BoundsUvwMin; // Normalized islands bounds excluding padding Min
        public Vector3[] BoundsUvwMax;// Normalized islands bounds excluding padding Max
    }

    // Holds information about each source texture during processing.
    private struct TextureInfo {
        public int Index;
        public Texture3D Texture;
        public Vector3Int Size;
        public Vector3Int PaddedSize => Size + Vector3Int.one * 2 * Padding;   // Dimensions including padding
    }

    // 3D Texture island after it has been placed within the atlas
    private struct Island {
        public int Index;
        public RectInt Bounds => new RectInt(PaddedBounds.Position + Vector3Int.one * Padding, PaddedBounds.Size - Vector3Int.one * 2 * Padding);
        public RectInt PaddedBounds;
    }

    // Single packing iteration result
    private struct PackingResult {
        public Island[] Islands;
        public Vector3Int Size;
        public long Volume => (long)Size.x * Size.y * Size.z;
    }

    // Represents a 3D axis-aligned bounding box using integers, suitable for texture coordinate calculations
    private struct RectInt {

        public Vector3Int Position; // Minimum corner (x, y, z)
        public Vector3Int Size;     // Dimensions (width, height, depth)

        public Vector3Int Min => Position;
        public Vector3Int Max => Position + Size;

        public RectInt(Vector3Int position, Vector3Int size) {
            Position = position;
            Size = size;
        }

        // Checks for intersection with another RectInt based on AABB overlap
        public bool Intersects(RectInt other) {
            return (Min.x < other.Max.x && Max.x > other.Min.x && Min.y < other.Max.y && Max.y > other.Min.y && Min.z < other.Max.z && Max.z > other.Min.z);
        }

    }

    // Creates a 3D texture atlas using stochastic packing over multiple iterations. Runs a 3D point-based packing algorithm with randomized input order N times and selects the result with the smallest bounding box volume. Includes edge padding.
    public static Atlas3D CreateAtlasStochastic(Texture3D[] Textures, int iterations) {

        // Linearize SH
        int count = Textures.Length / 3;
        for (int i = 0; i < count; i++) {
            Texture3D[] texs = new Texture3D[3];
            texs[0] = Textures[i * 3];
            texs[1] = Textures[i * 3 + 1];
            texs[2] = Textures[i * 3 + 2];
            texs = LinearizeSphericalHarmonics(texs);
            Textures[i * 3] = texs[0];
            Textures[i * 3 + 1] = texs[1];
            Textures[i * 3 + 2] = texs[2];
        }

        // Validating and getting texture infos based on provided arrays
        TextureInfo[] textureInfos = GetTextureInfos(Textures);
        if (textureInfos == null) return new Atlas3D();

        // Ensure at least one attempt
        iterations = Mathf.Max(iterations, 1);
        
        PackingResult bestResult = new PackingResult(); // Best packing result
        System.Random rng = new System.Random(); // Random number generator for shuffling

        // Finally, iterative packing
        for (int i = 0; i < iterations; i++) {
            
            TextureInfo[] shuffledInfos = textureInfos.OrderBy(x => rng.Next()).ToArray(); // Randomize input
            PackingResult currentResult = PackPointBased3DInternal(shuffledInfos); // Packing result

            // Compare with the best result found so far
            if (i == 0 || currentResult.Volume < bestResult.Volume) bestResult = currentResult;

        }

        // Finally, packing everything into a single Texture3D
        Atlas3D atlas = FinalizeAtlas(bestResult, textureInfos);

        if (atlas.Texture == null) {
            Debug.LogError("[LightVolumeAtlaser] Error while packing final Texture3D atlas!");
        }

        return atlas;

    }

    // Generates a TextureInfo array based on 3 Texture3D arrays provided. Validates provied data and can return null in case of an error
    private static TextureInfo[] GetTextureInfos(Texture3D[] Textures) {
        
        // Arrays and textures validation
        //if(!IsTextureArrayValid(Textures0) || !IsTextureArrayValid(Textures1) || !IsTextureArrayValid(Textures2) || !IsTextureArraysSimilar(new Texture3D[][] { Textures0, Textures1, Textures2 } ) ) {
        //    return null;
        //}

        // Filling texture info array
        int islandsCount = Textures.Length;
        TextureInfo[] textureInfos =  new TextureInfo[islandsCount];
        for (int i = 0; i < islandsCount; i++) {
            textureInfos[i] = new TextureInfo {
                Texture = Textures[i],
                Size = new Vector3Int(Textures[i].width, Textures[i].height, Textures[i].depth),
                Index = i
            };
        }

        return textureInfos;
    }

    // Checks if texture array is initialized, has no nulls and reradable
    private static bool IsTextureArrayValid(Texture3D[] Textures) {
        if (Textures == null) {
            Debug.LogError("[LightVolumeAtlaser] Provided SH 3DTexture array is null!");
            return false;
        }
        if (Textures.Length == 0) {
            Debug.LogError("[LightVolumeAtlaser] Provided SH 3DTexture array is empty!");
            return false;
        }
        for (int i = 0; i < Textures.Length; i++) {
            if (Textures[i] == null) {
                Debug.LogError("[LightVolumeAtlaser] Provided SH 3DTexture is null!");
                return false;
            }
            try {
                Textures[i].GetPixel(0, 0, 0);
            } catch {
                Debug.LogError($"[LightVolumeAtlaser] Texture {Textures[i].name} is not readable! (Read/Write Disabled?)");
                return false;
            }
            if (Textures[i].width <= 0 || Textures[i].height <= 0 || Textures[i].depth <= 0) {
                Debug.LogError($"[LightVolumeAtlaser] Texture {Textures[i].name} has zero dimension!");
                continue;
            }
        }
        return true;
    }

    // Checks if provided 3D Texture arrays has textures with similar count and dimensions 
    private static bool IsTextureArraysSimilar(Texture3D[][] TextureArrays) {
        // Buffered properties to compare
        int count = 0;
        int[] widths = new int[0];
        int[] heights = new int[0];
        int[] depths = new int[0];
        for (int i = 0; i < TextureArrays.Length; i++) {
            if (i == 0) { // Remember first texture arrays properties
                count = TextureArrays[i].Length;
                widths = new int[count];
                heights = new int[count];
                depths = new int[count];
                for (int j = 0; j < count; j++) {
                    widths[j] = TextureArrays[i][j].width;
                    heights[j] = TextureArrays[i][j].height;
                    depths[j] = TextureArrays[i][j].depth;
                }
            } else {
                if (TextureArrays[i].Length != count) {
                    Debug.LogError("[LightVolumeAtlaser] Provided SH 3DTexture arrays has different lengths!");
                    return false;
                }
                for (int j = 0; j < count; j++) {
                    if (widths[j] != TextureArrays[i][j].width || heights[j] != TextureArrays[i][j].height || depths[j] != TextureArrays[i][j].depth) {
                        Debug.LogError("[LightVolumeAtlaser] Provided SH 3DTexture arrays has different dimensions!");
                        return false;
                    }
                }
            }
        }
        return true;
    }


    // Internal 3D point-based packing algorithm.
    private static PackingResult PackPointBased3DInternal(TextureInfo[] textureInfos) {

        List<Island> placedIslands = new List<Island>(); // Successful island placements
        List<RectInt> placedBounds3D = new List<RectInt>(); // AABBs of placed islands for collision checks
        List<Vector3Int> potentialPositions3D = new List<Vector3Int> { Vector3Int.zero }; // Candidate start points
        Vector3Int currentAtlasBounds = Vector3Int.zero; // Tracks the required atlas size

        foreach (var textureInfo in textureInfos) { // Process textures in the provided order

            Vector3Int paddedSize = textureInfo.PaddedSize; // Use padded size for placement checks
            int bestFitListIndex = -1; // Index of the chosen potential position
            Vector3Int bestPosition3D = Vector3Int.zero; // Best chosen position
            long bestScore = long.MaxValue; // Lower score is better (closer to origin)

            // Find the best available position from the candidate list
            for (int i = 0; i < potentialPositions3D.Count; i++) {
                Vector3Int potentialPos = potentialPositions3D[i];
                RectInt potentialRect = new RectInt(potentialPos, paddedSize);

                // Check for overlaps with already placed items
                bool intersects = false;
                foreach (var existingBounds in placedBounds3D) {
                    if (potentialRect.Intersects(existingBounds)) {
                        intersects = true;
                        break;
                    }
                }

                // If it fits, calculate its score and compare with the best found so far
                if (!intersects) {
                    // Score: Squared Euclidean distance from origin (favors positions near 0,0,0)
                    long currentScore = (long)potentialPos.x * potentialPos.x + (long)potentialPos.y * potentialPos.y + (long)potentialPos.z * potentialPos.z;
                    if (currentScore < bestScore) {
                        bestScore = currentScore;
                        bestFitListIndex = i;
                        bestPosition3D = potentialPos;
                    }
                }
            }

            // If no position was found for this item, the packing attempt fails
            if (bestFitListIndex == -1) {
                Debug.LogError($"[LightVolumeAtlaser] Failed to place texture {textureInfo.Index} ('{textureInfo.Texture.name}'). Aborting this packing attempt as incomplete.");
                return new PackingResult() { Size = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue) };
            }

            // Place the item at the best found position
            RectInt finalBounds = new RectInt(bestPosition3D, paddedSize);
            placedIslands.Add(new Island { PaddedBounds = finalBounds, Index = textureInfo.Index });
            placedBounds3D.Add(finalBounds); // Add bounds for future collision checks

            // Update the overall atlas dimensions required
            currentAtlasBounds.x = Mathf.Max(currentAtlasBounds.x, finalBounds.Max.x);
            currentAtlasBounds.y = Mathf.Max(currentAtlasBounds.y, finalBounds.Max.y);
            currentAtlasBounds.z = Mathf.Max(currentAtlasBounds.z, finalBounds.Max.z);

            // Remove the position that was just used
            potentialPositions3D.RemoveAt(bestFitListIndex);

            // Generate new candidate points based on the 8 corners of the newly placed item
            Vector3Int[] corners = new Vector3Int[8];
            corners[0] = bestPosition3D;
            corners[1] = bestPosition3D + new Vector3Int(paddedSize.x, 0, 0);
            corners[2] = bestPosition3D + new Vector3Int(0, paddedSize.y, 0);
            corners[3] = bestPosition3D + new Vector3Int(0, 0, paddedSize.z);
            corners[4] = bestPosition3D + new Vector3Int(paddedSize.x, paddedSize.y, 0);
            corners[5] = bestPosition3D + new Vector3Int(paddedSize.x, 0, paddedSize.z);
            corners[6] = bestPosition3D + new Vector3Int(0, paddedSize.y, paddedSize.z);
            corners[7] = bestPosition3D + paddedSize;
            foreach (var corner in corners) {
                AddIfNotPresent3D(potentialPositions3D, corner);
            }

        }

        // Final validation check (e.g., positive bounds)
        bool isValid = currentAtlasBounds.x > 0 && currentAtlasBounds.y > 0 && currentAtlasBounds.z > 0 && placedIslands.Count == textureInfos.Length; // Ensure all items were actually placed

        if (!isValid) {
            Debug.LogError($"[LightVolumeAtlaser] Mismatch! Filed to pack islands.");
            return new PackingResult() { Size = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue) };
        }

        return new PackingResult {
            Islands = placedIslands.ToArray(),
            Size = currentAtlasBounds
        };
    }

    // Creates the final Texture3D object from the best packing result, copies pixel data with edge padding replication, and calculates normalized bounds
    private static Atlas3D FinalizeAtlas(PackingResult atlasPacking, TextureInfo[] textureInfos) {

        // Checking texture format
        TextureFormat format = textureInfos[0].Texture.format;
        if (!SystemInfo.SupportsTextureFormat(format) || IsCompressedFormat(format)) {
            Debug.LogWarning($"[LightVolumeAtlaser] Format {format} unsupported/compressed. Using Fallback to RGBAHalf.");
            format = TextureFormat.RGBAHalf;
        }

        // Atlas Sizes
        var atlasSize = atlasPacking.Size;
        int atlasW = atlasSize.x; // Three 3D textures in a row
        int atlasH = atlasSize.y;
        int atlasD = atlasSize.z;

        // Creating Texture3D with specified format and dimensions
        Texture3D atlasTexture;
        try {
            atlasTexture = new Texture3D(atlasW, atlasH, atlasD, format, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        } catch (UnityException ex) {
            Debug.LogError($"[LightVolumeAtlaser] Failed to create Texture3D. Dimensions: {atlasW}õ{atlasH}õ{atlasD}, Format: {format}. Error message: {ex.Message}");
            return new Atlas3D();
        } catch (System.ArgumentException ex) {
            Debug.LogError($"[LightVolumeAtlaser] Invalid arguments for Texture3D creation (Likely dimensions too large?). Dimensions: {atlasW}õ{atlasH}õ{atlasD}, Format: {format}. Error message: {ex.Message}");
            return new Atlas3D();
        }

        Island[] islands = atlasPacking.Islands; // All islands of this atlas

        Vector3[] boundsUvwMin = new Vector3[textureInfos.Length];  // Initializing bounds Min
        Vector3[] boundsUvwMax = new Vector3[textureInfos.Length];  // Initializing bounds Max
        Color[] atlasPixelData = new Color[atlasPacking.Volume]; // Initializing Pixel Buffer for three 3D textures in a row

        // Iterating through islands
        for (int i = 0; i < islands.Length; i++) {

            TextureInfo texInfo = textureInfos[islands[i].Index]; // TexInfo of this island
            Color[] islandPixels = texInfo.Texture.GetPixels(); // Get source pixels for this island

            // Island Sizes
            var islandSize = texInfo.Size;
            int islandW = islandSize.x;
            int islandH = islandSize.y;
            int islandD = islandSize.z;

            RectInt paddedBounds = islands[i].PaddedBounds; // Island bounds including padding
            Vector3Int islandPos = islands[i].Bounds.Position; // Island no bounds position

            // Loop through the padded island bounds in the atlas
            for (int iz = paddedBounds.Position.z; iz < paddedBounds.Max.z; iz++) {
                for (int iy = paddedBounds.Position.y; iy < paddedBounds.Max.y; iy++) {
                    for (int ix = paddedBounds.Position.x; ix < paddedBounds.Max.x; ix++) {

                        // Calculate coordinates relative to the content area's origin. Clamping makes the edge pixel replication
                        int rx = Mathf.Clamp(ix - islandPos.x, 0, islandW - 1);
                        int ry = Mathf.Clamp(iy - islandPos.y, 0, islandH - 1);
                        int rz = Mathf.Clamp(iz - islandPos.z, 0, islandD - 1);

                        // Calculate 1D index into the source texture's pixel array using clamped coordinates
                        int sourceIndex = rz * (islandW * islandH) + ry * islandW + rx;

                        // Calculate 1D index into the destination atlas pixel array
                        int destIndex = iz * (atlasW * atlasH) + iy * atlasW + ix;

                        // Actually copy pixels
                        atlasPixelData[destIndex] = islandPixels[sourceIndex];

                    }
                }
            }

            // Calculate Normalized Bounds
            boundsUvwMin[texInfo.Index] = new Vector3(
                (float)islandPos.x / atlasW,
                (float)islandPos.y / atlasH,
                (float)islandPos.z / atlasD
            );
            boundsUvwMax[texInfo.Index] = new Vector3(
                (float)(islandPos.x + islandW) / atlasW,
                (float)(islandPos.y + islandH) / atlasH,
                (float)(islandPos.z + islandD) / atlasD
            );

        }

        LVUtils.Apply3DTextureData(atlasTexture, atlasPixelData);

        return new Atlas3D(atlasTexture, boundsUvwMin, boundsUvwMax);
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

                    Vector3 L0  = new Vector3(tex0.r, tex0.g, tex0.b);
                    Vector3 L1r = new Vector3(tex1.r, tex2.r, tex0.a);
                    Vector3 L1g = new Vector3(tex1.g, tex2.g, tex1.a);
                    Vector3 L1b = new Vector3(tex1.b, tex2.b, tex2.a);

                    L1r = LinearizeSingleSH(L0.x, L1r);
                    L1g = LinearizeSingleSH(L0.y, L1g);
                    L1b = LinearizeSingleSH(L0.z, L1b);

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

    // Returns modified L1
    private static Vector3 LinearizeSingleSH(float L0, Vector3 L1) {
        L1 = L1 / 2;
        float L1length = L1.magnitude;
        if (L1length > 0.0 && L0 > 0.0) {
            L1 *= Mathf.Min(L0 / L1length, 1.13f);
        }
        return L1;
    }

    // Adds a 3D position to the list if it's not already present.
    private static void AddIfNotPresent3D(List<Vector3Int> list, Vector3Int position) {
        if (!list.Contains(position)) {
            list.Add(position);
        }
    }

    // Checks if a given TextureFormat is a compressed format that usually doesn't work with GetPixels/SetPixels.
    private static bool IsCompressedFormat(TextureFormat format) {
#pragma warning disable CS0618
        return format == TextureFormat.DXT1 || format == TextureFormat.DXT5 ||
              format == TextureFormat.PVRTC_RGB2 || format == TextureFormat.PVRTC_RGBA2 ||
              format == TextureFormat.PVRTC_RGB4 || format == TextureFormat.PVRTC_RGBA4 ||
              format == TextureFormat.ETC_RGB4 || format == TextureFormat.ETC_RGB4_3DS ||
              format == TextureFormat.ETC2_RGB || format == TextureFormat.ETC2_RGBA1 ||
              format == TextureFormat.ETC2_RGBA8 || format == TextureFormat.EAC_R || format == TextureFormat.EAC_R_SIGNED ||
              format == TextureFormat.EAC_RG || format == TextureFormat.EAC_RG_SIGNED ||
              format == TextureFormat.ASTC_4x4 || format == TextureFormat.ASTC_5x5 ||
              format == TextureFormat.ASTC_6x6 || format == TextureFormat.ASTC_8x8 ||
              format == TextureFormat.ASTC_10x10 || format == TextureFormat.ASTC_12x12 ||
              format == TextureFormat.ASTC_HDR_4x4 || format == TextureFormat.ASTC_HDR_5x5 ||
              format == TextureFormat.ASTC_HDR_6x6 || format == TextureFormat.ASTC_HDR_8x8 ||
              format == TextureFormat.ASTC_HDR_10x10 || format == TextureFormat.ASTC_HDR_12x12 ||
              format == TextureFormat.BC4 || format == TextureFormat.BC5 ||
              format == TextureFormat.BC6H || format == TextureFormat.BC7;
#pragma warning restore CS0618
    }

}