using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Shared;


namespace SharedParameter
{
    internal class Item : ItemBase
    {
        internal Item()
        {
            isValid = true;
        }

        /// <summary>
        /// Хранилище данных параметра ориентированное на структуру строки ФОП,
        /// за исключением поля GROUP в коем здесь подразумевается имя группы
        /// </summary>
        internal Dictionary<string, string> Data => _data;

        private Dictionary<string, string> _data = new Dictionary<string, string>()
        {
            { _Guid, _GuidInvalid },
            { _Name, string.Empty },
            { _Type, string.Empty },
            { _Cat, string.Empty },
            { _Group, string.Empty },
            { _Vis, _BoolInvalid },
            { _Descr, string.Empty },
            { _UserMod, _BoolInvalid },
            { _HideNoVal, _BoolInvalid }
        };


        internal bool SetStringField(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return false;
            switch (key)
            {
                case _Guid: return SetGuid(value);
                case _Name: return SetName(value);
                case _Type: return SetDataType(value);
                case _Cat: return SetDataCategory(value);
                case _Group: return SetGroup(value);
                case _Vis: return SetIsVisible(value);
                case _Descr: return SetDescription(value);
                case _UserMod: return SetUserModifiable(value);
                case _HideNoVal: return SetHideWhenNoValue(value);
                default:
                    // TODO: Скорее всего стоит пропускать левые поля
                    Logger.Warning($"Поле {key}: не распознано");
                    return false;
            }
        }

        /// <summary>
        /// Проверить целостность данных
        /// </summary>
        /// <returns>Результат проверки</returns>
        internal bool Validate()
        {
            if (!isValid) return isValid;
            if (_data.Keys.Count != Keys.Count && _data.Keys.ToHashSet().IsSupersetOf(Keys))
            {
                Logger.Warning($"Валидация: ключей {_data.Keys.Count} ");
                isValid = false;
                return false;
            }

            foreach (string fieldName in new List<string>() { _Guid, _Name, _Type, _Group, _Vis, _UserMod, _HideNoVal })
            {
                if (string.IsNullOrEmpty(_data[fieldName]))
                {
                    Logger.Error($"Валидация: поле \'{fieldName}\' пусто");
                    isValid = false;
                    return false;
                }
            }

            if (_data[_Guid] == _GuidInvalid || !Guid.TryParse(_data[_Guid], out _))
            {
                Logger.Error($"Валидация {_Guid}: \'{_data[_Guid]}\' некорректный Guid");
                isValid = false;
                return false;
            }

            foreach (string fieldName in new List<string>() { _Vis, _UserMod, _HideNoVal })
            {
                if (!StrIsBool(_data[fieldName]))
                {
                    Logger.Error($"Валидация {fieldName}: \'{_data[fieldName]}\' не логично");
                    isValid = false;
                    return false;
                }
            }

            return isValid;
        }

        #region Свойства

        /// <summary>
        /// Guid общего параметра
        /// </summary>
        internal Guid GuidValue
        {
            get => Guid.Parse(GetGuid());
            set => SetGuid(value);
        }

        /// <summary>
        /// Имя параметра
        /// </summary>
        public string Name
        {
            get => GetName();
            set => SetName(value);
        }

        /// <summary>
        /// Тип данных параметра.
        /// TODO: Надо бы добавить проверку типов.
        /// </summary>
        internal string DataType
        {
            get => GetDataType();
            set => SetDataType(value);
        }

        /// <summary>
        /// Категория данных (почему-то всегда пустой)
        /// </summary>
        internal string DataCategory
        {
            get => GetDataCategory();
            set => SetDataCategory(value);
        }

        /// <summary>
        /// Группа параметра
        /// </summary>
        public string Group
        {
            get => GetGroup();
            set => SetGroup(value);
        }

        /// <summary>
        /// Видимость параметра
        /// </summary>
        internal bool IsVisible
        {
            get => StrToBool(GetIsVisible());
            set => SetIsVisible(value);
        }

        /// <summary>
        /// Описание параметра
        /// </summary>
        internal string Description
        {
            get => GetDescription();
            set => SetDescription(value);
        }

        /// <summary>
        /// Редактируемый
        /// </summary>
        internal bool UserModifiable
        {
            get => StrToBool(GetUserModifiable());
            set => SetUserModifiable(value);
        }

        /// <summary>
        /// Скрывать, если нет значения
        /// </summary>
        internal bool HideWhenNoValue
        {
            get => StrToBool(GetHideWhenNoValue());
            set => SetHideWhenNoValue(value);
        }

        #endregion

        #region Методы

        internal string GetGuid()
        {
            return GetValueByKey(_Guid);
        }

