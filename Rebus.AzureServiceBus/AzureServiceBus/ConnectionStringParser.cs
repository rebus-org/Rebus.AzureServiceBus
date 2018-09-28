using System;
using System.Collections.Generic;
using System.Linq;
using Rebus.Extensions;

namespace Rebus.AzureServiceBus
{
    class ConnectionStringParser
    {
        readonly Dictionary<string, string> _parts;

        public string ConnectionString { get; }

        public ConnectionStringParser(string connectionString)
        {
            ConnectionString = connectionString;

            _parts = connectionString.Split(';')
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
        }

        public string Endpoint => _parts.GetValue("Endpoint");
        public string SharedAccessKeyName => _parts.GetValue("SharedAccessKeyName");
        public string SharedAccessKey => _parts.GetValue("SharedAccessKey");
        public string EntityPath => _parts.GetValueOrNull("EntityPath");

        public override string ToString()
        {
            return $@"{ConnectionString}
           Endpoint: {Endpoint}
SharedAccessKeyName: {SharedAccessKeyName}
    SharedAccessKey: {SharedAccessKey}";
        }

        public string GetConnectionStringWithoutEntityPath() => string.Join(";", _parts.Where(p => !string.Equals(p.Key, "EntityPath")).Select(kvp => $"{kvp.Key}={kvp.Value}"));
    }
}