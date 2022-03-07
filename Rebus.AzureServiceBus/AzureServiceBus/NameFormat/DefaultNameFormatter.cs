using System.Linq;

namespace Rebus.AzureServiceBus.NameFormat;

/// <summary>
/// A formatter that formats queue, topic and subscription names using a default convention.
/// </summary>
public class DefaultNameFormatter : INameFormatter
{
    readonly char[] _additionalValidCharacters;

    /// <summary>
    /// Creates the name formatter.
    /// </summary>
    public DefaultNameFormatter()
    {
        _additionalValidCharacters = new[] { '.', '-', '_' };
    }

    private string ReplaceInvalidCharacters(string str)
    {
        var name = string.Concat(str.Select(c =>
        {
            if (c == '/') return '/';

            return IsValidCharacter(c) ? c : '_';
        }));

        return name;
    }

    bool IsValidCharacter(char c)
    {
        return char.IsLetterOrDigit(c)
               || _additionalValidCharacters.Contains(c);
    }

    /// <summary>
    /// Formats the queue name into a usable name on ASB, normalizing if needed.
    /// </summary>
    public string FormatQueueName(string queueName)
    {
        return ReplaceInvalidCharacters(queueName);
    }

    /// <summary>
    /// Formats the subscription name into a usable name on ASB, normalizing if needed.
    /// </summary>
    public string FormatSubscriptionName(string subscriptionName)
    {
        // queue names can have multiple segments in them separated by / - subscription names cannot!
        return ReplaceInvalidCharacters(subscriptionName.Replace("/", "_"));
    }

    /// <summary>
    /// Formats the topic name into a usable name on ASB, normalizing if needed.
    /// </summary>
    public string FormatTopicName(string topicName)
    {
        return ReplaceInvalidCharacters(topicName);
    }
}