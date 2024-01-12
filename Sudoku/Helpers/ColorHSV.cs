using System;
using System.Windows.Media;

namespace Sudoku;

public class ColorHSV(double h, double s, double v)
{
    public double Hue
    {
        get { return h; }
        set { if (value >= 0 && value <= 1.0) h = value; }
    }

    public double Saturation
    {
        get { return s; }
        set { if (value >= 0 && value <= 1.0) s = value; }
    }

    public double Value
    {
        get { return v; }
        set { if (value >= 0 && value <= 1.0) v = value; }
    }

    public override bool Equals(object? obj)
    {
        if (obj is ColorHSV x)
        {
            return
                Hue == x.Hue &&
                Saturation == x.Saturation &&
                Value == x.Value;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(h, s, v);
    }

    public static bool operator ==(ColorHSV left, ColorHSV right)
    {
        return left.Hue == right.Hue &&
            left.Saturation == right.Saturation &&
            left.Value == right.Value;
    }

    public static bool operator !=(ColorHSV left, ColorHSV right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return $"{h * 360:f0}°, {s * 100:f0}%, {v * 100:f0}%";
    }

    public Color ToColor()
    {
        return ConvertToColor(h, s, v);
    }

    public static Color ConvertToColor(double h, double s, double v)
    {
        double r, g, b;

        r = v;   // default to gray
        g = v;
        b = v;

        int hi = (int)(h * 6.0);
        double f = (h * 6.0) - hi;
        double p = v * (1 - s);
        double q = v * (1 - f * s);
        double t = v * (1 - (1 - f) * s);

        switch (hi)
        {
            case 0:
                r = v;
                g = t;
                b = p;
                break;

            case 1:
                r = q;
                g = v;
                b = p;
                break;

            case 2:
                r = p;
                g = v;
                b = t;
                break;

            case 3:
                r = p;
                g = q;
                b = v;
                break;

            case 4:
                r = t;
                g = p;
                b = v;
                break;

            case 5:
                r = v;
                g = p;
                b = q;
                break;
        }

        Color rgb = Color.FromArgb(255,
            Convert.ToByte(r * 255.0f),
            Convert.ToByte(g * 255.0f),
            Convert.ToByte(b * 255.0f));

        return rgb;
    }

    public static ColorHSV ConvertFrom(Color c)
    {
        double r = c.R / 255.0;
        double g = c.G / 255.0;
        double b = c.B / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;

        double hue, sat, val;
        if (max == min)
            hue = 0.0;
        else if (max == r)
            hue = ((60 * ((g - b) / delta)) + 360) % 360;
        else if (max == g)
            hue = (60 * ((b - r) / delta)) + 120;
        else
            hue = (60 * ((r - g) / delta)) + 240;

        hue /= 360;

        if (max == 0.0)
            sat = 0.0;
        else
            sat = delta / max;

        val = max;

        return new ColorHSV(hue, sat, val);
    }
}
