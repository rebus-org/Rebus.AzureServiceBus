using System;
using System.Linq;

namespace Rebus.AzureServiceBus
{
    class AzureServiceBusEntityNameHelper
    {
        static readonly char[] AdditionalValidCharacters = { '.', '-' };

        public string ReplaceInvalidCharacters(string str)
        {
            return string.Concat(str.Select(c => IsValidCharacter(c) ? c : '_'));
        }

        public void EnsureIsValidQueueName(string queueName)
        {
            var segments = queueName.Split('/');

            if (segments.All(segment => segment.All(IsValidCharacter))) return;

            throw new ArgumentException($"The string '{queueName}' is not a valid Azure Service Bus queue name! ASB queue names must consist of only letters (a-z and A-Z), digits (0-9), hyphens (-), periods (.), and underscores (_), and possibly multiple segments of these separated by slash (/).");
        }

        static bool IsValidCharacter(char c)
        {
            return char.IsLetterOrDigit(c)
                   || AdditionalValidCharacters.Contains(c);
        }
    }
}