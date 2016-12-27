using UnityEngine;
namespace WahnStudio.TerrainAssets
{
    [ExecuteInEditMode]
    public class HeightShader : MonoBehaviour
    {
        [HideInInspector]
        public Terrain terrain;
        [HideInInspector]
        public MeshRenderer meshRenderer;
        [SerializeField, HideInInspector]
        private Material m_heightmaterial;
        public Material heightMaterial
        {
            get
            {
                if (terrain != null && m_heightmaterial != terrain.materialTemplate) terrain.materialTemplate = m_heightmaterial;
                return m_heightmaterial;
            }
            set
            {
                m_heightmaterial = value;
                if (terrain != null && m_heightmaterial != terrain.materialTemplate) terrain.materialTemplate = m_heightmaterial;

            }
        }

        public float topY
        {
            get
            {
                if (terrain != null)
                    return terrain.terrainData.size.y;
                else
                    return meshRenderer.bounds.size.y;
            }
        }
        [HideInInspector]
        public Shader shader;
        [HideInInspector]
        public HeightShaderProperties properties;
        [HideInInspector]
        public bool isInitialized = false;

        public float toLocal { get { return properties.relationType == HeightShaderProperties.RelationType.Global ? 0.0f : transform.position.y; } }
        public float terrainDistance
        {
            get { return terrain.basemapDistance; }
            set { terrain.basemapDistance = value; }
        }

        public virtual void Initialize(Material material)
        {
            DefaultInitialize(material);
        }

        public virtual void ReadProperties()
        {
            DefaultReadProperties();
        }
        public virtual void WriteProperties()
        {
            DefaultWriteProperties();

        }
        /// <summary>
        /// Initializes the Shader, and creates dependencies
        /// </summary>
        /// <param name="material">The material for the terrain / Mesh</param>
        public void DefaultInitialize(Material material)
        {

            terrain = gameObject.GetComponent<Terrain>();
#if UNITY_EDITOR
            if (material == null)
            {
                if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Materials_HeightShader"))
                {
                    UnityEditor.AssetDatabase.CreateFolder("Assets", "Materials_HeightShader");
                }
                material = new Material(Shader.Find("WahnStudio/HeightShader"));

                UnityEditor.AssetDatabase.CreateAsset(material, "Assets/Materials_HeightShader/" + gameObject.name + "HS.mat");
                heightMaterial = (Material)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/Materials_HeightShader/" + gameObject.name + "HS.mat", typeof(Material));
            }
            else
            {
                heightMaterial = material;
            }
#endif

            ReadProperties();
            if (terrain != null)
            {
                terrain.materialTemplate = heightMaterial;
                terrain.materialType = Terrain.MaterialType.Custom;
            }
            else
            {
                meshRenderer = GetComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = heightMaterial;
            }
            isInitialized = true;
            WriteProperties();

        }
        public void DefaultReadProperties()
        {

            float scaleLevels;
            if (!isInitialized)
            {
                if (terrain != null)
                    scaleLevels = terrain.terrainData.size.y / 260;
                else
                    scaleLevels = GetComponent<MeshRenderer>().bounds.size.y / 260;
            }
            else {
                scaleLevels = 1;
            }
            properties = new HeightShaderProperties(heightMaterial.GetFloat("_WaterLevel") * scaleLevels);
            foreach (HeightShaderLevel level in properties.levels)
            {
                if (level.name != "UnderWater")
                {
                    level.height = heightMaterial.GetFloat("_" + level.name) * scaleLevels;
                }
                else
                {
                    level.height = properties.waterLevel;
                }
                level.texture = heightMaterial.GetTexture("_" + level.name + "Texture");
                level.color = heightMaterial.GetColor("_" + level.name + "Color");
                level.textureTiling = heightMaterial.GetFloat("_" + level.name + "TextureSize");
            }
            properties.brightness = heightMaterial.GetFloat("_Brightness");
            properties.slopeFader = heightMaterial.GetFloat("_Slope");
        }
        public void CurrentGlobalToLocal()
        {
            foreach (HeightShaderLevel level in properties.levels)
            {
                if (level.name != "UnderWater")
                {
                    level.height -= transform.position.y;
                }
                else
                {
                    properties.waterLevel = properties.waterLevel - transform.position.y;
                    level.height = properties.waterLevel;
                }
            }
            WriteProperties();
            properties.relationType = HeightShaderProperties.RelationType.Local;
        }
        public void DefaultWriteProperties()
        {

            if (heightMaterial != null)
            {
                foreach (HeightShaderLevel level in properties.levels)
                {
                    if (level.name != "UnderWater")
                    {
                        heightMaterial.SetFloat("_" + level.name, level.height + toLocal);
                    }
                    heightMaterial.SetTexture("_" + level.name + "Texture", level.texture);
                    heightMaterial.SetColor("_" + level.name + "Color", level.color);
                    heightMaterial.SetFloat("_" + level.name + "TextureSize", level.textureTiling);
                }
                heightMaterial.SetFloat("_WaterLevel", properties.waterLevel + toLocal);
                heightMaterial.SetFloat("_Slope", properties.slopeFader);
                heightMaterial.SetFloat("_Brightness", properties.brightness);
            }
            else {
                Debug.LogError("Height Shader has been corrupted, please Initialize");
                isInitialized = false;
            }
        }
    }
}


