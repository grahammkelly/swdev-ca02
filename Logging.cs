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

public enum LogLevel {
    Trace,
    Debug,
    Info,
    Warning,
    Error
}

public abstract class Logger {

    private static Logger? instance;
    private static LogLevel _minLogLevel = LogLevel.Debug;

    public static void InitializeLogger(string logTo = "CONSOLE", LogLevel minLevel = LogLevel.Debug) {        
        _minLogLevel = minLevel;
        if (instance != null) {
            Logger.Warning($"Logger has already been initialized to {instance.GetType()}");
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
        Logger.Debug($"[{instance}] initialized");
    }

    public static void Log(LogLevel level, string message) {
        GetLogger().WriteLogEvent(level, message);
    }

    public static void LogWithBlanks(LogLevel level, string message, int initialLineCount = 1) {
        GetLogger().WriteLogEvent(level, message, initialLineCount);
    }

    public static Logger GetLogger() {
        if (instance == null) {
            instance = new ConsoleLogger();
            Logger.Info($"Logger was not initialized. Defaulting to [{instance}]");
        }
        return instance;
    }

    protected Logger() {}

    protected abstract void AppendLogEvent(string message);
    
    private void WriteLogEvent(LogLevel level, string message, int initialLineCount) {
        if (level < _minLogLevel) {
            return;
        }
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffZ");
        AppendLogEvent($"{new String('\n', initialLineCount)}[{timestamp}]: [{level.ToString().ToUpper()}] - {message}");
    }

    //Explicitly duplicated method so we don't create the new string each time if we don't want the newlines beforehand
    private void WriteLogEvent(LogLevel level, string message) {
        if (level < _minLogLevel) {
            return;
        }
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffZ");
        AppendLogEvent($"[{timestamp}]: [{level.ToString().ToUpper()}] - {message}");
    }

    public static void Trace(string message) {
        GetLogger().WriteLogEvent(LogLevel.Trace, message);
    }

    public static void Debug(string message) {
        GetLogger().WriteLogEvent(LogLevel.Debug, message);
    }

    public static void Info(string message) {
        GetLogger().WriteLogEvent(LogLevel.Info, message);
    }

    public static void Warning(string message) {
        GetLogger().WriteLogEvent(LogLevel.Warning, message);
    }

    public static void Error(string message) {
        GetLogger().WriteLogEvent(LogLevel.Error, message);
    }
}

class NoopLogger : Logger {
    protected override void AppendLogEvent(string message) {
        // Do nothing
    }
}

class ConsoleLogger : Logger {
    protected override void AppendLogEvent(string message) {
        Console.WriteLine(message);
    }
}

class InMemoryLogger : Logger {

    private List<string> _logs = new List<string>();
    public List<string> Logs { get => _logs; }

    protected override void AppendLogEvent(string message) {
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

    protected override void AppendLogEvent(string message) {
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

        Logger.Warning("Test warning message");
        Logger.Error("Test error message 1");
        Logger.Error("Test error message 2");
        RunTest(memLogger.Logs.Count == numLogsBefore + 3, "3 log events added");
        RunTest(memLogger.Logs.Count(log => log.Contains("[WARNING]")) == 1, "1 log event at WARNING level");
        RunTest(memLogger.Logs.Count(logEvent => logEvent.Contains("[ERROR]")) == 2, "2 log events at ERROR level");
    }

}