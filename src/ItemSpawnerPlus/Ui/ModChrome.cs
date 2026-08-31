using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ItemSpawnerPlus
{
    // Procedural UI chrome for the spawner menu, lifted and trimmed from PEAK Quick
    // Resume's SavePicker so both mods read as the same UI family (palette and jag/
    // grain constants kept identical on purpose)
    internal static class ModChrome
    {
        internal static readonly Color DimColor = new Color(0f, 0f, 0f, 0.78f);
        internal static readonly Color PanelFillColor = new Color(0x34 / 255f, 0x54 / 255f, 0xD1 / 255f);
        internal static readonly Color PanelBorderColor = new Color(0x21 / 255f, 0x31 / 255f, 0x7E / 255f);
        internal static readonly Color BadgeBorderColor = new Color(0x0A / 255f, 0x0D / 255f, 0x1A / 255f);
        internal static readonly Color TitleColor = new Color(0.98f, 0.99f, 1f);
        internal static readonly Color FooterColor = new Color(0.85f, 0.9f, 1f);
        // shared by the search field and the vanilla item tiles so they read the same
        internal static readonly Color PanelInsetColor = new Color(0f, 0f, 0f, 0.38f);
        // absolute tile fills written straight to Image.color (see TileHover), not tints
        internal static readonly Color TileHoverColor = new Color(0.70f, 0.53f, 0.17f, 0.72f);
        internal static readonly Color TilePressColor = new Color(0.84f, 0.64f, 0.22f, 0.90f);
        // per-class tile fills: vanilla keeps the plain inset, modded reads purple,
        // not-normally-available reads teal, creatures read blood red
        internal static readonly Color TileModdedColor = new Color(0.40f, 0.13f, 0.58f, 0.70f);
        internal static readonly Color TileSpecialColor = new Color(0.06f, 0.42f, 0.44f, 0.68f);
        internal static readonly Color TileCreatureColor = new Color(0.66f, 0.13f, 0.24f, 0.72f);

        internal static Color TileColorFor(ItemClass c) =>
            c == ItemClass.Modded ? TileModdedColor
            : c == ItemClass.Special ? TileSpecialColor
            : c == ItemClass.Creature ? TileCreatureColor
            : PanelInsetColor;
        internal static readonly Color KeyChipFillColor = new Color(0.10f, 0.16f, 0.44f);
        internal static readonly Color KeyTextColor = new Color(1f, 0.95f, 0.72f);

        internal const float PanelCornerRadius = 26f;
        internal const float PanelBorderThickness = 11f;
        internal const float PanelOuterMargin = PanelBorderThickness - 7f;

        private const float EdgeJagAmplitude = 5.0f;
        private const float EdgeJagFrequency = 1.2f;
        private const int EdgeJagOctaves = 2;
        private const float EdgeJagPersistence = 0.5f;
        private const float EdgeJagLacunarity = 2.44f;
        internal const int JagFrameCount = 3;
        internal const float JagFrameInterval = 0.5f;
        private static readonly float[] JagFrameSeedOffsets = { 0f, 173.2f, 401.7f };

        internal const int GrainTextureSize = 368;
        private const float GrainSeed = 1337f;
        private const float GrainEnvelopeFreq = 14.0f;
        private const int GrainOctaves = 6;
        private const float GrainPersistence = 0.76f;
        private const float GrainLacunarity = 2.98f;
        private const float GrainSharpenMin = 0.61f;
        private const float GrainSharpenMax = 0.00f;
        private const float GrainLightMul = 1.03f;
        private const float GrainDarkMul = 1.00f;

        private static readonly Dictionary<(int width, int height), Sprite[]> _panelSpriteCache = new();
        private static readonly Dictionary<(int width, int height), Sprite> _panelSpriteFlatCache = new();
        private static Sprite _panelInnerMaskSprite;
        private static Sprite _badgeSprite;
        private static Sprite _tileSprite;
        private static Texture2D _grainTexturePanel;
        private static Material _chromeOutlineMaterial;

        private static readonly string[] PreferredFontNames =
        {
            "DarumaDropOne-Regular SDF", "Pangolin-Regular SDF", "Montserrat-Medium SDF", "LiberationSans SDF",
        };

        internal static TMP_FontAsset FindGameFont()
        {
            try
            {
                var all = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                foreach (string name in PreferredFontNames)
                    foreach (var f in all)
                        if (f != null && f.name == name) return f;
                return all.Length > 0 ? all[0] : null;
            }
            catch { return null; }
        }

        // borrows the game's own outline+shadow TMP material, retried on demand since
        // the native UI may not have created an instance yet
        internal static Material FindChromeOutlineMaterial()
        {
            if (_chromeOutlineMaterial != null) return _chromeOutlineMaterial;
            try
            {
                var texts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
                foreach (var t in texts)
                {
                    var mat = t != null ? t.materialForRendering : null;
                    if (mat != null && mat.name.Contains("DarumaDropOne-Regular SDF Outline"))
                    {
                        _chromeOutlineMaterial = mat;
                        break;
                    }
                }
            }
            catch { }
            return _chromeOutlineMaterial;
        }

        internal static void ApplyChromeTextStyle(TextMeshProUGUI tmp)
        {
            var mat = FindChromeOutlineMaterial();
            if (mat != null && tmp != null) tmp.fontSharedMaterial = mat;
        }

        // pins the canvas to a constant 1080 reference height so ultrawide aspect
        // ratios don't shrink it below the panel's own height
        internal static void ApplyWidescreenScaler(Canvas canvas)
        {
            var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
        }

        internal static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        internal static TextMeshProUGUI MakeText(Transform parent, string name, float fontSize, Color color,
            TextAlignmentOptions alignment, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Normal; // this font has no real bold face, TMP faking it is unreadable
            tmp.color = color;
            tmp.richText = true;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        // baked at the panel's exact size: Type.Simple stretches the whole texture,
        // so baking at the wrong height flattens the round corners into ellipses
        internal static Sprite PanelSprite(int width, int height, int frame, bool minimal)
        {
            if (minimal)
            {
                var flatKey = (width, height);
                if (!_panelSpriteFlatCache.TryGetValue(flatKey, out Sprite flat))
                {
                    flat = MakeFullPanelSprite(width, height, PanelCornerRadius, PanelBorderThickness,
                        PanelFillColor, PanelBorderColor, 0f, EdgeJagFrequency, 0f);
                    _panelSpriteFlatCache[flatKey] = flat;
                }
                return flat;
            }

            var key = (width, height);
            if (!_panelSpriteCache.TryGetValue(key, out Sprite[] frames))
            {
                frames = new Sprite[JagFrameCount];
                _panelSpriteCache[key] = frames;
            }
            if (frames[frame] == null)
            {
                frames[frame] = MakeFullPanelSprite(width, height, PanelCornerRadius, PanelBorderThickness,
                    PanelFillColor, PanelBorderColor, EdgeJagAmplitude, EdgeJagFrequency, JagFrameSeedOffsets[frame]);
            }
            return frames[frame];
        }

        private static Sprite MakeFullPanelSprite(int width, int height, float radius, float borderThickness,
            Color fill, Color border, float edgeJag, float jagFreq, float seedOffset)
        {
            var tex = new Texture2D(width, height, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float jagOuter = edgeJag > 0f ? (Fbm(fx * jagFreq + 11.3f + seedOffset, fy * jagFreq + 11.3f + seedOffset, EdgeJagOctaves, EdgeJagPersistence, EdgeJagLacunarity) - 0.5f) * edgeJag : 0f;
                    float jagInner = edgeJag > 0f ? (Fbm(fx * jagFreq + 77.1f + seedOffset, fy * jagFreq + 41.9f + seedOffset, EdgeJagOctaves, EdgeJagPersistence, EdgeJagLacunarity) - 0.5f) * edgeJag : 0f;
                    float cx = Mathf.Clamp(fx, radius, width - radius);
                    float cy = Mathf.Clamp(fy, radius, height - radius);
                    float dist = Mathf.Sqrt((fx - cx) * (fx - cx) + (fy - cy) * (fy - cy));
                    float shapeAlpha = Mathf.Clamp01(radius - dist + jagOuter + 0.5f);
                    float insideDist = radius - dist;
                    float fillT = Mathf.Clamp01(insideDist - borderThickness + jagInner + 0.5f);
                    Color c = Color.Lerp(border, fill, fillT);
                    c.a = shapeAlpha;
                    pixels[y * width + x] = c;
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        internal static Sprite PanelInnerMaskSprite() => _panelInnerMaskSprite ??=
            MakeCapSprite(Mathf.Max(1f, PanelCornerRadius - PanelBorderThickness));

        internal static Sprite TileSprite() => _tileSprite ??= MakeCapSprite(12f);

        // alpha-only rounded shape, tinted per use via Image.color
        internal static Sprite MakeCapSprite(float radius)
        {
            const int size = 48;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float cx = Mathf.Clamp(fx, radius, size - radius);
                    float cy = Mathf.Clamp(fy, radius, size - radius);
                    float dist = Mathf.Sqrt((fx - cx) * (fx - cx) + (fy - cy) * (fy - cy));
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            var b = new Vector4(radius, radius, radius, radius);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, b);
        }

        internal static Sprite BadgeSprite() => _badgeSprite ??=
            MakeRoundedSprite(size: 32, radius: 10f, borderThickness: 3f, fill: KeyChipFillColor, border: BadgeBorderColor);

        private static Sprite _checkSprite;

        // checkbox tick: two strokes, white
        internal static Sprite CheckSprite() => _checkSprite ??= BakeCheck();

        private static Sprite BakeCheck()
        {
            const int s = 24;
            var tex = new Texture2D(s, s, TextureFormat.ARGB32, false)
            { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[s * s];
            // texture y is bottom-up: the mid vertex is the LOW point, the long stroke ends high
            var a1 = new Vector2(s * 0.16f, s * 0.54f);
            var b1 = new Vector2(s * 0.40f, s * 0.28f);
            var a2 = b1;
            var b2 = new Vector2(s * 0.84f, s * 0.76f);
            float half = s * 0.11f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = Mathf.Min(SegDist(p, a1, b1), SegDist(p, a2, b2)) - half;
                    px[y * s + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        private static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }

        private static Sprite _flameSprite, _filterSprite, _clearSprite, _errGlyph, _warnGlyph, _errChip, _warnChip;

        // Material Symbols icons, shipped as embedded white-on-transparent PNGs
        internal static Sprite FlameSprite() => _flameSprite ??= LoadEmbeddedSprite("ItemSpawnerPlus.flame.png");
        internal static Sprite FilterSprite() => _filterSprite ??= LoadEmbeddedSprite("ItemSpawnerPlus.filter.png");
        internal static Sprite ClearSprite() => _clearSprite ??= LoadEmbeddedSprite("ItemSpawnerPlus.clear.png");

        // corner badges: near-black chip, coloured ring + glyph (error red, warning orange)
        private static readonly Color BadgeChipFill = new Color(0.09f, 0.08f, 0.04f, 0.97f);
        internal static readonly Color ErrorGlyphColor = new Color(0.93f, 0.13f, 0.14f);
        internal static readonly Color ExplodeGlyphColor = new Color(1f, 0.56f, 0.05f);
        internal static Sprite ErrorGlyphSprite() => _errGlyph ??= LoadEmbeddedSprite("ItemSpawnerPlus.warn-error.png");
        internal static Sprite ExplodeGlyphSprite() => _warnGlyph ??= LoadEmbeddedSprite("ItemSpawnerPlus.warn-explode.png");
        internal static Sprite ErrorChipSprite() => _errChip ??=
            MakeRoundedSprite(36, 18f, 2.4f, BadgeChipFill, new Color(0.90f, 0.11f, 0.13f, 0.96f));
        internal static Sprite ExplodeChipSprite() => _warnChip ??=
            MakeRoundedSprite(36, 18f, 2.2f, BadgeChipFill, new Color(0.98f, 0.62f, 0.08f, 0.95f));

        private static Sprite LoadEmbeddedSprite(string resource)
        {
            var tex = LoadEmbeddedTexture(resource);
            return tex != null
                ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f)
                : null;
        }

        // LoadImage handles PNG/JPG only, no webp
        internal static Texture2D LoadEmbeddedTexture(string resource)
        {
            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                using var st = asm.GetManifestResourceStream(resource);
                if (st == null) return null;
                var buf = new byte[st.Length];
                int read = 0;
                while (read < buf.Length)
                {
                    int n = st.Read(buf, read, buf.Length - read);
                    if (n <= 0) break;
                    read += n;
                }
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
                if (tex.LoadImage(buf)) return tex;
            }
            catch { }
            return null;
        }

        private static Sprite MakeRoundedSprite(int size, float radius, float borderThickness, Color fill, Color border)
        {
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float cx = Mathf.Clamp(fx, radius, size - radius);
                    float cy = Mathf.Clamp(fy, radius, size - radius);
                    float dist = Mathf.Sqrt((fx - cx) * (fx - cx) + (fy - cy) * (fy - cy));
                    float shapeAlpha = Mathf.Clamp01(radius - dist + 0.5f);
                    float insideDist = radius - dist;
                    float fillT = Mathf.Clamp01(insideDist - borderThickness + 0.5f);
                    Color c = Color.Lerp(border, fill, fillT);
                    c.a = shapeAlpha;
                    pixels[y * size + x] = c;
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            var b = new Vector4(radius, radius, radius, radius);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, b);
        }

        internal static Texture2D PanelGrainTexture() =>
            _grainTexturePanel != null ? _grainTexturePanel
                : (_grainTexturePanel = GenerateGrainTexture(PanelFillColor, GrainTextureSize, GrainTextureSize));

        private static Texture2D GenerateGrainTexture(Color baseColor, int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            Color dark = new Color(
                Mathf.Clamp01(baseColor.r * GrainDarkMul), Mathf.Clamp01(baseColor.g * GrainDarkMul), Mathf.Clamp01(baseColor.b * GrainDarkMul));
            Color light = new Color(
                Mathf.Clamp01(baseColor.r * GrainLightMul), Mathf.Clamp01(baseColor.g * GrainLightMul), Mathf.Clamp01(baseColor.b * GrainLightMul));

            var envelopes = new float[width * height];
            float minEnvelope = float.MaxValue, maxEnvelope = float.MinValue;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = x / (float)width, ny = y / (float)height;
                    float envelope = Fbm(nx * GrainEnvelopeFreq + GrainSeed * 0.001f, ny * GrainEnvelopeFreq + GrainSeed * 0.001f,
                        GrainOctaves, GrainPersistence, GrainLacunarity);
                    envelopes[y * width + x] = envelope;
                    if (envelope < minEnvelope) minEnvelope = envelope;
                    if (envelope > maxEnvelope) maxEnvelope = envelope;
                }
            }
            float envelopeRange = Mathf.Max(0.0001f, maxEnvelope - minEnvelope);
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float normalized = (envelopes[y * width + x] - minEnvelope) / envelopeRange;
                    float n = SmoothStepEdge(GrainSharpenMin, GrainSharpenMax, normalized);
                    pixels[y * width + x] = Color.Lerp(dark, light, n);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        private static float Fbm(float x, float y, int octaves, float persistence, float lacunarity)
        {
            float total = 0f, amplitude = 1f, frequency = 1f, max = 0f;
            for (int i = 0; i < octaves; i++)
            {
                total += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
                max += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            return total / max;
        }

        // GLSL-style smoothstep(edge0, edge1, x), not Mathf.SmoothStep which lerps between from/to
        private static float SmoothStepEdge(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / Mathf.Max(0.0001f, edge1 - edge0));
            return t * t * (3f - 2f * t);
        }
    }
}
