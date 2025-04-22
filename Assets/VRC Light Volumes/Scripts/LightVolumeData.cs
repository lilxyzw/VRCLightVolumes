using UnityEngine;

[System.Serializable]
public struct LightVolumeData {

    public float Weight;
    public Vector4[] UvwMin; // 3 UVW for each light volume SH L1 component
    public Vector4[] UvwMax; // 3 UVW for each light volume SH L1 component
    public Vector4 InvLocalEdgeSmooth;
    public Matrix4x4 InvWorldMatrix;

    public LightVolumeData(float weight, Vector4 invLocalEdgeSmooth, Matrix4x4 invWorldMatrix, Vector4 uvwMin1, Vector4 uvwMin2, Vector4 uvwMin3, Vector4 uvwMax1, Vector4 uvwMax2, Vector4 uvwMax3) {
        Weight = weight;
        InvLocalEdgeSmooth = invLocalEdgeSmooth;
        UvwMin = new Vector4[] { uvwMin1, uvwMin2, uvwMin3 };
        UvwMax = new Vector4[] { uvwMax1, uvwMax2, uvwMax3 };
        InvWorldMatrix = invWorldMatrix;
    }

}