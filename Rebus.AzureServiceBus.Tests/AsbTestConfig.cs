using System;
using System.Configuration;
using System.IO;

namespace Rebus.AzureServiceBus.Tests
{
    static class AsbTestConfig
    {
        public static string ConnectionString => ConnectionStringFromFileOrNull(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asb_connection_string.txt"))
                                                 ?? ConnectionStringFromEnvironmentVariable("rebus2_asb_connection_string")
                                                 ?? throw new ConfigurationErrorsException("Could not find Azure Service Bus connection string!");

        static string ConnectionStringFromFileOrNull(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Could not find file {0}", filePath);
                return null;
            }

            Console.WriteLine("Using Azure Service Bus connection string from file {0}", filePath);
            return File.ReadAllText(filePath);
        }

        static string ConnectionStringFromEnvironmentVariable(string environmentVariableName)
        {
            var value = Environment.GetEnvironmentVariable(environmentVariableName);

            if (value == null)
            {
                Console.WriteLine("Could not find env variable {0}", environmentVariableName);
                return null;
            }

            Console.WriteLine("Using Azure Service Bus connection string from env variable {0}", environmentVariableName);

            return value;
        }

    }
}