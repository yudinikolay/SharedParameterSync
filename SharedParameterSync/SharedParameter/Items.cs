using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Documents;
using Shared;

namespace SharedParameter
{
    internal class Items : ItemBase
    {
        internal Items()
        {
            isValid = true;
        }

        internal ObservableCollection<Item> Data => _data;
        private ObservableCollection<Item> _data = new ObservableCollection<Item>();

        internal void ApplyChanges()
        {
            for (int index = 0; index < _data.Count; index++)
            {
                ApplyChanges(index);
            }
        }

        internal void ApplyChanges(int index)
        {
            Item item = _data[index];
            if ((item.ChangesAllowed && item.Status == State.Removed) ||
                (!item.ChangesAllowed && item.Status == State.Added))
            {
                Logger.Info($"{item.Name} - успешно удалён");
                _data.RemoveAt(index);
                return;
            }
            if (item.ChangesAllowed && item.Status == State.Fixed)
            {
                Logger.Info($"{item.Name} - успешно обновлён");
                _data[index].ApplyChanges();
                return;
            }
            if (!item.ChangesAllowed)
            {
                item.DiscardChanges();
                return;
            }
            if (item.Status != State.Ok)
            {
                _data[index].Status = State.Ok;
            }
        }

        internal bool ReadChanges(Items items2)
        {
            if (!IsValid || !items2.IsValid) { return false; }

            // Каждый элемент первого списка будем искать во втором списке
            foreach (Item item in _data)
            {
                item.DiscardChanges();
                Item itemNew = items2.Data.FirstOrDefault(x => x.GuidValue == item.GuidValue);

                if (itemNew is null)
                {
                    // В зависимости от направления "чтение-запись"
                    // считаем отсутствующий элемент удаленным или добавленным
                    Logger.Info($"{item.Name} - удалён");
                    item.Status = State.Removed;
                }
                else
                {
                    //Если найден, проверяем на идентичность и в случае найденных отличий помечаем измененным
                    item.ReadChanges(itemNew);
                    if (item.Status == State.Fixed)
                        Logger.Info($"{item.Name} - изменён");
                }
            }

            // Каждый элемент второго списка проверяем на отсутствие в первом
            foreach (Item item in items2.Data)
            {
                if (_data.FirstOrDefault(x => x.GuidValue == item.GuidValue) is null)
                {
                    // В зависимости от направления "чтение-запись"
                    // считаем отсутствующий элемент добавленным или удаленным 
                    Logger.Info($"{item.Name} - добавлен");
                    item.Status = State.Added;
                    _data.Add(item);
                }
            }
            OrderElements();
            return true;
        }


        internal IList<IList<object>> AsObjectsTable()
        {
            List<IList<object>> parTable = new List<IList<object>>() { Keys.Select(x => x as object).ToList() };
            foreach (Item item in Data)
            {
                List<object> row = Keys.Select(x => item.Data[x] as object).ToList();
                parTable.Add(row);
            }

            return parTable;
        }

        internal bool WriteRevitSharedParameterFile(string filePath)
        {
            List<string> groups = _data
                .Where(x => x.IsValid)
                .Select(x => x.Group)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            using (StreamWriter sw = new StreamWriter(filePath, false, new UnicodeEncoding(false, true)))
            {
                sw.WriteLine("# This is a Revit shared parameter file.");
                sw.WriteLine("# Do not edit manually.");
                sw.WriteLine("*META\tVERSION\tMINVERSION");
                sw.WriteLine("META\t2\t1");
                sw.WriteLine("*GROUP\tID\tNAME");

                for (int i = 0; i < groups.Count; i++)
                {
                    // Номер группы = индекс имени в списке + 1 (чтобы не с нуля начинался)
                    sw.WriteLine($"{_Group}{_T}{i + 1}{_T}{groups[i]}");
                }

                sw.WriteLine($"{_XParam}{_T}{string.Join(_T.ToString(), Keys)}");
                foreach (Item item in Data)
                {
                    string row = $"{item.GetGuid()}{_T}" +
                                 $"{item.GetName()}{_T}" +
                                 $"{item.GetDataType()}{_T}" +
                                 $"{item.GetDataCategory()}{_T}" +
                                 $"{item.GetGroupId(groups)}{_T}" +
                                 $"{item.GetIsVisible()}{_T}" +
                                 $"{item.GetDescription()}{_T}" +
                                 $"{item.GetUserModifiable()}{_T}" +
                                 $"{item.GetHideWhenNoValue()}";

                    sw.WriteLine($"{_Param}{_T}{row}");
                }
            }
            return isValid;
        }

