using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEditor;
#if UNITY_EDITOR
using System.IO;
#endif

[ExecuteAlways]
public class LightVolume : MonoBehaviour {

    // Inspector
    [Header("Configuration")]
    public VolumeRotation RotationType = VolumeRotation.Fixed;
    public bool Static = true;

    [Header("Spherical Harmonics Data")]
    public Texture3D Texture1;
    public Texture3D Texture2;
    public Texture3D Texture3;

    [Header("Baking")]
    public Baking BakingMode = Baking.DontBake;
    public bool Denoise;
    public bool AdaptiveResolution;
    public float VoxelsPerUnit = 2;
    public Vector3Int Resolution = new Vector3Int(16, 16, 16);
    public bool PreviewProbes;
#if BAKERY_INCLUDED
    public BakeryVolume BakeryVolume;
#endif

    public LightProbeGroup ProbeGroup;

    // Public properties
    public Vector3 Position => transform.position;
    public Vector3 Scale => transform.lossyScale;
    public Quaternion Rotation { 
        get {
            if (RotationType == VolumeRotation.Fixed) {
                return Quaternion.identity;
            } else if (RotationType == VolumeRotation.AroundY || (RotationType == VolumeRotation.Free && BakingMode == Baking.Bakery && !Application.isPlaying) ) {
                return Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            } else {
                return transform.rotation;
            }
        }
    }

    // Private variables
    private Vector3[] _probesLocalPositions;


    [ContextMenu("Set Light Probes")]
    // Sets Additional Probes to bake with Unity Lightmapper
    public void SetAdditionalProbes() {
        if(ProbeGroup == null) {
            Debug.LogError("Setup Light Probe Group!");
        }

        RecalculateProbesLocalPositions();

        int vCount = Resolution.x * Resolution.y * Resolution.z;

        Vector3[] probesPoses = new Vector3[vCount];

        for (int i = 0; i < vCount; i++) {
            probesPoses[i] = UniformTransformPoint(_probesLocalPositions[i]);
        }

        ProbeGroup.probePositions = probesPoses;

        //UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(0, _probesLocalPositions);
    }

    // Gets Additional Probes taht baked with Unity Lightmapper (Debug)
    [ContextMenu("Get Light Probes")]
    public void GetAdditionalLightProbes() {
        NativeArray<SphericalHarmonicsL2> outBakedProbeSH = new NativeArray<SphericalHarmonicsL2>(Resolution.x * Resolution.y * Resolution.z, Allocator.Temp);
        NativeArray<float> outBakedProbeValidity = new NativeArray<float>(Resolution.x * Resolution.y * Resolution.z, Allocator.Temp);
        //NativeArray<float> outBakedProbeOctahedralDepth = new NativeArray<float>(8000, Allocator.Temp);
        if (UnityEditor.Experimental.Lightmapping.GetAdditionalBakedProbes(0, outBakedProbeSH, outBakedProbeValidity)) {
            foreach (var o in outBakedProbeSH) {
                Debug.Log($"Color: {o[0, 0]} {o[1, 0]} {o[2, 0]}");
            }
        } else {
            Debug.Log("No Probes found!");
        }
    }

    // Recalculates probes local positions in 1x1x1 size
    public void RecalculateProbesLocalPositions() {
        _probesLocalPositions = new Vector3[Resolution.x * Resolution.y * Resolution.z];
        Matrix4x4 matrix = Matrix4x4.TRS(Position, Rotation, Scale);
        Vector3 offset = new Vector3(0.5f, 0.5f, 0.5f);
        int id = 0;
        for (int z = 0; z < Resolution.z; z++) {
            for (int y = 0; y < Resolution.y; y++) {
                for (int x = 0; x < Resolution.x; x++) {
                    _probesLocalPositions[id] = new Vector3((float)(x + 0.5f) / Resolution.x, (float)(y + 0.5f) / Resolution.y, (float)(z + 0.5f) / Resolution.z) - offset;
                    id++;
                }
            }
        }
    }

