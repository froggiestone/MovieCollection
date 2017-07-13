using MovieCollection.DataModels;
using MovieCollection.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace MovieCollection.ViewModels
{
    public class MainViewModel : NotificationBase
    {
        public MainViewModel()
        {

        }

        ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;

        public ObservableCollection<Movie> MovieList = new ObservableCollection<Movie>();
        public ObservableCollection<Movie> AddToDataBaseList = new ObservableCollection<Movie>();
        public ObservableCollection<Movie> RecentAddedList = new ObservableCollection<Movie>();
        public ObservableCollection<SearchResult> SeachResults = new ObservableCollection<SearchResult>();

        public List<Episode> EpisodesList = new List<Episode>();

        private const string ApiKey = "f12083e989c4b901f728d799fb5b7804";
        private const string SecureBaseUrl = "https://api.themoviedb.org/3/";
        private const string SecureImageBaseUrl = "https://image.tmdb.org/t/p/";


        private const string SearchUrl = SecureBaseUrl + "search/multi?api_key=" + ApiKey + "&query=";
        private const string imdbUrl = "http://www.omdbapi.com/?i=";

        private object _SelectedMovie;
        public object SelectedMovie
        {
            get { return _SelectedMovie; }
            set { SetProperty(_SelectedMovie, value, () => _SelectedMovie = value); }
        }

        public async void Initialize()
        {
           await LoadDataBase();
           await LoadEpisodeList();
           await LoadRecentList();
        }

        public async Task<bool> SerializeSearchResults(string SearchQuery)
        {
            try
            {
                string url = SearchUrl + SearchQuery;

                if (string.IsNullOrEmpty(SearchQuery))
                {
                    return false;
                }

                var jsonMessage = await HttpRequestAsync(url);

                if (!string.IsNullOrEmpty(jsonMessage))
                {
                    var serializer = new DataContractJsonSerializer(typeof(SearchResultDataModel));
                    var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonMessage));
                    var List = (SearchResultDataModel)serializer.ReadObject(ms);

                    SeachResults.Clear();

                    foreach (var item in List.results)
                    {
                        // filter out persons, we only want movies and tv shows
                        if (item.media_type != "person")
                        {
                            var incollection = MovieList.FirstOrDefault(i => i.id == item.id);

                            if (incollection != null)
                            {
                                item.incollection = true;
                            }

                            // filter out anything without a poster, abd build the poster path
                            if (!string.IsNullOrEmpty(item.poster_path))
                            {
                                item.poster = SecureImageBaseUrl + "w185" + item.poster_path;

                                SeachResults.Add(item);
                            }



                        }



                    }

                    ms.Dispose();
                    serializer = null;

                    return true;
                }

                else
                {
                    return false;
                }
            }

            catch (Exception) { return false; }
        }

        public async Task<bool> SerializeMovieResult(int Id, string MediaType)
        {
            try
            {
                string url = "";
                if(MediaType == "movie")
                {
                    url = SecureBaseUrl + "movie/" + Id + "?api_key=" + ApiKey;
                }
                else
                {
                    url = SecureBaseUrl + "tv/" + Id + "?api_key=" + ApiKey;
                }

                string jsonMessage = await HttpRequestAsync(url);

                if (!string.IsNullOrEmpty(jsonMessage))
                {
                    var serializer = new DataContractJsonSerializer(typeof(Movie));
                    var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonMessage));
                    var movie = (Movie)serializer.ReadObject(ms);

                    movie.poster_thumbnail = SecureImageBaseUrl + "w185" + movie.poster_path;
                    movie.poster = SecureImageBaseUrl + "w780" + movie.poster_path;
                    movie.backdrop_path = SecureImageBaseUrl + "w1280" + movie.backdrop_path;
                    movie.media_type = MediaType;

                    // make a second request to get the credits 

                    if (MediaType == "movie")
                    {
                        url = SecureBaseUrl + "movie/" + Id + "/credits?api_key=" + ApiKey;
                    }
                    else
                    {
                        url = SecureBaseUrl + "tv/" + Id + "/credits?api_key=" + ApiKey;
                    }

                    jsonMessage = await HttpRequestAsync(url);

                    serializer = new DataContractJsonSerializer(typeof(Movie));
                    ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonMessage));
                    var credits = (Movie)serializer.ReadObject(ms);

                    // loop through the lists and build the image paths
                    foreach (var castitem in credits.cast)
                    {
                        castitem.portrait = SecureImageBaseUrl + "w185" + castitem.profile_path;
                    }

                    foreach (var crewitem in credits.crew)
                    {
                        crewitem.portrait = SecureImageBaseUrl + "w185" + crewitem.profile_path;
                    }

                    movie.crew = credits.crew;
                    movie.cast = credits.cast;

                    AddToDataBaseList.Add(movie);

                    ms.Dispose();
                    serializer = null;

                    // if its a tv show, we need all the information on seasons and episodes
                    if (movie.media_type == "tv")
                    {
                        
                        for (int i = 0; i <= movie.number_of_seasons; i++)
                        {
                            // /tv/{tv_id}/season/{season_number}
                            url = SecureBaseUrl + "tv/" + Id + "/season/" + i + "?api_key=" + ApiKey;

                            jsonMessage = await HttpRequestAsync(url);

                            serializer = new DataContractJsonSerializer(typeof(SeasonsDataModel));
                            ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonMessage));
                            var season = (SeasonsDataModel)serializer.ReadObject(ms);

                            foreach (var episode in season.episodes)
                            {
                                episode.series_id = Id;
                                EpisodesList.Add(episode);
                                episode.still_path = SecureImageBaseUrl + "w300" + episode.still_path;
                            }
                        }

                       await SaveEpisodeList();
                    }
                    
                   

                    return true;
                }

                else
                {
                    return false;
                }
            }

            catch (Exception) { return false; }
        }

        private async Task<string> HttpRequestAsync(string url)
        {
            try
            {
                HttpClient http = new HttpClient();
                var response = await http.GetAsync(url);

                string jsonmessage = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(jsonmessage))
                {
                    http.Dispose();
                    return jsonmessage;
                }
                else
                {
                    http.Dispose();
                    return null;
                }

            }
            catch (Exception)
            {

                return null;
            }

        }

        public async Task<bool> SaveDataBase()
        {
            var filename = "MovieDataBase.json";

            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

                IRandomAccessStream raStream = await file.OpenAsync(FileAccessMode.ReadWrite);

                using (IOutputStream outStream = raStream.GetOutputStreamAt(0))
                {
                    // Serialize the Session State. 
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<Movie>));

                    serializer.WriteObject(outStream.AsStreamForWrite(), MovieList);

                    await outStream.FlushAsync();
                    outStream.Dispose();
                    raStream.Dispose();

                    Debug.WriteLine("MovieDataBase saved");
                    return true;
                }
            }
            catch (Exception) { Debug.WriteLine("movie data failed to save"); return false; }


        }
        public async Task<bool> SaveRecentList()
        {
            var filename = "RecentAdded.json";

            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

                IRandomAccessStream raStream = await file.OpenAsync(FileAccessMode.ReadWrite);

                using (IOutputStream outStream = raStream.GetOutputStreamAt(0))
                {
                    // Serialize the Session State. 
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<Movie>));

                    serializer.WriteObject(outStream.AsStreamForWrite(), RecentAddedList);

                    await outStream.FlushAsync();
                    outStream.Dispose();
                    raStream.Dispose();

                    Debug.WriteLine("Recent List saved");
                    return true;
                }
            }
            catch (Exception) { Debug.WriteLine("recent list failed to save"); return false; }


        }
        public async Task<bool> LoadRecentList()
        {
            var filename = "RecentAdded.json";

            try
            {
                var Serializer = new DataContractJsonSerializer(typeof(List<Movie>));
                var filecheck = await ApplicationData.Current.LocalFolder.TryGetItemAsync(filename);

                if (filecheck != null)
                {
                    var stream = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(filename);

                    if (stream.Length > 0)
                    {
                        var list = (List<Movie>)Serializer.ReadObject(stream);

                        foreach (var item in list)
                        {
                            RecentAddedList.Add(item);
                        }

                        stream.Dispose();
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                }
                else
                {
                    return false;
                }

            }
            catch (Exception) { Debug.WriteLine("Failed loading achievement data"); return false; }

        }

        public async Task<bool> SaveEpisodeList()
        {
            var filename = "Episodes.json";

            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

                IRandomAccessStream raStream = await file.OpenAsync(FileAccessMode.ReadWrite);

                using (IOutputStream outStream = raStream.GetOutputStreamAt(0))
                {
                    // Serialize the Session State. 
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<Episode>));

                    serializer.WriteObject(outStream.AsStreamForWrite(), EpisodesList);

                    await outStream.FlushAsync();
                    outStream.Dispose();
                    raStream.Dispose();

                    Debug.WriteLine("Episodelist saved");
                    return true;
                }
            }
            catch (Exception) { Debug.WriteLine("episodelist failed to save"); return false; }


        }
        public async Task<bool> LoadEpisodeList()
        {
            var filename = "Episodes.json";

            try
            {
                var Serializer = new DataContractJsonSerializer(typeof(List<Episode>));
                var filecheck = await ApplicationData.Current.LocalFolder.TryGetItemAsync(filename);

                if (filecheck != null)
                {
                    var stream = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(filename);

                    if (stream.Length > 0)
                    {
                        var episodelist = (List<Episode>)Serializer.ReadObject(stream);

                        foreach (var episode in episodelist)
                        {
                            EpisodesList.Add(episode);
                        }

                        stream.Dispose();
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                }
                else
                {
                    return false;
                }

            }
            catch (Exception) { Debug.WriteLine("Failed loading achievement data"); return false; }

        }

        public async Task<bool> LoadDataBase()
        {
            var filename = "MovieDataBase.json";

            try
            {
                var Serializer = new DataContractJsonSerializer(typeof(List<Movie>));
                var filecheck = await ApplicationData.Current.LocalFolder.TryGetItemAsync(filename);

                if (filecheck != null)
                {
                    var stream = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(filename);

                    if (stream.Length > 0)
                    {
                        var MovieDataBase = (List<Movie>)Serializer.ReadObject(stream);

                        foreach (var movie in MovieDataBase)
                        {
                            MovieList.Add(movie);
                        }

                        stream.Dispose();
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                }
                else
                {
                    return false;
                }

            }
            catch (Exception) { Debug.WriteLine("Failed loading achievement data"); return false; }

        }

       
        public async void AddToDataBase()
        {
            foreach (var item in AddToDataBaseList)
            {
                MovieList.Add(item);
                
                // most resent should be added to the start of the list aka index 0
                RecentAddedList.Insert(0, item);

            }

            await SaveDataBase();

            AddToDataBaseList.Clear();

            if (RecentAddedList.Count < 20)
            {
                int length = RecentAddedList.Count - 20;
                for (int i = 0; i < length; i++)
                {
                    var itemtoremove = RecentAddedList.Last();
                    RecentAddedList.Remove(itemtoremove);
                }
            }
            await SaveRecentList();

        }

        public async Task<bool> ResetCollection()
        {
            var folder = ApplicationData.Current.LocalFolder;
            var files = await folder.GetFilesAsync();

            foreach (var file in files)
            {
                await file.DeleteAsync(StorageDeleteOption.Default);
            }

            return true;
        }

        
    }
}
