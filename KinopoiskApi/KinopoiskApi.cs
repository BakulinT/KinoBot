using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using ТелеграммБот.KinopoiskApi.KinoSearch;
using ТелеграммБот.KinopoiskApi.KinoPremiers;

namespace ТелеграммБот.KinopoiskApi
{
    class KinopoiskApi
    {
        private static string url = "https://kinopoiskapiunofficial.tech/api/";
        private static string key = "key";
        public static KinoSearchBot SearchFilm(string film)
        {
            string query = $"v2.1/films/search-by-keyword?keyword={film.Replace(" ", "%")}&page=1";

            WebRequest wb = WebRequest.Create(url + query);

            wb.Method = "GET";
            wb.Headers.Add("X-API-KEY", key);
            wb.ContentType = "application/json";
            try
            {
                using (Stream s = wb.GetResponse().GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(s))
                    {
                        string jsonResponse = sr.ReadToEnd();
                        return JsonConvert.DeserializeObject<KinoSearchBot>(jsonResponse);
                    }
                }
            }
            catch (WebException e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

        }
        public static kinoPremieres QueryPremieres(int year, int month = 1)
        {
            string query = $"v2.2/films/premieres?year={year}&month={MonthConvert(month)}";

            WebRequest wb = WebRequest.Create(url + query);

            wb.Method = "GET";
            wb.Headers.Add("X-API-KEY", key);
            wb.ContentType = "application/json";

            try
            {
                using (Stream s = wb.GetResponse().GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(s))
                    {
                        string jsonResponse = sr.ReadToEnd();
                        return JsonConvert.DeserializeObject<kinoPremieres>(jsonResponse);
                    }
                }
            }
            catch (WebException e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
        private static string MonthConvert(int month)
        {
            if (month < 0 || month > 12)
                return "JANUARY";

            string[] monthArr = {
                "JANUARY", "FEBRUARY",
                "MARCH", "APRIL", "MAY",
                "JUNE", "JULY", "AUGUST",
                "SEPTEMBER", "OCTOBER", "NOVEMBER",
                "DECEMBER"
            };

            return monthArr[month - 1];
        }
    }
}
