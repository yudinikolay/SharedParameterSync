using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MVVM;
using Shared;
using Microsoft.Win32;
using System.Xaml;

namespace SharedParameterSync
{
    internal class MainViewModel : ViewModelBase
    {
        private List<List<string>> _source1;
        private List<List<string>> _source2;

        public string DocId {  get; set; }
        public string FilePath { get; set; }
        public string Range { get; set; }

        internal MainViewModel()
        {
            Logger.New();

            ReadSettings();

            NewSources();

            LoadSources();
        }

        private void ReadSettings()
        {
            //{
            //  "FILE_PATH": "C:\\Users\\User\\Documents\\MyFile.txt",
            //  "DOCUMENT_ID": "123456",
            //  "RANGE": "IMPORT"
            //}
            string settings = System.IO.File.ReadAllText("settings.json");
            dynamic settingsObject = Newtonsoft.Json.JsonConvert.DeserializeObject(settings);
            FilePath = settingsObject.FILE_PATH;
            DocId = settingsObject.DOCUMENT_ID;
            Range = settingsObject.RANGE;
        }

        private void NewSources()
        {
            if (!SharedParameter.Items.ByRevitSharedParameterFile(FilePath, out _source1))
            {
                return;
            }

            IList<IList<object>> table = Google.SpreadsheetConnector.ReadRangeFromTable(DocId, Range);
            if (!SharedParameter.Items.ByObjectsTable(table, out _source2))
            {
                return;
            }
        }

        public string SourceName { get; set; }
        public string TargetName { get; set; }

        private bool _flipped = false;
        private void LoadSources()
        {
            if (!_flipped)
            {
                LoadSources(_source2, _source1);
                SourceName = "Считан ФОП";
                TargetName = "Запись в Google-таблицу";
            }
            else
            {
                LoadSources(_source1, _source2);
                SourceName = "Считана Google-таблица";
                TargetName = "Запись в ФОП";
            }
            OnPropertyChanged(nameof(SourceName));
            OnPropertyChanged(nameof(TargetName));
        }

        public ObservableCollection<SharedParameter.Item> Data => _sourceModel.Data;
        private readonly SharedParameter.Items _sourceModel = new SharedParameter.Items();

        private void LoadSources(List<List<string>> source1, List<List<string>> source2)
        {
            _sourceModel.ByStringTable(source1);
            SharedParameter.Items newModel = new SharedParameter.Items();
            newModel.ByStringTable(source2);

            _sourceModel.ReadChanges(newModel);
            OnPropertyChanged(nameof(_sourceModel.Data));
            SelectAll();
        }

        public RelayCommand FlipSourcesCommand => new RelayCommand(obj => FlipSources());

        internal void FlipSources()
        {
            _flipped = !_flipped;
            LoadSources();
        }

        public RelayCommand SelectAllCommand => new RelayCommand(obj => SelectAll());

        private void SelectAll()
        {
            foreach (SharedParameter.Item item in Data)
            {
                item.ChangesAllowed = true;
            }
        }

        public RelayCommand OkCommand => new RelayCommand(obj => Ok(obj as Window));
        private void Ok(Window window)
        {
            _sourceModel.ApplyChanges();
            if (!_flipped)
            {
                Google.SpreadsheetConnector.WriteRangeToTable(DocId, Range, _sourceModel.AsObjectsTable());
            }
            else
            {
                _sourceModel.WriteRevitSharedParameterFile(FilePath);
            }
            window.Close();
        }

        public RelayCommand SharedParFilePathCommand => new RelayCommand(obj => SharedParFilePath());

        private void SharedParFilePath()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = FilePath,
                Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                FilePath = filePath;
                OnPropertyChanged(nameof(FilePath));
            }
        }

        public RelayCommand SettingsCommand => new RelayCommand(obj => OpenSettings());
        private Settings settingsWindow; 
        private void OpenSettings()
        {
            if (settingsWindow == null || !settingsWindow.IsVisible)
            {
                settingsWindow = new Settings(this);
                settingsWindow.Show();
            }
            else
            {
                settingsWindow.Focus();
            }
        }

        public RelayCommand SaveSettingsCommand => new RelayCommand(obj => SaveSettings(obj as Window));
        private void SaveSettings(Window window)
        {
            Dictionary<string, string> settings = new Dictionary<string, string>()
            {
                {"FILE_PATH", FilePath },{"DOCUMENT_ID", DocId },{"RANGE", Range }
            };
            string settingText = Newtonsoft.Json.JsonConvert.SerializeObject(settings);
            System.IO.File.WriteAllText("settings.json", settingText);
            window.Close();

            NewSources();
            LoadSources();
        }

    }
}
