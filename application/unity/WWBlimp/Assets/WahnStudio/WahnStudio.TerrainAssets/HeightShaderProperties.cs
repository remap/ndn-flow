using System.Collections.Generic;

namespace WahnStudio.TerrainAssets
{
    [System.Serializable]
    public class HeightShaderProperties
    {
        public enum RelationType
        {
            Global,
            Local
        }
        public HeightShaderLevel[] levels;
        public float waterLevel;
        public float slopeFader;
        public float brightness;
        public float baseMapDistance { get; set; }
        public RelationType relationType = RelationType.Global;
        public HeightShaderProperties(float _waterLevel)
        {
            levels = SetLevels();
            waterLevel = _waterLevel;
        }
        public HeightShaderLevel[] SetLevels()
        {
            List<HeightShaderLevel> listLevel = new List<HeightShaderLevel>();
            string thisName = "";
            for (int i = 0; i < 6; i++)
            {
                switch (i)
                {
                    case 0:
                        thisName = "UnderWater";
                        break;
                    case 1:
                        thisName = "Beach";
                        break;
                    case 2:
                        thisName = "Level1";
                        break;
                    case 3:
                        thisName = "Level2";
                        break;
                    case 4:
                        thisName = "Level3";
                        break;
                    case 5:
                        thisName = "LastLevel";
                        break;
                }
                listLevel.Add(new HeightShaderLevel(thisName));
            }
            return listLevel.ToArray();
        }
    }
}
