using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Identity.Client;

namespace Rebus.AzureServiceBus.Tests.TestUtilities;

internal class StubTokenCredential : TokenCredential
{
    private readonly string clientId;
    private readonly string clientSecret;
    private readonly string tenantId;

    public StubTokenCredential(string clientId, string clientSecret, string tenantId)
    {
        this.clientId = clientId;
        this.clientSecret = clientSecret;
        this.tenantId = tenantId;
    }

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        AccessToken token = default;
            
        Task.Run(async () => token = await this.GetTokenAsync(requestContext, cancellationToken)).Wait();

        return token;
    }

    public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(clientId)
            .WithAuthority($"https://login.windows.net/{tenantId}")
            .WithClientSecret(clientSecret)
            .Build();

        var serviceBusAudience = new Uri("https://servicebus.azure.net");

        var authResult = await app.AcquireTokenForClient(new string[] { $"{serviceBusAudience}/.default" }).ExecuteAsync(cancellationToken);

        return new AccessToken(authResult.AccessToken, authResult.ExpiresOn);
    }
}