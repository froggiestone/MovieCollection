using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieCollection.DataModels
{
    public class Episode
    {
        public string air_date { get; set; }
        public int episode_number { get; set; }
        public int series_id { get; set; }
        public string name { get; set; }
        public string overview { get; set; }
        public int id { get; set; }
        public object production_code { get; set; }
        public int season_number { get; set; }
        public string still_path { get; set; }
        public double vote_average { get; set; }
        public int vote_count { get; set; }
    }

    public class SeasonsDataModel
    {
        public string _id { get; set; }
        public string air_date { get; set; }
        public List<Episode> episodes { get; set; }
        public string name { get; set; }
        public string overview { get; set; }
        public int id { get; set; }
        public string poster_path { get; set; }
        public int season_number { get; set; }
    }
    
}
