using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.IO;

public class AtlasCreatorExample : MonoBehaviour {

    public BakeryVolume[] BakeryVolumes;
    public float[] BakeryVolumesWeights;

    public int stochasticIterations = 5000;

    [SerializeField] private Texture3D generatedAtlas;
    [SerializeField] private Vector3[] boundsMin;
    [SerializeField] private Vector3[] boundsMax;
    [SerializeField] private Texture3DAtlasGenerator.Atlas3D Atlas;
    [SerializeField] private Vector3[] boundsWMin;
    [SerializeField] private Vector3[] boundsWMax;

    [ContextMenu("Generate 3D Atlas")]
    private void GenerateAtlas() {

        Texture3D[] Textures = new Texture3D[BakeryVolumes.Length * 3];

        for (int i = 0; i < BakeryVolumes.Length; i ++) {
            if(BakeryVolumes[i].bakedTexture0 == null || BakeryVolumes[i].bakedTexture1 == null || BakeryVolumes[i].bakedTexture2 == null) {
                Debug.LogError("One of the bakery volumes is not baked!");
                return;
            }
            Textures[i * 3]     = BakeryVolumes[i].bakedTexture0;
            Textures[i * 3 + 1] = BakeryVolumes[i].bakedTexture1;
            Textures[i * 3 + 2] = BakeryVolumes[i].bakedTexture2;
        }

        Atlas = Texture3DAtlasGenerator.CreateAtlasStochastic(Textures, stochasticIterations);
        generatedAtlas = Atlas.Texture;

        boundsMin = Atlas.BoundsUvwMin;
        boundsMax = Atlas.BoundsUvwMax;

        boundsWMin = new Vector3[BakeryVolumes.Length];
        boundsWMax = new Vector3[BakeryVolumes.Length];

        for (int i = 0; i < BakeryVolumes.Length; i++) {
            boundsWMin[i] = BakeryVolumes[i].bounds.min;
            boundsWMax[i] = BakeryVolumes[i].bounds.max;
        }

        SaveTexture3DAsAsset(generatedAtlas, "Atlas3D.asset", true);


    }

    [ContextMenu("SetSaderVars")]
    private void SetVars() {
        SetShaderVariables(BakeryVolumes);
    }

    private void Update() {
        if (Atlas.Texture != null && BakeryVolumes != null && BakeryVolumes.Length != 0)
        SetShaderVariables(BakeryVolumes);
    }

    private void SetShaderVariables(BakeryVolume[] volumes) {

        Shader.SetKeyword(GlobalKeyword.Create("LightVolumesEnabled"), true);

        float[] LightVolumeWeight = new float[256];
        Vector4[] LightVolumeWorldMin = new Vector4[256];
        Vector4[] LightVolumeWorldMax = new Vector4[256];
        Vector4[] LightVolumeUvwMin = new Vector4[768];
        Vector4[] LightVolumeUvwMax = new Vector4[768];

        for (int i = 0; i < volumes.Length; i++) {

            // Weight
            LightVolumeWeight[i] = BakeryVolumesWeights.Length > 0 ? BakeryVolumesWeights[Mathf.Clamp(i, 0, BakeryVolumesWeights.Length)] : 0;
            
            // World bounds
            LightVolumeWorldMin[i] = boundsWMin[i];
            LightVolumeWorldMax[i] = boundsWMax[i];

            // UVW bounds
            LightVolumeUvwMin[i * 3] = boundsMin[i * 3];
            LightVolumeUvwMax[i * 3] = boundsMax[i * 3];
            LightVolumeUvwMin[i * 3 + 1] = boundsMin[i * 3 + 1];
            LightVolumeUvwMax[i * 3 + 1] = boundsMax[i * 3 + 1];
            LightVolumeUvwMin[i * 3 + 2] = boundsMin[i * 3 + 2];
            LightVolumeUvwMax[i * 3 + 2] = boundsMax[i * 3 + 2];

        }

        Shader.SetGlobalFloat("_UdonLightVolumeCount", volumes.Length);
        Shader.SetGlobalTexture("_UdonLightVolume", generatedAtlas);

        Shader.SetGlobalFloatArray("_UdonLightVolumeWeight", LightVolumeWeight);
        Shader.SetGlobalVectorArray("_UdonLightVolumeWorldMin", LightVolumeWorldMin);
        Shader.SetGlobalVectorArray("_UdonLightVolumeWorldMax", LightVolumeWorldMax);

        Shader.SetGlobalVectorArray("_UdonLightVolumeUvwMin", LightVolumeUvwMin);
        Shader.SetGlobalVectorArray("_UdonLightVolumeUvwMax", LightVolumeUvwMax);

    }

