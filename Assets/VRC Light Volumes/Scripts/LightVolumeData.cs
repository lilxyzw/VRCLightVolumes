using UnityEngine;

[System.Serializable]
public struct LightVolumeData {

    public float Weight;
    public float RotationMode;
    public Vector4 DataA;
    public Vector4 DataB;
    public Vector4[] UvwMin; // 3 UVW for each light volume SH L1 component
    public Vector4[] UvwMax; // 3 UVW for each light volume SH L1 component
    public Matrix4x4 InvWorldMatrix;

    public LightVolumeData(float weight, float rotationMode, Vector4 dataA, Vector4 dataB, Vector4 uvwMin1, Vector4 uvwMin2, Vector4 uvwMin3, Vector4 uvwMax1, Vector4 uvwMax2, Vector4 uvwMax3, Matrix4x4 invWorldMatrix) {
        Weight = weight;
        RotationMode = rotationMode;
        DataA = dataA;
        DataB = dataB;
        UvwMin = new Vector4[] { uvwMin1, uvwMin2, uvwMin3 };
        UvwMax = new Vector4[] { uvwMax1, uvwMax2, uvwMax3 };
        InvWorldMatrix = invWorldMatrix;
    }

}