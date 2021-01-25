using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SdoConnect
{
    /// <summary>
    /// Аргументы события авторизации пользователя
    /// </summary>
    public class ClientAuthorizrEvenArgs : EventArgs
    {
        private bool success;
        private User user;

        /// <summary>
        /// Аргументы события авторизации пользователя
        /// </summary>
        /// <param name="success">Успешно ли прошла авторизация</param>
        /// <param name="user">Пользователь, с помощью учётных данных которого производилась авторизация</param>
        public ClientAuthorizrEvenArgs(bool success, User user)
        {
            Success = success;
            User = user;
        }
        /// <summary>
        /// Успешная авторизация
        /// </summary>
        public bool Success { get => success; private set => success = value; }
        /// <summary>
        /// Пользователь с помощью которого производилась авторизация
        /// </summary>
        public User User { get => user; private set => user = value; }
    }
    
    /// <summary>
    /// Модель пользователя авторизации
    /// </summary>
    public class User
    {
        private string _login;
        private string _password;
        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="login">Логин учётной записи sdo.srspu.ru</param>
        /// <param name="password">Пароль от учётной записи sdo.srspu.ru</param>
        public User(string login, string password)
        {
            Login = login;
            Password = password;
        }
        /// <summary>
        /// Логин учётной записи sdo.srspu.ru
        /// </summary>
        public string Login { get => _login; set => _login = value; }
        /// <summary>
        /// Пароль от учётной записи sdo.srspu.ru
        /// </summary>
        public string Password { get => _password; set => _password = value; }
    }
    /// <summary>
    /// Класс асинхронной авторизации пользователей в системе sdo.srspu.ru
    /// </summary>
    public class SdoConnecter
    {
        #region privateFields
        private List<User> users;
        #endregion
        /// <summary>
        /// Парсер Anglesharp
        /// </summary>
        private HtmlParser parser = new HtmlParser();
        /// <summary>
        /// URL страницы авторизации sdo.srspu.ru
        /// </summary>
        private readonly string loginPageUrl = "https://sdo.srspu.ru/login/index.php";
        
        /// <summary>
        /// Список авторизованных клиентов системы
        /// </summary>
        private List<HttpClient> clients;

        /// <summary>
        /// Изначальный список пользователей
        /// </summary>
        public List<User> Users { get => users; private set => users = value; }

        #region events
        /// <summary>
        /// Срабатывает когда завершено добавление клиентов
        /// </summary>
        public event EventHandler AddedClients;
        /// <summary>
        /// Срабатывает при завершении авторизации клиента
        /// </summary>
        public event EventHandler EndAuthorizeClient;
        #endregion

        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="users">Список пользователей, которых нужно авторизовать в системе</param>
        public SdoConnecter(List<User> users)
        {
            clients = new List<HttpClient>();
            Users = users;
        }
        /// <summary>
        /// Авторизация пользователей users и сохранение авторизованных клиентов в clients
        /// </summary>
        public void AuthorizeClients()
        {
            //Список задач на получение авторизованных клиентов
            List<Task<HttpClient>> getClientTasks = new List<Task<HttpClient>>();
            foreach (var user in Users)//Перебор пользователей
            {
                getClientTasks.Add(getAuthorizedClient(user));//Добавление асинхронной задачи на получение авторизованного клиента
            }
            Task.WaitAll(getClientTasks.ToArray());//Ожидать пока все пользователи произведут авторизацию
            foreach (var getClientTask in getClientTasks)//Перебор списка задач на получение клиента 
            {
                if(getClientTask.Result != null)//Если клиент не  null (авторизация не прошла)
                {
                    clients.Add(getClientTask.Result);//Добавить клиента в список
                }
            }
            AddedClients?.Invoke(clients, null);//Вызвать событие завершения добавления клиентов
        }

        /// <summary>
        /// Возвращает авторизованного клиента или null, если авторизация не удалась
        /// </summary>
        /// <param name="user">Учётные данные авторизации</param>
        /// <returns>
        /// <para><see cref="Task">Task</see> <see cref="HttpClient">HttpClient</see>, если авторизация успешная</para>
        /// <para><c>null</c>, если авторизация прошла неуспешно</para>
        /// </returns>
        private async Task<HttpClient> getAuthorizedClient(User user)
        {
            //Обработчик пользователя, в котором устанавливается режим, сохранения cookies
            var handler = new HttpClientHandler() { UseCookies = true };
            var client = new HttpClient(handler);//Создаём нового клиента и передаём ему handler

            //Назначение заголовков запросов клиента по умолчанию
            Action setDefaultHeaders = new Action(() => {
                client.DefaultRequestHeaders.Add("Host", "sdo.srspu.ru");//Хост ресурса
                                                                         //Фейковый заголовок, устанавливающий, что запрос происходит из браузера Mozilla Firefox
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:84.0) Gecko/20100101 Firefox/84.0");
                //Постоянное соединение
                client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            });
            setDefaultHeaders();

            //Асинхронный запрос к странице авторизации
            var getRequest = await client.GetAsync(loginPageUrl);

            //Асинхронный парсинг результата запроса к странице авторизации
            var document = await parser.ParseDocumentAsync(await getRequest.Content.ReadAsStringAsync());
            //Получаем logintoken из скрытого тега
            var logintoken = document.QuerySelector("input[name=logintoken]").GetAttribute("value");

            //Формируем авторизационные данные, для отправки POST запросом
            List<KeyValuePair<string, string>> keyValues = new List<KeyValuePair<string, string>>() {
             new KeyValuePair<string, string>("anchor", ""),//Якорь?
             new KeyValuePair<string, string>("logintoken", logintoken),//Логинтокен
             new KeyValuePair<string, string>("username", user.Login),//Логин
             new KeyValuePair<string, string>("password", user.Password),//Пароль
            };
            //Формируем тело для POST запроса из авторизоционных данных
            FormUrlEncodedContent content = new FormUrlEncodedContent(keyValues);
            var postRequest = await client.PostAsync(loginPageUrl, content);//POST запрос к странице авторизации
            var postRequestDocument = parser.ParseDocument(postRequest.Content.ReadAsStringAsync().Result);//Страница, которую вернул POST запрос
            var errorTag = postRequestDocument.QuerySelector("a#loginerrormessage");//Ищем на ней тег ошибки
            bool successAuthorize;//Прошла ли авторизация успешно
            if (errorTag != null)//Если такой тег найден, авторизация не удалась
            {
                successAuthorize = false;
                EndAuthorizeClient?.Invoke(null, new ClientAuthorizrEvenArgs(successAuthorize, user));
                return null;
            }
            else
            {
                successAuthorize = true;
                EndAuthorizeClient?.Invoke(null, new ClientAuthorizrEvenArgs(successAuthorize, user));
                return client;
            }

        }
    }
}
