using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SdoConnect
{
    /// <summary>
    /// Логгер класса <see cref="SdoConnecter"/>
    /// </summary>
    public class SdoLogger
    {
        private readonly SdoConnecter connecter;
        /// <summary>
        /// Конструктор логгера класса <see cref="SdoConnecter"/>
        /// </summary>
        /// <param name="sdoConnecter"></param>
        public SdoLogger(SdoConnecter sdoConnecter)
        {
            connecter = sdoConnecter;
            listenEvents();
            connecter.AuthorizeClients();

        }
        /// <summary>
        /// Слушать события <see cref="SdoConnecter"/>
        /// </summary>
        private void listenEvents()
        {
            connecter.AddedClients += SdoAddedClients;//Слушать событие добавления всех клиентов
            connecter.EndAuthorizeClient += SdoEndAuthorizeClient;//Слушать событие завершение авторизации пользователя
        }
        /// <summary>
        /// Вывести сообщение о том, как прошла авторизация клиента
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SdoEndAuthorizeClient(object sender, EventArgs e)
        {
            ClientAuthorizrEvenArgs args = (ClientAuthorizrEvenArgs)e;
            if (args.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Пользователь {args.User.Login} успешно авторизован");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Неправильный логин или пароль {args.User.Login}");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        /// <summary>
        /// Вывести сообщение о том, сколько авторизовано клиентов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SdoAddedClients(object sender, EventArgs e)
        {
            Console.WriteLine($"Добавлено {((List<HttpClient>)sender).Count} клиентов");
        }
    }
}
