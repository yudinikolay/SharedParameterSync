﻿using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SharedParameterSync.Properties;
using MVVM;
using Shared;

namespace SharedParameterSync
{
    internal class MainViewModel : ViewModelBase
    {
        private readonly List<List<string>> _source1;
        private readonly List<List<string>> _source2;

        internal MainViewModel()
        {
            Logger.New();
            if (!SharedParameter.Items.ByRevitSharedParameterFile(Resources.FilePath, out _source1))
            {
                return;
            }

            IList<IList<object>> table = Google.SpreadsheetConnector.ReadRangeFromTable(Resources.DocId, Resources.Range);
            if (!SharedParameter.Items.ByObjectsTable(table, out _source2))
            {
                return;
            }

            LoadSources();
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
                Google.SpreadsheetConnector.WriteRangeToTable(Resources.DocId, Resources.Range, _sourceModel.AsObjectsTable());
            }
            else
            {
                _sourceModel.WriteRevitSharedParameterFile(Resources.FilePath);
            }
            window.Close();
        }
    }
}