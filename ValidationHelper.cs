using System.Text.RegularExpressions;

namespace PVZ_Simulator
{
    public static class ValidationHelper
    {
        public static string FormatPhone(string input)
        {
            string rawPhone = Regex.Replace(input, @"[^\d]", "");
            if (rawPhone.Length == 12 && rawPhone.StartsWith("375"))
            {
                return $"+{rawPhone.Substring(0, 3)} ({rawPhone.Substring(3, 2)}) {rawPhone.Substring(5, 3)}-{rawPhone.Substring(8, 2)}-{rawPhone.Substring(10, 2)}";
            }
            return "Ошибка формата";
        }
    }
}