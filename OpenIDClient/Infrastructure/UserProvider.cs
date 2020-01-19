namespace OpenIDClient.Infrastructure
{
    public static class UserProvider
    {
        private static User _user;
        public static User Current => _user ??= new User();
    }
}