using System;
using System.Collections.Generic;
using System.Text;

namespace ТелеграммБот.KinopoiskApi.KinoPremiers
{
    class items
    {
        public int kinopoiskId;
        public string nameRu;
        public string nameEn;
        public int year;
        public string posterUrl;
        public string posterUrlPreview;
        public countries[] countries;
        public genres[] genres;
        public int? duration;
        public string premiereRu;
    }
}