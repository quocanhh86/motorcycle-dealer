namespace MotorcycleDealer.Mobile.App.Controls;

public partial class QrCodeDisplayView : ContentView
{
    private readonly QrMatrixDrawable _drawable = new();

    public static readonly BindableProperty PayloadProperty = BindableProperty.Create(
        nameof(Payload),
        typeof(string),
        typeof(QrCodeDisplayView),
        defaultValue: string.Empty,
        propertyChanged: OnPayloadChanged);

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title),
        typeof(string),
        typeof(QrCodeDisplayView),
        defaultValue: string.Empty,
        propertyChanged: (b, _, _) => ((QrCodeDisplayView)b).OnPropertyChanged(nameof(HasTitle)));

    public static readonly BindableProperty CaptionProperty = BindableProperty.Create(
        nameof(Caption),
        typeof(string),
        typeof(QrCodeDisplayView),
        defaultValue: string.Empty,
        propertyChanged: (b, _, _) => ((QrCodeDisplayView)b).OnPropertyChanged(nameof(HasCaption)));

    public static readonly BindableProperty QrSizeProperty = BindableProperty.Create(
        nameof(QrSize),
        typeof(double),
        typeof(QrCodeDisplayView),
        defaultValue: 184d);

    public string Payload
    {
        get => (string)GetValue(PayloadProperty);
        set => SetValue(PayloadProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Caption
    {
        get => (string)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public double QrSize
    {
        get => (double)GetValue(QrSizeProperty);
        set => SetValue(QrSizeProperty, value);
    }

    public bool HasTitle => !string.IsNullOrWhiteSpace(Title);
    public bool HasCaption => !string.IsNullOrWhiteSpace(Caption);

    public QrCodeDisplayView()
    {
        InitializeComponent();
        QrGraphics.Drawable = _drawable;
        _drawable.UpdatePayload(Payload);
    }

    private static void OnPayloadChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is QrCodeDisplayView view)
        {
            view._drawable.UpdatePayload(newValue as string ?? string.Empty);
            view.QrGraphics.Invalidate();
        }
    }

    private sealed class QrMatrixDrawable : IDrawable
    {
        private bool[,] _matrix = QrCodeMatrixEncoder.Encode("APEX-MOTO");
        private static readonly Color ModuleDark = Color.FromArgb("#020617");

        public void UpdatePayload(string payload)
        {
            _matrix = QrCodeMatrixEncoder.Encode(payload);
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.SaveState();
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(dirtyRect);

            int count = _matrix.GetLength(0);
            const int quietZone = 2;
            int totalModules = count + (quietZone * 2);
            float side = Math.Min(dirtyRect.Width, dirtyRect.Height);
            float cell = side / totalModules;
            float offsetX = dirtyRect.X + ((dirtyRect.Width - side) / 2f);
            float offsetY = dirtyRect.Y + ((dirtyRect.Height - side) / 2f);

            canvas.FillColor = ModuleDark;
            for (int y = 0; y < count; y++)
            {
                for (int x = 0; x < count; x++)
                {
                    if (_matrix[y, x])
                    {
                        float rx = offsetX + ((x + quietZone) * cell);
                        float ry = offsetY + ((y + quietZone) * cell);
                        // Add 0.25f bleed to prevent subpixel hairlines on high-DPI Android panels
                        canvas.FillRectangle(rx, ry, cell + 0.25f, cell + 0.25f);
                    }
                }
            }

            canvas.RestoreState();
        }
    }
}
