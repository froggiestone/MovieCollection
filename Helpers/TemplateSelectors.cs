using MovieCollection.DataModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MovieCollection.Helpers
{
    class SearchBoxTemplateSelector : DataTemplateSelector
    {

        // persons are filtered out, so its just here for possible future use
        public DataTemplate SearchBoxPerson { get; set; }
        public DataTemplate SearchBoxMovie { get; set; }
        public DataTemplate SearchBoxTv { get; set; }


        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            try
            {
                if (item != null)
                {
                    var result = item as SearchResult;

                    if (result.media_type == "movie")
                    {
                        return SearchBoxMovie;
                    }
                    else
                    {
                        return SearchBoxTv;
                    }
                }

            }
            catch (Exception)
            { Debug.Write("templateselector failed"); }


            return base.SelectTemplateCore(item, container);



        }
    }

    class MovieTvShowPreview : DataTemplateSelector
    {
        public DataTemplate MoviePreview { get; set; }
        public DataTemplate TvShowPreview { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            try
            {
                if (item != null)
                {
                    var result = item as Movie;

                    if (result.media_type == "movie")
                    {
                        return MoviePreview;
                    }
                    else
                    {
                        return TvShowPreview;
                    }
                }

            }
            catch (Exception)
            { Debug.Write("templateselector failed"); }


            return base.SelectTemplateCore(item, container);



        }
    }

    class MovieTvShow : DataTemplateSelector
    {
        public DataTemplate Movie { get; set; }
        public DataTemplate TvShow { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            try
            {
                if (item != null)
                {
                    var result = item as Movie;

                    if (result.media_type == "movie")
                    {
                        return Movie;
                    }
                    else
                    {
                        return TvShow;
                    }
                }

            }
            catch (Exception)
            { Debug.Write("templateselector failed"); }


            return base.SelectTemplateCore(item, container);



        }
    }
}
