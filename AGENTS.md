# **Development Guide**

As an AI coding agent for the CastingManager project, you must adhere strictly to the following rules. Failure to do so will result in rework.

## ⭐ Golden Rules

These are your most important directives. Follow them for every task.

1.  **Read Your Memory First:** Before starting any task, you **must** read the `JULES_MEMORY.md` file to learn from past sessions and avoid repeating mistakes.
2.  **Consistency is Key:** Your primary goal is to write code that is consistent with the existing style, patterns, and conventions in the project. Adhere to the MVVM architecture.
3.  **The Definition of Done is Law:** Your work is only complete when you have fulfilled all acceptance criteria, which always includes:
    - Implementing the feature.
    - Creating or updating tests (using MSTest).
    - Updating all relevant user documentation.
4.  **No Unapproved Packages:** You **must not** install any new NuGet packages without explicit approval.

## ✅ Your Workflow & Verification Process

1.  **Understand the Goal:** Read the prompt carefully.
2.  **Analyze Existing Code:** Use your tools to read relevant files (`.cs`, `.xaml`) and understand the context before making changes.
3.  **Implement Changes:** Write code that follows all principles in this guide.
4.  **Verify Your Work:** After every code modification, you **must** run the following commands in order:
    1.  `dotnet build`
    2.  `dotnet test` (if tests are present)

## 🛠️ Tech Stack & Architecture

- **Core Stack:** The project uses **UWP**, **.NET 8**, and **C#**.
- **Architecture:** A strict **MVVM (Model-View-ViewModel)** pattern is enforced. Refer to `TECHNICAL_ARCHITECTURE.md` for a detailed diagram.
    - **Models:** `CastingManager/Models/`
    - **Views:** `CastingManager/Views/` (and `MainPage.xaml`)
    - **ViewModels:** `CastingManager/ViewModels/`
    - **Services:** `CastingManager/Services/`
- **UI:** The UI is defined in XAML files and should follow **Windows 11 Fluent Design** principles.
- **Testing:** Unit Tests are created using **MSTest**. New tests should be added to a corresponding test project and follow existing patterns.

### Key Architectural Principles

- **Separation of Concerns:** Keep UI logic out of Views (code-behind), business logic out of ViewModels, and API/platform interactions inside Services.
- **Data Binding:** Use XAML data binding extensively to connect Views to ViewModels. Avoid manipulating UI elements directly from the code-behind.
- **Asynchronous Operations:** All potentially blocking calls (network, file I/O, device interactions) **must** be `async` and use the `await` keyword to prevent locking up the UI thread.
- **Error Handling:** Errors should be caught in the Service layer, packaged into meaningful exceptions or error messages, and passed to the ViewModel to be displayed to the user gracefully.