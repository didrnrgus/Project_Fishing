# 07. 셰이더 기법

[← 기술 문서 목차](README.md) · 코드: `Assets/Shaders/Water.shader`, `Ground.shader`, `FishingNoise.cginc`, `Grass.shader`, `Skin.shader`, `World/WaterSurface.cs`

Built-in 렌더 파이프라인의 Surface Shader로 썼다. 비싼 기능(깊이 텍스처, 화면 공간 효과) 없이 "그럴듯하게" 보이게 한 방법들이다.

## 1. 물가 거품: 해저 높이 텍스처

물이 땅·방파제·바위에 닿는 곳에 흰 거품 띠가 밀려왔다 빠진다. 보통은 **카메라 깊이 텍스처**로 "물 표면과 그 뒤 물체 사이 거리"를 재지만, 이 방법은 그림자·AO와 부딪치고 각도에 따라 띠 폭이 변한다.

### 굽기 (맵을 불러올 때 한 번, CPU)

```
WaterSurface.SetSeabed(map):
  맵 범위를 1m 간격(최대 512×512)으로 훑어
  texel = map.SeabedAt(x, z)      // 물 밑 땅 높이 (방파제·바위기둥은 그 윗면 높이)
  → RFloat 텍스처 _SeabedTex + 범위 _SeabedRect + 텍셀 크기 _SeabedStep
```

높이 함수([06](06-procedural-terrain.md))가 있으니 그대로 샘플링하면 된다.

### 그리기 (픽셀마다, GPU)

```hlsl
float bed   = tex2D(_SeabedTex, uv).r;
float depth = IN.worldPos.y - bed;                  // 수심 = 지금 수면(파도·물때 포함) − 해저

// 중앙 차분으로 해저 경사 (m/m)
float bx = (tex2D(_SeabedTex, uv + float2(_SeabedStep.x, 0)).r - tex2D(_SeabedTex, uv - float2(_SeabedStep.x, 0)).r) / (2 * _SeabedStep.z);
float bz = (tex2D(_SeabedTex, uv + float2(0, _SeabedStep.y)).r - tex2D(_SeabedTex, uv - float2(0, _SeabedStep.y)).r) / (2 * _SeabedStep.w);
float slope = max(length(float2(bx, bz)), 0.03);

float dist = max(depth, 0) / slope;                 // 물가까지 수평 거리 ≈ 수심 ÷ 경사
```

- **핵심 어림**: 해저가 일정한 경사로 올라간다고 보면 "물가까지 거리 = 수심 / 경사"다. 거리장(distance field)을 따로 굽지 않아도 된다.
- 결과가 자연스럽게 맞는다: 완만한 해변은 경사가 작아 **띠가 넓고**, 호안·방파제 같은 벽은 경사가 커서 **띠가 얇다**.
- 경사 하한 0.03은 평평한 바닥에서 0으로 나누는 것을 막는다.
- 수면 높이를 `worldPos.y`(버텍스 파도가 들어간 값)로 쓰니 **파도와 물때에 따라 거품 선이 움직인다**. 물때가 바뀌어도 텍스처를 다시 굽지 않는다.

### 거품 모양

```hlsl
float edge = saturate(1 - dist / _FoamWidth);                           // 물가 4.5m 안
float lap  = sin(dist * 2.1 + t * 1.7 + noise * 7.0) * 0.5 + 0.5;      // 물가 쪽으로 밀려오는 줄무늬
float foam = saturate(edge * edge * 1.5 + edge * lap * 0.75);
foam *= smoothstep(0.32, 0.62, noise + edge * 0.45);                    // 노이즈로 끊긴 가장자리
foam = saturate(foam + step(depth, 0.05) * 0.9);                        // 물이 땅에 닿는 선은 늘 하얗게
```

- `sin(거리 × 주파수 + 시간)`은 **거리 등고선을 따라 움직이는 파문**이다. 물가 모양이 어떻든 띠가 물가와 나란히 밀려온다.
- 노이즈는 이미 있는 물 노멀맵을 두 번(다른 크기·방향으로 흐르게) 읽어 섞는다. 텍스처를 새로 만들지 않는다.
- 얕은 물(수심 2.5m 미만)은 같은 depth로 밝게 해서 바닥이 비치는 느낌을 준다.

### 함정과 해결

- **물 위에 뜬 잔교**(한강 낚시 데크)를 해저로 구우면 데크 둘레에 해변 같은 거품이 생긴다 → `SeabedAt`에서 기둥 위 구조물은 깊은 물로 돌린다.
- 새 맵은 `SeabedAt`만 지형 높이 함수로 바꿔 주면 된다. 기본값은 "물이면 수심 3m".

## 2. 타일 반복 깨기 (`Fishing/Ground`)

