# Code Generator in C#

## Overview
This project is a **Software Code Generator** built using **C#**. It automatically generates boilerplate code for applications using **Entity Framework (EF)**. The tool is designed to accelerate development by scaffolding consistent.

## Features

- Automatically generate:
  - Repository and Unit of Work patterns
  - Data Transfer Objects (DTOs)
- Customizable templates
- Supports EF Core (5.0+)
- Scaffolds code based on existing EF models or database schema

## Technologies

- C# (.NET 6 or higher)
- Entity Framework Core
- CLI (.NET Console Application)

## Getting Started

### Installation

Clone the repository:
```bash
git clone https://github.com/JooScript/Code_Generator.git
cd ef-Code_Generator
```

Build the project:
```bash
dotnet build
```

### Usage

#### 1. Generate EF Models from an Existing Database
```bash
dotnet ef dbcontext scaffold "Your-Connection-String" Microsoft.EntityFrameworkCore.SqlServer --context AppDbContext --output-dir Entities --context-dir Data
```

#### 2. Put The Connection String In appsettings.json

#### 3. Run the Code Generator
```bash
dotnet run --project CodeGenerator_ConsoleApp.csproj
```

#### 4. Output Structure
```
CodeGenerator/
├── BusinessLogic/
├── DataAccess/
```

## License

This project is licensed under the MIT License. See `LICENSE` for details.

## Contributions

Contributions are welcome. Please fork the repository and submit a pull request with your improvements or fixes.
