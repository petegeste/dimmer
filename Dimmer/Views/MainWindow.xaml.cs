using System.Windows;
using System.Windows.Controls;

namespace Dimmer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel vm)
        {
            DataContext = vm;
            InitializeComponent();
        }

        private void Grid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var grid = sender as ListViewItem;
            if (grid != null)
            {
                var preset = grid.DataContext as PresetModel;
                preset.EditingName = true;
            }
        }

        private void TextBox_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            var textbox = sender as TextBox;
            if (textbox != null)
            {
                var preset = textbox.DataContext as PresetModel;
                preset.EditingName = false;
            }
        }
    }
}
