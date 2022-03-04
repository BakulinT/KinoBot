using System;
using System.Collections.Generic;
using System.Text;

namespace ТелеграммБот.KinopoiskApi.KinoSearch
{
    class films
    {
        public int filmId;
        public string nameRu;
        public string nameEn;
        public string type;
        public string year;
        public string description;
        public string filmLength;
        public countries[] countries;
        public genres[] genres;
        public string rating;
        public int ratingVoteCount;
        public string posterUrl;
        public string posterUrlPreview;
    }
}
