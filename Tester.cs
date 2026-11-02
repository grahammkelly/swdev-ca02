namespace LMS;

public abstract class Tester {
  
  private static int _overallTestCount = 0;
  private static int _overallTestsFailed = 0;
  
  private static readonly List<Tester> _moduleTesters = new List<Tester>();
  
  protected static void AddModuleTester(Tester tester) {
    _moduleTesters.Add(tester);
  }

  public static void RunTestsForAllModules() {
    _moduleTesters.ForEach(tester => tester.ExecuteTests());

    OutputOverallTestStatistics();
  }

  private static void OutputOverallTestStatistics() {
    Console.WriteLine("\n");
    _moduleTesters.ForEach(tester => tester.OutputModuleTestStatistics());
    if (_overallTestsFailed == 0) {
      Console.WriteLine($"Overall, all {_overallTestCount} tests PASSED");
    } else if (_overallTestsFailed == _overallTestCount) {
      Console.WriteLine($"Overall, all {_overallTestCount} tests FAILED");
    } else {
      Console.WriteLine($"Overall, {_overallTestsFailed} of {_overallTestCount} tests FAILED");
    }
  }

  private int _moduleTestCount = 0;
  private int _moduleTestsFailed = 0;

  private string _moduleBeingTested;

  public int ModuleTestCount {
    get => _moduleTestCount;
  }
  public int ModuleTestsFailed {
    get => _moduleTestsFailed;
  }

  protected Tester(string moduleBeingTested) {
    _moduleBeingTested = moduleBeingTested;
  }

  private void IncrementModuleTestsFailed(bool testPassed) {
    if (!testPassed) {
      _moduleTestsFailed++;
    }
  }

  public void ExecuteTests() {
    Console.WriteLine($"\nExecuting the tests for {_moduleBeingTested}");
    ExecuteModuleTests();

    _overallTestCount += _moduleTestCount;
    _overallTestsFailed += _moduleTestsFailed;
  }

  protected abstract void ExecuteModuleTests();
  
  protected void RunTest(bool passCondition, string testDescription) {
    _moduleTestCount++;
    IncrementModuleTestsFailed(passCondition);
    Console.WriteLine($"Test: {testDescription} - {(passCondition ? "PASS" : "FAIL")}");
  }

  private void OutputModuleTestStatistics() {
    if (_moduleTestsFailed == _moduleTestCount) {
      Console.WriteLine($"For {_moduleBeingTested}, all {_moduleTestCount} tests FAILED");
    } else {
      Console.WriteLine($"For {_moduleBeingTested}, {_moduleTestsFailed} of {_moduleTestCount} tests FAILED");
    }
  }
}