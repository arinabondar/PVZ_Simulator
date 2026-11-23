using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;

namespace PVZ_Simulator
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private const string DataFile = "data.json";
        private const string LogFile = "history.log";

        public ObservableCollection<BaseDelivery> Orders { get; set; }
        public ObservableCollection<Cell> Cells { get; set; }
        public ObservableCollection<string> Logs { get; set; }

        // --- ФИЛЬТРАЦИЯ ---
        public string[] FilterOptions { get; } = { "Все заказы", "Ожидают выдачи", "Выданные", "Возвраты" };

        private string _selectedFilter = "Все заказы";
        public string SelectedFilter
        {
            get => _selectedFilter;
            set { _selectedFilter = value; OnPropertyChanged(); FilterOrders(); }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); FilterOrders(); }
        }

        // --- СТАТИСТИКА ---
        public int TotalOrders => Orders.Count;
        public int FreeCells => Cells.Count(c => !c.IsOccupied);
        public int TodayIssued => Orders.Count(o => o.Status.Contains("Выдан") && o.ArrivalTime.Date == DateTime.Today);

        public MainViewModel()
        {
            Orders = new ObservableCollection<BaseDelivery>();
            Logs = new ObservableCollection<string>();
            Cells = new ObservableCollection<Cell>();

            GenerateWarehouse();
            LoadData();
            LogAction("Система запущена. Склад готов к работе.");
        }

        private void GenerateWarehouse()
        {
            for (int i = 1; i <= 12; i++) Cells.Add(new Cell { Name = $"A-{i}", Size = "S" });
            for (int i = 1; i <= 12; i++) Cells.Add(new Cell { Name = $"B-{i}", Size = "M" });
            for (int i = 1; i <= 6; i++) Cells.Add(new Cell { Name = $"C-{i}", Size = "L" });
        }

        public bool AddOrder(string name, string phone, string orderSize, bool isFragile)
        {
            Cell freeCell = null;
            foreach (var cell in Cells)
            {
                if (!cell.IsOccupied)
                {
                    if (orderSize.StartsWith("S") && cell.Size == "S") { freeCell = cell; break; }
                    if (orderSize.StartsWith("M") && (cell.Size == "M" || cell.Size == "L")) { freeCell = cell; break; }
                    if (orderSize.StartsWith("L") && cell.Size == "L") { freeCell = cell; break; }
                }
            }

            if (freeCell == null)
            {
                MessageBox.Show($"Нет свободных ячеек для {orderSize}!", "Склад переполнен");
                return false;
            }

            BaseDelivery newOrder;
            if (isFragile)
                newOrder = new FragileOrder(name, phone, orderSize, freeCell.Name);
            else
                newOrder = new Order(name, phone, orderSize, freeCell.Name);

            Orders.Add(newOrder);
            freeCell.CurrentOrder = newOrder;

            LogAction($"Принят заказ {newOrder.Id} ({orderSize}) от {name}. Ячейка {freeCell.Name}.");
            UpdateStats();
            FilterOrders();
            SaveData();
            return true;
        }

        public void SimulateArrival()
        {
            string[] names = { "Иванов Иван", "Петрова Анна", "Сидоров Олег", "Кузнецова Мария", "Смирнов Алексей", "Волкова Елена" };
            string[] sizes = { "S (Мелкий)", "M (Средний)", "L (Крупный)" };

            Random rnd = new Random();
            string randomName = names[rnd.Next(names.Length)];
            string randomSize = sizes[rnd.Next(sizes.Length)];
            string randomPhone = $"+375 ({rnd.Next(25, 30)}) {rnd.Next(100, 999)}-{rnd.Next(10, 99)}-{rnd.Next(10, 99)}";
            bool isFragile = rnd.Next(2) == 1;

            AddOrder(randomName, randomPhone, randomSize, isFragile);
        }

        public void IssueOrder(string searchTerm)
        {
            string cleanSearch = new string(searchTerm.Where(char.IsDigit).ToArray());

            foreach (var order in Orders)
            {
                string cleanPhone = new string(order.Phone.Where(char.IsDigit).ToArray());
                bool isMatch = order.Id.ToUpper() == searchTerm.ToUpper() ||
                               (cleanSearch.Length > 5 && cleanPhone.Contains(cleanSearch));

                if (isMatch && order.Status.Contains("Ожидает"))
                {
                    order.Status = "Выдан";
                    FreeCell(order);
                    LogAction($"Выдан заказ {order.Id} (Клиент: {order.ClientName}). Ячейка {order.CellNumber} освобождена.");
                    UpdateStats();
                    FilterOrders();
                    SaveData();
                    MessageBox.Show($"Заказ {order.Id} успешно выдан!", "Успех");
                    return;
                }
            }
            MessageBox.Show("Заказ не найден или уже выдан!", "Ошибка");
        }

        public void ReturnOrder(string searchTerm)
        {
            string cleanSearch = new string(searchTerm.Where(char.IsDigit).ToArray());

            foreach (var order in Orders)
            {
                string cleanPhone = new string(order.Phone.Where(char.IsDigit).ToArray());
                bool isMatch = order.Id.ToUpper() == searchTerm.ToUpper() ||
                               (cleanSearch.Length > 5 && cleanPhone.Contains(cleanSearch));

                if (isMatch && order.Status.Contains("Ожидает"))
                {
                    order.Status = "Возврат";
                    FreeCell(order);
                    LogAction($"Оформлен возврат заказа {order.Id} (Клиент: {order.ClientName}).");
                    UpdateStats();
                    FilterOrders();
                    SaveData();
                    MessageBox.Show($"Заказ {order.Id} отправлен обратно.", "Возврат");
                    return;
                }
            }
            MessageBox.Show("Заказ не найден или уже выдан!", "Ошибка");
        }

        // Списание просроченных посылок (лежат >= 10 дней)
        public void WriteOffOverdue()
        {
            var overdue = Orders.Where(o => o.Status.Contains("Ожидает") && (DateTime.Now - o.ArrivalTime).TotalDays >= 10).ToList();

            if (overdue.Count == 0)
            {
                MessageBox.Show("Просроченных посылок (лежат 10 и более дней) не найдено.", "Информация");
                return;
            }

            foreach (var order in overdue)
            {
                order.Status = "Возврат (просрочен)";
                FreeCell(order);
                LogAction($"Заказ {order.Id} просрочен (лежал > 10 дней). Отправлен на возврат.");
            }

            UpdateStats();
            FilterOrders();
            SaveData();
            MessageBox.Show($"Успешно списано просроченных заказов: {overdue.Count}", "Успех");
        }

        private void FreeCell(BaseDelivery order)
        {
            foreach (var cell in Cells)
            {
                if (cell.CurrentOrder == order)
                {
                    cell.CurrentOrder = null;
                    break;
                }
            }
        }

        public void LogAction(string action)
        {
            string logEntry = $"[{DateTime.Now:dd.MM.yyyy HH:mm:ss}] {action}";
            Logs.Add(logEntry);
            try { File.AppendAllText(LogFile, logEntry + Environment.NewLine, Encoding.UTF8); } catch { }
        }

        public void ExportToCsv(string filePath)
        {
            try
            {
                var csv = new StringBuilder();
                csv.AppendLine("Номер;Клиент;Телефон;Размер;Ячейка;Статус;Время");
                foreach (var order in Orders)
                {
                    csv.AppendLine($"{order.Id};{order.ClientName};{order.Phone};{order.Size};{order.CellNumber};{order.Status};{order.ArrivalTime:dd.MM.yyyy HH:mm}");
                }
                File.WriteAllText(filePath, csv.ToString(), Encoding.UTF8);
                MessageBox.Show("Отчет успешно сохранен!", "Экспорт");
                LogAction($"Выполнен экспорт в файл {filePath}");
            }
            catch (Exception ex) { MessageBox.Show("Ошибка экспорта: " + ex.Message); }
        }

        private void UpdateStats()
        {
            OnPropertyChanged(nameof(TotalOrders));
            OnPropertyChanged(nameof(FreeCells));
            OnPropertyChanged(nameof(TodayIssued));
        }

        private void FilterOrders()
        {
            var view = CollectionViewSource.GetDefaultView(Orders);
            view.Filter = obj =>
            {
                if (!(obj is BaseDelivery order)) return false;

                bool matchesSearch = string.IsNullOrWhiteSpace(SearchText) ||
                                     order.ClientName.ToLower().Contains(SearchText.ToLower()) ||
                                     order.Id.ToLower().Contains(SearchText.ToLower());

                bool matchesStatus = true;
                if (SelectedFilter == "Ожидают выдачи") matchesStatus = order.Status.Contains("Ожидает");
                else if (SelectedFilter == "Выданные") matchesStatus = order.Status.Contains("Выдан");
                else if (SelectedFilter == "Возвраты") matchesStatus = order.Status.Contains("Возврат");

                return matchesSearch && matchesStatus;
            };
        }

        private void SaveData()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Orders, options);
                File.WriteAllText(DataFile, json);
            }
            catch (Exception ex) { MessageBox.Show("Ошибка сохранения: " + ex.Message); }
        }

        private void LoadData()
        {
            try
            {
                if (File.Exists(DataFile))
                {
                    string json = File.ReadAllText(DataFile);
                    var loadedOrders = JsonSerializer.Deserialize<ObservableCollection<BaseDelivery>>(json);

                    if (loadedOrders != null)
                    {
                        foreach (var order in loadedOrders)
                        {
                            Orders.Add(order);
                            if (order.Status.Contains("Ожидает"))
                            {
                                foreach (var cell in Cells)
                                {
                                    if (cell.Name == order.CellNumber) { cell.CurrentOrder = order; break; }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка загрузки: " + ex.Message); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}