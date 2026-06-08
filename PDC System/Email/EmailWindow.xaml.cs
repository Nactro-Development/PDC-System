using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using PDC_System.Models;
using PDC_System.Services;

namespace PDC_System.Email
{
    public partial class EmailWindow : UserControl
    {
        private EmailViewModel? _vm;

        public EmailWindow()
        {
            InitializeComponent();
            Loaded += EmailWindow_Loaded;
        


        }


        private void EmailWindow_Loaded(object sender, RoutedEventArgs e)
        {
            FontFamilyCombo.SelectionChanged += FontFamilyCombo_SelectionChanged;
            FontSizeCombo.SelectionChanged += FontSizeCombo_SelectionChanged;
        }


        private void FontFamilyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            if (BodyEditor == null)
                return;

            if (FontFamilyCombo.SelectedItem is ComboBoxItem item)
            {
                BodyEditor.Selection.ApplyPropertyValue(
                    TextElement.FontFamilyProperty,
                    new FontFamily(item.Content?.ToString() ?? "Segoe UI"));

                BodyEditor.Focus();
            }
        }

        // Call this from the parent window / shell to inject dependencies
        public void Initialize(EmailDatabase db, EmailService svc)
        {
            _vm = new EmailViewModel(db, svc);
            DataContext = _vm;

            // Wire up RichTextBox → ViewModel HTML sync on text change
            BodyEditor.TextChanged += (_, _) => SyncBodyToViewModel();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _vm?.StopAutoRefresh();
        }

        // ── RTE: sync FlowDocument → HTML string in ViewModel ─────────────
        private void SyncBodyToViewModel()
        {
            if (_vm == null) return;
            _vm.ComposeBodyHtml = FlowDocumentToHtml(BodyEditor.Document);
        }

        private static string FlowDocumentToHtml(FlowDocument doc)
        {
            var sb = new StringBuilder();
            foreach (var block in doc.Blocks)
            {
                sb.Append(BlockToHtml(block));
            }
            return sb.ToString();
        }

        private static string BlockToHtml(Block block)
        {
            if (block is Paragraph para)
            {
                string align = para.TextAlignment switch
                {
                    TextAlignment.Center  => "center",
                    TextAlignment.Right   => "right",
                    TextAlignment.Justify => "justify",
                    _                     => "left"
                };
                var sb = new StringBuilder($"<p style=\"text-align:{align}\">");
                foreach (var inline in para.Inlines)
                    sb.Append(InlineToHtml(inline));
                sb.Append("</p>");
                return sb.ToString();
            }
            if (block is List list)
            {
                bool ordered = list.MarkerStyle == TextMarkerStyle.Decimal;
                string tag = ordered ? "ol" : "ul";
                var sb = new StringBuilder($"<{tag}>");
                foreach (var item in list.ListItems)
                {
                    sb.Append("<li>");
                    foreach (var b in item.Blocks)
                        sb.Append(BlockToHtml(b));
                    sb.Append("</li>");
                }
                sb.Append($"</{tag}>");
                return sb.ToString();
            }
            return "";
        }

    
        private static string InlineToHtml(Inline inline)
        {
            if (inline is Run run)
            {
                string text = System.Net.WebUtility.HtmlEncode(run.Text);
                var style = new StringBuilder();

                if (run.FontWeight == FontWeights.Bold)   text = $"<strong>{text}</strong>";
                if (run.FontStyle  == FontStyles.Italic)  text = $"<em>{text}</em>";
                if (run.TextDecorations.Contains(TextDecorations.Underline[0]))
                    text = $"<u>{text}</u>";

                if (run.FontSize > 0)
                    style.Append($"font-size:{run.FontSize}px;");
                if (run.FontFamily != null)
                    style.Append($"font-family:{run.FontFamily.Source};");
                if (run.Foreground is SolidColorBrush scb && scb.Color != Colors.Black)
                    style.Append($"color:{ColorToHex(scb.Color)};");

                if (style.Length > 0)
                    text = $"<span style=\"{style}\">{text}</span>";

                return text;
            }
            if (inline is Hyperlink hl)
            {
                string href = hl.NavigateUri?.ToString() ?? "#";
                var sb = new StringBuilder($"<a href=\"{href}\">");
                foreach (var i in hl.Inlines)
                    sb.Append(InlineToHtml(i));
                sb.Append("</a>");
                return sb.ToString();
            }
            if (inline is Span span)
            {
                var sb = new StringBuilder();
                foreach (var i in span.Inlines)
                    sb.Append(InlineToHtml(i));
                return sb.ToString();
            }
            return "";
        }

