using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.GridPrototype;

public static class GridPrototypeBuilder
{
    public const string ScenePath = "Assets/Scenes/GridPrototype.unity";

    [MenuItem("Tools/Game/Create Grid Prototype Scene")]
    public static void CreateScene()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("Stop play mode before creating the prototype scene."); return; }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)) { Debug.Log("Grid prototype scene already exists; not overwriting."); return; }
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(9, 23, -11);
            camera.transform.LookAt(new Vector3(9, 0, 7));
            camera.orthographic = false; camera.fieldOfView = 55;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.035f, 0.045f, 0.065f);
            var light = new GameObject("Directional Light", typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(45, -30, 0);
            var arena = new GameObject("Grid Prototype", typeof(GridPrototypeArena)).GetComponent<GridPrototypeArena>();
            arena.SurfaceMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Player.mat");
            arena.ArenaCamera = camera;
            arena.OverlayShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Scripts/GridPrototype/GridOverlay.shader");
            if (!arena.SurfaceMaterial) throw new System.InvalidOperationException("Prototype requires the existing M_Player material.");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Created " + ScenePath + ". Open it and press Play to test movement and action budgets.");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
        }
    }
}
