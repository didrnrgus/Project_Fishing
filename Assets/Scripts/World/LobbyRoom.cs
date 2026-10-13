using Fishing.Angling;
using Fishing.Characters;
using Fishing.Core;
using Fishing.Data;
using Fishing.UI;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 대기실 "한국 낚시 조합". 원작 대기실처럼 목재 실내에 박제 물고기, 지도, 책장, 지구본, 태극기를 두고
    /// 가운데에 내 캐릭터가 낚싯대를 들고 서 있다. 필드에서 멀리 떨어진 곳에 만들어 두고 카메라만 옮긴다.
    /// </summary>
    public class LobbyRoom : MonoBehaviour
    {
        public static readonly Vector3 Origin = new Vector3(0f, 0f, -420f);
        const float W = 13f, D = 9f, H = 4.6f;

        public Vector3 CameraPos => Origin + new Vector3(-0.9f, 1.7f, 4.3f);
        public Quaternion CameraRot => Quaternion.LookRotation((Origin + new Vector3(0.2f, 1.25f, -1f)) - CameraPos);

        static readonly Color Wood = new Color(0.42f, 0.28f, 0.17f);
        static readonly Color WoodDark = new Color(0.28f, 0.18f, 0.11f);
        static readonly Color Brick = new Color(0.5f, 0.34f, 0.25f);

        public static LobbyRoom Build()
        {
            var go = new GameObject("LobbyRoom");
            go.transform.position = Origin;
            var room = go.AddComponent<LobbyRoom>();
            room.BuildRoom();
            return room;
        }

        void BuildRoom()
        {
            var t = transform;
            var floor = Mats.Lit(Wood, 0.35f);
            var dark = Mats.Lit(WoodDark, 0.2f);
            var brick = Mats.Lit(Brick, 0.1f);
            var plaster = Mats.Lit(new Color(0.78f, 0.72f, 0.62f), 0.05f);

            // 바닥, 천장, 벽
            Mats.Prim(PrimitiveType.Cube, "Floor", t, new Vector3(0f, -0.05f, 0f), new Vector3(W, 0.1f, D), floor, false);
            for (float x = -W / 2f + 0.5f; x < W / 2f; x += 1f)
                Mats.Prim(PrimitiveType.Cube, "Plank", t, new Vector3(x, 0.001f, 0f), new Vector3(0.02f, 0.002f, D), dark, false);
            Mats.Prim(PrimitiveType.Cube, "Ceiling", t, new Vector3(0f, H, 0f), new Vector3(W, 0.2f, D), dark, false);
            Mats.Prim(PrimitiveType.Cube, "BackWall", t, new Vector3(0f, H / 2f, -D / 2f), new Vector3(W, H, 0.2f), brick, false);
            Mats.Prim(PrimitiveType.Cube, "Wainscot", t, new Vector3(0f, 0.6f, -D / 2f + 0.12f), new Vector3(W, 1.2f, 0.06f), dark, false);
            Mats.Prim(PrimitiveType.Cube, "LeftWall", t, new Vector3(-W / 2f, H / 2f, 0f), new Vector3(0.2f, H, D), plaster, false);
            Mats.Prim(PrimitiveType.Cube, "RightWall", t, new Vector3(W / 2f, H / 2f, 0f), new Vector3(0.2f, H, D), plaster, false);
            Mats.Prim(PrimitiveType.Cube, "FrontWall", t, new Vector3(0f, H / 2f, D / 2f + 1.5f), new Vector3(W, H, 0.2f), plaster, false);
            for (float x = -W / 2f + 1f; x < W / 2f; x += 2.2f)
                Mats.Prim(PrimitiveType.Cube, "Beam", t, new Vector3(x, H - 0.2f, 0f), new Vector3(0.25f, 0.25f, D), dark, false);

            // 러그
            Mats.Prim(PrimitiveType.Cylinder, "Rug", t, new Vector3(0.2f, 0.01f, -0.6f), new Vector3(4.2f, 0.005f, 2.8f), Mats.Lit(new Color(0.45f, 0.18f, 0.12f)), false);
            Mats.Prim(PrimitiveType.Cylinder, "RugInner", t, new Vector3(0.2f, 0.013f, -0.6f), new Vector3(3.3f, 0.005f, 2.1f), Mats.Lit(new Color(0.75f, 0.6f, 0.38f)), false);

            // 뒷벽: 한반도 지도, 박제 물고기
            var mapMat = new Material(Shader.Find("Standard")) { mainTexture = KoreaMap.Texture };
            mapMat.SetFloat("_Glossiness", 0.1f);
            Mats.Prim(PrimitiveType.Cube, "MapFrame", t, new Vector3(1.4f, 2.5f, -D / 2f + 0.13f), new Vector3(1.75f, 2.2f, 0.05f), dark, false);
            var map = Mats.Prim(PrimitiveType.Quad, "Map", t, new Vector3(1.4f, 2.5f, -D / 2f + 0.17f), new Vector3(1.55f, 2.05f, 1f), mapMat, false,
                Quaternion.Euler(0f, 180f, 0f));
            MountFish(t, "carp", 82f, new Vector3(-1.8f, 3.1f, -D / 2f + 0.2f));
            MountFish(t, "catfish", 74f, new Vector3(-1.8f, 2.2f, -D / 2f + 0.2f));
            MountFish(t, "mandarin", 50f, new Vector3(3.7f, 3.2f, -D / 2f + 0.2f));

            // 왼쪽: 책장
            var shelf = new GameObject("Bookshelf").transform;
            shelf.SetParent(t, false);
            shelf.localPosition = new Vector3(-5.3f, 0f, -3.2f);
            Mats.Prim(PrimitiveType.Cube, "Frame", shelf, new Vector3(0f, 1.3f, 0f), new Vector3(1.6f, 2.6f, 0.5f), dark, false);
            var rng = new System.Random(7);
            for (int row = 0; row < 4; row++)
            for (float x = -0.65f; x < 0.65f; x += 0.11f)
            {
                var c = Color.HSVToRGB((float)rng.NextDouble(), 0.45f, 0.55f);
                float h = 0.35f + (float)rng.NextDouble() * 0.15f;
                Mats.Prim(PrimitiveType.Cube, "Book", shelf, new Vector3(x, 0.25f + row * 0.6f + h / 2f, 0.2f), new Vector3(0.09f, h, 0.32f), Mats.Lit(c), false);
            }

            // 오른쪽: 책상, 모니터, 지구본, 태극기
            Mats.Prim(PrimitiveType.Cube, "Desk", t, new Vector3(4.4f, 0.75f, -2.6f), new Vector3(2.2f, 0.08f, 1f), Mats.Lit(Wood, 0.4f), false);
            Mats.Prim(PrimitiveType.Cube, "DeskLegL", t, new Vector3(3.4f, 0.37f, -2.6f), new Vector3(0.08f, 0.74f, 0.9f), dark, false);
            Mats.Prim(PrimitiveType.Cube, "DeskLegR", t, new Vector3(5.4f, 0.37f, -2.6f), new Vector3(0.08f, 0.74f, 0.9f), dark, false);
            Mats.Prim(PrimitiveType.Cube, "Monitor", t, new Vector3(4.6f, 1.15f, -2.85f), new Vector3(0.75f, 0.5f, 0.05f), Mats.Lit(new Color(0.08f, 0.08f, 0.1f), 0.8f), false);
            Mats.Prim(PrimitiveType.Cube, "Screen", t, new Vector3(4.6f, 1.15f, -2.82f), new Vector3(0.68f, 0.42f, 0.01f), Mats.Emissive(new Color(0.2f, 0.45f, 0.6f), 0.8f), false);
            Mats.Prim(PrimitiveType.Cylinder, "GlobeStand", t, new Vector3(5.2f, 0.5f, -0.9f), new Vector3(0.12f, 0.5f, 0.12f), dark, false);
            Mats.Prim(PrimitiveType.Sphere, "Globe", t, new Vector3(5.2f, 1.25f, -0.9f), Vector3.one * 0.55f, Mats.Lit(new Color(0.25f, 0.45f, 0.65f), 0.5f), false);
            Mats.Prim(PrimitiveType.Cylinder, "FlagPole", t, new Vector3(6.0f, 1.4f, -3.3f), new Vector3(0.04f, 1.4f, 0.04f), Mats.Lit(new Color(0.8f, 0.7f, 0.4f), 0.6f, 0.6f), false);
            var flagMat = new Material(Shader.Find("Standard")) { mainTexture = Taegukgi(240, 160) };
            Mats.Prim(PrimitiveType.Quad, "Flag", t, new Vector3(5.6f, 2.2f, -3.25f), new Vector3(0.8f, 0.54f, 1f), flagMat, false,
                Quaternion.Euler(0f, 160f, 0f));

            // 의자와 원탁
            Mats.Prim(PrimitiveType.Cylinder, "Table", t, new Vector3(-2.6f, 0.72f, -2.4f), new Vector3(1.1f, 0.03f, 1.1f), Mats.Lit(Wood, 0.4f), false);
            Mats.Prim(PrimitiveType.Cylinder, "TableLeg", t, new Vector3(-2.6f, 0.36f, -2.4f), new Vector3(0.1f, 0.36f, 0.1f), dark, false);
            for (int i = 0; i < 2; i++)
            {
                float x = -3.3f + i * 1.4f;
                Mats.Prim(PrimitiveType.Cube, "ChairSeat", t, new Vector3(x, 0.45f, -2.0f), new Vector3(0.45f, 0.06f, 0.45f), dark, false);
                Mats.Prim(PrimitiveType.Cube, "ChairBack", t, new Vector3(x, 0.8f, -1.8f), new Vector3(0.45f, 0.6f, 0.05f), dark, false);
            }

            // 창문 (블라인드 사이로 빛)
            var window = Mats.Emissive(new Color(1f, 0.92f, 0.75f), 1.2f);
            Mats.Prim(PrimitiveType.Cube, "Window", t, new Vector3(-W / 2f + 0.12f, 2.3f, 0.2f), new Vector3(0.02f, 1.6f, 2.4f), window, false);
            for (float y = 1.6f; y < 3.1f; y += 0.12f)
                Mats.Prim(PrimitiveType.Cube, "Blind", t, new Vector3(-W / 2f + 0.15f, y, 0.2f), new Vector3(0.02f, 0.05f, 2.4f), dark, false);

            // 조명
            AddLight(new Vector3(-1.5f, H - 0.6f, 0.5f), 2.2f, 11f);
            AddLight(new Vector3(2.5f, H - 0.6f, -1.5f), 2.0f, 11f);
            AddLight(new Vector3(0f, 2.2f, 3.2f), 1.2f, 7f);
            for (int i = 0; i < 2; i++)
                Mats.Prim(PrimitiveType.Sphere, "Lamp", t, new Vector3(i == 0 ? -1.5f : 2.5f, H - 0.45f, i == 0 ? 0.5f : -1.5f), Vector3.one * 0.35f,
                    Mats.Emissive(new Color(1f, 0.85f, 0.6f), 2f), false);

            // 가운데 캐릭터
            var look = CharacterLook.Player;
            var npc = NpcAngler.Spawn(t, new Vector3(0.2f, 0f, -0.6f), 0f, new CharacterLook
            {
                Name = look.Name, Title = look.Title, Hair = look.Hair, HairColor = look.HairColor, Shirt = look.Shirt,
                Pants = look.Pants, Shoes = look.Shoes, Hat = look.Hat, HatColor = look.HatColor, Vest = look.Vest, VestColor = look.VestColor
            }, NpcAngler.Role.Angler, showNameTag: false);
            npc.Idle = true;
        }

        void MountFish(Transform parent, string speciesId, float length, Vector3 pos)
        {
            var species = FishDatabase.Get(speciesId);
            Mats.Prim(PrimitiveType.Cube, "Plaque", parent, pos + new Vector3(0f, 0f, -0.06f), new Vector3(length / 100f + 0.25f, 0.42f, 0.04f),
                Mats.Lit(WoodDark, 0.4f), false);
            var holder = new GameObject("Trophy_" + speciesId).transform;
            holder.SetParent(parent, false);
            holder.localPosition = pos;
            holder.localRotation = Quaternion.Euler(0f, 90f, 0f);
            var fish = FishModels.Create(species, length, holder);
            var anim = fish.GetComponent<Animation>();
            if (anim != null) anim.Stop();
        }

        void AddLight(Vector3 local, float intensity, float range)
        {
            var go = new GameObject("RoomLight");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = local;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.85f, 0.65f);
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
        }

        /// <summary>태극기 (대략적인 태극 문양 + 4괘).</summary>
        static Texture2D Taegukgi(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true);
            var red = new Color(0.8f, 0.1f, 0.18f);
            var blue = new Color(0f, 0.2f, 0.55f);
            float r = h * 0.25f;
            var center = new Vector2(w / 2f, h / 2f);
            float ang = -33.7f * Mathf.Deg2Rad;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var p = new Vector2(x, y) - center;
                var q = new Vector2(p.x * Mathf.Cos(ang) - p.y * Mathf.Sin(ang), p.x * Mathf.Sin(ang) + p.y * Mathf.Cos(ang));
                var c = Color.white;
                if (q.magnitude < r)
                {
                    bool upper = q.y > 0f;
                    bool leftSmall = (q - new Vector2(-r / 2f, 0f)).magnitude < r / 2f;
                    bool rightSmall = (q - new Vector2(r / 2f, 0f)).magnitude < r / 2f;
                    bool isRed = (upper && !rightSmall) || leftSmall;
                    c = isRed ? red : blue;
                }
                else
                {
                    // 4괘: 모서리 대각선 방향의 검은 막대 세 줄
                    for (int k = 0; k < 4; k++)
                    {
                        float sx = k % 2 == 0 ? -1f : 1f, sy = k < 2 ? 1f : -1f;
                        var gc = center + new Vector2(sx * r * 2.05f, sy * r * 1.35f);
                        var d = new Vector2(x, y) - gc;
                        float a = Mathf.Atan2(sy, sx);
                        var local = new Vector2(d.x * Mathf.Cos(-a) - d.y * Mathf.Sin(-a), d.x * Mathf.Sin(-a) + d.y * Mathf.Cos(-a));
                        if (Mathf.Abs(local.y) < r * 0.5f && Mathf.Abs(local.x) < r * 0.36f)
                        {
                            float band = (local.x + r * 0.36f) / (r * 0.72f) * 5f;
                            if ((int)band % 2 == 0) c = Color.black;
                        }
                    }
                }
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return tex;
        }
    }
}
