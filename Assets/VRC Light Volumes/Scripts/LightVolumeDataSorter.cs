using System.Collections.Generic;
using System.Linq;

public static class LightVolumeDataSorter {

    public static List<LightVolumeData> SortData(List<LightVolumeData> lightVolumeDataList) {
        lightVolumeDataList.RemoveAll(item => item.LightVolumeInstance == null);
        return lightVolumeDataList.OrderByDescending(item => item.Weight).ToList();
    }

    public static LightVolumeInstance[] GetData(List<LightVolumeData> sortedData) {
        int count = sortedData.Count;
        LightVolumeInstance[] volumes = new LightVolumeInstance[count];
        for (int i = 0; i < count; ++i) {
            volumes[i] = sortedData[i].LightVolumeInstance;
        }
        return volumes;
    }

}