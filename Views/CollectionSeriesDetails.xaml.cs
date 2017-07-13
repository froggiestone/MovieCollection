using MovieCollection.DataModels;
using MovieCollection.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace MovieCollection.Views
{

    public sealed partial class CollectionSeriesDetails : Page
    {
        public CollectionSeriesDetails()
        {
            this.InitializeComponent();
        }

        MainViewModel mainviewmodel { get; set; }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // viewmodel passed from previous page
            mainviewmodel = e.Parameter as MainViewModel;
            DataContext = mainviewmodel.SelectedMovie;

            var SelectedMovie = mainviewmodel.SelectedMovie as Movie;

            PhotoImage.Source = new BitmapImage(new Uri((string)SelectedMovie.poster));

            // grab the connected animation
            var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("PosterImage");
            if (animation != null)
            {
                // Wait for image opened. In future Insider Preview releases, this won't be necessary.
                PhotoImage.ImageOpened += (sender_, e_) =>
                {
                    PhotoImage.Opacity = 1;
                    animation.TryStart(PhotoImage);
                    //animation.TryStart(PhotoImage);
                };
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            // set up a new animation for navigating back
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("ThumbImage", PhotoImage);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (mainviewmodel.SelectedMovie != null)
            {
                var item = mainviewmodel.SelectedMovie as Movie;

                if (!string.IsNullOrEmpty(item.backdrop_path))
                {
                    BackDrop.ImageSource = new BitmapImage(new Uri(item.backdrop_path, UriKind.Absolute));
                }
                else
                {
                    // TODO create a generic backdrop when missing
                    // for now, just use the cover as the backdrop when missing
                    BackDrop.ImageSource = new BitmapImage(new Uri(item.poster, UriKind.Absolute));
                }

                DateTime date;

                // try to parse the string to a system datetime object
                bool parsedate = DateTime.TryParse(item.release_date, out date);

                if (parsedate)
                {
                    ReleaseDate.Text = String.Format("{0:d MMMM, yyyy}", date);
                }

                int seasonnumber = 0;
                foreach (var season in item.seasons)
                {
                    seasonnumber++;
                    var PivotItem = new PivotItem();
                    var listview = new ListView();

                    listview.ItemsSource = mainviewmodel.EpisodesList.Where(x => x.series_id == item.id && x.season_number == seasonnumber);
                    listview.ItemTemplate = (DataTemplate)Resources["EpisodeTemplate"];

                    PivotItem.Content = listview;
                    PivotItem.Header = "Season " + seasonnumber;
                    SeasonsPivot.Items.Add(PivotItem);
                }
            }
            else
            {
                // for some reason SelectedMovie is null and the laws of physics breaks down.
            }
        }

        private void BackDrop_ImageOpened(object sender, RoutedEventArgs e)
        {
          //  FadeinBackdropGrid.Begin();
        }


        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            var item = mainviewmodel.SelectedMovie as Movie;

            // no path set yet, so prompt the user to add a file(path)
            if (string.IsNullOrEmpty(item.filepath))
            {
                LaunchMessage.Text = "There is currently no file associated with this movie / tv show";
                PickFileFlyout.ShowAt(PlayButton);
            }
            else
            {
                // for some reason this always returns false, most likely a permission issue
                // so just wrap it in a catch for now

                //FileInfo fInfo = new FileInfo(item.filepath);

                try
                {
                    var file = await StorageFile.GetFileFromPathAsync(item.filepath);
                    var success = await Launcher.LaunchFileAsync(file);

                }catch(Exception)
                {
                    PickFileFlyout.ShowAt(PlayButton);
                    LaunchMessage.Text = "The current path is not valid anymore, did you change the filename or location?";
                }
                
            }
        }

        private async void FilePickerButton_Click(object sender, RoutedEventArgs e)
        {
            var item = mainviewmodel.SelectedMovie as Movie;
            var picker = new Windows.Storage.Pickers.FileOpenPicker();

            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            picker.FileTypeFilter.Add(".avi");
            picker.FileTypeFilter.Add(".mkv");
            picker.FileTypeFilter.Add(".mp4");

            StorageFile storagefile = await picker.PickSingleFileAsync();
            if (storagefile != null)
            {
                string token = StorageApplicationPermissions.FutureAccessList.Add(storagefile);
                //  filelocation.Text = "Picked photo: " + storagefile.Name;

                item.filepath = storagefile.Path;
               await mainviewmodel.SaveDataBase();
            }
            else
            {
                // filelocation.Text = "Operation cancelled.";
                return;
            }
        }
    }
}
