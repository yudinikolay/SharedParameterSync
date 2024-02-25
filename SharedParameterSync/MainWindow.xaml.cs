using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SharedParameterSync
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();

            // Костыль для устранения бага с использованием горячих клавиш при выборе в ListView
            itemList.SelectionMode = SelectionMode.Single;
            itemList.SelectedIndex = 0;
            itemList.SelectionMode = SelectionMode.Extended;

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