        internal string GetName()
        {
            return GetValueByKey(_Name);
        }

        internal string GetDataType()
        {
            return GetValueByKey(_Type);
        }

        internal string GetDataCategory()
        {
            return GetValueByKey(_Cat);
        }

        internal string GetGroupId(List<string> groups)
        {
            return $"{groups.IndexOf(GetGroup()) + 1}";
        }

        internal string GetGroup()
        {
            return GetValueByKey(_Group);
        }

        internal string GetIsVisible()
        {
            return GetValueByKey(_Vis);
        }

        internal string GetDescription()
        {
            return GetValueByKey(_Descr).Replace(_R, _CR).Replace(_N, _LF);
        }

        internal string GetUserModifiable()
        {
            return GetValueByKey(_UserMod);
        }

        internal string GetHideWhenNoValue()
        {
            return GetValueByKey(_HideNoVal);
        }

        internal string GetValueByKey(string key)
        {
            if (!_changes.TryGetValue(key, out var value) || string.IsNullOrEmpty(value))
            {
                if (!_data.TryGetValue(key, out value) || string.IsNullOrEmpty(value))
                {
                    return string.Empty;
                }
            }

            return value;
        }

        internal bool SetGuid(string value)
        {
            if (!SetGuid(value, ref _data))
            {
                isValid = false;
                return false;
            }
            return true;
        }

        internal bool SetGuid(Guid value)
        {
            return SetGuid(value.ToString());
        }

        internal bool SetName(string value)
        {
            return SetStringValue(_Name, value, ref _data);
        }

        internal bool SetDataType(string value)
        {
            return SetStringValue(_Type, value, ref _data);
        }

        internal bool SetDataCategory(string value)
        {
            return SetStringValue2(_Cat, value, ref _data);
        }

        internal bool SetGroup(string value)
        {
            return SetStringValue(_Group, value, ref _data);
        }

        internal bool SetIsVisible(string value)
        {
            return SetBoolValue(_Vis, value, ref _data);
        }

        internal bool SetIsVisible(bool value)
        {
            return SetBoolValue(_Vis, value, ref _data);
        }

        internal bool SetDescription(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return true;
            }

            return SetStringValue2(_Descr, value.Replace(_CR, _R).Replace(_LF, _N), ref _data);
        }

        internal bool SetUserModifiable(string value)
        {
            return SetBoolValue(_UserMod, value, ref _data);
        }

        internal bool SetUserModifiable(bool value)
        {
            return SetBoolValue(_UserMod, value, ref _data);
        }

        internal bool SetHideWhenNoValue(string value)
        {
            return SetBoolValue(_HideNoVal, value, ref _data);
        }

        internal bool SetHideWhenNoValue(bool value)
        {
            return SetBoolValue(_HideNoVal, value, ref _data);
        }


        /// <summary>
        /// Записать в словарь строку Guid
        /// </summary>
        /// <param name="value">Значение</param>
        /// <param name="dict">Словарь для записи значения</param>
        /// <returns>Статус выполнения</returns>
        private static bool SetGuid(string value, ref Dictionary<string, string> dict)
        {
            if (IsEmpty(_Guid, value))
            {
                return false;
            }

            try
            {
                Guid.TryParse(value, out _);
                dict[_Guid] = value;
                return true;
            }
            catch
            {
                Logger.Warning($"Поле {_Guid}: не удалось считать Guid из строки");
                return false;
            }
        }


        /// <summary>
        /// Записывает строковое значение в словарь по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="value">Значение</param>
        /// <param name="dict">Словарь</param>
        /// <returns>Успешно</returns>
        /// <remarks>Не допускает пустых значений</remarks>
        private static bool SetStringValue(string key, string value, ref Dictionary<string, string> dict)
        {
            return !IsEmpty(key, value) && SetStringValue2(key, value, ref dict);
        }

        /// <summary>
        /// Записывает строковое значение в словарь по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="value">Значение</param>
        /// <param name="dict">Словарь</param>
        /// <returns>Успешно</returns>
        /// <remarks>Допускает пустые значения</remarks>
        private static bool SetStringValue2(string key, string value, ref Dictionary<string, string> dict)
        {
            dict[key] = value;
            return true;
        }

        /// <summary>
        /// Назначает значение по ключу в предоставленный словарь
        /// </summary>
        /// <param name="key">ключ</param>
        /// <param name="value">значение</param>
        /// <param name="dict">словарь для записи</param>
        /// <returns>Успешно</returns>
        private static bool SetBoolValue(string key, bool value, ref Dictionary<string, string> dict)
        {
            dict[key] = value ? "1" : "0";
            return true;
        }

