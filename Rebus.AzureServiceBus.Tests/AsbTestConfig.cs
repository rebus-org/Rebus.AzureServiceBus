using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Rebus.AzureServiceBus.Tests;

static class AsbTestConfig
{
    static AsbTestConfig()
    {
        ConnectionString = GetConnectionString();

        //ConnectionStringFromFileOrNull(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asb_connection_string.txt"))
        //                   ?? ConnectionStringFromEnvironmentVariable("rebus2_asb_connection_string")
        //                   ?? throw new ConfigurationErrorsException("Could not find Azure Service Bus connection string!");
    }

    static string GetConnectionString()
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asb_connection_string.txt");

        if (File.Exists(filePath))
        {
            return ConnectionStringFromFile(filePath);
        }

        const string variableName = "rebus2_asb_connection_string";
        var environmentVariable = Environment.GetEnvironmentVariable(variableName);

        if (!string.IsNullOrWhiteSpace(environmentVariable)) return ConnectionStringFromEnvironmentVariable(variableName);

        throw new ApplicationException($@"Could not get Azure Service Bus connection string. Tried to load from file

    {filePath}

but the file did not exist. Also tried to get the environment variable named

    {variableName}

but it was empty (or didn't exist).

Please provide a connection string through one of the methods mentioned above.

");
    }

    /// <summary>
    /// Gets full connection string on the form <code>Endpoint=&lt;endpoint-uri&gt;;SharedAccessKeyName=&lt;key-name&gt;;SharedAccessKey=&lt;access-key&gt;</code>
    /// </summary>
    public static string ConnectionString { get; }

    /// <summary>
    /// Parses <see cref="ConnectionString"/> and gets the value from the "Endpoint" key-value pair, e.g. something like <code>sb://whatever.servicebus.windows.net</code>
    /// </summary>
    public static string GetEndpointUriFromConnectionString() =>
        ConnectionString.Split(';')
            .Select(token => token.Split('='))
            .Select(parts => new KeyValuePair<string, string>(parts.First(), parts.Skip(1).FirstOrDefault()))
            .FirstOrDefault(kvp => string.Equals("endpoint", kvp.Key, StringComparison.OrdinalIgnoreCase)).Value;

    /// <summary>
    /// Gets the value from <see cref="GetEndpointUriFromConnectionString"/> and trims the scheme and slashes parts from it, getting only the hostname
    /// </summary>
    public static string GetHostnameFromConnectionString() => new Uri(GetEndpointUriFromConnectionString()).Host;

    static string ConnectionStringFromFile(string filePath)
    {
        Console.WriteLine("Using Azure Service Bus connection string from file {0}", filePath);
        return File.ReadAllText(filePath);
    }

    static string ConnectionStringFromEnvironmentVariable(string environmentVariableName)
    {
        var value = Environment.GetEnvironmentVariable(environmentVariableName);

        Console.WriteLine("Using Azure Service Bus connection string from env variable {0}", environmentVariableName);

        return value;
    }

}