        internal static bool ByObjectsTable(IList<IList<object>> objValues, out List<List<string>> table)
        {
            if (TableStringFromObject(objValues, out table))
            {
                Logger.Info($"Из Google-таблицы успешно считалось {table.Count - 1} строк с параметрами");
                return true;
            }

            return false;
        }

        internal bool ByStringTable(List<List<string>> table)
        {
            if (ParamsFromTable(table, out _data))
            {
                Logger.Info($"Из Google-таблицы успешно считалось {_data.Count} параметров");
                return true;
            }

            return false;
        }

        internal static bool ByRevitSharedParameterFile(string filePath, out List<List<string>> table)
        {
            table = new List<List<string>>();
            using (StreamReader sr = new StreamReader(filePath, new UnicodeEncoding(false, true)))
            {
                string data = sr.ReadToEnd();
                if (TableFromSharedParameterFile(data, out table))
                {
                    Logger.Info($"Из ФОП успешно считалось {table.Count - 1} строк с параметрами");
                    return true;
                }
            }

            return false;
        }

        internal bool ByRevitSharedParameterFile(string filePath)
        {
            using (StreamReader sr = new StreamReader(filePath, new UnicodeEncoding(false, true)))
            {
                string data = sr.ReadToEnd();
                isValid = ReadSharedParameterFileContent(data, out _data);
            }

            return isValid;
        }

        private static bool TableStringFromObject(IList<IList<object>> objValues, out List<List<string>> strValues)
        {
            strValues = new List<List<string>>();
            if (objValues.Count < 2)
            {
                return false;
            }

            if (objValues.Any(x => x.Count != objValues.First().Count))
            {
                return false;
            }

            foreach (IList<object> objRow in objValues)
            {
                List<string> strRow = objRow.Select(x => $"{x}").ToList();
                strValues.Add(strRow);
            }

            return true;
        }

        internal static bool TableFromSharedParameterFile(string text, out List<List<string>> table)
        {
            table = new List<List<string>>();
            if (string.IsNullOrEmpty(text))
            {
                Logger.Error($"ФОП: пусто");
                return false;
            }

            List<string> rows = text
                .Split(new[] { _R + _N }, StringSplitOptions.None)
                .Where(x => !x.StartsWith("#"))
                .ToList();

            if (rows.Count < 1)
            {
                Logger.Error($"ФОП: строк {rows.Count}");
                return false;
            }

            Dictionary<string, string> groups = rows
                .Where(row => row.StartsWith(_Group))
                .Select(row => row.Split(_T).Skip(1))
                .ToDictionary(x => x.First(), x => x.Last());

            if (groups.Keys.Count < 1)
            {
                Logger.Error($"ФОП: группы не считаны");
                return false;
            }

            string parTableKeysRow = rows.FirstOrDefault(x => x.StartsWith(_XParam));
            if (string.IsNullOrEmpty(parTableKeysRow))
            {
                Logger.Error($"ФОП: Строка с ключами параметров не обнаружена");
                return false;
            }

            List<string> parTableKeys = parTableKeysRow.Split(_T).Skip(1).ToList();
            int groupFieldIndex = parTableKeys.IndexOf(_Group);
            if (groupFieldIndex == -1)
            {
                Logger.Error($"ФОП: Столбец с группами не обнаружен");
                return false;
            }

            List<List<string>> parTableData = rows.Where(x => x.StartsWith(_Param))
                .Select(x => x.Split(_T).Skip(1).ToList())
                .Where(x => x.Count >= groupFieldIndex - 1)
                .ToList();

            foreach (var rowData in parTableData)
            {
                string groupId = rowData[groupFieldIndex];
                if (!groups.TryGetValue(groupId, out var groupName))
                {
                    Logger.Error($"ФОП: имя группы \'{groupId}\' не найдено");
                    return false;
                }
                else
                {
                    rowData[groupFieldIndex] = groupName;
                }
            }

            if (parTableData.Count < 1)
            {
                Logger.Error($"ФОП: ни один параметр не считан");
                return false;
            }

            table.Add(parTableKeys);
            table.AddRange(parTableData);
            Logger.Info($"Из ФОП считано {parTableData.Count} строк с параметрами");

            return TableValid(table);
        }

