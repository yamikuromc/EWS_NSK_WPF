using EWS_NSK_WPF.Models;
using EWS_NSK_WPF.Services;
using EWS_NSK_WPF.Services.ReportTemplates;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace EWS_NSK_WPF.Views
{
    public class HydrantReportRow
    {
        public string Number { get; set; }
        public string AddressObject { get; set; }
        public string Address { get; set; }
        public string Owner { get; set; }
        public string HydrantCount { get; set; }
        public string Diameter { get; set; }
        public string WaterVolumeCount { get; set; }
        public string WaterVolume { get; set; }
        public string FaultDate { get; set; }
        public string FixNote { get; set; }
        public bool IsGroupHeader { get; set; }
    }

    public class SummaryRow
    {
        public string Category { get; set; }
        public int Total { get; set; }
        public int Defective { get; set; }
    }

    public partial class ReportFormControl : UserControl
    {
        private List<Ewss> _hydrants;
        private string _reportType = "Defective";
        private string _currentUserName = "Пользователь";
        private double _currentFontSize = 11;

        // ComboBox'ы «Составил» создаются кодом (их нет в XAML)
        private ComboBox PositionCombo;
        private ComboBox AuthorCombo;

        private const int MaxRowsPerPage = 40;

        private static readonly Dictionary<string, string> ReportTitles = new Dictionary<string, string>
        {
            ["All"] = "Общий список пожарных гидрантов и водоемов,",
            ["Defective"] = "Список неисправных пожарных гидрантов и водоемов,",
            ["ByOwner"] = "Перечень пожарных гидрантов по балансодержателям,"
        };

        // ═══ ЧИСТЫЕ ПРЕДИКАТЫ КАТЕГОРИЙ (ничего не теряется) ═══
        private static bool IsMunicipal(Ewss h) =>
            h.OwnerName?.Contains("МУП") == true || h.OwnerName?.Contains("Администрация") == true;

        private static bool IsOwnerless(Ewss h) =>
    string.IsNullOrEmpty(h.OwnerName) ||
    (h.OwnerName?.Contains("езхоз") == true); // ловит и "Бесхозный", и "Безхозный"

        private static bool IsDepartmental(Ewss h) =>
            !IsMunicipal(h) && !IsOwnerless(h); // всё остальное непустое

        public ReportFormControl(List<Ewss> hydrants, string userName, string reportType = "Defective")
        {
            InitializeComponent();
            _hydrants = hydrants;
            _currentUserName = userName;
            _reportType = reportType;

            _ = LoadDataAsync();
        }

        // ═══ Поиск в ComboBox ═══
        private void ComboBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox searchBox)
            {
                var popup = FindParent<Popup>(searchBox);
                if (popup?.TemplatedParent is ComboBox comboBox)
                {
                    string searchText = searchBox.Text.ToLower();
                    foreach (var item in comboBox.Items)
                    {
                        if (item is ComboBoxItem comboItem)
                        {
                            string content = comboItem.Content?.ToString()?.ToLower() ?? "";
                            comboItem.Visibility = content.Contains(searchText)
                                ? Visibility.Visible
                                : Visibility.Collapsed;
                        }
                    }
                }
            }
        }
        private void Table_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        }
        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is T))
                parent = VisualTreeHelper.GetParent(parent);
            return parent as T;
        }

        // ═══ Асинхронная загрузка ═══
        private async Task LoadDataAsync()
        {
            var territories = await Task.Run(() => DatabaseHelper.GetUniqueTerritories());
            var positions = await Task.Run(() => DatabaseHelper.GetStaffPositions());
            var names = await Task.Run(() => DatabaseHelper.GetStaffNames());

            Dispatcher.Invoke(() =>
            {
                TerritoryCombo.Items.Clear();
                foreach (var t in territories)
                    TerritoryCombo.Items.Add(new ComboBoxItem { Content = t });
                if (TerritoryCombo.Items.Count > 0) TerritoryCombo.SelectedIndex = 0;

                SeasonCombo.Items.Clear();
                SeasonCombo.Items.Add(new ComboBoxItem { Content = "весенней" });
                SeasonCombo.Items.Add(new ComboBoxItem { Content = "осенней" });
                SeasonCombo.SelectedIndex = 1;

                YearCombo.Items.Clear();
                int currentYear = DateTime.Now.Year;
                for (int y = currentYear - 3; y <= currentYear + 1; y++)
                    YearCombo.Items.Add(new ComboBoxItem { Content = y.ToString() });
                YearCombo.SelectedIndex = 3;

                PositionCombo = new ComboBox
                {
                    Width = 420,
                    Height = 24,
                    Margin = new Thickness(0),   // ← убить -4 из стиля
                    Style = (Style)FindResource("DocComboBoxStyle"),
                    ItemContainerStyle = (Style)FindResource("DocComboItemStyle")
                };
                foreach (var p in positions)
                    PositionCombo.Items.Add(new ComboBoxItem { Content = p });
                if (PositionCombo.Items.Count > 0) PositionCombo.SelectedIndex = 0;

                AuthorCombo = new ComboBox
                {
                    Width = 260,
                    Height = 24,
                    Margin = new Thickness(0),   // ← убить -4 из стиля
                    Style = (Style)FindResource("DocComboBoxStyle"),
                    ItemContainerStyle = (Style)FindResource("DocComboItemStyle")
                };
                if (!string.IsNullOrEmpty(_currentUserName) && names.Contains(_currentUserName))
                {
                    AuthorCombo.Items.Add(new ComboBoxItem { Content = _currentUserName });
                    foreach (var n in names)
                        if (n != _currentUserName)
                            AuthorCombo.Items.Add(new ComboBoxItem { Content = n });
                }
                else
                {
                    if (!string.IsNullOrEmpty(_currentUserName))
                        AuthorCombo.Items.Add(new ComboBoxItem { Content = _currentUserName });
                    foreach (var n in names)
                        AuthorCombo.Items.Add(new ComboBoxItem { Content = n });
                }
                if (AuthorCombo.Items.Count > 0) AuthorCombo.SelectedIndex = 0;

                LoadHydrantsTable();
                ApplyReportTemplate();

                AuthorCombo.SelectionChanged += (s, e) =>
                {
                    string fio = (AuthorCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
                    if (string.IsNullOrEmpty(fio)) return;
                    // Подтягиваем должность этого человека из БД
                    var pos = DatabaseHelper.GetPositionByStaffName(fio);
                    if (!string.IsNullOrEmpty(pos))
                    {
                        for (int i = 0; i < PositionCombo.Items.Count; i++)
                            if ((PositionCombo.Items[i] as ComboBoxItem)?.Content?.ToString() == pos)
                            {
                                PositionCombo.SelectedIndex = i;
                                break;
                            }
                    }
                };
            });
        }

        private void LoadHydrantsTable()
        {
            if (_hydrants == null) return;

            var defective = _hydrants.Where(h =>
                (h.EwsStatusCod == 2 || h.EwsStatusCod == 3 || h.EwsStatusCod == 4) &&
                (!string.IsNullOrEmpty(h.AddressObjectName) || !string.IsNullOrEmpty(h.EwsHouseNumber))
            ).ToList();

            var rows = new List<HydrantReportRow>();
            int number = 1;

            var municipal = defective.Where(IsMunicipal).ToList();
            var departmental = defective.Where(IsDepartmental).ToList();
            var ownerless = defective.Where(IsOwnerless).ToList();

            if (municipal.Count > 0)
            {
                rows.Add(new HydrantReportRow { AddressObject = "Муниципальные", IsGroupHeader = true });
                foreach (var h in municipal)
                    rows.Add(MakeRow(h, ref number));
            }
            if (departmental.Count > 0)
            {
                rows.Add(new HydrantReportRow { AddressObject = "Ведомственные", IsGroupHeader = true });
                foreach (var h in departmental)
                    rows.Add(MakeRow(h, ref number));
            }
            if (ownerless.Count > 0)
            {
                rows.Add(new HydrantReportRow { AddressObject = "Бесхозные", IsGroupHeader = true });
                foreach (var h in ownerless)
                    rows.Add(MakeRow(h, ref number));
            }

            CheckAndCreateSecondPage(rows);
        }

        private HydrantReportRow MakeRow(Ewss h, ref int number)
        {
            return new HydrantReportRow
            {
                Number = (number++).ToString(),
                AddressObject = h.AddressObjectName ?? "",
                Address = $"{h.EwsHouseNumber} {h.EwsPriviazka}".Trim(),
                Owner = h.OwnerName ?? "",
                HydrantCount = "1",
                Diameter = h.PipeInfo ?? ""
            };
        }

        private void CheckAndCreateSecondPage(List<HydrantReportRow> allRows)
        {
            if (allRows.Count <= MaxRowsPerPage)
            {
                HydrantsTable.ItemsSource = allRows;
                BuildFooter(Page1Footer);
            }
            else
            {
                HydrantsTable.ItemsSource = allRows.Take(MaxRowsPerPage).ToList();

                var page2Border = new Border
                {
                    Background = new SolidColorBrush(Colors.White),
                    Width = 1684,
                    Height = 1191,
                    Padding = new Thickness(50, 40, 50, 40),
                    Margin = new Thickness(20),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                    BorderThickness = new Thickness(1)
                };

                var page2Stack = new StackPanel();

                var table2 = CloneTable(allRows.Skip(MaxRowsPerPage).ToList());
                page2Stack.Children.Add(table2);

                BuildFooter(page2Stack);
                page2Border.Child = page2Stack;
                PagesContainer.Children.Add(page2Border);
            }
        }

        private DataGrid CloneTable(List<HydrantReportRow> rows)
        {
            var dg = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                BorderBrush = new SolidColorBrush(Colors.Black),
                BorderThickness = new Thickness(1),
                GridLinesVisibility = DataGridGridLinesVisibility.All,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                FontSize = 9,
                Margin = new Thickness(0, 0, 0, 0),
                Background = new SolidColorBrush(Colors.White),
                Foreground = new SolidColorBrush(Colors.Black),
                ColumnHeaderStyle = (Style)FindResource("TableHeaderStyle"),
                CellStyle = (Style)FindResource("TableCellStyle"),
                CanUserAddRows = false,
                CanUserResizeRows = false,
                RowHeight = 18,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            void AddCol(string header, string path, double min) =>
                dg.Columns.Add(new DataGridTextColumn { Header = header, Width = new DataGridLength(1, DataGridLengthUnitType.Auto), MinWidth = min, Binding = new Binding(path) });

            AddCol("№", "Number", 30);
            AddCol("Наименование населенного пункта", "AddressObject", 120);
            AddCol("Адрес привязки", "Address", 140);
            AddCol("Наименование предприятия", "Owner", 150);
            AddCol("Количество ПГ", "HydrantCount", 50);
            AddCol("Диаметр водовода", "Diameter", 80);
            AddCol("Количество ПВ", "WaterVolumeCount", 50);
            AddCol("Емкость водоема", "WaterVolume", 80);
            AddCol("Дата обнаружения неисправности", "FaultDate", 100);
            AddCol("Отметка об устранении неисправности", "FixNote", 120);

            dg.ItemsSource = rows;
            dg.PreviewMouseWheel += Table_PreviewMouseWheel;
            return dg;
        }

        // ═══ «Составил» + сводная таблица — как в Word ═══
        private void BuildFooter(Panel panel)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "Составил:",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 20, 0, 6)
            });

            // Должность слева, ФИО справа + полупрозрачные прочерки под каждой надписью
            var signGrid = new Grid
            {
                Width = 1584,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 24)
            };
            signGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(760) });
            signGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Левая колонка: должность + прочерк
            var leftStack = new StackPanel();
            Grid.SetColumn(leftStack, 0);
            PositionCombo.HorizontalAlignment = HorizontalAlignment.Left;
            PositionCombo.Width = 740;
            leftStack.Children.Add(PositionCombo);
            leftStack.Children.Add(new Rectangle
            {
                Height = 1,
                Width = 740,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 3, 0, 0),
                Fill = new SolidColorBrush(Color.FromArgb(90, 0, 0, 0)) // полупрозрачный
            });
            signGrid.Children.Add(leftStack);

            // Правая колонка: ФИО + прочерк
            var rightStack = new StackPanel();
            Grid.SetColumn(rightStack, 1);
            AuthorCombo.HorizontalAlignment = HorizontalAlignment.Left;
            AuthorCombo.Width = 300;
            rightStack.Children.Add(AuthorCombo);
            rightStack.Children.Add(new Rectangle
            {
                Height = 1,
                Width = 300,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 3, 0, 0),
                Fill = new SolidColorBrush(Color.FromArgb(90, 0, 0, 0)) // полупрозрачный
            });
            signGrid.Children.Add(rightStack);

            panel.Children.Add(signGrid);

            // Сводная мини-таблица (как в образце: пустой заголовок + Всего + Неисправны)
            var summary = GetSummary();
            var sumTable = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                BorderBrush = new SolidColorBrush(Colors.Black),
                BorderThickness = new Thickness(1),
                GridLinesVisibility = DataGridGridLinesVisibility.All,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                FontSize = 10,
                Background = new SolidColorBrush(Colors.White),
                Foreground = new SolidColorBrush(Colors.Black),
                ColumnHeaderStyle = (Style)FindResource("TableHeaderStyle"),
                CellStyle = (Style)FindResource("TableCellStyle"),
                CanUserAddRows = false,
                CanUserResizeRows = false,
                RowHeight = 22,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 20)
            };
            sumTable.Columns.Add(new DataGridTextColumn { Header = "", Width = 160, Binding = new Binding("Category") });
            sumTable.Columns.Add(new DataGridTextColumn { Header = "Всего", Width = 90, Binding = new Binding("Total") });
            sumTable.Columns.Add(new DataGridTextColumn { Header = "Неисправны", Width = 110, Binding = new Binding("Defective") });
            sumTable.ItemsSource = summary;
            panel.Children.Add(sumTable);

            var button = new Button
            {
                Content = "💾 Сформировать и сохранить Word-документ",
                Width = 360,
                Height = 40,
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Foreground = new SolidColorBrush(Colors.White),
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                Cursor = Cursors.Hand
            };
            button.Click += GenerateButton_Click;
            panel.Children.Add(button);
        }

        private List<SummaryRow> GetSummary()
        {
            if (_hydrants == null) return new List<SummaryRow>();

            var all = _hydrants;
            var def = all.Where(h => h.EwsStatusCod == 2 || h.EwsStatusCod == 3 || h.EwsStatusCod == 4).ToList();

            return new List<SummaryRow>
            {
                new SummaryRow { Category = "Муниципальные", Total = all.Count(IsMunicipal), Defective = def.Count(IsMunicipal) },
                new SummaryRow { Category = "Ведомственные", Total = all.Count(IsDepartmental), Defective = def.Count(IsDepartmental) },
                new SummaryRow { Category = "Бесхозные", Total = all.Count(IsOwnerless), Defective = def.Count(IsOwnerless) },
            };
        }

        private void ApplyReportTemplate()
        {
            if (ReportTitles.ContainsKey(_reportType))
                ReportTitle.Text = ReportTitles[_reportType];
        }

        public void SetReportType(string type)
        {
            _reportType = type;
            ApplyReportTemplate();
        }

        public void SetFontSize(double size)
        {
            _currentFontSize = size;
            ApplyFontSizeToElement(PagesContainer, size);
        }

        private void ApplyFontSizeToElement(DependencyObject parent, double size)
        {
            if (parent is TextBlock tb) tb.FontSize = size;
            else if (parent is Button btn) btn.FontSize = size;
            else if (parent is DataGrid dg)
            {
                dg.FontSize = Math.Max(7, size - 2);
                dg.RowHeight = Math.Max(14, 18 * (size / 11));
            }
            else if (parent is ComboBox cb) cb.FontSize = size;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
                ApplyFontSizeToElement(VisualTreeHelper.GetChild(parent, i), size);
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Документ Word|*.docx",
                FileName = $"Отчет_{_reportType}_{DateTime.Now:yyyyMMdd}.docx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var parameters = new Dictionary<string, object>
                    {
                        ["territory"] = (TerritoryCombo.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                        ["season"] = (SeasonCombo.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                        ["year"] = (YearCombo.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                        ["position"] = (PositionCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                        ["author"] = (AuthorCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString()
                    };

                    ReportGenerator.GenerateReport(_reportType, _hydrants, dialog.FileName, parameters);

                    MessageBox.Show($"Отчёт успешно сформирован:\n{dialog.FileName}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}