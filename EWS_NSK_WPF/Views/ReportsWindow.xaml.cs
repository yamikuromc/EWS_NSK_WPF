using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EWS_NSK_WPF.Models;
using EWS_NSK_WPF.Services; // Если нужен DatabaseHelper

namespace EWS_NSK_WPF.Views
{
    public partial class ReportsWindow : Window
    {
        private List<Ewss> _allHydrants;
        private List<Ewss> _filteredHydrants;
        private string _currentReportType = "Defective"; // По умолчанию неисправные
        private string _currentUserName = "Пользователь";
        private bool _canEdit = false;

        public ReportsWindow(string userName, string position, string accessLevel, WindowMode requestedMode)
        {
            InitializeComponent();

            _currentUserName = userName;
            // Теперь проверяем только роль, режим окна (View/Edit) не важен для отчетов
            _canEdit = (accessLevel != "Operator");

            UserInfoLabel.Text = $"Пользователь: {userName} ({position})";
            PermissionsLabel.Text = _canEdit ? "Режим: Формирование и экспорт отчетов" : "Режим: Только просмотр";

            this.Loaded += (s, e) =>
            {
                ThemeManager.ApplyThemeToReportsWindow(this);
                LoadData();
            };
        }

        private void LoadData()
        {
            // Загружаем все гидранты (используй свой метод из DatabaseHelper)
            _allHydrants = DatabaseHelper.GetAllHydrantsForReport();
            ApplyFilter();
        }

        private void ReportType_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                _currentReportType = tag;
                ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            if (_allHydrants == null) return;

            // Фильтруем данные в зависимости от выбранного типа
            _filteredHydrants = _currentReportType switch
            {
                "Defective" => _allHydrants.Where(h => h.EwsStatusCod == 2 || h.EwsStatusCod == 3 || h.EwsStatusCod == 4).ToList(),
                "ByOwner" => _allHydrants.Where(h => !string.IsNullOrEmpty(h.OwnerName)).OrderBy(h => h.OwnerName).ToList(),
                _ => _allHydrants // "All"
            };

            // ВМЕСТО DATAGRID: Создаем наш красивый лист А4 и кладем его в контейнер
            string defaultPosition = "Начальник караула смены №2 37 ПСЧ 5 ПСО ФПС ГПС ГУ по Новосибирской области";

            ReportFormContainer.Content = new ReportFormControl(_filteredHydrants, _currentUserName, defaultPosition);
        }

        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Этот слайдер теперь декоративный или может масштабировать весь ContentControl, 
            // но основная генерация идет через кнопку внутри листа А4.
        }
    }
}