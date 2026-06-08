using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace PDC_System.Email
{
    // ── Bool → Visibility ────────────────────────────────────────────────────
    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public static readonly BoolToVisibilityConverter Instance = new();
        public object Convert(object v, Type t, object p, CultureInfo c)
            => v is true ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object v, Type t, object p, CultureInfo c)
            => v is Visibility.Visible;
    }

    // ── Inverse Bool → Visibility (Collapsed when true) ──────────────────────
    public sealed class InverseBoolToVisibilityConverter : IValueConverter
    {
        public static readonly InverseBoolToVisibilityConverter Instance = new();
        public object Convert(object v, Type t, object p, CultureInfo c)
            => v is true ? Visibility.Collapsed : Visibility.Visible;
        public object ConvertBack(object v, Type t, object p, CultureInfo c)
            => v is Visibility.Collapsed;
    }

    // ── Inverse Bool ─────────────────────────────────────────────────────────
    public sealed class InverseBoolConverter : IValueConverter
    {
        public static readonly InverseBoolConverter Instance = new();
        public object Convert(object v, Type t, object p, CultureInfo c)
            => v is bool b && !b;
        public object ConvertBack(object v, Type t, object p, CultureInfo c)
            => v is bool b && !b;
    }

    // ── Bool → FontWeight (Bold when false = unread) ──────────────────────────
    public sealed class BoolToFontWeightConverter : IValueConverter
    {
        public static readonly BoolToFontWeightConverter Instance = new();
        // isRead=true → Normal, isRead=false → Bold
        public object Convert(object v, Type t, object p, CultureInfo c)
            => v is true ? FontWeights.Normal : FontWeights.SemiBold;
        public object ConvertBack(object v, Type t, object p, CultureInfo c)
            => throw new NotSupportedException();
    }

    // ── Drop shadow for compose card ─────────────────────────────────────────
    public static class ShadowHelper
    {
        public static readonly DropShadowEffect CardShadow = new()
        {
            BlurRadius   = 32,
            ShadowDepth  = 8,
            Direction    = 270,
            Color        = Colors.Black,
            Opacity      = 0.18
        };
    }
}
