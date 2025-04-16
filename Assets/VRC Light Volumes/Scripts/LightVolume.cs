using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEditor;

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

    // Sets Additional Probes to bake with Unity Lightmapper
    public void SetAdditionalProbes() {
        RecalculateProbesLocalPositions();
        UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(0, _probesLocalPositions);
    }

    // Gets Additional Probes taht baked with Unity Lightmapper (Debug)
    public void GetAdditionalLightProbes() {
        NativeArray<SphericalHarmonicsL2> outBakedProbeSH = new NativeArray<SphericalHarmonicsL2>(Resolution.x * Resolution.y * Resolution.z, Allocator.Temp);
        NativeArray<float> outBakedProbeValidity = new NativeArray<float>(Resolution.x * Resolution.y * Resolution.z, Allocator.Temp);
        NativeArray<float> outBakedProbeOctahedralDepth = new NativeArray<float>(8000, Allocator.Temp);
        if (UnityEditor.Experimental.Lightmapping.GetAdditionalBakedProbes(0, outBakedProbeSH, outBakedProbeValidity, outBakedProbeOctahedralDepth)) {
            foreach (var o in outBakedProbeSH) {
                Debug.Log(o[0, 0]);
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
