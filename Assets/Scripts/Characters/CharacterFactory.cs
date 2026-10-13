using System.Collections.Generic;
using Fishing.Core;
using Fishing.UI;
using UnityEngine;

namespace Fishing.Characters
{
    /// <summary>캐릭터 외형. 옷은 Angler FBX 안의 옷 메쉬를 골라 켜고 색을 입힌다.</summary>
    public class CharacterLook
    {
        public bool Female;
        public bool DarkSkin;
        public string Hair = "Hair_SimpleParted";
        public Color HairColor = new Color(0.25f, 0.18f, 0.12f);
        public bool Beard;
        public Color Shirt = new Color(0.92f, 0.92f, 0.94f);
        public Color Pants = new Color(0.32f, 0.36f, 0.3f);
        public Color Shoes = new Color(0.2f, 0.2f, 0.22f);
        public bool Hat = true;
        public Color HatColor = new Color(0.9f, 0.84f, 0.62f);
        /// <summary>모자 모양: 0 벙거지, 1 야구모자, 2 밀짚모자.</summary>
        public int HatStyle;
        public bool Glasses;
        /// <summary>손가락 없는 반장갑 (검정 + 주황 손목띠, 원작 1인칭처럼). 내 캐릭터만 낀다.</summary>
        public bool Gloves;
        public Color GlassesColor = new Color(0.08f, 0.08f, 0.1f);
        public bool Vest = true;
        public Color VestColor = new Color(0.25f, 0.4f, 0.3f);
        // 옷 메쉬 (CharacterFactory.Garments)
        public string TopModel = "Top_Tee";
        public int TopPattern;
        public string VestModel = "Vest_Fishing";
        public string BottomModel = "Pants_Cargo";
        public int BottomPattern;
        public string ShoeModel = "Shoes_Sneaker";
        public string Name;
        public string Title;

        public static CharacterLook Player => new CharacterLook { Name = "강태공", Title = "초보 조사", Gloves = true };
    }

    /// <summary>
    /// 낚시꾼 캐릭터를 만든다. 모델은 tools/blender/build_characters.py가 Quaternius Universal Base Characters(CC0)로 만든
    /// Resources/Characters/Angler_Male·Angler_Female.fbx:
    ///  - 근육질 Superhero 몸을 보통 체형으로 다듬은 Body
    ///  - 같은 뼈에 스키닝된 옷 메쉬(반팔·긴팔·남방(깃·단추)·래시가드, 낚시조끼·구명조끼, 카고·청바지·웨이더, 운동화·장화, 모자 3종, 선글라스, 반장갑)
    /// 고른 옷만 남기고 나머지는 지운다. 몸(Fishing/Skin)은 입은 옷 속 피부를 잘라내 살이 옷을 뚫지 않는다.
    /// </summary>
    public static class CharacterFactory
    {
        public const float TargetHeight = 1.76f;

        public static readonly string[] Garments =
        {
            "Top_Tee", "Top_Long", "Top_Shirt", "Top_Rash", "Vest_Fishing", "Vest_Life", "Pants_Cargo", "Pants_Jeans", "Pants_Wader",
            "Shoes_Sneaker", "Shoes_Boots", "Hat_Bucket", "Hat_Cap", "Hat_Straw", "Glasses", "Gloves",
        };

        static readonly string[] HatModels = { "Hat_Bucket", "Hat_Cap", "Hat_Straw" };

        /// <summary>장화 윗단(무릎 아래 약 0.45m)보다 낮은 바지는 장화 속에 들어간 것으로 보고 그리지 않는다.</summary>
        const float BootTuckHeight = 0.4f;

        static string ModelName(bool female) => female ? "Angler_Female" : "Angler_Male";

        public static bool Available => Resources.Load<GameObject>("Characters/" + ModelName(false)) != null;

        /// <summary>캐릭터를 만든다. 반환값의 루트에 Animator와 CharacterAnimator가 붙어 있다. 발이 원점.</summary>
        public static GameObject Create(CharacterLook look, Transform parent)
        {
            string model = ModelName(look.Female);
            var prefab = Resources.Load<GameObject>("Characters/" + model) ?? Resources.Load<GameObject>("Characters/" + ModelName(false));
            var go = Object.Instantiate(prefab, parent, false);
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // 모델이 -Z를 보고 있어서 게임 정면(+Z)으로 돌린다
            go.name = "Character_" + (look.Name ?? model);

            float rawHeight = look.Female ? 1.85f : 1.92f;
            go.transform.localScale = Vector3.one * (TargetHeight * (look.Female ? 0.97f : 1f) / rawHeight);

            var bones = new Dictionary<string, Transform>();
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) bones[t.name] = t;

