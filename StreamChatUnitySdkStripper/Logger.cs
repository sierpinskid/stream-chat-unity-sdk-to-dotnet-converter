namespace StreamChatUnitySdkStripper;

public static class Logger
{
    public static void Info(string message) => Console.WriteLine(message);

    public static void Error(string message)
    {
        using (new ColorScope(ConsoleColor.Red))
        {
            Console.WriteLine($"[ERROR]] {message}");
        }
    }

    public static void Warning(string message)
    {
        using (new ColorScope(ConsoleColor.Yellow))
        {
            Console.WriteLine($"[WARNING]] {message}");
        }
    }

    private class ColorScope : IDisposable
    {
        public ColorScope(ConsoleColor color)
        {
            _prevColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
        }

        public void Dispose()
        {
            Console.ForegroundColor = _prevColor;
        }

        private ConsoleColor _prevColor;
    }
}