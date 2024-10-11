namespace SOCKS5_proxy;

public class Program
{
    public const int s_DefaultServerPort = 1080;
    public static void Start(string[] args)
    {
        int port = s_DefaultServerPort;
        if (args.Length == 1)
        {
            port = short.Parse(args[0]);
            if (!MeetConstrains(port))
            {
                throw new ArgumentException("Invalid port.");
            }
        }
        Server server = new(port);
        server.Start();
    }

    private static bool MeetConstrains(int port)
    {
        return port >= 1024 && port <= 49151;
    }
}



