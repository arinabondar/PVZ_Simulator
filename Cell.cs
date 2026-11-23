using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PVZ_Simulator
{
    public class Cell : INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;

        private BaseDelivery? _currentOrder; // Добавили ?
        public BaseDelivery? CurrentOrder
        {
            get => _currentOrder;
            set
            {
                _currentOrder = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsOccupied));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(DaysOnShelfText));
            }
        }

        public bool IsOccupied => CurrentOrder != null;

        public string StatusColor => IsOccupied ? "#FFCDD2" : "#C8E6C9";

        public string DaysOnShelfText
        {
            get
            {
                // Проверяем на null, чтобы программа не вылетела
                if (!IsOccupied || CurrentOrder == null) return "";
                int days = (DateTime.Now - CurrentOrder.ArrivalTime).Days;
                return $"{days} дн. на складе";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}