        private static bool ReadSharedParameterFileContent(string text, out ObservableCollection<Item> items)
        {
            items = new ObservableCollection<Item>();
            if (string.IsNullOrEmpty(text))
            {
                Logger.Error($"ФОП: пусто");
                return false;
            }

            List<string> rows = text
                .Split(new[] { _R + _N }, StringSplitOptions.None)
                .Where(x => !x.StartsWith("#"))
                .ToList();

            if (rows.Count < 1)
            {
                Logger.Error($"ФОП: строк {rows.Count}");
                return false;
            }

            Dictionary<string, string> groups = rows
                .Where(row => row.StartsWith(_Group))
                .Select(row => row.Split(_T).Skip(1))
                .ToDictionary(x => x.First(), x => x.Last());

            if (groups.Keys.Count < 1)
            {
                Logger.Error($"ФОП: группы не считаны");
                return false;
            }

            string parTableKeysRow = rows.FirstOrDefault(x => x.StartsWith(_XParam));
            if (string.IsNullOrEmpty(parTableKeysRow))
            {
                Logger.Error($"ФОП: Строка с ключами параметров не обнаружена");
                return false;
            }

            List<string> parTableKeys = parTableKeysRow.Split(_T).Skip(1).ToList();
            int groupFieldIndex = parTableKeys.IndexOf(_Group);
            if (groupFieldIndex == -1)
            {
                Logger.Error($"ФОП: Столбец с группами не обнаружен");
                return false;
            }

            List<List<string>> parTableData = rows.Where(x => x.StartsWith(_Param))
                .Select(x => x.Split(_T).Skip(1).ToList())
                .Where(x => x.Count >= groupFieldIndex - 1)
                .ToList();

            foreach (var rowData in parTableData)
            {
                string groupId = rowData[groupFieldIndex];
                if (!groups.TryGetValue(groupId, out var groupName))
                {
                    Logger.Error($"ФОП: имя группы \'{groupId}\' не найдено");
                    return false;
                }
                else
                {
                    rowData[groupFieldIndex] = groupName;
                }
            }

            if (parTableData.Count < 1)
            {
                Logger.Error($"ФОП: ни один параметр не считан");
                return false;
            }

            List<List<string>> parTable = new List<List<string>>() { parTableKeys };
            parTable.AddRange(parTableData);
            Logger.Info($"Из ФОП считано {parTableData.Count} строк с параметрами");

            return ParamsFromTable(parTable, out items);
        }

        private static bool ParamsFromTable(List<List<string>> parTable, out ObservableCollection<Item> items)
        {
            items = new ObservableCollection<Item>();

            if (!TableValid(parTable))
            {
                return false;
            }

            IList<string> keys = parTable.First();
            foreach (IList<string> values in parTable.Skip(1))
            {
                Item item = new Item();
                for (int i = 0; i < keys.Count; i++)
                {
                    if (!item.SetStringField(keys[i], values[i]))
                    {
                        break;
                    }
                }
                if (item.IsValid && item.Validate())
                {
                    items.Add(item);
                }
                else
                {
                    Logger.Error($"Пропущена строка: \'{string.Join(_T.ToString(), values)}\'");
                    continue;
                }
            }
            return ItemsValidate(items);
        }

        internal void OrderElements()
        {
            _data = new ObservableCollection<Item>(_data
                .OrderBy(x => x.Status)
                .ThenBy(x => x.Name)
                .ThenBy(x => x.Group)
                .ToList());
        }

        private static bool TableValid(List<List<string>> parTable)
        {
            if (parTable.Count < 2)
            {
                Logger.Error($"Таблица параметров: недостаточно данных для считывания");
                return false;
            }

            IList<string> keys = parTable.First();
            if (!keys.ToHashSet().IsSupersetOf(new List<string>
                { _Guid, _Name, _Type, _Vis, _Descr, _UserMod, _HideNoVal }))
            {
                Logger.Error($"Таблица параметров: в заголовках недостаточный набор столбцов");
                return false;
            }

            if (parTable.Skip(1).Any(x => x.Count != parTable.First().Count))
            {
                Logger.Error($"Таблица параметров: количество заголовков и значений в строках отличается");
                return false;
            }

            Logger.Info($"Успешно считано {parTable.Count - 1} параметров");
            return true;
        }

        private static bool ItemsValidate(ICollection<Item> items)
        {
            if (items.Count == 0)
            {
                Logger.Error($"Не удалось считать ни одного параметра");
                return false;
            }

            if (items.Any(item => !item.IsValid))
            {
                Logger.Warning($"Ошибки найдены в {items.Count(item => !item.IsValid)} из {items.Count}");
                return false;
            }

            // Поиск дубликатов Guid
            if (FindAndPrintDuplicates(items, x => x.GuidValue.ToString(), _Guid))
            {
                return false;
            }

            // Поиск дубликатов Name
            FindAndPrintDuplicates(items, x => x.Name, _Name);

            return true;
        }

        private static bool FindAndPrintDuplicates<T>(ICollection<Item> items,
            Func<Item, T> propertySelector, string propertyName)
        {
            List<T> duplicates = items.GroupBy(propertySelector)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key).ToList();

            if (duplicates.Count > 0)
            {
                foreach (var duplicate in duplicates)
                {
                    Logger.Warning($"{propertyName} \'{duplicate}\': найден дубликат");
                }

                return true;
            }

            return false;
        }
    }
}
