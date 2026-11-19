using UnityEngine;
using UnityEditor;

public static class GlowLinePrefabCreator
{
    [MenuItem("Tools/创建可交互发光线条预制体")]
    public static void Create()
    {
        const string dir = "Assets/Resources/Interaction";
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Resources", "Interaction");

        string matPath = dir + "/GlowLineMat.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            Shader s = Shader.Find("Sprites/Default");
            if (s == null) s = Shader.Find("Unlit/Color");
            mat = new Material(s) { name = "GlowLineMat" };
            AssetDatabase.CreateAsset(mat, matPath);
        }

        GameObject go = new GameObject("GlowLinePrefab");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = mat;
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.startWidth = 0.06f;
        lr.endWidth = 0.06f;
        lr.startColor = new Color(1f, 0.92f, 0.55f, 1f);
        lr.endColor = new Color(1f, 0.92f, 0.55f, 1f);
        lr.numCapVertices = 2;
        lr.numCornerVertices = 2;
        lr.alignment = LineAlignment.View;
        lr.textureMode = LineTextureMode.Stretch;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.sortingOrder = 100;

        PrefabUtility.SaveAsPrefabAsset(go, dir + "/GlowLinePrefab.prefab");
        Object.DestroyImmediate(go);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[高亮] 已生成：\n" + matPath + "\n" + dir + "/GlowLinePrefab.prefab");
    }
}
