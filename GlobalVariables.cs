namespace KMC.EventClient.Helpers
{
    public static class GlobalVariables
    {
        public static string ApiBaseUrl { get; set; } = "https://localhost:7045/";
        public static string Token { get; set; } = string.Empty;
        public static string UserName { get; set; } = string.Empty;
        public static string UserRole { get; set; } = string.Empty;
    }
}