namespace StreamChatUnitySdkStripper;

public static class Logger
{
    public static void Info(string message) => Console.WriteLine(message);

    public static void Error(string message)
    {
        var prevColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERROR]] {message}");
        Console.ForegroundColor = prevColor;
    }
}