using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class LightVolumeDataSorter {

    public static List<LightVolumeData> SortData(List<LightVolumeData> lightVolumeDataList) {
        return lightVolumeDataList.OrderByDescending(item => item.Weight).ToList();
    }

    public static void GetData(List<LightVolumeData> sortedData, out float[] rotationMode, out Vector4[] dataA, out Vector4[] dataB, out Vector4[] uvwMin, out Vector4[] uvwMax, out Matrix4x4[] invWorldMatrix) {
        
        int count = sortedData.Count;
        rotationMode = new float[count];
        dataA = new Vector4[count];
        dataB = new Vector4[count];
        uvwMin = new Vector4[count * 3];
        uvwMax = new Vector4[count * 3];
        invWorldMatrix = new Matrix4x4[count];

        for (int i = 0; i < count; ++i) {

            LightVolumeData item = sortedData[i];
            rotationMode[i] = item.RotationMode;
            dataA[i] = item.DataA;
            dataB[i] = item.DataB;
            invWorldMatrix[i] = item.InvWorldMatrix;

            int i3 = i * 3;

            if (item.UvwMin != null && item.UvwMin.Length == 3) {
                uvwMin[i3] = item.UvwMin[0];
                uvwMin[i3 + 1] = item.UvwMin[1];
                uvwMin[i3 + 2] = item.UvwMin[2];
            }

            if (item.UvwMax != null && item.UvwMax.Length == 3) {
                uvwMax[i3] = item.UvwMax[0];
                uvwMax[i3 + 1] = item.UvwMax[1];
                uvwMax[i3 + 2] = item.UvwMax[2];
            }

        }

    }

}