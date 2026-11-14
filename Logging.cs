// Copyright (C) 2026 Graham Kelly

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://gnu.org>.

using LMS;

//
// Obvoiusly, I'd prefer a 'real' Log framework, but this will suffice for the assignment.
// Does not implement any guards for performance or thread-safety (`...isEnabledFor(LogLevel)`, etc.)
// Definitely NOT production quality
//


namespace Logging;

//Only allowing 3 levels of logs for the LMS application
public enum LogLevel {
    Info,
    Warning,
    Error
}

public abstract class Logger {

    private static Logger? instance;

    public static void InitializeLogger(string logTo) {        
        if (instance != null) {
            throw new InvalidOperationException("Logger has already been initialized");
        }

        switch (logTo.ToUpper()) {
            case "NOOP":
                instance = new NoopLogger();
                break;
            case "CONSOLE":
                instance = new ConsoleLogger();
                break;
            case "MEMORY":
                instance = new InMemoryLogger();
                break;
            default:
                instance = new FileLogger(logTo);
                break;
        }
        instance.Info($"[{instance}] initialized");
    }

    public static void Log(LogLevel level, string message) {
        GetLogger().WriteLog(level, message);
    }

    public static Logger GetLogger() {
        if (instance == null) {
            instance = new ConsoleLogger();
            instance.Info($"Logger was not initialized. Defaulting to [{instance}]");
        }
        return instance;
    }

    protected Logger() {}

    protected abstract void AppendToLogStream(string message);
    
    private void WriteLog(LogLevel level, string message) {
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffZ");
        AppendToLogStream($"[{timestamp}]: [{level.ToString().ToUpper()}] - {message}");
    }

    public void Info(string message) {
        WriteLog(LogLevel.Info, message);
    }

    public void Warning(string message) {
        WriteLog(LogLevel.Warning, message);
    }

    public void Error(string message) {
        WriteLog(LogLevel.Error, message);
    }
}

class NoopLogger : Logger {
    protected override void AppendToLogStream(string message) {
        // Do nothing
    }
}

class ConsoleLogger : Logger {
    protected override void AppendToLogStream(string message) {
        Console.WriteLine(message);
    }
}

class InMemoryLogger : Logger {

    private List<string> _logs = new List<string>();
    public List<string> Logs { get => _logs; }

    protected override void AppendToLogStream(string message) {
        // Store logs in memory (for testing purposes)
        _logs.Add(message);
    }
}

class FileLogger : Logger {
    private string _logFile;

    public string LogFile { get => _logFile; }

    public FileLogger(string logFile = "./log.txt") {
        this._logFile = logFile;
    }

    protected override void AppendToLogStream(string message) {
        System.IO.File.AppendAllText(_logFile, message + Environment.NewLine);
    }

    public override string ToString() {
        return $"FileLogger: {_logFile}";
    }
}

public class LoggingTester: Tester {
    public static void PrepareLoggingTests() {
        Tester.AddModuleTester(new LoggingTester());
    }

    private LoggingTester() : base("Logging") {}

    protected override void ExecuteModuleTests() {
        Logger.InitializeLogger("MEMORY");
        Logger logger = Logger.GetLogger() ?? new NoopLogger(); //Test against an InMemory Logger, if not initialized correctly, use NoopLogger to avoid null reference
        RunTest(logger is InMemoryLogger, "GetLogger() returns correct type and did not fall back to NoOp logger");
        InMemoryLogger memLogger = (InMemoryLogger)logger;

        int numLogsBefore = memLogger.Logs.Count;

        Logger.Log(LogLevel.Info, "Test message");
        RunTest(memLogger.Logs.Count == numLogsBefore + 1, "Log count increased as log written through the static method");
        RunTest(memLogger.Logs.LastOrDefault("").Contains("[INFO]"), "Log added was at INFO level");
        RunTest(memLogger.Logs.LastOrDefault("").EndsWith("Test message"), "Log added contains the correct message");

        numLogsBefore++;    //Incremented because of the last test

        logger.Warning("Test warning message");
        logger.Error("Test error message 1");
        logger.Error("Test error message 2");
        RunTest(memLogger.Logs.Count == numLogsBefore + 3, "3 log events added when logged through the instantiated method");
        RunTest(memLogger.Logs.Count(log => log.Contains("[WARNING]")) == 1, "1 log event at WARNING level");
        RunTest(memLogger.Logs.Count(logEvent => logEvent.Contains("[ERROR]")) == 2, "2 log events at ERROR level");
    }

}