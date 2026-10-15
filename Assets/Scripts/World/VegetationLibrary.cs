using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 받은 실사풍 식생·바위 에셋 참조 묶음 (Assets/Resources/VegetationLibrary.asset). 빌드할 때 에디터(Editor/VegetationBake)가 채운다.
    /// 프리팹이 Resources 밖(Forst·Rocks and Boulders 2·Tree9/10 굽기 결과)에 있어도 여기서 참조하면 빌드에 들어간다.
    /// - conifers: Conifers [BOTD] (forst) 소나무 3종 (CTI 셰이더, LOD + 빌보드)
    /// - broadleaf: Realistic Tree 9·10 (Pixel Games) 활엽수 — Tree Creator라 Resources/NatureKit/Baked에 구운 것
    /// - rocks / snowRocks: Rock and Boulders 2 단일 바위 14종 (눈 덮인 판)
    /// - grass / flowers: Grass Flowers Pack Free 풀·들꽃 그림 (<see cref="GrassField"/>)
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
