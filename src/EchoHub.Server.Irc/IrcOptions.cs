namespace EchoHub.Server.Irc;

public sealed class IrcOptions
{
    public const string SectionName = "Irc";

    public bool Enabled { get; set; }
    public int Port { get; set; } = 6667;
    public bool TlsEnabled { get; set; }
    public int TlsPort { get; set; } = 6697;
    public string? TlsCertPath { get; set; }
    public string? TlsCertPassword { get; set; }
    public string ServerName { get; set; } = "echohub";
    public string? Motd { get; set; }
}
