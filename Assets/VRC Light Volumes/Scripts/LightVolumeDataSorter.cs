using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class LightVolumeDataSorter {

    public static List<LightVolumeData> SortData(List<LightVolumeData> lightVolumeDataList) {
        lightVolumeDataList.RemoveAll(item => item.VolumeTransform == null);
        return lightVolumeDataList.OrderByDescending(item => item.Weight).ToList();
    }

    public static void GetData(List<LightVolumeData> sortedData, out Vector4[] invLocalEdgeSmooth, out Matrix4x4[] invWorldMatrix, out Vector4[] uvwMin, out Vector4[] uvwMax, out Quaternion[] bakedRotation, out Transform[] volumeTransforms, out float[] isAdditive) {
        
        int count = sortedData.Count;
        invLocalEdgeSmooth = new Vector4[count];
        uvwMin = new Vector4[count * 3];
        uvwMax = new Vector4[count * 3];
        invWorldMatrix = new Matrix4x4[count];
        bakedRotation = new Quaternion[count];
        volumeTransforms = new Transform[count];
        isAdditive = new float[count];

        for (int i = 0; i < count; ++i) {

            LightVolumeData item = sortedData[i];
            invLocalEdgeSmooth[i] = item.InvLocalEdgeSmooth;
            invWorldMatrix[i] = item.InvWorldMatrix;
            bakedRotation[i] = item.BakedRotation;
            volumeTransforms[i] = item.VolumeTransform;
            isAdditive[i] = item.IsAdditive ? 1 : 0;

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