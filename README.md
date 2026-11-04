# Lecture Management System

This repository contains the code for the Software Development second continuous assessment project. 

Generally (excepting test code) the code will follow Uncle Bob's 'Clean Code' principles. This includes;

- Writing clear and readable code.
- Using meaningful names for methods and classes (variables too, but less critically).
- Keeping functions and methods small and focused on a single task (_note_ I'm not as excessive as Bob on method length, 
but I do aim for clarity and maintainability).
- Avoiding deep nesting (no more than 2 levels deep [e.g., loops within loops, etc.]).
- Reducing the need for excessive comments by making the code self-explanatory (a clear method name, or a method doing 
a single thing, should not need a comment except where a specific implementation detail, or a decision that could have 
gone either way,requires explanation.)


## Methods

The software was;
* written on Mac (M1 Max, OS: Tahoe v26.6.2)
* compiled and run against Microsoft .NET SDK 10.0.403
* generated within [Visual Studio Code](https://code.visualstudio.com/) 1.145.0 (Editor) and 
[JetBrains Rider](https://www.jetbrains.com/rider/) 2026.2.4.1 (IDE)

# System requirements

The requirements are covered in detail in the included [`Brief.pdf`](./Brief.pdf)

In summary;

- design and implement a **C# Lecture Management System**
    - design and implement an appropriate class hierarchy to represent different types of lecture subjects
        - solution must demonstrate your understanding of object-oriented programming and the C# functionality
    - system should allow users to add, view, list, edit and delete lecture subjects

- The system must allow users to perform the following operations:
    - **Add**: Create a new lecture subject.
    - **View**: Display the details of a selected lecture subject.
    - **List**: Display all lecture subjects stored in the system.
    - **Edit**: Modify the details of an existing lecture subject.
    - **Delete**: Remove a lecture subject from the system.

## Assessment requirements

The solution must demonstrate:

- Inheritance between classes.
- Appropriate use of constructors.
- Method overriding, using virtual and override where appropriate.
- Appropriate use of fields, properties and methods.
- The use of at least one static field, property or method.
- The solution must save lecture subject information to a file so that the data is not lost when the application closes.
    - Saving lecture subject data to a file.
    - Loading saved data when the application starts.
    - Retaining changes made when subjects are added, edited or deleted.
    - Handling situations where the data file does not yet exist.
- The system should validate user input and handle common errors appropriately;
    - Preventing duplicate subject codes.
    - Handling invalid menu selections.
    - Ensuring that required fields are entered.
    - Validating numeric values e.g. lecture hours.
    - Handling attempts to view, edit or delete a subject that does not exist.
    - Handling missing or invalid data files.

# Project Overview

## Software requirements

* .NET SDK 10.0+

## Building

To build the project, navigate to the project directory in your terminal and run:

```bash
$ dotnet build
```

## Running

To run the project, navigate to the project directory in your terminal and run:

```bash
$ dotnet run
```

### Running tests

The tests are triggered from the `Tests.cs` file. To run the tests, navigate to the project directory in your terminal 
and run:

```bash
$ dotnet run Tests.cs
```

# License

This project is licensed under the GNU General Public License v3.0 - see the LICENSE file for details.
