using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Text;

namespace Google
{
    internal class SpreadsheetConnector

    {
        private const string ApplicationName = "Google Sheets Reader v.1";
        private const string TabSeparator = "\t";

        /// <summary>
        /// Получение экземпляра SheetsService с использованием учетных данных и токена.
        /// </summary>
        /// <param name="credentialsPath">Путь к файлу с учетными данными.</param>
        /// <param name="tokenPath">Путь к файлу с токеном.</param>
        /// <returns>Экземпляр SheetsService.</returns>
        private static SheetsService GetService(string credentialsPath, string tokenPath, FileMode fileMode, FileAccess fileAccess)
        {
            // Установка необходимых областей доступа
            // В данном случае только к гугл таблицам.
            string[] scopes = { SheetsService.Scope.Spreadsheets };

            UserCredential credential;

            // Авторизация с использованием учетных данных и получение учетной записи пользователя.
            using (FileStream stream = new FileStream(credentialsPath, fileMode, fileAccess))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(tokenPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + tokenPath);
            }

            // Создание экземпляра SheetsService с использованием учетных данных и имени приложения.
            SheetsService service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
            return service;
        }

        /// <summary>
        /// Получение экземпляра SheetsService с использованием стандартных путей к файлам учетных данных и токена.
        /// </summary>
        /// <returns>Экземпляр SheetsService.</returns>
        private static SheetsService GetServiceReadOnly()
        {
            string credentialsPath = @"credentials.json";
            string tokenPath = @"token.json";
            return GetService(credentialsPath, tokenPath, FileMode.Open,FileAccess.Read);
        }

        private static SheetsService GetServiceWrite()
        {
            string credentialsPath = @"credentials.json";
            string tokenPath = @"token.json";
            return GetService(credentialsPath, tokenPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }

        /// <summary>
        /// Чтение диапазона данных из таблицы.
        /// </summary>
        /// <param name="documentId">Идентификатор документа.</param>
        /// <param name="range">Диапазон данных для чтения.</param>
        /// <returns>Список списков объектов, представляющих значения из таблицы.</returns>
        public static IList<IList<object>> ReadRangeFromTable(string documentId, string range)
        {
            // Получение сервиса Sheets.
            SheetsService service = GetServiceReadOnly();

            // Создание запроса на чтение данных из таблицы.
            SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get(documentId, range);

            // Выполнение запроса и получение ответа.
            ValueRange response = request.Execute();


            // Получение имени документа и имени листа.
            string documentName = request?.Range;

            // Вывод информации об имени документа и имени листа в консоль.
            Console.WriteLine("Имя документа: " + documentName);

            // Возвращение списка списков объектов, представляющих значения из таблицы.
            return response.Values;
        }

        /// <summary>
        /// Класс для форматирования строк таблицы.
        /// </summary>
        private class TableFormatter
        {
            /// <summary>
            /// Форматирует строку таблицы путем объединения значений с использованием разделителя.
            /// </summary>
            /// <param name="values">Значения, которые необходимо объединить в строку.</param>
            /// <returns>Отформатированная строка таблицы.</returns>
            public string FormatRow(IEnumerable<object> values)
            {
                // Используется метод string.Join для объединения значений с использованием разделителя.
                // В данном случае разделителем является TabSeparator.
                return string.Join(TabSeparator, values);
            }
        }

        /// <summary>
        /// Записывает таблицу в файл.
        /// </summary>
        /// <param name="table">Таблица, представленная в виде списка списков объектов.</param>
        /// <param name="resultPath">Путь к файлу, в который будет записана таблица.</param>
        public static void WriteTableToFile(IList<IList<object>> table, string resultPath)
        {
            // Используется блок using для автоматического закрытия StreamWriter после использования.
            using (StreamWriter sw = new StreamWriter(resultPath, false, new UnicodeEncoding(false, true)))
            {
                TableFormatter tableFormatter = new TableFormatter();

                // Перебираем каждую строку таблицы.
                foreach (var row in table)
                {
                    // Проверяем, что в строке есть хотя бы один элемент.
                    if (row.Count >= 1)
                    {
                        // Форматируем строку таблицы с помощью TableFormatter.
                        string newLine = tableFormatter.FormatRow(row);

                        // Записываем отформатированную строку в файл.
                        sw.WriteLine(newLine);
                    }
                }
            }
        }

        public static void WriteRangeToTable(string documentId, string range, IList<IList<object>> values)
        {
            SheetsService service = GetServiceWrite();
            ValueRange valueRange = new ValueRange
            {
                Values = values
            };

            SpreadsheetsResource.ValuesResource.UpdateRequest request = service.Spreadsheets.Values.Update(valueRange, documentId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            UpdateValuesResponse response = request.Execute();

            Console.WriteLine("Данные успешно записаны в таблицу.");
        }
    }
}
