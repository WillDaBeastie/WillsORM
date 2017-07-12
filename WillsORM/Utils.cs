using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WillsORM
{
    public class Utils
    {
        public static string ConvertFullIndexSearch(string searchTerm)
        {
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                searchTerm = searchTerm.Replace("'", "''");
                searchTerm = Regex.Replace(searchTerm, @"\s+", " ");
                searchTerm = searchTerm.Replace(" ", " OR ");
            }

            return searchTerm;
        }

        public static DateTime ToSQLDate(string date)
        {
            DateTime returnDate = DateTime.Now;
            DateTime.TryParse(date, out returnDate);

            return returnDate;
        }

        public static bool IsNAN(string str)
        {
            Regex reg = new Regex(@"^\d*");
            return !reg.IsMatch(str);
        }
    }
}
