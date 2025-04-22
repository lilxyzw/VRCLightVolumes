using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEditor;

[ExecuteAlways]
public class LightVolume : MonoBehaviour {

    // Inspector
    [Header("Configuration")]
    public bool Static = true;

    [Header("Spherical Harmonics Data")]
    public Texture3D Texture0;
    public Texture3D Texture1;
    public Texture3D Texture2;

    [Header("Baking")]
    public bool Bake = true;
    public bool Denoise;
    public bool AdaptiveResolution;
    public float VoxelsPerUnit = 2;
    public Vector3Int Resolution = new Vector3Int(16, 16, 16);
    public bool PreviewProbes;
#if BAKERY_INCLUDED
    public BakeryVolume BakeryVolume;
#endif

    // Light probes world positions
    private Vector3[] _probesPositions;

    // To check if object was edited this frame
    private Vector3 _prevPos = Vector3.zero;
    private Quaternion _prevRot = Quaternion.identity;
    private Vector3 _prevScl = Vector3.one;

    // Position, Rotation and Scale of the final light volume, depending on the current setup
    public Vector3 GetPosition() {
        return transform.position;
    }
    public Vector3 GetScale() {
        return transform.lossyScale;
    }
    public Quaternion GetRotation() {
        if (LightVolumeSetup.Instance.IsBakeryMode && !Application.isPlaying) {
            return Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        } else {
            return transform.rotation;
        }
    }
    public Matrix4x4 GetMatrixTRS() {
        return Matrix4x4.TRS(GetPosition(), GetRotation(), GetScale());
    }

    // Returns volume voxel count
    public int GetVoxelCount() {
        return Resolution.x * Resolution.y * Resolution.z;
    }

    // Sets Additional Probes to bake with Unity Lightmapper
    [ContextMenu("Set Light Probes")]
    public void SetAdditionalProbes() {
        RecalculateProbesPositions();
        UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(0, _probesPositions);
    }

    // Recalculates probes world positions
    public void RecalculateProbesPositions() {
        _probesPositions = new Vector3[GetVoxelCount()];
        Vector3 offset = new Vector3(0.5f, 0.5f, 0.5f);
        var pos = GetPosition();
        var rot = GetRotation();
        var scl = GetScale();
        int id = 0;
        Vector3 localPos;
        for (int z = 0; z < Resolution.z; z++) {
            for (int y = 0; y < Resolution.y; y++) {
                for (int x = 0; x < Resolution.x; x++) {
                    localPos = new Vector3((float)(x + 0.5f) / Resolution.x, (float)(y + 0.5f) / Resolution.y, (float)(z + 0.5f) / Resolution.z) - offset;
                    _probesPositions[id] = LVUtils.TransformPoint(localPos, pos, rot, scl);
                    id++;
                }
            }
        }
    }

    // Recalculates resolution based on Adaptive Resolution
    public void RecalculateAdaptiveResolution() {
        Vector3 count = Vector3.Scale(Vector3.one, GetScale()) * VoxelsPerUnit;
        int x = Mathf.Max((int)Mathf.Round(count.x), 1);
        int y = Mathf.Max((int)Mathf.Round(count.y), 1);
        int z = Mathf.Max((int)Mathf.Round(count.z), 1);
        Resolution = new Vector3Int(x, y, z);
    }

    // Recalculates adaptive resolution and local positions if required
    public void Recalculate() {
        if (AdaptiveResolution)
            RecalculateAdaptiveResolution();
        if (PreviewProbes && Bake)
            RecalculateProbesPositions();
    }

