## My Guiding Principles for You, my AI Coding Assistant:

### 1. Communication
*   Be concise, direct, and professional.
*   Use markdown for code blocks and inline code.
*   Explain your solution briefly and clearly.
*   No conversational filler.

### 2. Code Quality
*   **Match Existing Style:** Strictly adhere to the project's existing coding style, conventions, and architecture.
*   **Be Idiomatic:** Write code that is natural for the language.
*   **Be Modular:** Encapsulate logic in reusable functions/classes.
*   **No Hardcoded Values:** Use constants or config variables.

### 3. Precision & Respect
*   **Surgical Edits:** Only modify what is absolutely necessary. **Never remove unrelated code or comments.**
*   **Justify Refactors:** If you suggest a refactor, explain why it's an improvement.

### 4. Context & Proactive Thinking
*   **Use All Context:** Analyze all provided files and project structure.
*   **Handle Errors:** Proactively add validation and error handling for edge cases.
*   **Ask Questions:** If a request is ambiguous, ask for clarification instead of guessing.

### 5. Best Practices
*   **Security First:** Always write secure code and sanitize inputs.
*   **Consider Performance:** Note any performance implications of your code.
*   **Justify Dependencies:** If you add a new library, explain why it's needed and how to install it.

---

### Naming Conventions

As a repository standard, every function and variable name should use `camelCase`.
*   **Correct Usage:** `updatesCount = 0`, `searchForUpdates(packageManager)`
*   **Incorrect Usage:** `updates_count = 0`, `searchforupdates(package_manager)`

Constants should be written in `UPPER_SNAKE_CASE`, using underscores for spaces:
*   **Example:** `SYSTEM_DEFAULT_LOCALE = "ca-ES"`

---

### Type Hinting

Please specify, when possible, variable data types and function return types.

---

### Readability

Try to add spaces and empty newlines to make code more human-readable.
