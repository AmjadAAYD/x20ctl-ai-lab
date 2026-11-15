using X20Ctl.Zone.Lighting;

namespace X20Ctl.Product;

/// <summary>
/// Samples the dominant colour of the primary screen for Ambient Screen Sync lighting.
/// Uses a small 16x9 downsampled capture so each poll stays fast and allocation-light.
/// </summary>
internal static class ScreenAmbientSampler
{
    static (byte R, byte G, byte B) lastRgb = (0x1E, 0x88, 0xE5);

    public static LightingFrame Sample(LightingEffectSpec spec)
    {
        try
        {
            var bounds = System.Windows.Forms.Screen.PrimaryScreen?.Bounds
                ?? new System.Drawing.Rectangle(0, 0, 1920, 1080);
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return Fallback(spec);

            using var raw = new System.Drawing.Bitmap(bounds.Width / 60 is > 8 and < 64 ? bounds.Width / 60 : 24,
                                                     bounds.Height / 60 is > 6 and < 40 ? bounds.Height / 60 : 14);
            // CopyFromScreen(0,0,0,0, raw.Size) doesn't scale; capture full screen into a scaled graphics surface?
            // CopyFromScreen always copies 1:1 pixels, so sample a 3x3 grid of 1x1 screen points or CopyFromScreen at center+edges.
            // Sampling a 4x3 grid of points across the screen avoids allocating a full-screen 4K bitmap!
            using var pixel = new System.Drawing.Bitmap(1, 1);
            using var g = System.Drawing.Graphics.FromImage(pixel);

            long sumR = 0, sumG = 0, sumB = 0;
            int count = 0;
            int cols = 4, rows = 3;
            for (int row = 0; row < rows; row++)
            {
                int y = bounds.Top + (bounds.Height * (row + 1)) / (rows + 1);
                for (int col = 0; col < cols; col++)
                {
                    int x = bounds.Left + (bounds.Width * (col + 1)) / (cols + 1);
                    g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(1, 1));
                    var c = pixel.GetPixel(0, 0);
                    sumR += c.R;
                    sumG += c.G;
                    sumB += c.B;
                    count++;
                }
            }

            if (count == 0) return Fallback(spec);

            byte avgR = (byte)(sumR / count);
            byte avgG = (byte)(sumG / count);
            byte avgB = (byte)(sumB / count);

            // Smooth transitions slightly toward the new screen sample to avoid jarring flicker.
            byte r = (byte)Math.Clamp((lastRgb.R + avgR * 2) / 3, 0, 255);
            byte gCh = (byte)Math.Clamp((lastRgb.G + avgG * 2) / 3, 0, 255);
            byte b = (byte)Math.Clamp((lastRgb.B + avgB * 2) / 3, 0, 255);
            lastRgb = (r, gCh, b);
            return new LightingFrame(r, gCh, b, spec.MaxBrightness);
        }
        catch
        {
            return Fallback(spec);
        }
    }

    static LightingFrame Fallback(LightingEffectSpec spec)
    {
        var (r, g, b) = LightingEffectSpec.ParseRgb(spec.Palette[0]);
        return new LightingFrame(r, g, b, spec.MaxBrightness);
    }
}
