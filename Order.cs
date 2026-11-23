using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace PVZ_Simulator
{
    [JsonDerivedType(typeof(Order), typeDiscriminator: "order")]
    [JsonDerivedType(typeof(FragileOrder), typeDiscriminator: "fragile")]
    public abstract class BaseDelivery
    {
        public string Id { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string CellNumber { get; set; } = string.Empty;
        public DateTime ArrivalTime { get; set; }

        private string _status = string.Empty;
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public abstract string GetStorageRules();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Order : BaseDelivery
    {
        public Order() { }

        public Order(string clientName, string phone, string size, string cellNumber)
        {
            Id = Guid.NewGuid().ToString().Substring(0, 5).ToUpper();
            ClientName = clientName;
            Phone = phone;
            Size = size;
            CellNumber = cellNumber;
            Status = "Ожидает выдачи";
            ArrivalTime = DateTime.Now;
        }

        public override string GetStorageRules()
        {
            return "Стандартные условия хранения. Можно класть на любую полку.";
        }
    }

    public class FragileOrder : BaseDelivery
    {
        public FragileOrder() { }

        public FragileOrder(string clientName, string phone, string size, string cellNumber)
        {
            Id = Guid.NewGuid().ToString().Substring(0, 5).ToUpper();
            ClientName = clientName;
            Phone = phone;
            Size = size;
            CellNumber = cellNumber;
            Status = "Ожидает выдачи (Хрупкий!)";
            ArrivalTime = DateTime.Now;
        }

        public override string GetStorageRules()
        {
            return "ВНИМАНИЕ! Хрупкий груз. Запрещено ставить тяжелые предметы сверху!";
        }
    }
}