using SdoConnect;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace General
{

    class Program
    {
        static void Main(string[] args)
        {
            List<User> users = new List<User>() {
                new User("clackgot@gmail.com", "uVJ3e3Uf"),
                new User("samarkin20022002@gmail.com", "q54541c8"),
            };
            SdoConnecter sdo = new SdoConnecter(users);
            SdoLogger logger = new SdoLogger(sdo);
        }


    }
}
