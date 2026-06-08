using System.Windows;
using System.Windows.Controls;

namespace PDC_System.Email
{
    /// <summary>
    /// Lightweight dialog to insert a hyperlink in the compose editor.
    /// Add <Window> XAML for this or use the code-only approach below.
    /// </summary>
    public class HyperlinkDialog : Window
    {
        private readonly TextBox _urlBox;
        private readonly TextBox _textBox;

        public string Url         => _urlBox.Text.Trim();
        public string DisplayText => _textBox.Text.Trim();

        public HyperlinkDialog()
        {
            Title           = "Insert Hyperlink";
            Width           = 420;
            Height          = 200;
            ResizeMode      = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background      = System.Windows.Media.Brushes.White;

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            AddLabel(grid, "URL", 0);
            _urlBox = AddTextBox(grid, 0);

            AddLabel(grid, "Display text", 1);
            _textBox = AddTextBox(grid, 1);

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };

            var ok = new Button
            {
                Content = "Insert",
                Width = 80, Height = 32,
                Margin = new Thickness(0, 0, 8, 0),
                Background = System.Windows.Media.Brushes.DodgerBlue,
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0)
            };
            ok.Click += (_, _) => { DialogResult = true; };

            var cancel = new Button
            {
                Content = "Cancel",
                Width = 80, Height = 32,
                BorderThickness = new Thickness(1)
            };
            cancel.Click += (_, _) => { DialogResult = false; };

            btnPanel.Children.Add(ok);
            btnPanel.Children.Add(cancel);

            Grid.SetRow(btnPanel, 3);
            Grid.SetColumnSpan(btnPanel, 2);
            grid.Children.Add(btnPanel);

            Content = grid;
        }

        private static void AddLabel(Grid g, string text, int row)
        {
            var tb = new TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, row == 0 ? 8 : 0),
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 13
            };
            Grid.SetRow(tb, row);
            Grid.SetColumn(tb, 0);
            g.Children.Add(tb);
        }

        private static TextBox AddTextBox(Grid g, int row)
        {
            var tb = new TextBox
            {
                Margin = new Thickness(0, 0, 0, row == 0 ? 8 : 0),
                Padding = new Thickness(8, 5, 8, 5),
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 13
            };
            Grid.SetRow(tb, row);
            Grid.SetColumn(tb, 1);
            g.Children.Add(tb);
            return tb;
        }
    }
}
