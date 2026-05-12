using UnityEngine;

internal static class TacticalBackgroundBaker
{
    public static Color32[] BakeDiagonalGradient(int W, int H,
                                                  Color32 warmCorner, Color32 coldCorner,
                                                  int noiseAmplitude, int noiseSeed)
    {
        Color32[] dest = new Color32[W * H];
        for (int y = 0; y < H; y++)
        {
            int row = y * W;

            float v = 1f - y / (float)(H - 1);
            for (int x = 0; x < W; x++)
            {
                float u = x / (float)(W - 1);
                float t = Mathf.Clamp01((u + v) * 0.5f);
                t = t * t * (3f - 2f * t);
                dest[row + x] = new Color32(
                    (byte)Mathf.Lerp(warmCorner.r, coldCorner.r, t),
                    (byte)Mathf.Lerp(warmCorner.g, coldCorner.g, t),
                    (byte)Mathf.Lerp(warmCorner.b, coldCorner.b, t),
                    255);
            }
        }
        if (noiseAmplitude > 0) AddNoise(dest, noiseSeed, noiseAmplitude);
        return dest;
    }

    public static Color32[] BakeRadialGradient(int W, int H,
                                               Color32 center, Color32 corner,
                                               int noiseAmplitude, int noiseSeed)
    {
        Color32[] dest = new Color32[W * H];
        float cx = (W - 1) * 0.5f, cy = (H - 1) * 0.5f;
        float maxD = Mathf.Sqrt(cx * cx + cy * cy);
        for (int y = 0; y < H; y++)
        {
            int row = y * W;
            for (int x = 0; x < W; x++)
            {
                float dx = x - cx, dy = y - cy;
                float t = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / maxD);
                t = t * t * (3f - 2f * t);
                dest[row + x] = new Color32(
                    (byte)Mathf.Lerp(center.r, corner.r, t),
                    (byte)Mathf.Lerp(center.g, corner.g, t),
                    (byte)Mathf.Lerp(center.b, corner.b, t),
                    255);
            }
        }
        if (noiseAmplitude > 0) AddNoise(dest, noiseSeed, noiseAmplitude);
        return dest;
    }

    public static Color32[] BakeRadialGlow(int size, byte centerAlpha)
    {
        Color32[] dest = new Color32[size * size];
        float c = (size - 1) * 0.5f;
        float maxD = c;
        for (int y = 0; y < size; y++)
        {
            int row = y * size;
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float t = Mathf.Clamp01(1f - d / maxD);
                t = t * t * (3f - 2f * t);
                dest[row + x] = new Color32(255, 255, 255, (byte)(t * centerAlpha));
            }
        }
        return dest;
    }

    public static Color32[] BakeHexEmblem(int W, int H, float[] ringApothems, float strokePx)
    {
        Color32[] dest = new Color32[W * H];
        float cx = (W - 1) * 0.5f;
        float cy = (H - 1) * 0.5f;
        float scale = Mathf.Min(cx, cy);
        float halfStroke = strokePx * 0.5f;
        for (int y = 0; y < H; y++)
        {
            int row = y * W;
            for (int x = 0; x < W; x++)
            {
                float px = Mathf.Abs(x - cx);
                float py = Mathf.Abs(y - cy);
                float bestAlpha = 0f;
                for (int i = 0; i < ringApothems.Length; i++)
                {
                    float a = ringApothems[i] * scale;

                    float d = Mathf.Max(px * 0.8660254f + py * 0.5f, py) - a;
                    float dist = Mathf.Abs(d);
                    float t = Mathf.Clamp01((halfStroke + 1f - dist) / 2f);
                    if (t > bestAlpha) bestAlpha = t;
                }
                dest[row + x] = new Color32(255, 255, 255, (byte)(bestAlpha * 255));
            }
        }
        return dest;
    }

    public static Color32[] BakeSunRays(int W, int H, int rayCount, int seed)
    {
        Color32[] dest = new Color32[W * H];
        float cx = (W - 1) * 0.5f;

        System.Random rng = new System.Random(seed);
        float[] angles = new float[rayCount];
        float[] widths = new float[rayCount];
        float[] strengths = new float[rayCount];
        for (int i = 0; i < rayCount; i++)
        {
            angles[i]   = (float)(rng.NextDouble() - 0.5) * 70f * Mathf.Deg2Rad;
            widths[i]   = 0.04f + (float)rng.NextDouble() * 0.05f;
            strengths[i] = 0.5f + (float)rng.NextDouble() * 0.5f;
        }

        for (int y = 0; y < H; y++)
        {

            float fade = y / (float)(H - 1);
            fade = fade * fade;

            float dy = (H - y) + 0.5f;

            int row = y * W;
            for (int x = 0; x < W; x++)
            {
                float dx = x - cx;
                float ang = Mathf.Atan2(dx, dy);

                float maxBand = 0f;
                for (int i = 0; i < rayCount; i++)
                {
                    float dAng = Mathf.Abs(ang - angles[i]);
                    float band = Mathf.Clamp01(1f - dAng / widths[i]);
                    band = band * band * strengths[i];
                    if (band > maxBand) maxBand = band;
                }

                float a = maxBand * fade;
                dest[row + x] = new Color32(255, 255, 255, (byte)(a * 200));
            }
        }
        return dest;
    }

    public static Color32[] BakeFloorBand(int W, int H)
    {
        Color32[] dest = new Color32[W * H];
        float cx = (W - 1) * 0.5f;
        for (int y = 0; y < H; y++)
        {
            float v = y / (float)(H - 1);
            float vAlpha = Mathf.Clamp01(1f - v);
            vAlpha = vAlpha * vAlpha;

            int row = y * W;
            for (int x = 0; x < W; x++)
            {
                float u = (x - cx) / cx;
                float hAlpha = Mathf.Clamp01(1f - Mathf.Abs(u) * 0.7f);
                hAlpha = hAlpha * hAlpha;

                float a = vAlpha * hAlpha;
                dest[row + x] = new Color32(255, 255, 255, (byte)(a * 220));
            }
        }
        return dest;
    }

    public static Color32[] BakeSilhouette(string sourcePngPath, int W, int H, int blurRadius)
    {
        Color32[] buf = LoadAndScale(sourcePngPath, W, H);
        if (buf == null) return null;
        for (int i = 0; i < buf.Length; i++)
        {
            buf[i].r = 255;
            buf[i].g = 255;
            buf[i].b = 255;
        }
        if (blurRadius > 0) BoxBlurSeparable(buf, W, H, blurRadius);
        return buf;
    }

    private static void AddNoise(Color32[] dest, int seed, int amplitude)
    {
        uint s = (uint)seed;
        for (int i = 0; i < dest.Length; i++)
        {
            s = s * 1664525u + 1013904223u;
            int n = (int)(s & 0xFF) - 128;
            int delta = (n * amplitude) / 128;
            Color32 c = dest[i];
            c.r = ClampByte(c.r + delta);
            c.g = ClampByte(c.g + delta);
            c.b = ClampByte(c.b + delta);
            dest[i] = c;
        }
    }

    private static byte ClampByte(int v)
    {
        if (v < 0) return 0;
        if (v > 255) return 255;
        return (byte)v;
    }

    private static Color32[] LoadAndScale(string assetPath, int destW, int destH)
    {
        if (!System.IO.File.Exists(assetPath))
        {
            Debug.LogWarning("[TacticalBackgroundBaker] missing: " + assetPath);
            return null;
        }
        byte[] bytes = System.IO.File.ReadAllBytes(assetPath);
        Texture2D src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!src.LoadImage(bytes))
        {
            Debug.LogWarning("[TacticalBackgroundBaker] decode failed: " + assetPath);
            Object.DestroyImmediate(src);
            return null;
        }
        int srcW = src.width, srcH = src.height;
        Color32[] srcPx = src.GetPixels32();
        Object.DestroyImmediate(src);

        Color32[] dst = new Color32[destW * destH];
        for (int y = 0; y < destH; y++)
        {
            float vy = (y + 0.5f) / destH * srcH - 0.5f;
            int y0 = Mathf.Clamp((int)Mathf.Floor(vy), 0, srcH - 1);
            int y1 = Mathf.Clamp(y0 + 1, 0, srcH - 1);
            float fy = Mathf.Clamp01(vy - y0);
            for (int x = 0; x < destW; x++)
            {
                float vx = (x + 0.5f) / destW * srcW - 0.5f;
                int x0 = Mathf.Clamp((int)Mathf.Floor(vx), 0, srcW - 1);
                int x1 = Mathf.Clamp(x0 + 1, 0, srcW - 1);
                float fx = Mathf.Clamp01(vx - x0);

                Color32 c00 = srcPx[y0 * srcW + x0];
                Color32 c01 = srcPx[y0 * srcW + x1];
                Color32 c10 = srcPx[y1 * srcW + x0];
                Color32 c11 = srcPx[y1 * srcW + x1];

                float w00 = (1f - fx) * (1f - fy);
                float w01 = fx * (1f - fy);
                float w10 = (1f - fx) * fy;
                float w11 = fx * fy;

                dst[y * destW + x] = new Color32(
                    (byte)(c00.r * w00 + c01.r * w01 + c10.r * w10 + c11.r * w11),
                    (byte)(c00.g * w00 + c01.g * w01 + c10.g * w10 + c11.g * w11),
                    (byte)(c00.b * w00 + c01.b * w01 + c10.b * w10 + c11.b * w11),
                    (byte)(c00.a * w00 + c01.a * w01 + c10.a * w10 + c11.a * w11));
            }
        }
        return dst;
    }

    private static void BoxBlurSeparable(Color32[] buf, int w, int h, int radius)
    {
        Color32[] tmp = new Color32[buf.Length];

        for (int y = 0; y < h; y++)
        {
            int row = y * w;
            for (int x = 0; x < w; x++)
            {
                int sumR = 0, sumG = 0, sumB = 0, sumA = 0, n = 0;
                int xMin = Mathf.Max(0, x - radius);
                int xMax = Mathf.Min(w - 1, x + radius);
                for (int sx = xMin; sx <= xMax; sx++)
                {
                    Color32 c = buf[row + sx];
                    sumR += c.r; sumG += c.g; sumB += c.b; sumA += c.a;
                    n++;
                }
                tmp[row + x] = new Color32(
                    (byte)(sumR / n), (byte)(sumG / n), (byte)(sumB / n), (byte)(sumA / n));
            }
        }

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                int sumR = 0, sumG = 0, sumB = 0, sumA = 0, n = 0;
                int yMin = Mathf.Max(0, y - radius);
                int yMax = Mathf.Min(h - 1, y + radius);
                for (int sy = yMin; sy <= yMax; sy++)
                {
                    Color32 c = tmp[sy * w + x];
                    sumR += c.r; sumG += c.g; sumB += c.b; sumA += c.a;
                    n++;
                }
                buf[y * w + x] = new Color32(
                    (byte)(sumR / n), (byte)(sumG / n), (byte)(sumB / n), (byte)(sumA / n));
            }
        }
    }
}
