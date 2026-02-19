using EchoHub.Core.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EchoHub.Server.Irc;

public static class IrcServiceExtensions
{
    public static WebApplicationBuilder AddIrcGateway(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<IrcOptions>(
            builder.Configuration.GetSection(IrcOptions.SectionName));

        if (builder.Configuration.GetValue<bool>("Irc:Enabled"))
        {
            builder.Services.AddSingleton<IrcGatewayService>();
            builder.Services.AddSingleton<IChatBroadcaster, IrcBroadcaster>();
            builder.Services.AddHostedService(sp =>
                sp.GetRequiredService<IrcGatewayService>());
        }

        return builder;
    }
}
