# Gemini's Learning Log

This document serves as a log for me to learn from my mistakes and improve my performance.

## Standard Operating Procedure (SOP) for All Tasks

Before I begin analyzing a new request, I will perform the following pre-flight checklist to ensure quality and alignment.

1.  **Confirm Task Understanding:** Have I read the user's request carefully? Do I understand the core problem or goal?
2.  **Review Core Documentation:** I will quickly review `GEMINI.md`, `README.md`, and `TECHNICAL_ARCHITECTURE.md` to ensure the request aligns with our established workflow and the project's functional scope.
3.  **Analyze Existing Codebase:** I will identify and read all relevant existing files (`.cs`, `.xaml`, `.csproj`) to understand the current implementation.
4.  **Verify Definition of Done:** I will explicitly check the "Definition of Done" for the task. Does it require new tests? Does it require documentation updates? I will ensure these are included in the acceptance criteria.

## Common Errors and Corrections

- **Tool Reliability Workarounds:**
  - **`replace` tool:** This tool can be brittle if the `old_string` does not match the file content with 100% precision (including whitespace, newlines, etc.). If it fails, a more robust strategy is to read the file's content, perform the replacement in memory, and then use `write_file` to overwrite the entire file.
  - **`replace` tool with UTF-8 BOM:** The `replace` tool may fail if the file contains a UTF-8 BOM (Byte Order Mark). In this case, the best approach is to read the file, perform the replacement in memory, and then use `write_file` to overwrite the entire file, which will also remove the BOM.
  - **`read_many_files` tool:** This tool may fail with complex paths containing special characters like `( )` or `[ ]`. If it fails to find files that are known to exist, the correct workaround is to call the `read_file` tool individually for each file using its full, absolute path.
- **`list_directory` with relative paths:** The `list_directory` tool requires an absolute path. I will ensure to always provide an absolute path to this tool.
- **Using fixed absolute paths in tests/code:** When creating or modifying tests and code, always use paths relative to the project root directory or construct absolute paths. Never use fixed absolute paths from the user's environment (e.g., `D:/w/CastingManager/...`), as the agent's execution environment is a copy of the repository.

## Project-Specific Notes

- **Build & Test:** The primary command for building the UWP project is `msbuild`. The command to build the project for the x64 platform is: `"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "D:\w\CastingManager\CastingManager.UWP\CastingManager.UWP.csproj" /p:Platform=x64`. For testing, the command is `dotnet test`.
- **Architecture:** The project follows the MVVM (Model-View-ViewModel) pattern. All code changes must respect this pattern.
- **Dependencies:** NuGet packages are managed through the `.csproj` file. Do not add new packages without approval.