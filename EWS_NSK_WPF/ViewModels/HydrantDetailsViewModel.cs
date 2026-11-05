using System.Windows;
using System.Windows.Input;
using EWS_NSK_WPF.Core;
using EWS_NSK_WPF.Models;

namespace EWS_NSK_WPF.ViewModels
{
    public class HydrantDetailsViewModel : ViewModelBase
    {
        private Ewss _hydrant;
        private bool _isReadOnly;
        private bool _canSave;
        private bool _canDelete;

        public Ewss Hydrant
        {
            get => _hydrant;
            set => SetField(ref _hydrant, value);
        }

        public bool IsReadOnly
        {
            get => _isReadOnly;
            set => SetField(ref _isReadOnly, value);
        }

        public bool CanSave
        {
            get => _canSave;
            set => SetField(ref _canSave, value);
        }

        public bool CanDelete
        {
            get => _canDelete;
            set => SetField(ref _canDelete, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CloseCommand { get; }

        public HydrantDetailsViewModel(Ewss hydrant, int userRole)
        {
            Hydrant = hydrant;

            // Логика ролей:
            // 0 = Гость, 1 = Диспетчер (только просмотр)
            // 2+ = Администраторы (могут редактировать)
            IsReadOnly = userRole < 2;
            CanSave = userRole >= 2;
            CanDelete = userRole >= 2; // Главный админ или админ части

            SaveCommand = new RelayCommand(SaveHydrant);
            CloseCommand = new RelayCommand(CloseWindow);
        }

        private void SaveHydrant(object parameter)
        {
            // TODO: Здесь будет вызов DatabaseHelper.UpdateHydrant(Hydrant)
            MessageBox.Show($"Данные гидранта пг-{Hydrant.DisplayNumber} сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            // Закрываем окно после сохранения
            CloseWindow(null);
        }

        private void CloseWindow(object parameter)
        {
            if (parameter is Window window)
            {
                window.Close();
            }
        }
    }
}