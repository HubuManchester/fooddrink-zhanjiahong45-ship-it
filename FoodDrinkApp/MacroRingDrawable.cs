using Microsoft.Maui.Graphics;

namespace FoodDrinkApp;

public sealed class MacroRingDrawable : IDrawable
{
    private const float StartAngle = -90f;

    public float Protein { get; private set; }

    public float Carbs { get; private set; }

    public float Fat { get; private set; }

    public float Progress { get; set; } = 1f;

    public void SetMacros(float protein, float carbs, float fat)
    {
        Protein = Math.Max(0, protein);
        Carbs = Math.Max(0, carbs);
        Fat = Math.Max(0, fat);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var size = Math.Min(dirtyRect.Width, dirtyRect.Height);
        var inset = size * 0.12f;
        var centerX = dirtyRect.X + (dirtyRect.Width / 2f);
        var centerY = dirtyRect.Y + (dirtyRect.Height / 2f);
        var rect = new RectF(
            centerX - (size / 2f) + inset,
            centerY - (size / 2f) + inset,
            size - (inset * 2f),
            size - (inset * 2f));
        var stroke = Math.Max(10f, size * 0.075f);

        canvas.SaveState();
        canvas.StrokeSize = stroke;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeColor = Color.FromArgb("#2E2A36");
        canvas.DrawArc(rect.X, rect.Y, rect.Width, rect.Height, 0, 360, false, false);

        var total = Protein + Carbs + Fat;
        if (total <= 0)
        {
            canvas.RestoreState();
            return;
        }

        var current = StartAngle;
        DrawSegment(canvas, rect, Color.FromArgb("#FF6A3D"), Protein / total, ref current);
        DrawSegment(canvas, rect, Color.FromArgb("#FFB23D"), Carbs / total, ref current);
        DrawSegment(canvas, rect, Color.FromArgb("#3DE0C0"), Fat / total, ref current);

        canvas.StrokeSize = 1.5f;
        canvas.StrokeColor = Color.FromArgb("#40FFFFFF");
        for (var i = 0; i < 24; i++)
        {
            var angle = (float)(Math.PI * 2 * i / 24);
            var outer = size * 0.49f;
            var inner = size * 0.46f;
            canvas.DrawLine(
                centerX + (float)Math.Cos(angle) * inner,
                centerY + (float)Math.Sin(angle) * inner,
                centerX + (float)Math.Cos(angle) * outer,
                centerY + (float)Math.Sin(angle) * outer);
        }

        canvas.RestoreState();
    }

    private void DrawSegment(ICanvas canvas, RectF rect, Color color, float ratio, ref float currentAngle)
    {
        var sweep = ratio * 360f * Math.Clamp(Progress, 0f, 1f);
        if (sweep <= 0.1f)
        {
            return;
        }

        canvas.StrokeColor = color;
        canvas.DrawArc(rect.X, rect.Y, rect.Width, rect.Height, currentAngle, currentAngle + sweep - 2f, false, false);
        currentAngle += ratio * 360f;
    }
}
