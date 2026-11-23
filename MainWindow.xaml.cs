using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PVZ_Simulator
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void UpdateMenuHighlight(Button activeButton)
        {
            var defaultBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1565C0"));
            var defaultFg = Brushes.White;

            BtnMenuIntake.Background = defaultBg;
            BtnMenuIntake.Foreground = defaultFg;
            BtnMenuMap.Background = defaultBg;
            BtnMenuMap.Foreground = defaultFg;
            BtnMenuIssue.Background = defaultBg;
            BtnMenuIssue.Foreground = defaultFg;
            BtnMenuLog.Background = defaultBg;
            BtnMenuLog.Foreground = defaultFg;
            BtnMenuManual.Background = defaultBg;
            BtnMenuManual.Foreground = defaultFg;

            activeButton.Background = Brushes.White;
            activeButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2"));
        }

        private void BtnSimulate_Click(object sender, RoutedEventArgs e)
        {
            var vm = (MainViewModel)DataContext;
            vm.SimulateArrival();
        }

        private void BtnWriteOff_Click(object sender, RoutedEventArgs e)
        {
            var vm = (MainViewModel)DataContext;
            vm.WriteOffOverdue();
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "CSV файл (*.csv)|*.csv";
            saveFileDialog.FileName = $"Отчет_ПВЗ_{System.DateTime.Now:dd_MM_yyyy}.csv";

            if (saveFileDialog.ShowDialog() == true)
            {
                var vm = (MainViewModel)DataContext;
                vm.ExportToCsv(saveFileDialog.FileName);
            }
        }

        private void BtnAddOrder_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text) || string.IsNullOrWhiteSpace(TxtPhone.Text))
            {
                MessageBox.Show("Заполните ФИО и Телефон!", "Ошибка");
                return;
            }

            string formattedPhone = ValidationHelper.FormatPhone(TxtPhone.Text);

            if (formattedPhone == "Ошибка формата")
            {
                MessageBox.Show("Неверный формат телефона.\nВведите 12 цифр (например, 375298237369)", "Ошибка валидации");
                return;
            }

            string size = (CmbSize.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "M";
            bool isFragile = ChkFragile.IsChecked == true;
            var vm = (MainViewModel)DataContext;

            if (vm.AddOrder(TxtName.Text, formattedPhone, size, isFragile))
            {
                TxtName.Clear();
                TxtPhone.Clear();
                CmbSize.SelectedIndex = -1;
                ChkFragile.IsChecked = false;
            }
        }

        private void Cell_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is Cell clickedCell)
            {
                if (clickedCell.IsOccupied && clickedCell.CurrentOrder != null)
                {
                    var order = clickedCell.CurrentOrder;
                    string rules = order.GetStorageRules();
                    var days = (System.DateTime.Now - order.ArrivalTime).TotalDays;

                    MessageBox.Show(
                        $"Ячейка: {clickedCell.Name}\n\n" +
                        $"Номер заказа: {order.Id}\n" +
                        $"Клиент: {order.ClientName}\n" +
                        $"Телефон: {order.Phone}\n" +
                        $"Дней на складе: {days:F0}\n\n" +
                        $"{rules}",
                        "Информация о заказе",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Ячейка {clickedCell.Name} свободна.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void BtnIssueOrder_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtIssueId.Text)) return;
            var vm = (MainViewModel)DataContext;
            vm.IssueOrder(TxtIssueId.Text);
            TxtIssueId.Clear();
        }

        private void BtnReturnOrder_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtIssueId.Text)) return;
            var vm = (MainViewModel)DataContext;
            vm.ReturnOrder(TxtIssueId.Text);
            TxtIssueId.Clear();
        }

        private void BtnNavIntake_Click(object sender, RoutedEventArgs e)
        {
            ViewIntake.Visibility = Visibility.Visible;
            ViewMap.Visibility = Visibility.Collapsed;
            ViewIssue.Visibility = Visibility.Collapsed;
            ViewLog.Visibility = Visibility.Collapsed;
            ViewManual.Visibility = Visibility.Collapsed;
            UpdateMenuHighlight(BtnMenuIntake);
        }

        private void BtnNavMap_Click(object sender, RoutedEventArgs e)
        {
            ViewIntake.Visibility = Visibility.Collapsed;
            ViewMap.Visibility = Visibility.Visible;
            ViewIssue.Visibility = Visibility.Collapsed;
            ViewLog.Visibility = Visibility.Collapsed;
            ViewManual.Visibility = Visibility.Collapsed;
            UpdateMenuHighlight(BtnMenuMap);
        }

        private void BtnNavIssue_Click(object sender, RoutedEventArgs e)
        {
            ViewIntake.Visibility = Visibility.Collapsed;
            ViewMap.Visibility = Visibility.Collapsed;
            ViewIssue.Visibility = Visibility.Visible;
            ViewLog.Visibility = Visibility.Collapsed;
            ViewManual.Visibility = Visibility.Collapsed;
            UpdateMenuHighlight(BtnMenuIssue);
        }

        private void BtnNavLog_Click(object sender, RoutedEventArgs e)
        {
            ViewIntake.Visibility = Visibility.Collapsed;
            ViewMap.Visibility = Visibility.Collapsed;
            ViewIssue.Visibility = Visibility.Collapsed;
            ViewLog.Visibility = Visibility.Visible;
            ViewManual.Visibility = Visibility.Collapsed;
            UpdateMenuHighlight(BtnMenuLog);
        }

        private void BtnNavManual_Click(object sender, RoutedEventArgs e)
        {
            ViewIntake.Visibility = Visibility.Collapsed;
            ViewMap.Visibility = Visibility.Collapsed;
            ViewIssue.Visibility = Visibility.Collapsed;
            ViewLog.Visibility = Visibility.Collapsed;
            ViewManual.Visibility = Visibility.Visible;
            UpdateMenuHighlight(BtnMenuManual);
        }
    }
}