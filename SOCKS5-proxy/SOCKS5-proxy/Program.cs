
namespace SOCKS5_proxy;

public class Program
{
    public const int s_DefaultServerPort = 1080;

    private const string s_HelpMessage = """
        Usage:
            socks-proxy <PORT>
        Starts a proxy server running using protocol SOCKS-5 on a specified port number.
        Starting without specifying PORT will use the default port: 1080.
        """;
    public static void Start(string[] args)
    {
        int port = s_DefaultServerPort;
        switch (args.Length)
        {
            case 0:
                Console.WriteLine("No port provided. Starting with default port.");
                port = s_DefaultServerPort;
                break;
            case 1:
                if (args[0] == "-h")
                {
                    PrintHelpInfo();
                    return;
                }
                port = short.Parse(args[0]);
                if (!MeetConstrains(port))
                {
                    Console.Error.WriteLine("Invalid port.");
                    return;
                }
                break;
            default:
                Console.Error.WriteLine("Invalid number of arguments. Try '-h' for info.");
                return;

        }
        Server server = new(port);
        server.Start();
    }

    private static void PrintHelpInfo()
    {
        Console.WriteLine(s_HelpMessage);
    }

    private static bool MeetConstrains(int port)
    {
        return port >= 1024 && port <= 49151;
    }
}



