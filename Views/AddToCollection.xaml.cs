using MovieCollection.DataModels;
using MovieCollection.ViewModels;
using System;
using System.Linq;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MovieCollection.Views
{
    public sealed partial class AddToCollection : UserControl
    {
        public AddToCollection(MainViewModel _mainviewmodel)
        {
            mainviewmodel = _mainviewmodel;
            this.InitializeComponent();
        }

        public MainViewModel mainviewmodel { get; set; }

        private async void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            
            if(args.CheckCurrent())
            {
                await mainviewmodel.SerializeSearchResults(SearchBox.Text);
            }
            
        }

        private async void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {

            var item = args.SelectedItem as SearchResult;
            SearchBox.Text = "";
            await mainviewmodel.SerializeMovieResult(item.id, item.media_type);
        }

        private void AddToDataBase_Click(object sender, RoutedEventArgs e)
        {
            mainviewmodel.AddToDataBase();
        }

        private async void OpenLocation_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            picker.FileTypeFilter.Add(".avi");
            picker.FileTypeFilter.Add(".mkv");
            picker.FileTypeFilter.Add(".mp4");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                // uwp apps needs permission to reach into the OS, so
                // save the path reference so we can access the file later
                string token = StorageApplicationPermissions.FutureAccessList.Add(file);
                var item = mainviewmodel.AddToDataBaseList.First();

                item.filepath = file.Path;
            }
            else
            {
            }
        }

        private async void launchfile_Click(object sender, RoutedEventArgs e)
        {
            var item = mainviewmodel.AddToDataBaseList.First();

            var file = await StorageFile.GetFileFromPathAsync(item.filepath);

            if (file != null)
            {
                var success = await Launcher.LaunchFileAsync(file);

                if (success)
                {
                    // File launched
                }
                else
                {
                    // File launch failed
                }
            }
            else
            {
                // Could not find file
            }
        }
    }
}