    [ContextMenu("Save Texture From Light Probes")]
    public void Save3DTextures() {

        // Atlas Sizes
        int w = Resolution.x;
        int h = Resolution.y;
        int d = Resolution.z;
        int vCount = GetVoxelCount();

        // SH data output
        using (NativeArray<SphericalHarmonicsL2> probes = new NativeArray<SphericalHarmonicsL2>(vCount, Allocator.Temp))
        using (NativeArray<float> probesValidity = new NativeArray<float>(vCount, Allocator.Temp)) {

            // Checking data available
#pragma warning disable CS0618
            if (!UnityEditor.Experimental.Lightmapping.GetAdditionalBakedProbes(0, probes, probesValidity)) {
                Debug.LogError("[LightVolume] Can't grab light volume data. No additional baked probes found!");
                return;
            }
#pragma warning restore CS0618

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
            const int x = 3;
            const int y = 1;
            const int z = 2;

            // Setting voxel data
            for (int i = 0; i < vCount; i++) {
                c0[i] = new Color(probes[i][r, a], probes[i][g, a], probes[i][b, a], probes[i][r, z]);
                c1[i] = new Color(probes[i][r, x], probes[i][g, x], probes[i][b, x], probes[i][g, z]);
                c2[i] = new Color(probes[i][r, y], probes[i][g, y], probes[i][b, y], probes[i][b, z]);
            }

            // Apply Pixel Data to Texture
            LVUtils.Apply3DTextureData(tex0, c0);
            LVUtils.Apply3DTextureData(tex1, c1);
            LVUtils.Apply3DTextureData(tex2, c2);

            // Saving 3D Texture assets
            LVUtils.SaveTexture3DAsAsset(tex0, $"Assets/BakeryLightmaps/LightVolume_{gameObject.name}_1.asset");
            LVUtils.SaveTexture3DAsAsset(tex1, $"Assets/BakeryLightmaps/LightVolume_{gameObject.name}_2.asset");
            LVUtils.SaveTexture3DAsAsset(tex2, $"Assets/BakeryLightmaps/LightVolume_{gameObject.name}_3.asset");

        }

    }

    // Setups required game objects and components
    public void SetupDependencies() {
#if BAKERY_INCLUDED
        // Create or destroy Bakery Volume
        if (LightVolumeSetup.Instance.IsBakeryMode && Bake && BakeryVolume == null) {
            GameObject obj = new GameObject($"Bakery Volume - {gameObject.name}");
            obj.transform.parent = transform;
            BakeryVolume = obj.AddComponent<BakeryVolume>();
        } else if ((!LightVolumeSetup.Instance.IsBakeryMode || !Bake) && BakeryVolume != null) {
            if (Application.isPlaying) {
                Destroy(BakeryVolume.gameObject);
            } else {
                DestroyImmediate(BakeryVolume.gameObject);
            }
            BakeryVolume = null;
        }

        if (LightVolumeSetup.Instance.IsBakeryMode && BakeryVolume != null) {
            // Sync bakery volume with light volume
            BakeryVolume.gameObject.name = $"Bakery Volume - {gameObject.name}";
            if (BakeryVolume.transform.parent != transform) BakeryVolume.transform.parent = transform;
            BakeryVolume.rotateAroundY = true;
            BakeryVolume.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            BakeryVolume.transform.localScale = Vector3.one;
            BakeryVolume.bounds = new Bounds(GetPosition(), GetScale());
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

    private void Update() {
#if BAKERY_INCLUDED
        if (Bake && (Texture0 == null || Texture1 == null || Texture2 == null) && LightVolumeSetup.Instance.IsBakeryMode && BakeryVolume != null  && BakeryVolume.bakedTexture0 != null) {
            Texture0 = BakeryVolume.bakedTexture0;
            Texture1 = BakeryVolume.bakedTexture1;
            Texture2 = BakeryVolume.bakedTexture2;
        }
#endif
        if (Selection.activeGameObject != gameObject) return;
        SetupDependencies();

        // Update udon Behaviour if Volume changed transform
        if (_prevPos != transform.position || _prevRot != transform.rotation || _prevScl != transform.localScale) {
            LightVolumeSetup.Instance.SetupUdonBehaviour();
            _prevPos = transform.position;
            _prevRot = transform.rotation;
            _prevScl = transform.localScale;
        }
    }

    private void OnValidate() {
        Recalculate();
        LightVolumeSetup.Instance.SetupUdonBehaviour();
    }

    private void OnDrawGizmosSelected() {
        if (PreviewProbes && Bake && _probesPositions != null) {
            for (int i = 0; i < _probesPositions.Length; i++) {
                Gizmos.DrawSphere(_probesPositions[i], 0.05f);
            }
        }
    }

}