같은 바닥 텍스처를 넓게 깔면 격자 무늬가 눈에 띈다. 텍스처를 늘리지 않고 셰이더에서 깬다.

```hlsl
// ① 원래 UV를 37° 돌리고 0.73배 + 어긋남 → 원래 격자와 맞지 않는 두 번째 샘플
const float2x2 rot = float2x2(0.7986, -0.6018, 0.6018, 0.7986);
float2 uv2 = mul(rot, uv) * 0.73 + float2(0.37, 0.61);

// ② 타일보다 큰 노이즈로 두 샘플을 섞는다 (UV 기준 + 월드 기준)
float n = FishNoise(uv * 0.55) * 0.6 + FishNoise(IN.worldPos.xz * 0.11 + 7.1) * 0.4;
float m = smoothstep(0.35, 0.65, n);
fixed4 c = lerp(tex2D(_MainTex, uv), tex2D(_MainTex, uv2), m);

// ③ 더 큰 월드 얼룩으로 밝기·채도·광택을 흔든다
float macro = FishNoise(IN.worldPos.xz * 0.045) * 0.65 + FishNoise(IN.worldPos.xz * 0.19 + 3.3) * 0.35;
```

- **두 샘플의 격자가 서로 맞지 않게**(회전 + 무리수에 가까운 배율 0.73) 하는 게 요점이다. 섞인 결과의 반복 주기가 아주 길어진다.
- 섞는 비율 노이즈를 **타일보다 크게** 해야 한다. 작으면 두 텍스처가 지글지글 섞여 흐려 보인다. `smoothstep`으로 대비를 높여 "이쪽은 A, 저쪽은 B" 얼룩이 되게 한다.
- **노멀맵도 같이** 두 번 읽되, 회전해서 읽은 노멀은 xy를 다시 돌려 원래 방향으로 맞춘다(`nb.xy = mul(nb.xy, rot)`). 안 하면 빛 방향이 엉뚱해진다.
- 월드 좌표 노이즈를 쓰니 **정적 배칭해도 결과가 같다**([08](08-performance.md)). 오브젝트 좌표를 쓰면 배칭 뒤 무늬가 바뀐다.
- 같은 방법을 Unity Terrain 셰이더(`FishingTerrain.cginc`)에도 층마다 넣었다.

## 3. 값 노이즈 (`FishingNoise.cginc`)

```hlsl
float FishHash(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }

float FishNoise(float2 p)
{
    float2 i = floor(p), f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);            // smoothstep 보간 → 격자 경계가 안 보인다
    return lerp(lerp(FishHash(i), FishHash(i + float2(1, 0)), u.x),
                lerp(FishHash(i + float2(0, 1)), FishHash(i + float2(1, 1)), u.x), u.y);
}
```

- 텍스처 없는 해시 + 쌍선형 보간. Perlin(기울기 노이즈)보다 단순하고 이 용도(얼룩)엔 충분하다.
- 흔히 쓰는 `sin` 기반 해시(`frac(sin(dot(...)) * 43758)`)는 큰 입력에서 GPU마다 정밀도 차이로 줄무늬가 생기기 쉽다. 이 해시는 곱셈·`frac`만 써서 그 문제가 덜하다.
- 크기가 다른 두 노이즈를 6:4로 섞어 한 옥타브짜리의 뭉툭함을 줄인다.

## 4. 풀 흔들림: 버텍스 셰이더

```hlsl
float sway = sin(t * 1.7 + wp.x * 0.35 + wp.z * 0.27) + 0.45 * sin(t * 3.3 + wp.x * 1.1 - wp.z * 0.8);
float k = v.texcoord.y * v.texcoord.y;              // 뿌리(v=0)는 고정, 끝으로 갈수록 크게
v.vertex.xz += float2(sway, sway * 0.6) * _Wind * k;
```

- 위치를 위상에 넣어(`wp.x * 0.35 …`) 바람이 들판을 **물결처럼 지나간다**.
- 주파수가 다른 사인 두 개를 더해 규칙성을 깬다.
- UV의 v²으로 무게를 줘서 뿌리는 땅에 붙어 있다.
- 메쉬가 이미 월드 좌표로 구워져 있어서(48m 칸) `v.vertex`가 곧 월드 위치다.

## 5. 버텍스 컬러 마스크로 피부 자르기 (`Fishing/Skin`)

> 2026-10-20 캐릭터를 모두 VRM으로 바꾸면서 이 셰이더는 지웠다. 기법 기록으로 남긴다.

캐릭터 몸 위에 옷 메쉬를 입히면 몸이 옷을 뚫고 나온다(z-fighting·클리핑).
몸 메쉬 버텍스 컬러에 **어느 옷 아래인지**(R 상의, G 하의, B 신발, A 긴소매)를 칠해 두고, 셰이더가 지금 입은 옷을 벡터 `_Clip`으로 받아 잘라낸다.

