using System;
using System.Security.Cryptography;

namespace OpenIDClient.Infrastructure
{
    public class User
    {
        public User()
        {
            var id = new byte[20];
            RandomNumberGenerator.Create().GetBytes(id);
            SessionId = Convert.ToBase64String(id);
        }

        public string SessionId { get; }
    }
}