    public static bool SaveTexture3DAsAsset(Texture3D textureToSave, string assetPath, bool overwriteExisting = false) {
        if (textureToSave == null) {
            Debug.LogError("Ошибка сохранения Texture3D: Переданная текстура равна null.");
            return false;
        }

        if (string.IsNullOrEmpty(assetPath)) {
            Debug.LogError("Ошибка сохранения Texture3D: Путь для сохранения не может быть пустым.");
            return false;
        }

        // 1. Нормализация пути и проверка префикса "Assets/"
        string normalizedPath = assetPath.Replace("\\", "/");
        if (!normalizedPath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase)) {
            // Если путь не начинается с Assets/, пытаемся добавить его.
            // Считаем, что пользователь указал путь внутри Assets.
            if (normalizedPath.StartsWith("/")) // Убираем ведущий слэш, если он есть
            {
                normalizedPath = normalizedPath.Substring(1);
            }
            normalizedPath = "Assets/" + normalizedPath;
            Debug.LogWarning($"Путь '{assetPath}' не начинался с 'Assets/'. Используется путь '{normalizedPath}'.");
        }


        // 2. Проверка и добавление расширения .asset
        string extension = Path.GetExtension(normalizedPath);
        if (string.IsNullOrEmpty(extension)) {
            // Расширение отсутствует, добавляем .asset
            normalizedPath += ".asset";
            Debug.Log($"Добавлено расширение '.asset'. Итоговый путь: '{normalizedPath}'.");
        } else if (!extension.Equals(".asset", System.StringComparison.OrdinalIgnoreCase)) {
            // Указано другое расширение, это может быть ошибкой
            Debug.LogWarning($"Путь '{assetPath}' имеет расширение '{extension}', а не '.asset'. Сохранение Texture3D обычно выполняется в .asset файл.");
            // Тем не менее, попробуем сохранить с указанным расширением, если пользователь так хочет.
        }


        // 3. Создание директории, если она не существует
        try {
            string directoryPath = Path.GetDirectoryName(normalizedPath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath)) {
                Directory.CreateDirectory(directoryPath);
                Debug.Log($"Создана директория: '{directoryPath}'");
                // Важно обновить базу данных ассетов ПОСЛЕ создания директории,
                // чтобы Unity ее "увидел" перед созданием ассета внутри нее.
                AssetDatabase.Refresh();
            }
        } catch (System.Exception e) {
            Debug.LogError($"Ошибка при создании директории для '{normalizedPath}': {e.Message}");
            return false;
        }

        // 4. Обработка существующего файла / Генерация уникального пути
        string finalPath = normalizedPath;
        if (!overwriteExisting && File.Exists(finalPath)) // Используем File.Exists для проверки, т.к. AssetDatabase может не сразу обновиться
        {
            finalPath = AssetDatabase.GenerateUniqueAssetPath(normalizedPath);
            Debug.Log($"Файл '{normalizedPath}' уже существует. Генерируется уникальный путь: '{finalPath}'.");
        } else if (overwriteExisting && File.Exists(finalPath)) {
            Debug.LogWarning($"Перезапись существующего файла: '{finalPath}'.");
        }


        // 5. Создание ассета
        try {
            // Создаем ассет в базе данных Unity
            AssetDatabase.CreateAsset(textureToSave, finalPath);

            // Опционально, но рекомендуется: пометить ассет как "грязный", чтобы изменения точно сохранились
            EditorUtility.SetDirty(textureToSave);

            // Сохраняем изменения в базе данных ассетов
            AssetDatabase.SaveAssets();

            // Опционально: Обновить окно проекта, чтобы показать новый файл
            AssetDatabase.Refresh();

            // Опционально: Выделить созданный ассет в окне проекта
            // EditorGUIUtility.PingObject(textureToSave);

            Debug.Log($"Texture3D успешно сохранена как ассет по пути: '{finalPath}'");
            return true;
        } catch (System.Exception e) {
            Debug.LogError($"Ошибка при создании или сохранении ассета Texture3D по пути '{finalPath}': {e.Message}");
            return false;
        }
    }

}