```hlsl
clip(0.5 - dot(IN.color, _Clip));   // 입은 옷 채널이 칠해진 피부는 버린다
```

- `dot` 하나로 네 채널을 한 번에 검사한다. 옷을 바꿔도 메쉬는 그대로, `_Clip` 값만 바뀐다.
- 옷 셰이더(`Fishing/Garment`)는 반대로 트림·음영·보조 원단을 채널로 받아 체크·데님·줄무늬 무늬를 절차적으로 그린다.

## 6. 그 밖의 작은 기법

| 기법 | 어디 | 요점 |
|---|---|---|
| 알파 컷 지느러미 | `Fish.shader` | `clip()`이라 렌더 텍스처(결과 창 3D 물고기)에서도 알파가 유지된다 |
| 반투명 막 | `FishFin.shader` | 지느러미를 알파 블렌딩으로. 알파 채널은 따로 섞어(`One, OneMinusSrcAlpha`) 투명 렌더 텍스처에 그려도 막이 흐려지지 않는다. 점광원 추가 패스는 끈다(`noforwardadd`, 밤에 막이 빛나 보이는 문제) |
| 붓으로 쓰듯 드러남 | `UIInkReveal.shader` | `_Progress`에 따라 왼쪽부터 드러나고, 경계는 값 노이즈 두 겹으로 들쭉날쭉. 빛줄기 `_Shine`, 더하기 섞기로 물에 비친 달빛 |
| 흐린 하늘 돔 | `SkyCube.shader` | 스카이박스 뒤 `Transparent-300`에 큐브맵 돔을 덮고 덮임 정도·노을색으로 섞는다 |
| 하늘 반사 | `DayNightCycle` | 리얼타임 반사 프로브를 **하늘만**, 2초마다 갱신 |
| 야광충 | `Water.shader` `_BioGlow`·`_BioTouch` | 물가 거리 + 시간의 톱니 위상(`BioSaw`)으로 물가 쪽으로 밀려오는 파도 앞이 날카롭게 켜지고(가운데 흰빛 → 전기 파랑) 바다 쪽으로 꼬리를 끈다. 앞줄은 사인 두 개로 굽고, 10m 덩어리 노이즈(`BioPatch`)로 군데군데만 부서지며, 꼬리는 물결 노멀로 얼룩진다. 해시 칸마다 깜빡이는 빛 알갱이(`BioGrains`), 건드린 물(`_BioTouch` 8개: 찌·물보라·파문·발걸음)은 고리로 퍼지며 빛난다. 밤 정도(`_GlintNight`)를 곱한다. 맵이 `Bioluminescence`로 켠다(서천·삼척). `BioluminescenceFx.cs`가 같은 위상 식으로 캐릭터 둘레 16m 파도 앞에서 빛 알갱이 파티클(`Fishing/BioGlow`, 더하기·HDR)을 튀기고, `Effects.Splash`·`Ripple`에 붙어 물방울을 파랗게 한다 |
| 물속 그림자 | `FishShadow.shader` + `FishShadowFx` | 물이 불투명이라 물속 메쉬는 안 보인다 → 수면 바로 위에 납작한 띠(16칸)를 깔고 위에서 본 물고기 모양 텍스처(가장자리 흐림)의 알파로 검게 그린다. 꼬리짓은 C#이 띠의 정점을 꼬리로 갈수록 크게 `sin(위상 − v·4.2)`만큼 옆으로 민다(빠를수록 빨리). 소용돌이보다 앞 큐(`Transparent-10`)라 거품이 위에 얹힌다. 처음엔 남색 반투명(0.6)이 짙은 강물에 묻혀 거의 안 보였다 → 거의 검정·0.86, 길이 ×2 |
| 흐르는 폭포 | `Waterfall.shader` | 결 텍스처 두 겹을 다른 속도·배율로 `_Time` 스크롤(떨어질수록 가속), uv2(가로·세로 비율)로 들쭉날쭉한 가장자리·위 끝 청록 맑은 물·아래 물보라 덩어리, 역광 투과 발광. 메쉬는 `MapKit.FallSheet`(포물선) |

## 옮겨 쓸 때 체크리스트

- 물가 효과는 깊이 텍스처 대신 "해저 높이 텍스처 + 수심/경사"로. 파도·물때가 공짜로 반영된다.
- 반복 깨기는 격자가 안 맞는 두 번째 샘플 + 타일보다 큰 노이즈 섞기 + 노멀 방향 되돌리기.
- 배칭할 오브젝트의 셰이더는 월드 좌표 기반으로.
- 해시는 `sin` 없이.
