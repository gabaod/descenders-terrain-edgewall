using UnityEngine;
using UnityEditor;

public class TerrainEdgePlacerWindow : EditorWindow
{
    private GameObject treePrefab;
    private float spacing = 5f;
    private bool northEdge = true;
    private bool southEdge = true;
    private bool eastEdge = true;
    private bool westEdge = true;
    private Terrain targetTerrain;
    private GameObject parentObject;
    private bool showPreview = true;
    private Color previewColor = Color.green;
    
    [MenuItem("Tools/Terrain Edge Placer")]
    public static void ShowWindow()
    {
        TerrainEdgePlacerWindow window = GetWindow<TerrainEdgePlacerWindow>("Terrain Edge Placer");
        window.Show();
    }
    
    private void OnEnable()
    {
        SceneView.onSceneGUIDelegate += OnSceneGUI;
    }
    
    private void OnDisable()
    {
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Tree Settings", EditorStyles.boldLabel);
        treePrefab = (GameObject)EditorGUILayout.ObjectField("Tree Prefab", treePrefab, typeof(GameObject), false);
        spacing = EditorGUILayout.FloatField("Spacing", spacing);
        
        EditorGUILayout.Space();
        GUILayout.Label("Edge Selection", EditorStyles.boldLabel);
        northEdge = EditorGUILayout.Toggle("North Edge", northEdge);
        southEdge = EditorGUILayout.Toggle("South Edge", southEdge);
        eastEdge = EditorGUILayout.Toggle("East Edge", eastEdge);
        westEdge = EditorGUILayout.Toggle("West Edge", westEdge);
        
        EditorGUILayout.Space();
        GUILayout.Label("Terrain Reference", EditorStyles.boldLabel);
        targetTerrain = (Terrain)EditorGUILayout.ObjectField("Target Terrain", targetTerrain, typeof(Terrain), true);
        
        EditorGUILayout.Space();
        GUILayout.Label("Parent Object", EditorStyles.boldLabel);
        parentObject = (GameObject)EditorGUILayout.ObjectField("Parent (Optional)", parentObject, typeof(GameObject), true);
        EditorGUILayout.HelpBox("Trees will be created as children of this object. If not set, trees will be created at root level.", MessageType.Info);
        
        EditorGUILayout.Space();
        GUILayout.Label("Preview", EditorStyles.boldLabel);
        showPreview = EditorGUILayout.Toggle("Show Preview", showPreview);
        previewColor = EditorGUILayout.ColorField("Preview Color", previewColor);
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        GUILayout.Label("Actions", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Place Trees", GUILayout.Height(40)))
        {
            PlaceTrees();
        }
        
        if (GUILayout.Button("Clear Trees", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("Clear Trees", 
                "Are you sure you want to clear all trees from the parent object?", "Yes", "No"))
            {
                ClearTrees();
            }
        }
        
        if (showPreview)
        {
            SceneView.RepaintAll();
        }
    }
    
    private void OnSceneGUI(SceneView sceneView)
    {
        if (!showPreview || targetTerrain == null)
            return;
        
        TerrainData terrainData = targetTerrain.terrainData;
        Vector3 terrainPos = targetTerrain.transform.position;
        float terrainWidth = terrainData.size.x;
        float terrainLength = terrainData.size.z;
        
        Handles.color = previewColor;
        
        // North edge (Z = max)
        if (northEdge)
        {
            DrawEdgePreview(terrainPos, terrainWidth, terrainLength, true, terrainLength);
        }
        
        // South edge (Z = 0)
        if (southEdge)
        {
            DrawEdgePreview(terrainPos, terrainWidth, terrainLength, true, 0);
        }
        
        // East edge (X = max)
        if (eastEdge)
        {
            DrawEdgePreview(terrainPos, terrainWidth, terrainLength, false, terrainWidth);
        }
        
        // West edge (X = 0)
        if (westEdge)
        {
            DrawEdgePreview(terrainPos, terrainWidth, terrainLength, false, 0);
        }
    }
    
    private void DrawEdgePreview(Vector3 terrainPos, float width, float length, bool isHorizontal, float offset)
    {
        if (isHorizontal)
        {
            // North or South edge
            int count = Mathf.FloorToInt(width / spacing);
            for (int i = 0; i <= count; i++)
            {
                float x = Mathf.Min(i * spacing, width);
                float height = GetTerrainHeight(terrainPos, x, offset);
                Vector3 worldPos = new Vector3(terrainPos.x + x, terrainPos.y + height, terrainPos.z + offset);
                Handles.DrawWireCube(worldPos, Vector3.one * 0.5f);
            }
        }
        else
        {
            // East or West edge
            int count = Mathf.FloorToInt(length / spacing);
            for (int i = 0; i <= count; i++)
            {
                float z = Mathf.Min(i * spacing, length);
                float height = GetTerrainHeight(terrainPos, offset, z);
                Vector3 worldPos = new Vector3(terrainPos.x + offset, terrainPos.y + height, terrainPos.z + z);
                Handles.DrawWireCube(worldPos, Vector3.one * 0.5f);
            }
        }
    }
    
    private float GetTerrainHeight(Vector3 terrainPos, float localX, float localZ)
    {
        if (targetTerrain == null)
            return 0;
        
        TerrainData terrainData = targetTerrain.terrainData;
        
        // Convert local position to normalized terrain coordinates (0-1)
        float normalizedX = localX / terrainData.size.x;
        float normalizedZ = localZ / terrainData.size.z;
        
        // Clamp to valid range
        normalizedX = Mathf.Clamp01(normalizedX);
        normalizedZ = Mathf.Clamp01(normalizedZ);
        
        // Get interpolated height from heightmap
        return terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
    }
    
    private void PlaceTrees()
    {
        if (treePrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Tree prefab is not assigned!", "OK");
            return;
        }
        
        if (targetTerrain == null)
        {
            EditorUtility.DisplayDialog("Error", "Target terrain is not assigned!", "OK");
            return;
        }
        
        // Create parent object if not assigned
        if (parentObject == null)
        {
            parentObject = new GameObject("TerrainEdgeTrees");
            Undo.RegisterCreatedObjectUndo(parentObject, "Create Tree Parent");
        }
        
        // Clear existing children
        while (parentObject.transform.childCount > 0)
        {
            DestroyImmediate(parentObject.transform.GetChild(0).gameObject);
        }
        
        TerrainData terrainData = targetTerrain.terrainData;
        Vector3 terrainPos = targetTerrain.transform.position;
        float terrainWidth = terrainData.size.x;
        float terrainLength = terrainData.size.z;
        
        int totalTrees = 0;
        
        // Place trees on selected edges
        if (northEdge)
        {
            totalTrees += PlaceTreesOnEdge(terrainPos, terrainWidth, terrainLength, true, terrainLength, "North");
        }
        
        if (southEdge)
        {
            totalTrees += PlaceTreesOnEdge(terrainPos, terrainWidth, terrainLength, true, 0, "South");
        }
        
        if (eastEdge)
        {
            totalTrees += PlaceTreesOnEdge(terrainPos, terrainWidth, terrainLength, false, terrainWidth, "East");
        }
        
        if (westEdge)
        {
            totalTrees += PlaceTreesOnEdge(terrainPos, terrainWidth, terrainLength, false, 0, "West");
        }
        
        EditorUtility.DisplayDialog("Success", "Trees placed successfully! Total: " + totalTrees, "OK");
        Debug.Log("Trees placed successfully! Total: " + totalTrees);
    }
    
    private int PlaceTreesOnEdge(Vector3 terrainPos, float width, float length, bool isHorizontal, float offset, string edgeName)
    {
        int count = 0;
        
        if (isHorizontal)
        {
            // North or South edge
            int treeCount = Mathf.FloorToInt(width / spacing);
            for (int i = 0; i <= treeCount; i++)
            {
                float x = Mathf.Min(i * spacing, width);
                float height = GetTerrainHeight(terrainPos, x, offset);
                Vector3 worldPos = terrainPos + new Vector3(x, height, offset);
                
                GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab);
                tree.transform.position = worldPos;
                tree.transform.parent = parentObject.transform;
                tree.name = "Tree_" + edgeName + "_" + i;
                Undo.RegisterCreatedObjectUndo(tree, "Place Tree");
                count++;
            }
        }
        else
        {
            // East or West edge
            int treeCount = Mathf.FloorToInt(length / spacing);
            for (int i = 0; i <= treeCount; i++)
            {
                float z = Mathf.Min(i * spacing, length);
                float height = GetTerrainHeight(terrainPos, offset, z);
                Vector3 worldPos = terrainPos + new Vector3(offset, height, z);
                
                GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab);
                tree.transform.position = worldPos;
                tree.transform.parent = parentObject.transform;
                tree.name = "Tree_" + edgeName + "_" + i;
                Undo.RegisterCreatedObjectUndo(tree, "Place Tree");
                count++;
            }
        }
        
        return count;
    }
    
    private void ClearTrees()
    {
        if (parentObject == null)
        {
            EditorUtility.DisplayDialog("Error", "No parent object assigned!", "OK");
            return;
        }
        
        Undo.RegisterFullObjectHierarchyUndo(parentObject, "Clear Trees");
        
        while (parentObject.transform.childCount > 0)
        {
            DestroyImmediate(parentObject.transform.GetChild(0).gameObject);
        }
        
        Debug.Log("All trees cleared!");
    }
}
