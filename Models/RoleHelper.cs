namespace EIUBetApp.Models
{
    public static class RoleHelper
    {
        public const int Admin = 1;
        public const int Player = 2;

        public static string GetRoleName(int role)
        {
            return role switch
            {
                Admin => "Admin",
                Player => "Player",
                _ => "Unknown"
            };
        }
    }

}
