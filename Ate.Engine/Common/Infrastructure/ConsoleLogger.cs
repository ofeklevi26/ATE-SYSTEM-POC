using System;

namespace Ate.Engine.Infrastructure;

public sealed class ConsoleLogger : ILogger
{
    public void Info(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[{DateTime.Now:O}] INFO {message}");
        Console.ResetColor();
    }

    public void Error(string message, Exception? ex = null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[{DateTime.Now:O}] ERROR {message}");
        if (ex != null)
        {
            Console.WriteLine(ex);
        }

        Console.ResetColor();
    }
}
