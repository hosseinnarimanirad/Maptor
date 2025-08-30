# Contributing to Maptor

Thank you for considering contributing to Maptor! 🎉
Your contributions help make Maptor a powerful, reliable, and community-driven GIS solution for .NET developers.

## 📋 How Can You Contribute?
We welcome all kinds of contributions, including but not limited to:
  - **Bug Reports** – Found an issue? Report it with steps to reproduce.
  - **Feature Requests** – Got an idea? Open a discussion or issue.
  - **Code Contributions** – Implement fixes, new features, or refactor for better performance.
  - **Documentation** – Improve README, tutorials, XML docs, or add usage examples.
  - **Testing** – Write unit/integration tests to ensure quality.

---

## 🧩 Project Structure
The core code resides in the src/ folder. Here’s an overview:

```
src/
 ├── IRI.Maptor.Sta           # Core spatial operations, models, and analysis
 ├── IRI.Maptor.Ket           # Persistence, database, and infrastructures extensions
 ├── IRI.Maptor.Jab           # WPF-based map viewer and components
 └── ... (other modules)
```

---

## 🧪 Testing Guidelines
  - All new features and bug fixes must include tests.
  - Use xUnit (or the existing testing framework) for unit tests.
  - Place tests under the tests/ folder, following the structure of the src/ code.

---

## 🔍 Code Style
  - Follow .NET Coding Conventions (PascalCase for classes, camelCase for locals).
  - Use XML documentation comments for public classes and methods.
  - Keep methods small and focused.
  - Run dotnet format before committing:

---

## 🏗 Commit Messages

Use clear and descriptive commit messages. Preferred format:

```scss
type(scope): short description
```
Examples:
  - `fix(core): correct coordinate transformation in UTM`
  - `feat(viewer): add zoom-to-layer functionality`
  - `docs(readme): update installation instructions`

See [Commit Message Conventions](.github/COMMIT_CONVENTION.md)

--- 

## 🔄 Pull Request Process
1. Ensure your branch is **up to date** with master:

```git pull origin master```

2. **Run all tests** and verify the build passes.
3. Submit a **Pull Request (PR)** with:
    - A descriptive title
    - A detailed explanation of what you changed and why
    - Reference to related issues (if any)
4. Wait for **review and feedback**.
5. After approval, your PR will be merged.

---

## 💬 Communication
  - Use GitHub Issues for bug reports and feature requests.
  - For discussions, join the GitHub Discussions.

--- 

## ❤️ Thank You!

Your contributions make Maptor better for everyone!
