using System.Collections.Generic;
using Fishing.Core;
using Fishing.UI;
using UnityEngine;

namespace Fishing.Characters
{
    /// <summary>캐릭터 외형. 옷은 베이스 모델 위에 셰이더로 입힌다.</summary>
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
        public bool Vest = true;
        public Color VestColor = new Color(0.25f, 0.4f, 0.3f);
        public string Name;
        public string Title;

        public static CharacterLook Player => new CharacterLook { Name = "강태공", Title = "초보 조사" };
    }

    /// <summary>
    /// Quaternius Universal Base Characters(CC0)로 사람을 만든다.
    ///  - 뼈 가중치로 셔츠/바지/신발 마스크를 버텍스 컬러에 굽고 Fishing/Clothed 셰이더로 입힌다
    ///  - Head 뼈에 리깅된 머리카락을 뼈 이름으로 다시 연결한다
    ///  - 모자·조끼는 프리미티브로 덧붙인다
    /// </summary>
    public static class CharacterFactory
    {
        public const float TargetHeight = 1.76f;

        static readonly HashSet<string> ShirtBones = new HashSet<string>
            { "spine_01", "spine_02", "spine_03", "clavicle_l", "clavicle_r", "upperarm_l", "upperarm_r" };
        static readonly HashSet<string> PantsBones = new HashSet<string>
            { "pelvis", "thigh_l", "thigh_r", "calf_l", "calf_r" };
        static readonly HashSet<string> VestBones = new HashSet<string> { "spine_01", "spine_02", "spine_03" };
        static readonly HashSet<string> ShoeBones = new HashSet<string>
            { "foot_l", "foot_r", "ball_l", "ball_r", "ball_leaf_l", "ball_leaf_r" };

        static readonly Dictionary<string, Mesh> ClothedMeshCache = new Dictionary<string, Mesh>();

        public static bool Available => Resources.Load<GameObject>("Characters/Superhero_Male_FullBody") != null;

        /// <summary>캐릭터를 만든다. 반환값의 루트에 Animator와 CharacterAnimator가 붙어 있다. 발이 원점.</summary>
        public static GameObject Create(CharacterLook look, Transform parent)
        {
            string model = look.Female ? "Superhero_Female_FullBody" : "Superhero_Male_FullBody";
            var prefab = Resources.Load<GameObject>("Characters/" + model);
            var go = Object.Instantiate(prefab, parent, false);
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // 모델이 -Z를 보고 있어서 게임 정면(+Z)으로 돌린다
            go.name = "Character_" + (look.Name ?? model);

            float rawHeight = look.Female ? 1.85f : 1.92f;
            go.transform.localScale = Vector3.one * (TargetHeight * (look.Female ? 0.97f : 1f) / rawHeight);

            var bones = new Dictionary<string, Transform>();
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) bones[t.name] = t;

            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.updateWhenOffscreen = true;
                if (smr.name.Contains("Eyebrows")) smr.sharedMaterial = Mats.Lit(look.HairColor * 0.8f, 0.1f);
                else if (smr.name.Contains("Eyes")) smr.sharedMaterial = TexturedMat("Characters/Textures/T_Eye_Brown", Color.white, 0.8f);
                else
                {
                    smr.sharedMesh = ClothedMesh(smr, model);
                    smr.sharedMaterial = ClothesMaterial(look);
                }
            }

            AttachHair(go, bones, look.Hair, look.HairColor, look.Female ? "Characters/Hair/T_Hair_2_BaseColor" : "Characters/Hair/T_Hair_1_BaseColor");
            if (look.Beard) AttachHair(go, bones, "Hair_Beard", look.HairColor, "Characters/Hair/T_Hair_1_BaseColor");

            if (bones.TryGetValue("Head", out var head))
            {
                if (look.Hat) AttachBucketHat(head, look.HatColor, go.transform.lossyScale.x);
            }

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

        static Material ClothesMaterial(CharacterLook look)
        {
            var m = new Material(Shader.Find("Fishing/Clothed"));
            string tex = look.Female
                ? (look.DarkSkin ? "Characters/Textures/T_Superhero_Female_Dark_BaseColor" : "Characters/Textures/T_Superhero_Female_Light_BaseColor")
                : (look.DarkSkin ? "Characters/Textures/T_Superhero_Male_Dark" : "Characters/Textures/T_Superhero_Male_Ligh");
            m.mainTexture = Resources.Load<Texture2D>(tex);
            m.SetColor("_ShirtColor", look.Shirt);
            m.SetColor("_PantsColor", look.Pants);
            m.SetColor("_ShoeColor", look.Shoes);
            m.SetColor("_VestColor", look.VestColor);
            m.SetFloat("_VestAmount", look.Vest ? 1f : 0f);
            return m;
        }

        /// <summary>뼈 가중치로 옷 마스크를 구운 메쉬 사본. 모델별로 한 번만 만든다.</summary>
        static Mesh ClothedMesh(SkinnedMeshRenderer smr, string model)
        {
            string key = model + "/" + smr.name;
            if (ClothedMeshCache.TryGetValue(key, out var cached)) return cached;
            var src = smr.sharedMesh;
            if (!src.isReadable) return src;
            var mesh = Object.Instantiate(src);
            var weights = mesh.boneWeights;
            var bones = smr.bones;
            var colors = new Color[mesh.vertexCount];
            for (int i = 0; i < colors.Length; i++)
            {
                var w = weights[i];
                float shirt = 0f, pants = 0f, shoes = 0f, vest = 0f;
                Accumulate(bones, w.boneIndex0, w.weight0, ref shirt, ref pants, ref shoes, ref vest);
                Accumulate(bones, w.boneIndex1, w.weight1, ref shirt, ref pants, ref shoes, ref vest);
                Accumulate(bones, w.boneIndex2, w.weight2, ref shirt, ref pants, ref shoes, ref vest);
                Accumulate(bones, w.boneIndex3, w.weight3, ref shirt, ref pants, ref shoes, ref vest);
                colors[i] = new Color(shirt, pants, shoes, vest);
            }
            mesh.colors = colors;
            ClothedMeshCache[key] = mesh;
            return mesh;
        }

        static void Accumulate(Transform[] bones, int index, float weight, ref float shirt, ref float pants, ref float shoes, ref float vest)
        {
            if (weight <= 0f || index < 0 || index >= bones.Length || bones[index] == null) return;
            string n = bones[index].name;
            if (VestBones.Contains(n)) vest += weight;
            if (ShirtBones.Contains(n)) shirt += weight;
            else if (PantsBones.Contains(n)) pants += weight;
            else if (ShoeBones.Contains(n)) shoes += weight;
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

        /// <summary>낚시 모자(벙거지). 모델의 기본 자세에서 머리 위에 월드 기준으로 배치한 뒤 Head 뼈에 붙인다.</summary>
        static void AttachBucketHat(Transform head, Color color, float scale)
        {
            // Head 뼈는 두개골 아래쪽에 있어서 원본 기준 약 0.22m 위가 정수리다
            var hat = new GameObject("BucketHat").transform;
            hat.position = head.position + Vector3.up * 0.2f * scale / 0.92f;
            hat.rotation = Quaternion.identity;
            var mat = Mats.Lit(color, 0.1f);
            var band = Mats.Lit(color * 0.6f, 0.1f);
            Mats.Prim(PrimitiveType.Cylinder, "Brim", hat, new Vector3(0f, 0f, 0.01f), new Vector3(0.36f, 0.008f, 0.36f), mat, false);
            Mats.Prim(PrimitiveType.Cylinder, "Crown", hat, new Vector3(0f, 0.06f, 0.01f), new Vector3(0.22f, 0.06f, 0.22f), mat, false);
            Mats.Prim(PrimitiveType.Cylinder, "Band", hat, new Vector3(0f, 0.025f, 0.01f), new Vector3(0.225f, 0.012f, 0.225f), band, false);
            hat.SetParent(head, true);
        }

        /// <summary>머리 위 이름표(+칭호). 카메라를 향하게는 사용하는 쪽에서 돌린다.</summary>
        public static Transform AddNameTag(Transform parent, string name, string title, float height = 2.15f)
        {
            var tag = new GameObject("NameTag").transform;
            tag.SetParent(parent, false);
            tag.localPosition = new Vector3(0f, height, 0f);
            if (!string.IsNullOrEmpty(title))
                MakeText(tag, title, new Vector3(0f, 0.2f, 0f), new Color(0.55f, 0.85f, 1f), 0.032f);
            MakeText(tag, name, Vector3.zero, Color.white, 0.042f);
            return tag;
        }

        static void MakeText(Transform parent, string text, Vector3 pos, Color color, float size)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = UIFactory.Font;
            tm.fontSize = 48;
            tm.characterSize = size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = UIFactory.Font.material;
        }
    }
}
