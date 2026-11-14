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