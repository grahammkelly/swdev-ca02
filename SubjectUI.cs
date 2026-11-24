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

using LMS.DisplayDataModels;

namespace LMS.UI;

public class SubjectUI : UI {
    private DataStore _db;

    public SubjectUI(DataStore ds) {
        _db = ds;
    }

    protected override void DisplayChildMenu() {
        Console.WriteLine("Subject Menu:");
        Console.WriteLine("S - Select a subject (enter subject code)");
    }

    protected override string ValidSelectionsExcludingQuit() {
        return "S";
    }

    protected override bool HandleValidSelectionExcludingQuit(char choice) {
        switch (choice) {
            case 'S':
                Console.WriteLine("Subject option selected.");
                break;
            default:
                Console.WriteLine("Invalid selection.");
                break;
        }
        return true;
    }
}
