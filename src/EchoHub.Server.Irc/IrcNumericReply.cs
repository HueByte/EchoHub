namespace EchoHub.Server.Irc;

public static class IrcNumericReply
{
    // Connection registration
    public const string RPL_WELCOME = "001";
    public const string RPL_YOURHOST = "002";
    public const string RPL_CREATED = "003";
    public const string RPL_MYINFO = "004";
    public const string RPL_ISUPPORT = "005";

    // MOTD
    public const string RPL_MOTDSTART = "375";
    public const string RPL_MOTD = "372";
    public const string RPL_ENDOFMOTD = "376";
    public const string ERR_NOMOTD = "422";

    // Channel operations
    public const string RPL_NOTOPIC = "331";
    public const string RPL_TOPIC = "332";
    public const string RPL_NAMREPLY = "353";
    public const string RPL_ENDOFNAMES = "366";

    // LIST
    public const string RPL_LIST = "322";
    public const string RPL_LISTEND = "323";

    // WHO / WHOIS
    public const string RPL_WHOREPLY = "352";
    public const string RPL_ENDOFWHO = "315";
    public const string RPL_WHOISUSER = "311";
    public const string RPL_WHOISSERVER = "312";
    public const string RPL_WHOISIDLE = "317";
    public const string RPL_ENDOFWHOIS = "318";
    public const string RPL_WHOISCHANNELS = "319";

    // AWAY
    public const string RPL_UNAWAY = "305";
    public const string RPL_NOWAWAY = "306";
    public const string RPL_AWAY = "301";

    // MODE
    public const string RPL_CHANNELMODEIS = "324";
    public const string RPL_UMODEIS = "221";

    // Errors
    public const string ERR_NOSUCHNICK = "401";
    public const string ERR_NOSUCHCHANNEL = "403";
    public const string ERR_CANNOTSENDTOCHAN = "404";
    public const string ERR_UNKNOWNCOMMAND = "421";
    public const string ERR_NONICKNAMEGIVEN = "431";
    public const string ERR_ERRONEUSNICKNAME = "432";
    public const string ERR_NICKNAMEINUSE = "433";
    public const string ERR_NOTONCHANNEL = "442";
    public const string ERR_NOTREGISTERED = "451";
    public const string ERR_NEEDMOREPARAMS = "461";
    public const string ERR_ALREADYREGISTERED = "462";
    public const string ERR_PASSWDMISMATCH = "464";
    public const string ERR_CHANOPRIVSNEEDED = "482";

    // SASL
    public const string RPL_LOGGEDIN = "900";
    public const string RPL_SASLSUCCESS = "903";
    public const string ERR_SASLFAIL = "904";
}
