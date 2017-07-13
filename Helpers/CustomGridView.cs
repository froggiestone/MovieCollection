using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MovieCollection.Helpers
{
    class CustomGridView : GridView
    {

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            var container = element as GridViewItem;
            double WindowWidth = Window.Current.Bounds.Width;

            if(WindowWidth < 700)
            {
                container.Width = (WindowWidth / 3) - 4;
            }
            else
            {
               container.Width =  (WindowWidth / 7) - 10;
            }
            
        }
    }
}
