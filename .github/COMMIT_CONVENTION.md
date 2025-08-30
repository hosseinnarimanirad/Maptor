# ✅ Commit Message Conventions for Maptor Repository

This repository follows the **[Conventional Commits](https://www.conventionalcommits.org/)** standard to maintain consistent, descriptive, and automation-friendly commit messages.

---

## ✅ Format

```
<type>(<scope>): <summary>
```

- **type** → Nature of the change (see allowed types below)
- **scope** → Specific project/module affected (**optional** if not applicable)
- **summary** → Short description in **imperative mood**, starting with **lowercase**, max **72 characters**

**Example:**
```
refactor(jab-common): rename namespaces for consistency
```

---

## ✅ Allowed Types

| Type         | Icon | Description                                                                |
|--------------|------|----------------------------------------------------------------------------|
| **feat**     | ✨   | A new feature                                                             |
| **fix**      | 🐛   | A bug fix                                                                 |
| **docs**     | 📝   | Documentation only                                                        |
| **style**    | 🎨   | Code style changes (whitespace, formatting), no logic impact              |
| **refactor** | ♻️    | Code restructuring without changing functionality                         |
| **perf**     | ⚡    | Performance improvements                                                  |
| **test**     | ✅    | Adding or updating tests                                                  |
| **chore**    | 🔧    | Maintenance tasks (build, tooling, dependencies)                          |
| **build**    | 🏗️    | Changes to the build system or external dependencies                      |
| **ci**       | 🤖    | Continuous integration or deployment configuration changes                |

---

## ✅ Scopes

Use the **module/project name** or **feature area** as scope.  
If no specific module applies, omit the scope:

**Examples of scopes:**
```
jab-common
tile-services
layers
cartography
localization
presenters
office-formats
helpers
```

**Example commits:**
```
feat(tile-services): add Google Hybrid provider
fix(layers): resolve crash when adding empty feature set
chore: update .editorconfig
```

---

## ✅ Commit Examples

### Feature:
```
feat(tile-services): add support for Bing Road layer
```

### Bug Fix:
```
fix(cartography): correct color ramp scaling on high zoom levels
```

### Refactor:
```
refactor(jab-common): rename namespaces to align with new conventions
```

### Documentation:
```
docs(readme): add FeatureLayer usage example
```

### Chore:
```
chore(deps): update WriteableBitmapEx to 1.7.1
```

### Test:
```
test(helpers): add unit tests for color blending helper
```

---

## ✅ Breaking Changes

If a commit **introduces a breaking change**, include a footer with `BREAKING CHANGE:` followed by details.

**Example:**
```
refactor(layers): remove obsolete RenderAsync method

BREAKING CHANGE: RenderAsync is replaced by RenderAsyncEx.
Update all usages in the layer rendering pipeline.
```

---

## ✅ Best Practices (with examples)

✔ **Use imperative mood**  
✅ `fix(layers): add null check for feature set`  
❌ `fixed null check for feature set`  

✔ **Start summary in lowercase (unless proper noun)**  
✅ `docs(readme): update usage section`  
❌ `docs(readme): Update usage section`  

✔ **Keep summary under 72 characters**  
✅ `perf(tile-services): improve caching for faster tile loading`  

✔ **Add a body when needed (what and why, not how)**  
```
fix(layers): prevent null reference when loading empty dataset

The issue occurred when FeatureSet was null. Added null-check and early return.

Closes #123
```

✔ **Reference issues in the footer**  
✅ `Closes #45` or `Fixes #78`

---

## ✅ Tools for Enforcement

- **Commitizen** – Interactive CLI for writing conventional commits  
- **Husky + Commitlint** – Git hooks for commit message linting  
- **Conventional Changelog** – Generates changelogs automatically from commit history  

---

✅ **Happy committing!** 🚀
