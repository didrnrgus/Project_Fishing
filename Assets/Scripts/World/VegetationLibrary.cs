using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 실사풍 식생·바위 참조 묶음 (Assets/Resources/VegetationLibrary.asset). 빌드할 때 에디터(Editor/VegetationBake)가 채운다.
    /// 프리팹이 Resources 밖(Assets/Vegetation/Prefabs)에 있어도 여기서 참조하면 빌드에 들어간다. 만드는 곳은 tools/blender/build_vegetation.py.
    /// - conifers: ez-tree 소나무 3종 (잎 Fishing/Foliage, LOD1 = 십자 판)
    /// - broadleaf: ez-tree 참나무·물푸레·사시나무 4종
    /// - rocks / snowRocks: Poly Haven 바위 9종 (Fishing/Rock, 눈 판은 _Snow)
    /// - grass / flowers: Poly Haven 풀·들꽃을 찍은 카드 그림 (<see cref="GrassField"/>)
    /// </summary>
    public class VegetationLibrary : ScriptableObject
    {
        public GameObject[] conifers;
        public GameObject[] broadleaf;
        public GameObject[] rocks;
        public GameObject[] snowRocks;
        public Texture2D[] grass;
        public Texture2D[] flowers;

        static VegetationLibrary _lib;
        static bool _loaded;

        /// <summary>없으면 null (에셋을 지우거나 아직 굽지 않은 프로젝트에서는 예전 방식으로 그린다).</summary>
        public static VegetationLibrary Lib
        {
            get
            {
                if (!_loaded)
                {
                    _lib = Resources.Load<VegetationLibrary>("VegetationLibrary");
                    _loaded = true;
                }
                return _lib;
            }
        }

        public static bool HasConifers => Lib != null && Lib.conifers != null && Lib.conifers.Length > 0;
        public static bool HasBroadleaf => Lib != null && Lib.broadleaf != null && Lib.broadleaf.Length > 0;
        public static bool HasRocks => Lib != null && Lib.rocks != null && Lib.rocks.Length > 0;
        public static bool HasGrass => Lib != null && Lib.grass != null && Lib.grass.Length > 0;
    }
}
