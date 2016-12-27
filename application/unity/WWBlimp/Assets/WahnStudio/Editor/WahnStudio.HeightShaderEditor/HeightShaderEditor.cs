using System.Linq;
using UnityEditor;
using UnityEngine;
using WahnStudio.TerrainAssets;
namespace WahnStudio.AssetsEditor
{

    [CustomEditor(typeof(HeightShader), true)]
    internal class HeightShaderEditor : Editor
    {
        private HeightShader hShader
        {
            get { return (HeightShader)target; }
        }

        private SerializedProperty properties, waterLevel;
        private bool realTimeEdit = true;
        private static readonly string[] _dontIncludeMe = new string[] { "m_Script" };
        bool initializeSubMenu = false;
        Material mat;
        HeightShaderProperties.RelationType saveRelation;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            WahnStudiosTag();
            if (hShader != null && hShader.isInitialized && realTimeEdit)
            {
                hShader.WriteProperties();
            }
            var labelStyle = GUI.skin.GetStyle("Label");
            if (hShader.isInitialized)
            {
                GUILayout.Label("Material:" + hShader.heightMaterial.name);
                BaseMapSlider();
                labelStyle.fontStyle = FontStyle.Bold;
                saveRelation = hShader.properties.relationType;
                hShader.properties.relationType = (HeightShaderProperties.RelationType)EditorGUILayout.EnumPopup("Y Relation", hShader.properties.relationType as System.Enum);
                if (saveRelation != hShader.properties.relationType) SceneView.RepaintAll();
                if (hShader.properties.relationType == HeightShaderProperties.RelationType.Global)
                    if (GUILayout.Button("Current Look to Local")) hShader.CurrentGlobalToLocal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Water Level", GUILayout.Width(140));
                hShader.properties.waterLevel = GUILayout.HorizontalSlider(hShader.properties.waterLevel, 0, hShader.properties.levels[1].height, GUILayout.ExpandWidth(true));
                hShader.properties.waterLevel = EditorGUILayout.DelayedFloatField(hShader.properties.waterLevel, GUILayout.Width(50));
                GUILayout.EndHorizontal();
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Slope Fader");
                hShader.properties.slopeFader = GUILayout.HorizontalSlider(hShader.properties.slopeFader, 0, 1, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Brightness");
                hShader.properties.brightness = GUILayout.HorizontalSlider(hShader.properties.brightness, 0, 1, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                realTimeEdit = GUILayout.Toggle(realTimeEdit, "Write properties in real time");
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                int i = 0;
                foreach (HeightShaderLevel level in hShader.properties.levels.Reverse())
                {
                    GUILayout.BeginVertical("Box");
                    labelStyle.alignment = TextAnchor.MiddleCenter;
                    labelStyle.fontSize = 13;
                    GUILayout.Label(level.name);
                    labelStyle.alignment = TextAnchor.UpperLeft;
                    labelStyle.fontSize = 11;
                    GUILayout.Label("Height");
                    GUILayout.BeginHorizontal();
                    if (i == 0)
                    {
                        level.height = GUILayout.HorizontalSlider(level.height, hShader.properties.levels[4 - i].height, hShader.topY, GUILayout.ExpandWidth(true));
                        level.height = EditorGUILayout.FloatField(Mathf.Clamp(level.height, hShader.properties.levels[4 - i].height, hShader.topY), GUILayout.Width(100));
                    }
                    else if (i < 5)
                    {
                        level.height = GUILayout.HorizontalSlider(level.height, hShader.properties.levels[4 - i].height, hShader.properties.levels[6 - i].height, GUILayout.ExpandWidth(true));
                        level.height = EditorGUILayout.FloatField(Mathf.Clamp(level.height, hShader.properties.levels[4 - i].height, hShader.properties.levels[5 - i].height), GUILayout.Width(100));

                    }
                    else if (i == 5)
                    {
                        level.height = hShader.properties.waterLevel;
                        GUILayout.Label("Under Water Level: " + hShader.properties.waterLevel.ToString());
                    }

                    GUILayout.EndHorizontal();
                    level.texture = (Texture)EditorGUILayout.ObjectField(level.texture, typeof(Texture), true);
                    GUILayout.BeginHorizontal();
                    GUILayout.Box(level.texture, GUILayout.Width(120), GUILayout.Height(120));
                    GUILayout.BeginVertical();
                    GUILayout.Label("Tint Color");
                    level.color = EditorGUILayout.ColorField(level.color);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Texture Tiling");
                    level.textureTiling = EditorGUILayout.FloatField(level.textureTiling, GUILayout.Width(100));
                    GUILayout.EndHorizontal();
                    GUI.enabled = !realTimeEdit;
                    if (GUILayout.Button("Write Properties")) hShader.WriteProperties();
                    GUI.enabled = true;
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    i++;
                }

            }

            if (initializeSubMenu)
            {
                labelStyle.alignment = TextAnchor.MiddleCenter;
                GUILayout.Label("Initialization Options");
                Material mattest = mat;
                var saveRelation = hShader.properties.relationType = (HeightShaderProperties.RelationType)EditorGUILayout.EnumPopup("Relation Type", hShader.properties.relationType as System.Enum);
                mat = (Material)EditorGUILayout.ObjectField(new GUIContent("Height Shader Material*"), mat, typeof(Material), true);
                labelStyle.fontSize = 10;
                labelStyle.alignment = TextAnchor.UpperRight;
                GUILayout.Label("*Leave Empty if you don't have a Height Shader Material Asset");
                if (mat & mattest != null)
                    if (mattest != mat && mat.shader.name != "WahnStudio/HeightShader")
                    {
                        mat = null;
                        EditorUtility.DisplayDialog("Error!", "You Must use a Height Shader Material ", "OK");
                    }
                if (GUILayout.Button("OK"))
                {
                    hShader.Initialize(mat);
                    initializeSubMenu = false;
                    if (saveRelation == HeightShaderProperties.RelationType.Local) hShader.CurrentGlobalToLocal();

                }
                labelStyle.fontSize = 11;
            }
            else {
                labelStyle.alignment = TextAnchor.MiddleCenter;
                if (!hShader.isInitialized) GUILayout.Label("Thanks for Buying this asset, \n \n please use the INITIALIZE button");
                labelStyle.alignment = TextAnchor.UpperLeft;
                if (GUILayout.Button("Initialize"))
                {
                    if (hShader.isInitialized)
                    {
                        if (EditorUtility.DisplayDialog("Set Default Height Shader Options", "Are you sure that you want to recreate the Terrain /n if you click Recreate you will set the HeightShader to the Default values", "Recreate", "Cancel"))
                        {
                            hShader.isInitialized = false;
                            hShader.heightMaterial = null;
                            SceneView.RepaintAll();
                            initializeSubMenu = true;
                        }
                    }
                    else {

                        initializeSubMenu = true;
                    }

                }
            }

            labelStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("User Properties");
            labelStyle.alignment = TextAnchor.UpperLeft;
            GUILayout.BeginVertical("Box");
            DrawPropertiesExcluding(serializedObject, _dontIncludeMe);
            GUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();

        }
        float saveY;
        void OnSceneGUI()
        {
            if (!hShader.isInitialized) return;
            if (hShader.properties.relationType == HeightShaderProperties.RelationType.Local)
            {
                if (saveY != hShader.gameObject.transform.position.y) hShader.WriteProperties();
                saveY = hShader.gameObject.transform.position.y;

            }
        }
        private void WahnStudiosTag()
        {
            var tal = AssetDatabase.LoadMainAssetAtPath("Assets/WahnStudio/icon.png");
            GUILayout.BeginHorizontal();
            GUILayout.Box(AssetPreview.GetAssetPreview(tal), GUILayout.Width(120), GUILayout.Height(120));
            GUILayout.BeginVertical();
            GUILayout.BeginVertical("box", GUILayout.Height(80));
            var labelStyle = GUI.skin.GetStyle("Label");
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.fontStyle = FontStyle.Bold;
            GUILayout.Label("Height Shader");
            string scriptText = "Version 1.1";
            labelStyle.alignment = TextAnchor.UpperCenter;
            GUILayout.Label(scriptText, EditorStyles.centeredGreyMiniLabel);
            GUILayout.Label("", EditorStyles.centeredGreyMiniLabel);
            labelStyle.alignment = TextAnchor.LowerLeft;
            GUILayout.EndVertical();
            if (GUILayout.Button("Open Documentation", GUILayout.Height(30)))
            {
                AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath("Assets/WahnStudio/HeightShaderDoc.pdf", typeof(Object)));
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

        }
        private void BaseMapSlider()
        {
            if (hShader.terrain == null) return;
            EditorGUI.BeginChangeCheck();
            hShader.properties.baseMapDistance = EditorGUILayout.IntSlider("Base Map Distance", (int)hShader.properties.baseMapDistance, 0, 30000);
            if (EditorGUI.EndChangeCheck())
            {
                if (hShader.terrain.basemapDistance != hShader.properties.baseMapDistance) SceneView.RepaintAll();
                hShader.terrain.basemapDistance = hShader.properties.baseMapDistance;

            }
        }
    }
}