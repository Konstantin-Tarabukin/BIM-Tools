using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitCopyParams.Models;
using RevitCopyParams.Services;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using System.Linq;



namespace RevitCopyParams.UI
{
    public partial class SheetNumberWindow : Window
    {
        private class UnicodeSymbol
        {
            public string Name { get; set; }
            public string Code { get; set; }
            public string Symbol { get; set; }
            public string Icon { get; set; }

            public override string ToString()
            {
                return $"{Icon} {Name} ({Code})";
            }
        
        }
        private System.Windows.Controls.TextBox activeTextBox;
        private readonly UIApplication _uiapp;
        private void btnPrefixUnicode_Click(object sender, RoutedEventArgs e)
        {
            activeTextBox = tbPrefix;

            ShowUnicodeMenu(btnPrefixUnicode);
        }
        private void ShowUnicodeMenu(System.Windows.Controls.Button button)
        {
            ContextMenu menu = new ContextMenu();

            foreach (UnicodeSymbol unicode in GetUnicodeSymbols())
            {
                MenuItem item = new MenuItem();

                item.Header = unicode.Name + " (" + unicode.Code + ")";
                item.Icon = new TextBlock
                {
                    Text = unicode.Icon,
                    FontSize = 16
                };
                item.Tag = unicode.Icon;

                item.Click += UnicodeItem_Click;

                menu.Items.Add(item);
            }

            button.ContextMenu = menu;

            menu.IsOpen = true;
        }

        private void UnicodeItem_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (activeTextBox == null)
                return;


            MenuItem item = sender as MenuItem;

            if (item == null)
                return;


            string symbol = item.Tag.ToString();


            int position = activeTextBox.SelectionStart;


            activeTextBox.Text =
                activeTextBox.Text.Insert(
                    position,
                    symbol);


            activeTextBox.SelectionStart =
                position + symbol.Length;


            UpdatePreview();
        }


        private void btnSuffixUnicode_Click(object sender, RoutedEventArgs e)
        {
            activeTextBox = tbSuffix;

            ShowUnicodeMenu(btnSuffixUnicode);
        }

        public SheetNumberWindow(UIApplication uiapp)
        {
            InitializeComponent();

            _uiapp = uiapp;


            SheetNumberService service =
                new SheetNumberService(
                    uiapp.ActiveUIDocument.Document);


            treeSheets.ItemsSource =
                service.GetSheetsTree();
            allSheets = service.GetAllSheets();
        }
        private List<ViewSheet> selectedSheets = new List<ViewSheet>();
        private List<ViewSheet> allSheets = new List<ViewSheet>();
        private void treeSheets_SelectedItemChanged(object sender,
            RoutedPropertyChangedEventArgs<object> e)
        {
            selectedSheets.Clear();

            if (treeSheets.SelectedItem is SheetNode node)
            {
                if (node.Sheet != null)
                {
                    // выбран конкретный лист
                    selectedSheets.Add(node.Sheet);
                }
                else
                {
                    // выбрана группа раздела
                    foreach (SheetNode child in node.Children)
                    {
                        if (child.Sheet != null)
                        {
                            selectedSheets.Add(child.Sheet);
                        }
                    }
                }

                UpdatePreview();
            }
        }
        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            if (selectedSheets.Count == 0)
            {
                MessageBox.Show("Выберите листы");
                return;
            }


            string prefix = DecodeIcons(tbPrefix.Text);
            string suffix = DecodeIcons(tbSuffix.Text);


            using (Transaction transaction =
                new Transaction(_uiapp.ActiveUIDocument.Document,
                "Нумерация листов"))
            {
                transaction.Start();


                foreach (ViewSheet sheet in selectedSheets)
                {
                    sheet.SheetNumber =
                        prefix +
                        sheet.SheetNumber +
                        suffix;
                }


                transaction.Commit();
            }


            MessageBox.Show("Номера листов изменены");

