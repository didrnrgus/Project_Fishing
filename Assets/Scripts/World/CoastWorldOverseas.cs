using System.Collections.Generic;
using Fishing.Core;
using Fishing.UI;
using UnityEngine;

namespace Fishing.World
{
    /// <summary>
    /// 해외 낚시터 풍경 (원작 동남아·지중해·나일강 거점): 지역 양식 집과 나무, 강 맞은편 둑,
    /// 하롱베이 석회암 봉우리·정크선·수상 가옥, 산토리니 칼데라·하얀 마을·풍차, 나일강 펠루카·피라미드.
    /// 야자수·펠루카·정크선 모델은 tools/blender/build_props.py (Resources/Props).
    /// </summary>
    public partial class CoastWorldBuilder
    {
        // ---------------- 집 ----------------

        void House(Vector3 at, float yaw, bool sign)
        {
            switch (S.Houses)
            {
                case HouseStyle.Cyclades: CycladicHouse(at, yaw, _rng.NextDouble() < 0.12); break;
                case HouseStyle.Egypt: MudHouse(at, yaw); break;
                case HouseStyle.Vietnam: TubeHouse(at, yaw, sign); break;
                default: _props.KoreanHouse(at, yaw, sign); break;
            }
        }

        static readonly Color CycladicBlue = new Color(0.1f, 0.35f, 0.72f);

        /// <summary>
        /// 키클라데스 집: 하얀 회벽 상자(모서리가 둥근 느낌으로 처마 턱), 파란 문·덧창, 가끔 2단.
        /// church면 파란 돔을 얹은 작은 교회 (산토리니 이아 마을). 비탈에 서도 뜨지 않게 벽이 땅속 3m까지 내려간다.
        /// </summary>
        void CycladicHouse(Vector3 at, float yaw, bool church)
        {
            var h = new GameObject(church ? "CycladicChurch" : "CycladicHouse").transform;
            h.SetParent(_root, false);
            h.localPosition = at;
            h.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var white = Mats.Lit(new Color(0.97f, 0.97f, 0.95f), 0.1f);
            var blue = Mats.Lit(CycladicBlue, 0.35f);
            var dark = Mats.Lit(new Color(0.18f, 0.2f, 0.24f), 0.6f);
            float w = R(5f, 8f), d = R(5f, 7f), hgt = R(3f, 3.6f);
            const float below = 3f;
            Mats.Prim(PrimitiveType.Cube, "Wall", h, new Vector3(0f, (hgt - below) * 0.5f, 0f), new Vector3(w, hgt + below, d), white, false);
            Mats.Prim(PrimitiveType.Cube, "Lip", h, new Vector3(0f, hgt + 0.08f, 0f), new Vector3(w + 0.2f, 0.18f, d + 0.2f), white, false);
            // 문 + 덧창
            Mats.Prim(PrimitiveType.Cube, "Door", h, new Vector3(R(-w * 0.2f, w * 0.2f), 1f, d * 0.5f + 0.04f), new Vector3(0.95f, 2f, 0.06f), blue, false);
            for (int i = 0; i < 2; i++)
            {
                float x = (i == 0 ? -1f : 1f) * w * 0.32f;
                Mats.Prim(PrimitiveType.Cube, "Window", h, new Vector3(x, 1.7f, d * 0.5f + 0.03f), new Vector3(0.8f, 0.9f, 0.04f), dark, false);
                Mats.Prim(PrimitiveType.Cube, "Shutter", h, new Vector3(x - 0.5f, 1.7f, d * 0.5f + 0.05f), new Vector3(0.22f, 0.95f, 0.04f), blue, false);
                Mats.Prim(PrimitiveType.Cube, "Shutter", h, new Vector3(x + 0.5f, 1.7f, d * 0.5f + 0.05f), new Vector3(0.22f, 0.95f, 0.04f), blue, false);
            }
            float top = hgt;
            if (!church && _rng.NextDouble() < 0.35)
            {
                // 2단: 뒤쪽에 작은 상자 하나 더 (테라스가 앞에 남는다)
                float w2 = w * R(0.5f, 0.7f), d2 = d * 0.5f;
                Mats.Prim(PrimitiveType.Cube, "Upper", h, new Vector3(R(-0.5f, 0.5f), hgt + 1.5f, -d * 0.25f), new Vector3(w2, 3f, d2), white, false);
                Mats.Prim(PrimitiveType.Cube, "UpperWindow", h, new Vector3(0f, hgt + 1.6f, -d * 0.25f + d2 * 0.5f + 0.03f), new Vector3(0.8f, 0.9f, 0.04f), blue, false);
                top = hgt + 3f;
            }
            if (church)
            {
                // 둥근 드럼 + 파란 돔 + 꼭대기 십자가, 앞쪽에 종탑
                Mats.Prim(PrimitiveType.Cylinder, "Drum", h, new Vector3(0f, hgt + 0.7f, 0f), new Vector3(w * 0.55f, 0.7f, w * 0.55f), white, false);
                Mats.Prim(PrimitiveType.Sphere, "Dome", h, new Vector3(0f, hgt + 1.4f, 0f), new Vector3(w * 0.56f, w * 0.5f, w * 0.56f), blue, false);
                var gold = Mats.Lit(new Color(0.9f, 0.75f, 0.35f), 0.6f, 0.6f);
                Mats.Prim(PrimitiveType.Cube, "Cross", h, new Vector3(0f, hgt + 1.4f + w * 0.25f + 0.5f, 0f), new Vector3(0.08f, 0.9f, 0.08f), gold, false);
                Mats.Prim(PrimitiveType.Cube, "CrossBar", h, new Vector3(0f, hgt + 1.4f + w * 0.25f + 0.65f, 0f), new Vector3(0.45f, 0.08f, 0.08f), gold, false);
                Mats.Prim(PrimitiveType.Cube, "BellWall", h, new Vector3(w * 0.3f, hgt + 1.1f, d * 0.5f - 0.2f), new Vector3(1.6f, 2.2f, 0.4f), white, false);
                Mats.Prim(PrimitiveType.Cube, "BellArch", h, new Vector3(w * 0.3f, hgt + 1.3f, d * 0.5f), new Vector3(0.6f, 0.8f, 0.42f), dark, false);
                top = hgt + 1.4f + w * 0.25f;
            }
            var col = h.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, top * 0.5f, 0f);
            col.size = new Vector3(w, top + 0.5f, d);
        }