        private static string ColorToHex(Color c)
            => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        // ── RTE Toolbar Handlers ───────────────────────────────────────────

        private void BoldBtn_Click(object sender, RoutedEventArgs e)
            => EditingCommands.ToggleBold.Execute(null, BodyEditor);

        private void ItalicBtn_Click(object sender, RoutedEventArgs e)
            => EditingCommands.ToggleItalic.Execute(null, BodyEditor);

        private void UnderlineBtn_Click(object sender, RoutedEventArgs e)
            => EditingCommands.ToggleUnderline.Execute(null, BodyEditor);

        private void AlignLeftBtn_Click(object sender, RoutedEventArgs e)
            => EditingCommands.AlignLeft.Execute(null, BodyEditor);

        private void AlignCenterBtn_Click(object sender, RoutedEventArgs e)
            => EditingCommands.AlignCenter.Execute(null, BodyEditor);

        private void AlignRightBtn_Click(object sender, RoutedEventArgs e)
            => EditingCommands.AlignRight.Execute(null, BodyEditor);

        private void BulletListBtn_Click(object sender, RoutedEventArgs e)
            => EditingCommands.ToggleBullets.Execute(null, BodyEditor);

        private void NumberedListBtn_Click(object sender, RoutedEventArgs e)
            => EditingCommands.ToggleNumbering.Execute(null, BodyEditor);

        private void FontSizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            if (BodyEditor == null)
                return;

            if (FontSizeCombo?.SelectedItem is not ComboBoxItem item)
                return;

            if (!double.TryParse(item.Content?.ToString(), out double sz))
                return;

            BodyEditor.Selection.ApplyPropertyValue(
                TextElement.FontSizeProperty,
                sz);

            BodyEditor.Focus();
        }


    

        private void TextColorBtn_Click(object sender, RoutedEventArgs e)
        {
            // Use a simple color-picker dialog (System.Windows.Forms.ColorDialog)
            var dlg = new System.Windows.Forms.ColorDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var c = dlg.Color;
                var brush = new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B));
                BodyEditor.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
                BodyEditor.Focus();
            }
        }

        private void HyperlinkBtn_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new HyperlinkDialog { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true) return;

            string url  = dlg.Url;
            string text = string.IsNullOrWhiteSpace(dlg.DisplayText)
                          ? url
                          : dlg.DisplayText;

            if (!BodyEditor.Selection.IsEmpty)
                BodyEditor.Selection.Text = "";

            var hl = new Hyperlink(new Run(text))
            {
                NavigateUri = new Uri(url.StartsWith("http") ? url : "https://" + url)
            };
            BodyEditor.CaretPosition.Paragraph?.Inlines.Add(hl);
            BodyEditor.Focus();
        }

        // Keep toolbar toggle buttons in sync with caret
        private void BodyEditor_SelectionChanged(object sender, RoutedEventArgs e)
        {
            object bold = BodyEditor.Selection.GetPropertyValue(TextElement.FontWeightProperty);
            BoldBtn.IsChecked = bold != DependencyProperty.UnsetValue
                                && (FontWeight)bold == FontWeights.Bold;

            object italic = BodyEditor.Selection.GetPropertyValue(TextElement.FontStyleProperty);
            ItalicBtn.IsChecked = italic != DependencyProperty.UnsetValue
                                  && (FontStyle)italic == FontStyles.Italic;

            object dec = BodyEditor.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
            UnderlineBtn.IsChecked = dec != DependencyProperty.UnsetValue
                                     && dec is TextDecorationCollection tdc
                                     && tdc.Count > 0;
        }
    }
}
