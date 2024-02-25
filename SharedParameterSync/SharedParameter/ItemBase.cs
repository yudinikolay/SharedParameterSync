using System.Collections.Generic;
using System.ComponentModel;
using Shared;

namespace SharedParameter
{
    internal abstract class ItemBase : INotifyPropertyChanged
    {
        protected bool isValid;
        internal bool IsValid => isValid;

        // &#xD - символ возврата каретки \r или carriage return
        protected const string _CR = "&#xD";
        protected const string _R = "\r";

        // &#xA - символ перевода строки \n или line feed
        protected const string _LF = "&#xA";
        protected const string _N = "\n";

        // Значения для чтения строки параметра из ФОП
        protected const char _T = '\t';
        protected const string _XMeta = "*META";
        protected const string _Meta = "META";
        protected const string _XGroup = "*GROUP";
        protected const string _XParam = "*PARAM";
        protected const string _Param = "PARAM";

        protected const string _Id = "ID";

        // Значения полей по умолчанию
        protected const string _GuidInvalid = "00000000-0000-0000-0000-000000000000";
        protected const string _BoolInvalid = "-1";

        // Поля параметров в ФОП
        protected const string _Guid = "GUID";
        protected const string _Name = "NAME";
        protected const string _Type = "DATATYPE";
        protected const string _Cat = "DATACATEGORY";
        protected const string _Group = "GROUP";
        protected const string _Vis = "VISIBLE";
        protected const string _Descr = "DESCRIPTION";
        protected const string _UserMod = "USERMODIFIABLE";
        protected const string _HideNoVal = "HIDEWHENNOVALUE";

        /// <summary>
        /// Единственно верный порядок и состав полей параметра в ФОП
        /// </summary>
        protected readonly List<string> Keys = new List<string>
        {
            _Guid, _Name, _Type, _Cat, _Group, _Vis, _Descr, _UserMod, _HideNoVal
        };

        protected static bool StrToInt(string strVal, out int intVal)
        {
            if (!int.TryParse(strVal, out intVal))
            {
                Logger.Error($"\'{strVal}\' не целое число");
                return false;
            }

            return true;
        }

        protected static bool StrToBool(string input, out bool isBool)
        {
            isBool = true;
            switch (input)
            {
                case "0":
                    return false;
                case "1":
                    return true;
                case "TRUE":
                    return true;
                case "FALSE":
                    return false;
                default:
                    isBool = false;
                    return false;
            }
        }

        internal enum State
        {
            [Description("Удалён")] Removed,
            [Description("Добавлён")] Added,
            [Description("Изменён")] Fixed,
            [Description("ОК")] Ok,
        }

        protected Dictionary<State, string> StateDescription = new Dictionary<State, string>()
        {
            {State.Ok, "Нет"},
            {State.Fixed, "Обновить"},
            {State.Added, "Добавить"},
            {State.Removed, "Удалить"}
        };

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler eventHandler = this.PropertyChanged;
            if (eventHandler == null) return;
            eventHandler((object)this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