        /// <summary>이집트 시골집: 흙벽돌 평지붕, 낮은 난간, 작은 창. 가끔 비둘기탑(위가 둥근 흙탑).</summary>
        void MudHouse(Vector3 at, float yaw)
        {
            var h = new GameObject("MudHouse").transform;
            h.SetParent(_root, false);
            h.localPosition = at;
            h.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var tones = new[] { new Color(0.78f, 0.66f, 0.5f), new Color(0.72f, 0.58f, 0.42f), new Color(0.86f, 0.78f, 0.62f) };
            var mud = Mats.Lit(tones[_rng.Next(tones.Length)], 0.05f);
            var dark = Mats.Lit(new Color(0.15f, 0.12f, 0.1f), 0.2f);
            var door = Mats.Lit(_rng.NextDouble() < 0.5 ? new Color(0.2f, 0.45f, 0.55f) : new Color(0.45f, 0.3f, 0.18f), 0.2f);
            float w = R(6f, 9f), d = R(5f, 7f), hgt = R(3f, 4.2f);
            Mats.Prim(PrimitiveType.Cube, "Wall", h, new Vector3(0f, (hgt - 1.5f) * 0.5f, 0f), new Vector3(w, hgt + 1.5f, d), mud, false);
            Mats.Prim(PrimitiveType.Cube, "Parapet", h, new Vector3(0f, hgt + 0.3f, d * 0.5f - 0.15f), new Vector3(w, 0.6f, 0.3f), mud, false);
            Mats.Prim(PrimitiveType.Cube, "ParapetBack", h, new Vector3(0f, hgt + 0.3f, -d * 0.5f + 0.15f), new Vector3(w, 0.6f, 0.3f), mud, false);
            Mats.Prim(PrimitiveType.Cube, "Door", h, new Vector3(0f, 1f, d * 0.5f + 0.03f), new Vector3(1f, 2f, 0.06f), door, false);
            for (int i = -1; i <= 1; i += 2)
                Mats.Prim(PrimitiveType.Cube, "Window", h, new Vector3(i * w * 0.3f, 2.1f, d * 0.5f + 0.03f), new Vector3(0.55f, 0.55f, 0.05f), dark, false);
            // 지붕 위 짚단·장작
            Mats.Prim(PrimitiveType.Cube, "Straw", h, new Vector3(R(-w * 0.2f, w * 0.2f), hgt + 0.35f, R(-1f, 0.5f)), new Vector3(R(1.5f, 2.5f), 0.5f, R(1f, 1.8f)),
                Mats.Lit(new Color(0.8f, 0.7f, 0.4f), 0.05f), false);
            float top = hgt;
            if (_rng.NextDouble() < 0.25)
            {
                Mats.Prim(PrimitiveType.Cylinder, "Dovecote", h, new Vector3(w * 0.5f + 1.4f, 2.6f, -d * 0.2f), new Vector3(2f, 2.6f, 2f), mud, false);
                Mats.Prim(PrimitiveType.Sphere, "DovecoteTop", h, new Vector3(w * 0.5f + 1.4f, 5.2f, -d * 0.2f), new Vector3(2f, 2.4f, 2f), mud, false);
                for (int k = 0; k < 6; k++)
                {
                    float a = k * 60f * Mathf.Deg2Rad;
                    Mats.Prim(PrimitiveType.Cube, "Hole", h, new Vector3(w * 0.5f + 1.4f + Mathf.Cos(a) * 1f, 3.6f + (k % 2) * 0.6f, -d * 0.2f + Mathf.Sin(a) * 1f),
                        new Vector3(0.2f, 0.2f, 0.2f), dark, false);
                }
                top = 6.4f;
            }
            var col = h.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, hgt * 0.5f, 0f);
            col.size = new Vector3(w, hgt + 0.6f, d);
        }

        static readonly Color[] TubeColors =
        {
            new Color(0.96f, 0.82f, 0.35f), new Color(0.93f, 0.62f, 0.6f), new Color(0.6f, 0.85f, 0.72f), new Color(0.55f, 0.72f, 0.92f), new Color(0.97f, 0.95f, 0.88f),
        };
        static readonly string[] TubeSigns = { "PHO", "CAFE", "HAI SAN", "NHA NGHI", "BIA" };

        /// <summary>베트남 좁고 높은 집 (낮 하롱 시내의 nhà ống): 폭 4~5m, 3~4층, 파스텔 벽, 층마다 발코니, 꼭대기 빨간 기와.</summary>
        void TubeHouse(Vector3 at, float yaw, bool sign)
        {
            var h = new GameObject("TubeHouse").transform;
            h.SetParent(_root, false);
            h.localPosition = at;
            h.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var wall = Mats.Lit(TubeColors[_rng.Next(TubeColors.Length)], 0.15f);
            var rail = Mats.Lit(new Color(0.2f, 0.22f, 0.24f), 0.4f);
            var glass = Mats.Lit(new Color(0.2f, 0.28f, 0.32f), 0.8f);
            var tile = Mats.Lit(new Color(0.72f, 0.3f, 0.18f), 0.2f);
            float w = R(4f, 5.2f), d = R(8f, 11f);
            int floors = _rng.Next(3, 5);
            const float fh = 3.2f;
            float hgt = floors * fh;
            Mats.Prim(PrimitiveType.Cube, "Wall", h, new Vector3(0f, hgt * 0.5f - 0.4f, 0f), new Vector3(w, hgt + 0.8f, d), wall, false);
            for (int f = 0; f < floors; f++)
            {
                float y = f * fh;
                Mats.Prim(PrimitiveType.Cube, f == 0 ? "Shutter" : "Window", h, new Vector3(0f, y + (f == 0 ? 1.3f : 1.6f), d * 0.5f + 0.03f),
                    f == 0 ? new Vector3(w * 0.8f, 2.6f, 0.05f) : new Vector3(w * 0.6f, 1.6f, 0.05f), f == 0 ? rail : glass, false);
                if (f > 0)
                {
                    Mats.Prim(PrimitiveType.Cube, "Balcony", h, new Vector3(0f, y + 0.05f, d * 0.5f + 0.5f), new Vector3(w, 0.15f, 1f), wall, false);
                    Mats.Prim(PrimitiveType.Cube, "Rail", h, new Vector3(0f, y + 0.6f, d * 0.5f + 0.98f), new Vector3(w, 0.9f, 0.05f), rail, false);
                }
            }
            var roof = new GameObject("Roof");
            roof.transform.SetParent(h, false);
            roof.transform.localPosition = new Vector3(0f, hgt + 0.4f, -d * 0.15f);
            roof.transform.localScale = new Vector3(w + 0.5f, 1.4f, d * 0.6f);
            roof.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            roof.AddComponent<MeshFilter>().sharedMesh = GableMesh();
            roof.AddComponent<MeshRenderer>().sharedMaterial = tile;
            if (sign)
            {
                var board = Mats.Lit(new Color(0.85f, 0.15f, 0.12f), 0.3f);
                Mats.Prim(PrimitiveType.Cube, "SignBoard", h, new Vector3(0f, fh - 0.2f, d * 0.5f + 0.12f), new Vector3(w - 0.4f, 0.7f, 0.1f), board, false);
                var t = UIFactory.WorldText(h, TubeSigns[_rng.Next(TubeSigns.Length)], new Vector3(0f, fh - 0.2f, d * 0.5f + 0.19f), new Color(1f, 0.9f, 0.3f), 5f, "Sign");
                UIFactory.OneSided(t);
                t.transform.localScale = Vector3.one * 0.8f;
                t.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }
            var col = h.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, hgt * 0.5f, 0f);
            col.size = new Vector3(w, hgt + 1f, d);
        }

        static Mesh _gableMesh;

        /// <summary>박공지붕 단위 메쉬 (CoastProps와 같은 모양): 바닥 -0.5~0.5, 용마루 높이 1.</summary>
        static Mesh GableMesh()
        {
            if (_gableMesh != null) return _gableMesh;
            var a = new Vector3(-0.5f, 0f, -0.5f); var b = new Vector3(0.5f, 0f, -0.5f);
            var c = new Vector3(0.5f, 0f, 0.5f); var e = new Vector3(-0.5f, 0f, 0.5f);
            var r0 = new Vector3(-0.5f, 1f, 0f); var r1 = new Vector3(0.5f, 1f, 0f);
            var v = new[] { a, b, r1, r0, e, r0, r1, c, a, r0, e, b, c, r1, a, e, c, b };
            var t = new[] { 0, 2, 1, 0, 3, 2, 4, 6, 5, 4, 7, 6, 8, 10, 9, 11, 13, 12, 14, 15, 16, 14, 16, 17 };
            _gableMesh = new Mesh { name = "Gable", vertices = v, triangles = t };
            _gableMesh.RecalculateNormals();
            _gableMesh.RecalculateBounds();
            return _gableMesh;
        }

        /// <summary>
        /// 산토리니 이아처럼 절벽 꼭대기 가장자리를 따라 하얀 집이 층층이 매달린 마을.
        /// 절벽 위(뒤쪽 높은 곳)에서 비탈이 너무 가파르지 않은 자리를 골라 촘촘히 놓는다.
        /// </summary>
        void CliffVillage()
        {
            float halfW = S.LandHalfWidth > 0f ? S.LandHalfWidth - 25f : 190f;
            float edge = S.FlatWidth + S.CliffWidth * 0.55f;
            for (int i = 0; i < 260; i++)
            {
                float x = R(-halfW, halfW);
                float back = R(edge, edge + 55f);
                float z = _map.ShoreAt(x) - back;
                if (z < CoastMapDef.TerrainMinZ + 8f || InFeature(x, z)) continue;
                var (y, _) = Footing(x, z, 2.5f);
                float hi = H(x, z);
                if (hi - y > 3f || y < _map.LandY + S.CliffHeight * 0.55f) continue; // 가파른 벽면·아래쪽은 건너뛴다
                bool church = _rng.NextDouble() < 0.07;
                CycladicHouse(new Vector3(x, y - 0.1f, z), R(-8f, 8f), church);
            }
            if ((S.Features & CoastFeature.Windmills) != 0)
                for (int i = 0; i < 4; i++)
                {
                    float x = -halfW * 0.8f + i * 22f + R(-4f, 4f);
                    float z = _map.ShoreAt(x) - (edge + 30f);
                    var (y, flat) = Footing(x, z, 2.5f);
                    if (!flat) continue;
                    Windmill(new Vector3(x, y, z));
                }
        }

        /// <summary>키클라데스 풍차: 하얀 원통 몸, 원뿔 짚지붕, 삼각 돛 8장.</summary>
        void Windmill(Vector3 at)
        {
            var t = new GameObject("Windmill").transform;
            t.SetParent(_root, false);
            t.localPosition = at;
            t.localRotation = Quaternion.Euler(0f, R(150f, 210f), 0f);
            var white = Mats.Lit(new Color(0.96f, 0.96f, 0.94f), 0.1f);
            var thatch = Mats.Lit(new Color(0.45f, 0.36f, 0.25f), 0.05f);
            var wood = Mats.Lit(new Color(0.4f, 0.3f, 0.2f), 0.2f);
            var cloth = Mats.Lit(new Color(0.95f, 0.93f, 0.86f), 0.1f);
            Mats.Prim(PrimitiveType.Cylinder, "Tower", t, new Vector3(0f, 3.5f, 0f), new Vector3(4.2f, 3.5f, 4.2f), white);
            Mats.Prim(PrimitiveType.Sphere, "Cap", t, new Vector3(0f, 7.2f, 0f), new Vector3(4.4f, 2.6f, 4.4f), thatch, false);
            var hub = new Vector3(0f, 6.6f, 2.3f);
            for (int k = 0; k < 8; k++)
            {
                var rot = Quaternion.Euler(0f, 0f, k * 45f + 10f);
                Mats.Prim(PrimitiveType.Cube, "Spar", t, hub + rot * new Vector3(0f, 2.6f, 0f), new Vector3(0.12f, 5.2f, 0.12f), wood, false, rot);
                Mats.Prim(PrimitiveType.Cube, "Sail", t, hub + rot * new Vector3(0.5f, 3f, 0.05f), new Vector3(0.9f, 3.4f, 0.03f), cloth, false, rot);
            }
        }

        // ---------------- 나무 ----------------

        void Tree(Vector3 at)
        {
            if (!KenneyProps.Available) return;
            switch (S.TreeKind)
            {
                case TreeStyle.Palm:
                    KenneyProps.Spawn(_rng.NextDouble() < 0.5 ? "Props/palm_a" : "Props/palm_b", _root, at, R(0f, 360f), R(6f, 9.5f));
                    break;
                case TreeStyle.Tropical:
                    if (_rng.NextDouble() < 0.55) KenneyProps.Spawn(_rng.NextDouble() < 0.5 ? "Props/palm_a" : "Props/palm_b", _root, at, R(0f, 360f), R(6f, 9f));
                    else KenneyProps.Spawn("Kenney/Nature/tree_default", _root, at, R(0f, 360f), R(6f, 10f));
                    break;
                case TreeStyle.Mediterranean:
                    // 올리브(작고 둥근 나무)와 사이프러스(가늘고 높은 나무)
                    if (_rng.NextDouble() < 0.7) KenneyProps.Spawn("Kenney/Nature/tree_plateau", _root, at, R(0f, 360f), R(3f, 4.5f));
                    else KenneyProps.Spawn("Kenney/Nature/tree_tall", _root, at, R(0f, 360f), R(7f, 10f));
                    break;
                default:
                    KenneyProps.Spawn("Kenney/Nature/" + (S.Snow ? "tree_pineRoundC" : new[] { "tree_pineRoundA", "tree_pineRoundC", "tree_default", "tree_oak", "tree_pineRoundB" }[_rng.Next(5)]), _root,
                        at, R(0f, 360f), R(5f, 9f));
                    break;
            }
        }

        // ---------------- 강 맞은편 둑 ----------------

        /// <summary>강 건너: 물가를 따라 야자수 줄, 드문드문 흙집, 밭 (사막 쪽은 비워 둔다).</summary>
        void BuildFarBank()
        {
            if (S.FarShoreZ <= 0f) return;
            // 지형(x ±210, z -200~270) 밖으로도 양쪽 둑 땅이 이어져 보이게 사막(또는 들판) 판을 깐다.
            // 높이는 강가 평지(LandY)보다 0.3m 낮게: 지형 안에서는 땅 밑에 묻히고, 밖에서는 물(0.2)보다 높다
            var plain = S.SandLand ? Mats.Textured("sand", new Color(0.95f, 0.85f, 0.65f), 120f, 0.05f) : Mats.Textured("grass", Color.white, 120f, 0.05f);
            float py = _map.LandY - 0.3f - 0.2f;
            Mats.Prim(PrimitiveType.Cube, "FarPlain", _root, new Vector3(0f, py, (S.FarShoreZ + 30f + 1600f) * 0.5f), new Vector3(3200f, 0.4f, 1600f - S.FarShoreZ - 30f), plain, false);
            Mats.Prim(PrimitiveType.Cube, "NearPlain", _root, new Vector3(0f, py, (S.ShoreZ - 30f - 1000f) * 0.5f), new Vector3(3200f, 0.4f, 1000f + S.ShoreZ - 30f), plain, false);
            for (float x = -195f; x < 195f; x += R(6f, 13f))
            {
                float z = _map.FarShoreAt(x) + R(4f, 16f);
                float y = H(x, z);
                if (y < _map.LandY - 0.1f) continue;
                Tree(new Vector3(x, y - 0.05f, z));
            }
            for (float x = -180f; x < 180f; x += R(28f, 50f))
            {
                float z = _map.FarShoreAt(x) + R(24f, 34f);
                var (y, flat) = Footing(x, z, 4f);
                if (!flat) continue;
                House(new Vector3(x, y - 0.05f, z), 180f + R(-15f, 15f), false);
            }
        }

        // ---------------- 특별 풍경 ----------------

        void BuildOverseasFeatures()
        {
            if ((S.Features & CoastFeature.Karst) != 0) KarstHorizon();
            if ((S.Features & CoastFeature.Junks) != 0) Boats("Props/junk", 5, 13f);
            if ((S.Features & CoastFeature.Feluccas) != 0) Boats("Props/felucca", 6, 7.5f);
            if ((S.Features & CoastFeature.FloatingVillage) != 0) FloatingVillage();
            if ((S.Features & CoastFeature.Caldera) != 0) Caldera();
            if ((S.Features & CoastFeature.Pyramids) != 0) Pyramids();
        }

        /// <summary>하롱베이 수평선: 녹음 덮인 석회암 봉우리 수십 개 (먼 것은 안개에 묻힌다).</summary>
        void KarstHorizon()
        {
            // 회백색 석회암 절벽 + 젖은 아랫단 + 턱·꼭대기를 덮은 짙은 숲 (먼 것은 안개에 묻히게 엷게)
            var limestone = Mats.Textured("rock", new Color(0.72f, 0.72f, 0.66f), 1f, 0.15f);
            var wet = Mats.Textured("rock", new Color(0.3f, 0.32f, 0.3f), 1f, 0.6f);
            var jungle = Mats.Textured("grass", new Color(0.32f, 0.45f, 0.3f), 2f, 0.05f);
            var near = new[] { limestone, wet, jungle };
            var far = new[] { Mats.Lit(new Color(0.5f, 0.56f, 0.55f), 0.02f), Mats.Lit(new Color(0.42f, 0.48f, 0.48f), 0.02f), Mats.Lit(new Color(0.4f, 0.5f, 0.46f), 0.02f) };
            for (int i = 0; i < 46; i++)
            {
                float x = R(-650f, 650f), z = R(300f, 900f);
                float r = R(14f, 40f), h = R(40f, 120f);
                var go = new GameObject("Karst");
                go.transform.SetParent(_root, false);
                go.transform.localPosition = new Vector3(x, _map.BaseWaterY - 2f, z);
                go.AddComponent<MeshFilter>().sharedMesh = StackMesh(r, h, _rng.Next(1000), true, 4f);
                go.AddComponent<MeshRenderer>().sharedMaterials = z > 600f ? far : near;
            }
        }

        /// <summary>물 위에 떠 있는 배들 (정크선·펠루카). 낚시 칸 가운데는 피하고 가장자리·먼 쪽에 둔다.</summary>
        void Boats(string path, int count, float length)
        {
            int placed = 0;
            for (int i = 0; i < count * 6 && placed < count; i++)
            {
                float x = R(-190f, 190f);
                float z = S.FarShoreZ > 0f ? R(_map.ShoreAt(x) + 40f, _map.FarShoreAt(x) - 20f) : R(150f, 240f);
                var p = new Vector3(x, _map.BaseWaterY, z);
                if (!_map.IsWater(p) || _map.HeightAt(x, z) > _map.BaseWaterY - 1.5f) continue;
                if (new Vector2(x - S.Spawn.x, z - S.Spawn.z).magnitude < 50f) continue;
                KenneyProps.Spawn(path, _root, p + Vector3.down * 0.25f, R(0f, 360f), 0f, length);
                placed++;
            }
        }

        /// <summary>하롱베이 수상 마을: 플라스틱 통 위에 뜬 판잣집들과 그물 가두리 (멀리서 보는 풍경, 콜라이더 있음).</summary>
        void FloatingVillage()
        {
            var plank = Mats.Lit(new Color(0.55f, 0.42f, 0.3f), 0.1f);
            var drum = Mats.Lit(new Color(0.2f, 0.35f, 0.65f), 0.4f);
            var tin = Mats.Lit(new Color(0.6f, 0.62f, 0.62f), 0.4f, 0.3f);
            var colors = new[] { new Color(0.25f, 0.55f, 0.75f), new Color(0.85f, 0.75f, 0.55f), new Color(0.6f, 0.75f, 0.55f), new Color(0.95f, 0.95f, 0.9f) };
            float cx = 95f, cz = 205f;
            for (int i = 0; i < 9; i++)
            {
                var c = new Vector3(cx + (i % 3) * 16f + R(-3f, 3f), _map.BaseWaterY, cz + (i / 3) * 14f + R(-3f, 3f));
                if (!_map.IsWater(c)) continue;
                var t = new GameObject("FloatHouse").transform;
                t.SetParent(_root, false);
                t.localPosition = c;
                t.localRotation = Quaternion.Euler(0f, R(-10f, 10f), 0f);
                Mats.Prim(PrimitiveType.Cube, "Deck", t, new Vector3(0f, 0.35f, 0f), new Vector3(10f, 0.2f, 8f), plank);
                for (int k = 0; k < 6; k++)
                    Mats.Prim(PrimitiveType.Cylinder, "Drum", t, new Vector3(-4f + (k % 3) * 4f, 0.05f, -3f + (k / 3) * 6f), new Vector3(0.9f, 0.3f, 0.9f), drum, false);
                Mats.Prim(PrimitiveType.Cube, "Hut", t, new Vector3(0f, 1.7f, -1.2f), new Vector3(6f, 2.6f, 4.5f), Mats.Lit(colors[_rng.Next(colors.Length)], 0.1f));
                Mats.Prim(PrimitiveType.Cube, "HutRoof", t, new Vector3(0f, 3.1f, -1.2f), new Vector3(6.6f, 0.15f, 5.2f), tin, false, Quaternion.Euler(6f, 0f, 0f));
            }
        }

        /// <summary>
        /// 산토리니 칼데라: 바다 건너 멀리 높이 200m 넘는 붉은·검은 줄무늬 절벽이 활처럼 둘러 서 있고,
        /// 그 위로 하얀 마을 띠. 칼데라 가운데엔 검은 용암 섬 네아 카메니.
        /// </summary>
        /// <summary>
        /// 산토리니 칼데라: 바다 건너 반원으로 둘러선 절벽. 예전에는 거대한 구 9개를 줄지어 놓아 "반구가 늘어선" 모양이었다.
        /// 한 덩어리 절벽 메쉬(<see cref="CalderaMesh"/>)로 잇고, 붉은 화산암 / 짙은 용암층 띠 / 윗면 평지를 서브메쉬로 나누고,
        /// 절벽 위에 하얀 집·파란 지붕을 촘촘히 얹는다 (이아·피라 마을처럼).
        /// </summary>
        void Caldera()
        {
            var cliff = Mats.Textured("rock", new Color(0.72f, 0.5f, 0.42f), new Vector2(40f, 6f), 0.1f);
            var band = Mats.Textured("rock", new Color(0.3f, 0.26f, 0.25f), new Vector2(40f, 6f), 0.1f);
            var top = Mats.Textured("sand", new Color(0.78f, 0.66f, 0.5f), new Vector2(30f, 3f), 0.05f);
            var go = new GameObject("Caldera");
            go.transform.SetParent(_root, false);
            go.transform.localPosition = new Vector3(0f, _map.BaseWaterY - 25f, 260f);
            go.AddComponent<MeshFilter>().sharedMesh = CalderaMesh(S.Seed, out var rim);
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { cliff, band, top };
            // 절벽 위 마을: 하얀 상자 집, 드문드문 파란 둥근 지붕
            var white = Mats.Lit(new Color(0.96f, 0.96f, 0.93f), 0.15f);
            var blue = Mats.Lit(new Color(0.12f, 0.32f, 0.7f), 0.4f);
            foreach (var (p, inward) in rim)
            {
                if (_rng.NextDouble() < 0.35) continue;
                var at = go.transform.localPosition + p + inward * R(4f, 30f);
                float w = R(7f, 14f), h = R(5f, 9f);
                Mats.Prim(PrimitiveType.Cube, "CliffHouse", _root, at + Vector3.up * h * 0.4f, new Vector3(w, h, w * R(0.7f, 1.1f)), white, false,
                    Quaternion.Euler(0f, R(0f, 90f), 0f));
                if (_rng.NextDouble() < 0.12)
                    Mats.Prim(PrimitiveType.Sphere, "CliffDome", _root, at + Vector3.up * h * 0.9f, Vector3.one * w * 0.6f, blue, false);
            }
            var lava = Mats.Textured("rock", new Color(0.22f, 0.2f, 0.2f), new Vector2(8f, 4f), 0.1f);
            Rocks.Prim("NeaKameni", _root, new Vector3(-80f, _map.BaseWaterY + 6f, 520f), new Vector3(260f, 55f, 180f), lava);
        }

        /// <summary>
        /// 칼데라 절벽 메쉬: 각도 -78°~78°의 타원 호(가로 900, 세로 700) 위로 바다(-y)에서 절벽 꼭대기까지 올라가 안쪽 평지로 이어진다.
        /// 높이는 노이즈로 오르내리고(150~230m), 절벽면은 세로 협곡·층 굴곡이 있다. 서브메쉬 0 = 붉은 절벽, 1 = 짙은 용암층 띠, 2 = 윗면.
        /// rim: 꼭대기 가장자리 점과 안쪽(마을 놓을 쪽) 방향.
        /// </summary>
        static Mesh CalderaMesh(int seed, out List<(Vector3 p, Vector3 inward)> rim)
        {
            const int seg = 140, rows = 16;
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            rim = new List<(Vector3, Vector3)>();
            float sd = seed * 0.71f;
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.Lerp(-78f, 78f, i / (float)seg) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Sin(a) * 900f, 0f, Mathf.Cos(a) * 700f);
                var outward = new Vector3(Mathf.Sin(a) / 900f, 0f, Mathf.Cos(a) / 700f).normalized;
                float hgt = 150f + 80f * Mathf.PerlinNoise(sd + i * 0.045f, 3.1f) + 25f * (Mathf.PerlinNoise(sd + i * 0.21f, 8.7f) - 0.5f);
                // 세로 협곡: 몇 군데 절벽이 안으로 파였다
                float gully = Mathf.Pow(Mathf.Abs(Mathf.Sin(i * 0.31f + sd)), 14f) * 18f;
                for (int r = 0; r <= rows; r++)
                {
                    float t = r / (float)rows;
                    Vector3 p;
                    if (r < rows - 2)
                    {
                        // 절벽면: 아래는 바다 밑에서 시작, 위로 갈수록 살짝 뒤로 기운다. 층마다 튀어나오고 들어간다
                        float ft = t / ((rows - 2f) / rows);
                        float y = Mathf.Lerp(-10f, hgt, ft);
                        float lean = ft * 55f + Mathf.Sin(ft * 9f + i * 0.17f) * 6f + (Mathf.PerlinNoise(sd + i * 0.3f, ft * 6f) - 0.5f) * 22f + gully * (1f - ft * 0.5f);
                        p = dir + outward * lean + Vector3.up * y;
                    }
                    else
                    {
                        // 윗면: 가장자리에서 바깥(섬 안쪽)으로 평지
                        float back = r == rows - 1 ? 70f : 260f;
                        p = dir + outward * (55f + back) + Vector3.up * (hgt + (r == rows - 1 ? 3f : 12f));
                    }
                    v.Add(p);
                    uv.Add(new Vector2(i / (float)seg * 30f, p.y / 40f));
                    if (r == rows - 2) rim.Add((p, outward));
                }
            }
            var cliffT = new List<int>();
            var bandT = new List<int>();
            var topT = new List<int>();
            for (int i = 0; i < seg; i++)
            for (int r = 0; r < rows; r++)
            {
                int a0 = i * (rows + 1) + r, a1 = a0 + 1, b0 = a0 + rows + 1, b1 = b0 + 1;
                // 바다 쪽(-outward, 원점 쪽)에서 보이게: 호의 안쪽이 앞면
                var tri = new[] { a0, a1, b0, b0, a1, b1 };
                float yMid = (v[a0].y + v[a1].y) * 0.5f;
                float hgtHere = v[i * (rows + 1) + rows - 2].y;
                var list = r >= rows - 2 ? topT : (yMid > hgtHere * 0.42f && yMid < hgtHere * 0.55f) ? bandT : cliffT;
                list.AddRange(tri);
            }
            var m = new Mesh { name = "Caldera", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.subMeshCount = 3;
            m.SetTriangles(cliffT, 0);
            m.SetTriangles(bandT, 1);
            m.SetTriangles(topT, 2);
            m.RecalculateNormals();
            m.RecalculateTangents();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>나일강 하류 서쪽 사막 너머 기자 피라미드 셋 (멀리, 안개 속 모래색).</summary>
        void Pyramids()
        {
            var stone = Mats.Lit(new Color(0.82f, 0.72f, 0.52f), 0.05f);
            var pts = new[] { (new Vector3(-260f, 0f, 700f), 146f), (new Vector3(-80f, 0f, 760f), 136f), (new Vector3(70f, 0f, 820f), 65f) };
            foreach (var (p, h) in pts)
            {
                var go = new GameObject("Pyramid");
                go.transform.SetParent(_root, false);
                go.transform.localPosition = p + Vector3.up * (_map.LandY - 2f);
                go.transform.localRotation = Quaternion.Euler(0f, 12f, 0f);
                go.AddComponent<MeshFilter>().sharedMesh = PyramidMesh(h * 1.57f, h);
                go.AddComponent<MeshRenderer>().sharedMaterial = stone;
            }
        }

        static Mesh PyramidMesh(float baseW, float height)
        {
            float b = baseW * 0.5f;
            var apex = new Vector3(0f, height, 0f);
            var c = new[] { new Vector3(-b, 0f, -b), new Vector3(b, 0f, -b), new Vector3(b, 0f, b), new Vector3(-b, 0f, b) };
            var v = new Vector3[12];
            var t = new int[12];
            for (int i = 0; i < 4; i++)
            {
                v[i * 3] = c[i];
                v[i * 3 + 1] = apex;
                v[i * 3 + 2] = c[(i + 1) % 4];
                t[i * 3] = i * 3; t[i * 3 + 1] = i * 3 + 1; t[i * 3 + 2] = i * 3 + 2;
            }
            var m = new Mesh { name = "Pyramid", vertices = v, triangles = t };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }
}