        /// <summary>
        /// Назначает значение по ключу в предоставленный словарь
        /// </summary>
        /// <param name="key">ключ</param>
        /// <param name="value">значение</param>
        /// <param name="dict">словарь для записи</param>
        /// <returns>Успешно</returns>
        private static bool SetBoolValue(string key, string value, ref Dictionary<string, string> dict)
        {
            if (IsEmpty(key, value))
            {
                return false;
            }

            bool boolValue = StrToBool(value, out bool isBool);
            if (!isBool)
            {
                Logger.Warning($"Поле {key}: Не удалось считать логическое значение из \'{value}\'");
                return false;
            }

            dict[key] = boolValue ? "1" : "0";
            return true;
        }

        private static bool StrIsBool(string value)
        {
            StrToBool(value, out bool isBool);
            return isBool;
        }

        private bool StrToBool(string input)
        {
            bool value = StrToBool(input, out bool isBool);
            if (isBool) return value;
            isValid = false;
            return false;
        }

        /// <summary>
        /// Проверяет что обязательное значение на пустоту
        /// </summary>
        /// <param name="key">Ключ для сообщения об ошибке</param>
        /// <param name="value">Значение</param>
        /// <returns>Результат проверки</returns>
        /// <remarks>Так же выводит в лог сообщение об ошибке</remarks>
        private static bool IsEmpty(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                Logger.Error($"Поле {key}: Пусто");
                return true;
            }

            return false;
        }

        #endregion

        #region Обновление, изменения, сравнения

        internal State Status { get; set; } = State.Ok;
        public string StatusText => GetStatusDescription(Status);

        private string GetStatusDescription(State state)
        {
            if (!StateDescription.TryGetValue(Status, out string value)) return state.ToString();
            return value;
        }

        public string Info => GetInfo();
        private string GetInfo()
        {
            string info = string.Empty;
            foreach (string key in Keys.ToList())
            {
                bool isChanged = _changes.TryGetValue(key, out _);
                if (string.IsNullOrEmpty(_data[key]) && !isChanged) continue;
                info += $"{(isChanged ? "!ИЗМЕНЕНО! " : string.Empty)}{key}: {_data[key]}{ChangesField(key)}\n";
            }
            return info;
        }

        private string ChangesField(string key)
        {
            if (_changes.TryGetValue(key, out string value))
            {
                return $" → {value}";
            }
            return string.Empty;
        }

        private bool _changesAllowed = false;
        public bool ChangesAllowed
        {
            get => _changesAllowed;
            set
            {
                _changesAllowed = value;
                OnPropertyChanged(nameof(ChangesAllowed));
            }

        }

        internal Dictionary<string, string> Changes => _changes;

        private readonly Dictionary<string, string> _changes = new Dictionary<string, string>();

        internal void ApplyChanges()
        {
            if (Status == State.Ok || _changes.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<string, string> kvp in _changes)
            {
                _data[kvp.Key] = kvp.Value;
            }

            DiscardChanges();
        }

        internal void DiscardChanges()
        {
            _changes.Clear();
            Status = State.Ok;
        }

        internal bool ReadChanges(Item item)
        {
            if (!SameParameter(this, item, out bool isEqual) || isEqual)
            {
                return false;
            }

            foreach (string fieldKey in Keys)
            {
                ReadChanges(this, item, fieldKey);
                if (_changes.Keys.Count > 0) Status = State.Fixed;
            }

            return true;
        }


        private static bool SameParameter(Item item1, Item item2, out bool isEqual)
        {
            isEqual = false;
            if (!FieldsEquals(item1, item2, _Guid, out _))
            {
                return false;
            }

            if (item1._data.Keys.Count == item2._data.Keys.Count && item1._data.SequenceEqual(item2._data))
            {
                isEqual = true;
            }

            return true;
        }

        private static void ReadChanges(Item item1, Item item2, string field)
        {
            if (FieldsEquals(item1, item2, field, out string changed)) return;
            Logger.Info($"\'{item1.Name}\' - {field}: '{item1._data[field]}\' NOT \'{changed}\'");
            item1._changes.Add(field, changed);
        }

        private static bool FieldsEquals(Item item1, Item item2, string fieldName,
            out string changed)
        {
            changed = string.Empty;
            if (string.IsNullOrEmpty(item1._data[fieldName]) && string.IsNullOrEmpty(item2._data[fieldName]))
                return true;
            if (item1._data[fieldName].Equals(item2._data[fieldName], StringComparison.OrdinalIgnoreCase)) { return true; }
            changed = item2._data[fieldName];
            return false;
        }

        #endregion
    }
}