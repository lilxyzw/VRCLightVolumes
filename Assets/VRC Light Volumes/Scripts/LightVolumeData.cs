using UnityEngine;

[System.Serializable]
public struct LightVolumeData {

    public float Weight;
    public Vector4 WorldMin;
    public Vector4 WorldMax;
    public Vector4[] UvwMin; // 3 UVW for each light volume SH L1 component
    public Vector4[] UvwMax; // 3 UVW for each light volume SH L1 component

    public LightVolumeData(float weight, Vector4 worldMin, Vector4 worldMax, Vector4 uvwMin1, Vector4 uvwMin2, Vector4 uvwMin3, Vector4 uvwMax1, Vector4 uvwMax2, Vector4 uvwMax3) {
        Weight = weight;
        WorldMin = worldMin;
        WorldMax = worldMax;
        UvwMin = new Vector4[] { uvwMin1, uvwMin2, uvwMin3 };
        UvwMax = new Vector4[] { uvwMax1, uvwMax2, uvwMax3 };
    }

}