            var wear = new HashSet<string> { look.TopModel, look.BottomModel, look.ShoeModel };
            if (look.Vest) wear.Add(look.VestModel);
            if (look.Hat) wear.Add(HatModels[Mathf.Clamp(look.HatStyle, 0, HatModels.Length - 1)]);
            if (look.Glasses) wear.Add("Glasses");
            if (look.Gloves) wear.Add("Gloves");
            bool boots = look.ShoeModel == "Shoes_Boots";
            bool longSleeve = look.TopModel == "Top_Long" || look.TopModel == "Top_Shirt" || look.TopModel == "Top_Rash";

            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.updateWhenOffscreen = true;
                string n = smr.name;
                if (n.StartsWith("Eyebrows")) smr.sharedMaterial = Mats.Lit(look.HairColor * 0.8f, 0.1f);
                else if (n.StartsWith("Eyes")) smr.sharedMaterial = TexturedMat("Characters/Textures/T_Eye_Brown", Color.white, 0.8f);
                else if (n == "Body") smr.sharedMaterial = SkinMaterial(look, longSleeve);
                else if (System.Array.IndexOf(Garments, n) >= 0)
                {
                    if (!wear.Contains(n)) { Object.Destroy(smr.gameObject); continue; }
                    smr.sharedMaterial = GarmentMaterial(n, look, boots);
                }
            }

            AttachHair(go, bones, look.Hair, look.HairColor, look.Female ? "Characters/Hair/T_Hair_2_BaseColor" : "Characters/Hair/T_Hair_1_BaseColor");
            if (look.Beard) AttachHair(go, bones, "Hair_Beard", look.HairColor, "Characters/Hair/T_Hair_1_BaseColor");

