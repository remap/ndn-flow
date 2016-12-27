using UnityEngine;
namespace WahnStudio.TerrainAssets
{
    [System.Serializable]
    public class HeightShaderLevel
    {
        [SerializeField, HideInInspector]
        private string _name;
        public Texture texture;
        public Color color;
        public float height, textureTiling;
        [SerializeField, HideInInspector]
        private bool readOnlyName;

        public string name
        {
            get
            {
                return _name;
            }
            set
            {
                if (readOnlyName)
                {
                    Debug.LogError("The Name of each level is Read Only!");
                }
                else
                {
                    readOnlyName = true;
                    _name = value;
                }
            }
        }

        public HeightShaderLevel(string _name)
        {
            name = _name;
        }
    }
}
