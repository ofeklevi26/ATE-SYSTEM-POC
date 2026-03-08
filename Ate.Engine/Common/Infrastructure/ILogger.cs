using System;

namespace Ate.Engine.Infrastructure;

public interface ILogger
{
    void Info(string message);

    void Error(string message, Exception? ex = null);
}
