using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace PlayDo.Desktop;

// A static print on the counter face. It never intercepts input or starts a timer.
public sealed class MeterFace : Control
{
    public string Detail { get; private set; } = "Plain";
    Color color;

    public MeterFace()
    {
        IsHitTestVisible = false;
        ClipToBounds = true;
    }

    public void SetDetail(string detail, Color ink)
    {
        if (Detail == detail && color == ink) return;
        Detail = detail;
        color = ink;
        InvalidateVisual();
    }

    Color Tint(byte alpha) => Color.FromArgb(alpha, color.R, color.G, color.B);

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        double w = Bounds.Width, h = Bounds.Height;
        if (w < 1 || h < 1 || Detail == "Plain") return;

        // Leave the left edge quiet for long totals. The print gains definition
        // towards the right, including on smaller gallery specimens.
        double span = Math.Min(w * .63, 410), start = w - span;
        var wash = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, .5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, .5, RelativeUnit.Relative),
            GradientStops = {
                new GradientStop(Tint(0), 0),
                new GradientStop(Tint(22), 1)
            }
        };
        context.DrawRectangle(wash, null, new Rect(start, 0, span, h));
        var pen = new Pen(new SolidColorBrush(Tint(44)), 1);

        if (Detail == "Scale")
        {
            var center = new Point(w - span * .28, h * 1.60);
            double radius = h * 1.42;
            foreach (double factor in new[] { 1, .80 })
                context.DrawEllipse(null, pen, center, radius * factor, radius * factor);
            for (int i = -80; i <= 80; i += 4)
            {
                double angle = i * Math.PI / 180;
                double inner = radius - (i % 20 == 0 ? h * .135 : h * .055);
                Point Tick(double r) => new(center.X + r * Math.Sin(angle), center.Y - r * Math.Cos(angle));
                context.DrawLine(pen, Tick(inner), Tick(radius));
            }
            return;
        }

        double step = Detail == "Dots" ? 13 : 20;
        for (double x = w - 8; x > start; x -= step)
        {
            byte alpha = (byte)(58 * (x - start) / span);
            var ink = new SolidColorBrush(Tint(alpha));
            if (Detail == "Grid") context.DrawLine(new Pen(ink, 1), new Point(x, 0), new Point(x, h));
            if (Detail == "Dots")
                for (double y = 9; y < h; y += step) context.DrawEllipse(ink, null, new Point(x, y), 1.2, 1.2);
        }
        if (Detail is "Rules" or "Grid")
        {
            var lines = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, .5, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, .5, RelativeUnit.Relative),
                GradientStops = { new GradientStop(Tint(0), 0), new GradientStop(Tint(54), 1) }
            };
            for (double y = 10; y < h; y += step)
                context.DrawRectangle(lines, null, new Rect(start, y, span, 1));
        }
    }
}

public partial class MainWindow
{
    static readonly string[] FaceDetails = { "Preset", "Plain", "Scale", "Grid", "Rules", "Dots" };

    static string PresetDetail(string theme) => theme switch
    {
        "Graphite" or "Instrument" or "Arctic" => "Scale",
        "Blueprint" or "Winter" => "Grid",
        "Terminal" or "Aurora" => "Dots",
        "Paper" or "Sunset" => "Rules",
        _ => "Plain"
    };

    void BuildFaceOptions()
    {
        var choice = Find<ComboBox>("FaceDetail");
        choice.ItemsSource = FaceDetails;
        SyncFaceOptions();
        choice.SelectionChanged += (_, _) =>
        {
            if (initializing || choice.SelectedItem is not string selected) return;
            service.Settings.FaceDetail = selected == "Preset" ? null : selected;
            ApplyTheme();
            service.Save();
        };
    }

    void SyncFaceOptions()
    {
        var saved = service.Settings.FaceDetail;
        Find<ComboBox>("FaceDetail").SelectedItem = FaceDetails.Contains(saved) ? saved : "Preset";
    }

    void ApplyFaceDetail(ThemeSpec theme)
    {
        var saved = service.Settings.FaceDetail;
        string detail = saved != null && saved != "Preset" && FaceDetails.Contains(saved) ? saved : PresetDetail(theme.Name);
        var ink = Color.Parse(theme.Signal);
        Find<MeterFace>("DashboardFace").SetDetail(detail, ink);
        Find<MeterFace>("StudioFace").SetDetail(detail, ink);
    }
}
