using System;
using System.Configuration;
using System.IO;

namespace Rebus.AzureServiceBus.Tests
{
    static class TsTestConfig
    {
        static TsTestConfig()
        {
            ConnectionString = GetConnectionString();
            
            //ConnectionStringFromFileOrNull(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asb_connection_string.txt"))
            //                   ?? ConnectionStringFromEnvironmentVariable("rebus2_asb_connection_string")
            //                   ?? throw new ConfigurationErrorsException("Could not find Azure Service Bus connection string!");
        }

        static string GetConnectionString()
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ts_connection_string.txt");

            if (File.Exists(filePath))
            {
                return ConnectionStringFromFile(filePath);
            }

            const string variableName = "rebus2_ts_connection_string";
            var environmentVariable = Environment.GetEnvironmentVariable(variableName);

            if (!string.IsNullOrWhiteSpace(environmentVariable)) return ConnectionStringFromEnvironmentVariable(variableName);

            throw new ApplicationException($@"Could not get Table Storage connection string. Tried to load from file

    {filePath}

but the file did not exist. Also tried to get the environment variable named

    {variableName}

but it was empty (or didn't exist).

Please provide a connection string through one of the methods mentioned above.

");
        }

        public static string ConnectionString { get; }

        static string ConnectionStringFromFile(string filePath)
        {
            Console.WriteLine("Using Table Storage connection string from file {0}", filePath);
            return File.ReadAllText(filePath);
        }

        static string ConnectionStringFromEnvironmentVariable(string environmentVariableName)
        {
            var value = Environment.GetEnvironmentVariable(environmentVariableName);

            Console.WriteLine("Using Table Storage connection string from env variable {0}", environmentVariableName);

            return value;
        }

    }
}