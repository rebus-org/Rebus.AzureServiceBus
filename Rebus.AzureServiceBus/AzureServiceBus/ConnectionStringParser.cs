using System;
using System.Linq;
using Rebus.Extensions;

namespace Rebus.AzureServiceBus
{
    class ConnectionStringParser
    {
        public string ConnectionString { get; }

        public ConnectionStringParser(string connectionString)
        {
            ConnectionString = connectionString;
            var parts = connectionString.Split(';')
                .Select(token => token.Trim())
                .Where(token => !string.IsNullOrWhiteSpace(token))
                .Select(token =>
                {
                    var index = token.IndexOf('=');

                    if (index < 0) throw new FormatException($"Could not interpret '{token}' as a key-value pair");

                    return new
                    {
                        key = token.Substring(0, index),
                        value = token.Substring(index + 1)
                    };
                })
                .ToDictionary(a => a.key, a => a.value);

            Endpoint = parts.GetValue("Endpoint");
            SharedAccessKey = parts.GetValue("SharedAccessKey");
            SharedAccessKeyName = parts.GetValue("SharedAccessKeyName");
            EntityPath = parts.GetValueOrNull("EntityPath");
        }

        public string Endpoint { get; }
        public string SharedAccessKeyName { get; }
        public string SharedAccessKey { get; }
        public string EntityPath { get; }

        public override string ToString()
        {
            return $@"{ConnectionString}
           Endpoint: {Endpoint}
SharedAccessKeyName: {SharedAccessKeyName}
    SharedAccessKey: {SharedAccessKey}";
        }

        public string GetConnectionStringWithoutEntityPath() => $"Endpoint={Endpoint};SharedAccessKeyName={SharedAccessKeyName};SharedAccessKey={SharedAccessKey}";
    }
}