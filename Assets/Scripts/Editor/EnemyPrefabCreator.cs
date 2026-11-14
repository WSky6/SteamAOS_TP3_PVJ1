using UnityEditor;
using UnityEngine;

public static class EnemyPrefabCreator
{
    [MenuItem("Tools/Enemigos/Crear prefabs de enemigos")]
    static void CreateAll()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        Make("EnemyMelee", "Melee_Basico", 1f, typeof(EnemyMelee));
        Make("EnemyRanged", "Distancia_Basico", 1f, typeof(EnemyRanged));
        Make("EnemyBoss", "Boss_Data", 1.5f, typeof(EnemyBoss));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void Make(string prefabName, string dataName, float scale, System.Type scriptType)
    {
        string path = "Assets/Prefabs/" + prefabName + ".prefab";

        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.Log($"{prefabName}: ya existe, no lo toco.");
            return;
        }

        EnemyData data = FindData(dataName);
        if (data == null)
        {
            Debug.LogError($"No encuentro el asset '{dataName}'. Creá el asset y probá de nuevo.");
            return;
        }

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = prefabName;
        go.transform.localScale = Vector3.one * scale;

        // Los RequireComponent agregan solos el NavMeshAgent (y el EnemyJumper del boss)
        Component script = go.AddComponent(scriptType);
        go.AddComponent<EnemyWeapon>();

        SerializedObject so = new SerializedObject(script);
        so.FindProperty("data").objectReferenceValue = data;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        Debug.Log($"Prefab creado: {path}");
    }

    static EnemyData FindData(string assetName)
    {
        string[] guids = AssetDatabase.FindAssets(assetName + " t:EnemyData");

        foreach (string g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (System.IO.Path.GetFileNameWithoutExtension(p) == assetName)
                return AssetDatabase.LoadAssetAtPath<EnemyData>(p);
        }
        return null;
    }
}