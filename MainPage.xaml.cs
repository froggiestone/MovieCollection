using MovieCollection.DataModels;
using MovieCollection.ViewModels;
using MovieCollection.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace MovieCollection
{
    public sealed partial class MainPage : Page
    {
        public MainViewModel mainviewmodel = new MainViewModel();
        bool ViewModelInitialized = false;
        static bool _navigated = false;
        bool editmode = false;

        public MainPage()
        {
            //set the page's datacontext to the viewmodel
            DataContext = mainviewmodel;

            // require the page to be catched, so it doesn't reload when navigating to/from
            NavigationCacheMode = NavigationCacheMode.Required;
            // initialize page
            this.InitializeComponent();
            
        }

        
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // page_loaded triggers whenever you navigate to/from, so check if the viewmodel has been 
            //initialized before proceeding
            if (!ViewModelInitialized)
            {
                // initialize viewmodel
                mainviewmodel.Initialize();
                
                ViewModelInitialized = true;

                //this is for the new design language fluent design, making the content extends into the titlebar
                ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
                formattableTitleBar.ButtonBackgroundColor = Colors.Transparent;
                CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
                coreTitleBar.ExtendViewIntoTitleBar = true;
            }
            
        }

        private void MovieCollectionPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Pivot pivot = sender as Pivot;
            var item = pivot.SelectedItem as PivotItem;

            if (item.Name == "add" && AddContentPresenter.Content == null)
            {
                AddContentPresenter.Content = new AddToCollection(mainviewmodel);
            }

            else if (item.Name == "settings" && SettingsContentPresenter.Content == null)
            {
                SettingsContentPresenter.Content = new Settings(mainviewmodel);
            }
        }
       
        private void CollectionGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // make sure the gridview is not in "edit" mode;
            if (CollectionGridView.SelectionMode == ListViewSelectionMode.Single && editmode == false)
            {
                var item = CollectionGridView.SelectedItem as Movie;
                var container = CollectionGridView.ContainerFromItem(item) as GridViewItem;
                if (container != null)
                {
                    var root = (FrameworkElement)container.ContentTemplateRoot;
                    var image = (UIElement)root.FindName("PosterImage");

                    ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("PosterImage", image);
                }

                mainviewmodel.SelectedMovie = item;


                _navigated = true;

                if(item.media_type == "movie")
                {
                    Frame.Navigate(typeof(CollectionMovieDetails), mainviewmodel);
                }
                    
                else
                {
                    Frame.Navigate(typeof(CollectionSeriesDetails), mainviewmodel);
                }
            }

            else
            {
                // gridview is in multi select mode so 
                if(CollectionGridView.SelectedItems.Count > 0)
                {
                    DeleteFromCollectionButton.Visibility = Visibility.Visible;
                    DeleteFromCollectionButton.Content = "Delete (" + CollectionGridView.SelectedItems.Count + ")";
                }
                else
                {
                    DeleteFromCollectionButton.Visibility = Visibility.Collapsed;
                }
            }
        }
        
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Don't use vertical entrance animation with connected animation
            if (e.NavigationMode == NavigationMode.Back)
            {
                //EntranceTransition.FromVerticalOffset = 0;
            }
        }

        private async void ItemsGridView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_navigated)
            {
                var animationService = ConnectedAnimationService.GetForCurrentView();
                var animation = animationService.GetAnimation("ThumbImage");

                if (animation != null)
                {
                    CollectionGridView.ScrollIntoView(CollectionGridView.SelectedItem);

                    var container = CollectionGridView.ContainerFromItem(mainviewmodel.SelectedMovie) as GridViewItem;
                    if (container != null)
                    {
                        var root = (FrameworkElement)container.ContentTemplateRoot;
                        var image = (Microsoft.Toolkit.Uwp.UI.Controls.ImageEx)root.FindName("PosterImage");

                        await CollectionGridView.TryStartConnectedAnimationAsync(animation, mainviewmodel.SelectedMovie, "PosterImage");
                       
                    }
                    else
                    {
                        animation.Cancel();
                    }
                }

                _navigated = false;
            }
        }

        
        private void EditCollectionViewButton_Click(object sender, RoutedEventArgs e)
        {
            if(CollectionGridView.SelectionMode == ListViewSelectionMode.Single)
            {
                CollectionGridView.SelectionMode = ListViewSelectionMode.Multiple;
                EditCollectionViewButton.Content = "Cancel";
                editmode = true;
            }
            else
            {
                CollectionGridView.SelectionMode = ListViewSelectionMode.Single;
                EditCollectionViewButton.Content = "Edit Collection";
                editmode = false;
            }
            
        }

        private async void DeleteFromCollectionButton_Click(object sender, RoutedEventArgs e)
        {

            foreach (var item in CollectionGridView.SelectedItems.ToList())
            {
                var Citem = item as Movie;
                mainviewmodel.MovieList.Remove(Citem);
            }

           await mainviewmodel.SaveDataBase();
        }

        private void SearchCollection_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            
        }

       

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var panel = sender as RelativePanel;
            applyAcrylicAccent2(panel);
        }

        Compositor _compositor2;
        SpriteVisual _hostSprite2;
        private void applyAcrylicAccent2(Panel e)
        {
            _compositor2 = ElementCompositionPreview.GetElementVisual(this).Compositor;
            _hostSprite2 = _compositor2.CreateSpriteVisual();
            _hostSprite2.Size = new Vector2((float)e.ActualWidth, (float)e.ActualHeight);

            ElementCompositionPreview.SetElementChildVisual(
                    e, _hostSprite2);
            _hostSprite2.Brush = _compositor2.CreateHostBackdropBrush();

        }

    }
}
