using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SACE_MaskGenerator;

/*
 * Progetto di appoggio per creare maschere destinate alla classe board evaluation helper
 * e avere una visione grafica
 */


/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
internal partial class MainWindow : Window
{
    private Button? _selectedCell = null;
    private double[,] _values = new double[9, 9];
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        CreateGrid();
    }

    private void CreateGrid()
    {
        grdBoardVisualizer.Children.Clear();
        pnlNumbers.Children.Clear();
        pnlLetters.Children.Clear();

        for (int i = 8; i >= 1; i--)
        {
            pnlNumbers.Children.Add(CreateLabel(i.ToString()));
        }

        char[] letters = "abcdefgh".ToCharArray();
        foreach (char l in letters)
        {
            pnlLetters.Children.Add(CreateLabel(l.ToString()));
        }

        for (int r = 0; r < 8; r++) 
        {
            for (int c = 0; c < 8; c++)
            {
                Brush baseColor = (r + c) % 2 == 0 ? Brushes.LightYellow : Brushes.RosyBrown;

                Button btn = new Button
                {
                    Tag = new Point(r, c),
                    Background = baseColor,
                    BorderThickness = new Thickness(0),
                    Content = "" 
                };

                btn.Click += Cell_Click;
                grdBoardVisualizer.Children.Add(btn);
            }
        }
    }

    // Helper per creare le etichette delle coordinate
    private TextBlock CreateLabel(string text)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 14
        };
    }

    private void Cell_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCell != null)
        {
            Point oldCoord = (Point)_selectedCell.Tag;
            _selectedCell.BorderThickness = new Thickness(0);
        }

        _selectedCell = (Button)sender;
        Point coord = (Point)_selectedCell.Tag;

        char colLetter = (char)('a' + (int)coord.Y);
        int rowNum = 8 - (int)coord.X;
        lblSelectedCoord.Text = $"Casa: {colLetter}{rowNum}";

        _selectedCell.BorderBrush = Brushes.Gold;
        _selectedCell.BorderThickness = new Thickness(3);

        slSquareValue.Value = _values[(int)coord.X, (int)coord.Y];
    }

    private void slSquareValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_selectedCell == null) return;

        Point coord = (Point)_selectedCell.Tag;
        double val = e.NewValue;

        _values[(int)coord.X, (int)coord.Y] = val;

        _selectedCell.Background = GetColorFromValue(val);
        _selectedCell.Content = _values[(int)coord.X, (int)coord.Y].ToString("F2");

        UpdateMaskText();
    }
    private Brush GetColorFromValue(double value)
    {
        value = Math.Max(-1, Math.Min(1, value));

        byte intensity = (byte)(Math.Abs(value) * 255);
        byte secondaryChannels = (byte)(255 - intensity);

        if (value < 0)
        {
            // ROSSO
            return new SolidColorBrush(Color.FromRgb(255, secondaryChannels, secondaryChannels));
        }
        else
        {
            // VERDE
            return new SolidColorBrush(Color.FromRgb(secondaryChannels, 255, secondaryChannels));
        }
    }

    private void UpdateMaskText()
    {
        string mask = "";
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                mask += _values[r, c].ToString("F2") + "\t";
            }
            mask += "\n";
        }
        txtGeneratedMask.Text = mask;
    }

    private void btnCopy_Click(object sender, RoutedEventArgs e)
        => Clipboard.SetText(txtGeneratedMask.Text);
}
