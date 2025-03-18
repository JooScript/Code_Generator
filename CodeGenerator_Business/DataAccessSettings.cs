using Microsoft.Extensions.Configuration;

namespace CodeGenerator_Business
{
    public static class clsDataAccessSettings
    {
        private static readonly IConfigurationRoot _configuration;

        static clsDataAccessSettings()
        {
            try
            {
                _configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("AppSettings.json", optional: false, reloadOnChange: true)
                    .Build();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize configuration.", ex);
            }
        }

        public enum enConnectionStringType
        {
            DefaultConnection = 1
        }

        public static string ConnectionString(enConnectionStringType ConnectionStringType = enConnectionStringType.DefaultConnection)
        {
            if (_configuration == null)
            {
                throw new InvalidOperationException("Configuration is not initialized.");
            }

            string connectionStringKey = ConnectionStringType.ToString();
            var connectionString = _configuration.GetConnectionString(connectionStringKey);

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException($"Connection string '{connectionStringKey}' is missing or empty in AppSettings.json.");
            }

            return connectionString;
        }

        public static string AppName()
        {
            if (_configuration == null)
            {
                throw new InvalidOperationException("Configuration is not initialized.");
            }

            var appSettings = _configuration.GetSection("ApplicationSettings").Get<ApplicationSettings>();

            if (appSettings == null || string.IsNullOrEmpty(appSettings.AppName))
            {
                throw new InvalidOperationException("ApplicationSettings section or AppName is missing or empty in AppSettings.json.");
            }

            return appSettings.AppName;
        }

        public static string Version()
        {
            if (_configuration == null)
            {
                throw new InvalidOperationException("Configuration is not initialized.");
            }

            var appSettings = _configuration.GetSection("ApplicationSettings").Get<ApplicationSettings>();

            if (appSettings == null || string.IsNullOrEmpty(appSettings.Version))
            {
                throw new InvalidOperationException("ApplicationSettings section or Version is missing or empty in AppSettings.json.");
            }

            return appSettings.Version;
        }

        public static string Environment()
        {
            if (_configuration == null)
            {
                throw new InvalidOperationException("Configuration is not initialized.");
            }

            // Bind the ApplicationSettings section to the ApplicationSettings class
            var appSettings = _configuration.GetSection("ApplicationSettings").Get<ApplicationSettings>();

            if (appSettings == null || string.IsNullOrEmpty(appSettings.Environment))
            {
                throw new InvalidOperationException("ApplicationSettings section or Environment is missing or empty in AppSettings.json.");
            }

            return appSettings.Environment;
        }

    }

    internal class ApplicationSettings
    {
        public string? AppName { get; set; }
        public string? Version { get; set; }
        public string? Environment { get; set; }
    }

}