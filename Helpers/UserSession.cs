using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DocumentManagerApp.Models;

namespace DocumentManagerApp.Helpers
{
    public static class UserSession
    {
        public static User? CurrentUser { get; private set; }
        public static void Login(User user)
        {
            CurrentUser = user;
        }
        public static void Logout()
        {
            CurrentUser = null;
        }
        public static bool IsLoggedIn => CurrentUser != null;

        public static bool IsAdmin => CurrentUser?.Role == "admin";
    }
}


