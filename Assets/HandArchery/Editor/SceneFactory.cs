using System.IO;
using HandArchery.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HandArchery.EditorTools
{
    public static class SceneFactory
    {
        public const string ScenePath = "Assets/HandArchery/Scenes/HandArchery.unity";
        const string MaterialPath = "Assets/HandArchery/Materials/RangeLit.mat";
        [MenuItem("HandArchery/Create Playable Archery Scene")]
        public static void CreateAndSave()
        {
            Directory.CreateDirectory("Assets/HandArchery/Scenes");
            Directory.CreateDirectory("Assets/HandArchery/Materials");
            AssetDatabase.Refresh();
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new System.InvalidOperationException("URP Lit shader missing; retain the project's URP template pipeline.");
                material = new Material(shader) { name = "Range Lit", color = Color.white };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            if (Resources.Load<Font>("Fonts/NotoSansCJKkr-Regular") == null)
                throw new System.InvalidOperationException("Import the staged Resources/Fonts font and OFL before creating the scene.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var experience = new GameObject("Hand Archery Experience").AddComponent<ArcheryExperience>();
            experience.SetRangeMaterial(material);
            EditorUtility.SetDirty(experience);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Cannot save " + ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("HANDARCHERY SCENE SAVED: " + ScenePath + "; default input=real camera; runtime geometry/UI bootstrapped by ArcheryExperience");
        }
    }
}
