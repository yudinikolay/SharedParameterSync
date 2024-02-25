using System.ComponentModel;

namespace MVVM
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler eventHandler = this.PropertyChanged;
            if (eventHandler == null) return;
            eventHandler((object)this, new PropertyChangedEventArgs(propertyName));
        }

    }

}