    // Recalculates resolution based on Adaptive Resolution
    public void RecalculateAdaptiveResolution() {
        Vector3 count = Vector3.Scale(Vector3.one, Scale) * VoxelsPerUnit;
        int x = Mathf.Max((int)Mathf.Round(count.x), 1);
        int y = Mathf.Max((int)Mathf.Round(count.y), 1);
        int z = Mathf.Max((int)Mathf.Round(count.z), 1);
        Resolution = new Vector3Int(x, y, z);
    }

    // Transforms points from local to world space without skewing
    public Vector3 UniformTransformPoint(Vector3 point) {
        return Rotation * Vector3.Scale(point, Scale) + Position;
    }

    // Recalculates adaptive resolution and local positions if required
    public void Recalculate() {
        if (AdaptiveResolution)
            RecalculateAdaptiveResolution();
        if (PreviewProbes && BakingMode != Baking.DontBake)
            RecalculateProbesLocalPositions();
    }

    [ContextMenu("Save Texture From Light Probes")]
    public void Save3DTextures() {

        // Atlas Sizes
        int w = Resolution.x;
        int h = Resolution.y;
        int d = Resolution.z;

        // Voxels count
        int vCount = w * h * d;

        // SH data output
        NativeArray<SphericalHarmonicsL2> probes = new NativeArray<SphericalHarmonicsL2>(vCount, Allocator.Temp);
        NativeArray<float> probesValidity = new NativeArray<float>(vCount, Allocator.Temp);
        //NativeArray<float> probesOctahedralDepth = new NativeArray<float>(8000, Allocator.Temp);

        // Checking data available
        if (!UnityEditor.Experimental.Lightmapping.GetAdditionalBakedProbes(0, probes, probesValidity)) {
            Debug.Log("No Probes found!");
            return;
        }

        // Creating Texture3D with specified format and dimensions
        TextureFormat format = TextureFormat.RGBAHalf;
        Texture3D tex0 = new Texture3D(w, h, d, format, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        Texture3D tex1 = new Texture3D(w, h, d, format, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        Texture3D tex2 = new Texture3D(w, h, d, format, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        
        // Color arrays to store voxel data
        Color[] c0 = new Color[vCount];
        Color[] c1 = new Color[vCount];
        Color[] c2 = new Color[vCount];

        // Quick shortcuts to SH L1 components
        const int r = 0;
        const int g = 1;
        const int b = 2;
        const int a = 0;
        const int x = 1;
        const int y = 2;
        const int z = 3;

        SphericalHarmonicsL2 probe;

        // Setting voxel data
        for (int i = 0; i < vCount; i++) {
            LightProbes.GetInterpolatedProbe(UniformTransformPoint(_probesLocalPositions[i]), null, out probe);
            c0[i] = new Color(probe[r, a], probe[g, a], probe[b, a], probe[r, z]);
            c1[i] = new Color(probe[r, x], probe[g, x], probe[b, x], probe[g, z]);
            c2[i] = new Color(probe[r, y], probe[g, y], probe[b, y], probe[b, z]);
            //c0[i] = new Color(probes[i][r, a], probes[i][g, a], probes[i][b, a], probes[i][r, z]);
            //c1[i] = new Color(probes[i][r, x], probes[i][g, x], probes[i][b, x], probes[i][g, z]);
            //c2[i] = new Color(probes[i][r, y], probes[i][g, y], probes[i][b, y], probes[i][b, z]);
        }


        // Apply Pixel Data to Texture
        Apply3DTextureData(tex0, c0);
        Apply3DTextureData(tex1, c1);
        Apply3DTextureData(tex2, c2);

        SaveTexture3DAsAsset(tex0, $"Assets/BakeryLightmaps/LightVolume_{gameObject.name}_1.asset");
        SaveTexture3DAsAsset(tex1, $"Assets/BakeryLightmaps/LightVolume_{gameObject.name}_2.asset");
        SaveTexture3DAsAsset(tex2, $"Assets/BakeryLightmaps/LightVolume_{gameObject.name}_3.asset");

    }

    // Apply voxels to a 3D Texture
    private void Apply3DTextureData(Texture3D texture, Color[] colors) {
        try {
            texture.SetPixels(colors);
            texture.Apply(updateMipmaps: false);
        } catch (UnityException ex) {
            Debug.LogError($"[LightVolume] Failed to SetPixels in the final Texture3D atlas. Error: {ex.Message}");
        }
    }

    // Saves 3D Texture to Assets
    public static bool SaveTexture3DAsAsset(Texture3D textureToSave, string assetPath) {

        if (textureToSave == null) {
            Debug.LogError("[LightVolumeAtlaser] Error saving Texture3D: texture is null");
            return false;
        }

        if (string.IsNullOrEmpty(assetPath)) {
            Debug.LogError("[LightVolumeAtlaser] Error saving Texture3D: Saving path is null");
            return false;
        }

        try {
            string directoryPath = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath)) {
                Directory.CreateDirectory(directoryPath);
                AssetDatabase.Refresh();
            }
        } catch (System.Exception e) {
            Debug.LogError($"[LightVolumeAtlaser] Error while creating folders '{assetPath}': {e.Message}");
            return false;
        }

        try {
            AssetDatabase.CreateAsset(textureToSave, assetPath);
            EditorUtility.SetDirty(textureToSave);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[LightVolumeAtlaser] 3D Atlas saved at path: '{assetPath}'");
            return true;
        } catch (System.Exception e) {
            Debug.LogError($"[LightVolumeAtlaser] Error saving 3D Atlas at path: '{assetPath}': {e.Message}");
            return false;
        }

    }

    private void Update() {

        if (Selection.activeGameObject != gameObject) return;

#if BAKERY_INCLUDED

        // Create or destroy Bakery Volume

        if (BakingMode == Baking.Bakery && BakeryVolume == null) {
            GameObject obj = new GameObject($"Bakery Volume - {gameObject.name}");
            obj.transform.parent = transform;
            BakeryVolume = obj.AddComponent<BakeryVolume>();
        } else if (BakingMode != Baking.Bakery && BakeryVolume != null) {
            if (Application.isPlaying) {
                Destroy(BakeryVolume.gameObject);
            } else {
                DestroyImmediate(BakeryVolume.gameObject);
            }
            BakeryVolume = null;
        }

        if(BakeryVolume != null) {
            // Sync bakery volume with light volume
            if (BakeryVolume.transform.parent != transform) BakeryVolume.transform.parent = transform;
            BakeryVolume.rotateAroundY = RotationType == VolumeRotation.Fixed ? false : true;
            BakeryVolume.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            BakeryVolume.transform.localScale = Vector3.one;
            BakeryVolume.bounds = new Bounds(Position, Scale);
            BakeryVolume.enableBaking = true;
            BakeryVolume.denoise = Denoise;
            BakeryVolume.adaptiveRes = false;
            BakeryVolume.resolutionX = Resolution.x;
            BakeryVolume.resolutionY = Resolution.y;
            BakeryVolume.resolutionZ = Resolution.z;
            BakeryVolume.encoding = BakeryVolume.Encoding.Half4;
        }

#endif

    }

    private void OnValidate() {
        Recalculate();
    }

    private void OnDrawGizmosSelected() {
        if (PreviewProbes && BakingMode != Baking.DontBake && _probesLocalPositions != null) {
            for (int i = 0; i < _probesLocalPositions.Length; i++) {
                Gizmos.DrawSphere(UniformTransformPoint(_probesLocalPositions[i]), 0.1f);
            }
        }
    }

    public enum VolumeRotation {
        Fixed,
        AroundY,
        Free
    }

    public enum Baking {
        DontBake = 0,
        UnityLightmapper = 1,
        Bakery = 2
    }

}
