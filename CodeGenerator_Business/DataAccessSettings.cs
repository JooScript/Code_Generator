﻿using Microsoft.Extensions.Configuration;

namespace CodeGenerator_Business
{
    public static class clsDataAccessSettings
    {
        private static readonly IConfigurationRoot _configuration;

        static clsDataAccessSettings()
        {
            try
            {
                var basePath = Directory.GetCurrentDirectory();
                var settingsPath = Path.Combine(basePath, "AppSettings.json");

                if (!File.Exists(settingsPath))
                {
                    throw new FileNotFoundException($"AppSettings.json not found at: {settingsPath}");
                }

                _configuration = new ConfigurationBuilder()
                    .SetBasePath(basePath)
                    .AddJsonFile("AppSettings.json", optional: false, reloadOnChange: true)
                    .Build();

                var testConnection = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(testConnection))
                {
                    throw new InvalidOperationException("Default Connection is missing in AppSettings.json");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize configuration. Error: {ex.Message}", ex);
            }
        }

        public enum enConnectionStringType
        {
            DefaultConnection = 1
        }

        public static string ConnectionString(enConnectionStringType ConnectionStringType = enConnectionStringType.DefaultConnection)
        {
            try
            {
                string connectionStringKey = ConnectionStringType.ToString();
                var connectionString = _configuration.GetConnectionString(connectionStringKey);

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException($"Connection string '{connectionStringKey}' is missing or empty in AppSettings.json.");
                }

                return connectionString;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get connection string. Error: {ex.Message}", ex);
            }
        }

        public static string AppName()
        {
            try
            {
                var appSettings = _configuration.GetSection("ApplicationSettings").Get<ApplicationSettings>();

                if (appSettings == null || string.IsNullOrEmpty(appSettings.AppName))
                {
                    throw new InvalidOperationException("ApplicationSettings section or AppName is missing or empty in AppSettings.json.");
                }

                return appSettings.AppName;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get AppName. Error: {ex.Message}", ex);
            }
        }

        public static string Version()
        {
            try
            {
                var appSettings = _configuration.GetSection("ApplicationSettings").Get<ApplicationSettings>();

                if (appSettings == null || string.IsNullOrEmpty(appSettings.Version))
                {
                    throw new InvalidOperationException("ApplicationSettings section or Version is missing or empty in AppSettings.json.");
                }

                return appSettings.Version;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get Version. Error: {ex.Message}", ex);
            }
        }

        public static string Environment()
        {
            try
            {
                var appSettings = _configuration.GetSection("ApplicationSettings").Get<ApplicationSettings>();

                if (appSettings == null || string.IsNullOrEmpty(appSettings.Environment))
                {
                    throw new InvalidOperationException("ApplicationSettings section or Environment is missing or empty in AppSettings.json.");
                }

                return appSettings.Environment;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get Environment. Error: {ex.Message}", ex);
            }
        }
    }

    internal class ApplicationSettings
    {
        public string AppName { get; set; }
        public string Version { get; set; }
        public string Environment { get; set; }
    }
}