            var anim = go.GetComponent<Animator>();
            if (anim == null) anim = go.AddComponent<Animator>();
            go.AddComponent<CharacterAnimator>();
            return go;
        }

        static Material TexturedMat(string path, Color tint, float smoothness)
        {
            var m = new Material(Shader.Find("Standard")) { color = tint, mainTexture = Resources.Load<Texture2D>(path) };
            m.SetFloat("_Glossiness", smoothness);
            return m;
        }

        static Material SkinMaterial(CharacterLook look, bool longSleeve)
        {
            var m = new Material(Shader.Find("Fishing/Skin"));
            string tex = look.Female
                ? (look.DarkSkin ? "Characters/Textures/T_Superhero_Female_Dark_BaseColor" : "Characters/Textures/T_Superhero_Female_Light_BaseColor")
                : (look.DarkSkin ? "Characters/Textures/T_Superhero_Male_Dark" : "Characters/Textures/T_Superhero_Male_Ligh");
            m.mainTexture = Resources.Load<Texture2D>(tex);
            // 상의·하의·신발은 늘 입는다. 긴소매일 때만 팔뚝까지 잘라낸다.
            m.SetVector("_Clip", new Vector4(1f, 1f, 1f, longSleeve ? 1f : 0f));
            return m;
        }

        /// <summary>옷 머티리얼. 트림(목 리브·밑창·지퍼 등)과 보조 원단 색은 옷마다 정한다.</summary>
        static Material GarmentMaterial(string model, CharacterLook look, bool boots)
        {
            Color main, trim, pattern;
            int mode = 0;
            float gloss = 0.1f, clipBelow = -1f;
            switch (model)
            {
                case "Top_Tee":
                case "Top_Long":
                case "Top_Shirt":
                    main = look.Shirt; trim = main * 0.82f; pattern = Color.Lerp(main * 0.45f, Color.white, look.TopPattern == 3 ? 0.9f : 0f);
                    mode = look.TopPattern;
                    break;
                case "Top_Rash":
                    main = look.Shirt; trim = Color.Lerp(main, new Color(0.35f, 0.7f, 0.95f), 0.7f); pattern = main; gloss = 0.35f;
                    break;
                case "Vest_Life":
                    main = look.VestColor; trim = new Color(0.12f, 0.12f, 0.13f); pattern = main; gloss = 0.3f;
                    break;
                case "Vest_Fishing":
                    main = look.VestColor; trim = main * 0.62f; pattern = main * 0.55f;
                    break;
                case "Pants_Wader":
                    main = look.Pants; trim = new Color(0.1f, 0.1f, 0.1f); pattern = main; gloss = 0.35f; mode = look.BottomPattern;
                    break;
                case "Pants_Cargo":
                case "Pants_Jeans":
                    main = look.Pants; trim = main * 0.75f; pattern = main * 0.6f; mode = look.BottomPattern;
                    break;
                case "Shoes_Sneaker":
                    main = look.Shoes; trim = new Color(0.92f, 0.92f, 0.9f); pattern = new Color(0.9f, 0.9f, 0.88f); gloss = 0.25f; // 보조 원단 = 끈
                    break;
                case "Shoes_Boots":
                    main = look.Shoes; trim = main * 0.7f; pattern = main; gloss = 0.45f;
                    break;
                case "Hat_Straw":
                    main = look.HatColor; trim = new Color(0.75f, 0.15f, 0.12f); pattern = main; mode = 4;
                    break;
                case "Hat_Bucket":
                case "Hat_Cap":
                    main = look.HatColor; trim = main * (model == "Hat_Cap" ? 0.8f : 0.6f); pattern = main;
                    break;
                case "Gloves":
                    main = new Color(0.1f, 0.1f, 0.12f); trim = new Color(0.95f, 0.55f, 0.15f); pattern = main; gloss = 0.3f;
                    break;
                case "Glasses":
                    main = look.GlassesColor; trim = new Color(0.04f, 0.04f, 0.05f); pattern = main; gloss = 0.9f;
                    break;
                default:
                    main = Color.gray; trim = Color.gray; pattern = Color.gray;
                    break;
            }
            if (boots && model.StartsWith("Pants")) clipBelow = BootTuckHeight;

            var m = new Material(Shader.Find("Fishing/Garment"));
            m.SetColor("_Color", main);
            m.SetColor("_TrimColor", trim);
            m.SetColor("_PatternColor", pattern);
            m.SetFloat("_Pattern", mode);
            m.SetFloat("_ClipBelow", clipBelow);
            m.SetFloat("_Glossiness", gloss);
            if (model == "Glasses") m.SetFloat("_Metallic", 0.4f);
            return m;
        }

        /// <summary>Head 뼈에 리깅된 머리카락 메쉬를 캐릭터 뼈에 다시 연결한다.</summary>
        static void AttachHair(GameObject character, Dictionary<string, Transform> bones, string hair, Color color, string texture)
        {
            var prefab = Resources.Load<GameObject>("Characters/Hair/" + hair);
            if (prefab == null) return;
            var tmp = Object.Instantiate(prefab);
            foreach (var smr in tmp.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mapped = new Transform[smr.bones.Length];
                for (int i = 0; i < mapped.Length; i++)
                    mapped[i] = smr.bones[i] != null && bones.TryGetValue(smr.bones[i].name, out var b) ? b : null;
                smr.transform.SetParent(character.transform, false);
                smr.bones = mapped;
                if (smr.rootBone != null && bones.TryGetValue(smr.rootBone.name, out var root)) smr.rootBone = root;
                smr.sharedMaterial = TexturedMat(texture, color * 1.6f, 0.25f);
                smr.updateWhenOffscreen = true;
            }
            Object.Destroy(tmp);
        }

        /// <summary>머리 위 이름표(+칭호). 카메라를 향하게는 사용하는 쪽에서 돌린다.</summary>
        public static Transform AddNameTag(Transform parent, string name, string title, float height = 2.15f)
        {
            var tag = new GameObject("NameTag").transform;
            tag.SetParent(parent, false);
            tag.localPosition = new Vector3(0f, height, 0f);
            if (!string.IsNullOrEmpty(title))
                UIFactory.WorldText(tag, title, new Vector3(0f, 0.2f, 0f), new Color(0.55f, 0.85f, 1f), 1.5f);
            UIFactory.WorldText(tag, name, Vector3.zero, Color.white, 2f);
            return tag;
        }
    }
}