            UpdatePreview();
        }
        private void UpdatePreview()
        {
            lbPreview.Items.Clear();

            string prefix = DecodeIcons(tbPrefix.Text);
            string suffix = DecodeIcons(tbSuffix.Text);


            foreach (ViewSheet sheet in selectedSheets)
            {
                string newNumber =
                    prefix +
                    sheet.SheetNumber +
                    suffix;


                string preview =
                    DisplayUnicode(newNumber);


                lbPreview.Items.Add(
                    $"{sheet.SheetNumber} | {preview}");
            }
        }
        private void cbShowUnicode_Changed(
    object sender,
    RoutedEventArgs e)
        {
            UpdatePreview();
        }
        private string DisplayUnicode(string text)
        {
            if (!cbShowUnicode.IsChecked.GetValueOrDefault())
                return text;


            StringBuilder result = new StringBuilder();


            foreach (char c in text)
            {
                switch ((int)c)
                {
                    case 0x200E:
                        result.Append("←");
                        break;

                    case 0x200F:
                        result.Append("→");
                        break;

                    case 0x200B:
                        result.Append("·");
                        break;

                    case 0x200C:
                        result.Append("⟂");
                        break;

                    case 0x200D:
                        result.Append("↔");
                        break;

                    case 0x2060:
                        result.Append("•");
                        break;


                    case 0x202A:
                        result.Append("↢");
                        break;

                    case 0x202B:
                        result.Append("↣");
                        break;

                    case 0x202C:
                        result.Append("◈");
                        break;

                    case 0x202D:
                        result.Append("⇐");
                        break;

                    case 0x202E:
                        result.Append("⇒");
                        break;


                    case 0x2066:
                        result.Append("⟵");
                        break;

                    case 0x2067:
                        result.Append("⟶");
                        break;

                    case 0x2068:
                        result.Append("◌");
                        break;

                    case 0x2069:
                        result.Append("×");
                        break;


                    case 0xFEFF:
                        result.Append("¤");
                        break;


                    default:
                        result.Append(c);
                        break;
                }
            }

            return result.ToString();

        }
        private string EncodeIcons(string text)
        {
            return text
                .Replace("\u200E", "←")
                .Replace("\u200F", "→")

                .Replace("\u202A", "↢")
                .Replace("\u202B", "↣")
                .Replace("\u202C", "◈")
                .Replace("\u202D", "⇐")
                .Replace("\u202E", "⇒")

                .Replace("\u2066", "⟵")
                .Replace("\u2067", "⟶")
                .Replace("\u2068", "◌")
                .Replace("\u2069", "×")

                .Replace("\u200B", "·")
                .Replace("\u200C", "⟂")
                .Replace("\u200D", "↔")
                .Replace("\u2060", "•")

                .Replace("\uFEFF", "¤");
        }
        private string DecodeIcons(string text)
        {
            return text
                .Replace("←", "\u200E")
                .Replace("→", "\u200F")

                .Replace("↢", "\u202A")
                .Replace("↣", "\u202B")
                .Replace("◈", "\u202C")
                .Replace("⇐", "\u202D")
                .Replace("⇒", "\u202E")

                .Replace("⟵", "\u2066")
                .Replace("⟶", "\u2067")
                .Replace("◌", "\u2068")
                .Replace("×", "\u2069")

                .Replace("·", "\u200B")
                .Replace("⟂", "\u200C")
                .Replace("↔", "\u200D")
                .Replace("•", "\u2060")

                .Replace("¤", "\uFEFF");
        }
        private List<UnicodeSymbol> GetUnicodeSymbols()
        {
            return new List<UnicodeSymbol>
    {
        new UnicodeSymbol
        {
            Name="LRM — слева направо",
            Code="U+200E",
            Symbol="\u200E",
            Icon="←"
        },

        new UnicodeSymbol
        {
            Name="RLM — справа налево",
            Code="U+200F",
            Symbol="\u200F",
            Icon="→"
        },

        new UnicodeSymbol
        {
            Name="LRE — начало текста слева направо",
            Code="U+202A",
            Symbol="\u202A",
            Icon="↢"
        },

        new UnicodeSymbol
        {
            Name="RLE — начало текста справа налево",
            Code="U+202B",
            Symbol="\u202B",
            Icon="↣"
        },

        new UnicodeSymbol
        {
            Name="PDF — завершение направления",
            Code="U+202C",
            Symbol="\u202C",
            Icon="◈"
        },

        new UnicodeSymbol
        {
            Name="LRO — принудительно слева направо",
            Code="U+202D",
            Symbol="\u202D",
            Icon="⇐"
        },

        new UnicodeSymbol
        {
            Name="RLO — принудительно справа налево",
            Code="U+202E",
            Symbol="\u202E",
            Icon="⇒"
        },

        new UnicodeSymbol
        {
            Name="ZWSP — пробел нулевой ширины",
            Code="U+200B",
            Symbol="\u200B",
            Icon="·"
        },

        new UnicodeSymbol
        {
            Name="ZWNJ — разделитель соединения",
            Code="U+200C",
            Symbol="\u200C",
            Icon="⟂"
        },

        new UnicodeSymbol
        {
            Name="ZWJ — соединитель нулевой ширины",
            Code="U+200D",
            Symbol="\u200D",
            Icon="↔"
        },

        new UnicodeSymbol
        {
            Name="WJ — соединитель слов",
            Code="U+2060",
            Symbol="\u2060",
            Icon="•"
        },

        new UnicodeSymbol
        {
            Name="BOM — маркер порядка байтов",
            Code="U+FEFF",
            Symbol="\uFEFF",
            Icon="¤"
        }
    };
        }



        private void PrefixSuffix_TextChanged(
    object sender,
    System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdatePreview();
        }

    }

}