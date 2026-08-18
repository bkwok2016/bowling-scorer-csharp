# Bowling Scorer — C# / xUnit / Selenium Practice

A C# port of the Python bowling-scorer + Selenium practice projects,
built specifically to give you hands-on xUnit and C# Selenium practice —
both explicitly required by the SDET job description alongside Python.

**Why xUnit instead of pytest:** pytest is Python-only and can't test C#
code. xUnit is the modern .NET equivalent, and one of the two frameworks
(xUnit/NUnit) named directly in the job description. A short NUnit
comparison is included at the bottom of this README so you get exposure
to both without maintaining two parallel projects.

## Project structure

```
.
├── BowlingGame/                        # scoring library (class library)
│   ├── BowlingGame.cs
│   └── BowlingGame.csproj
├── BowlingGame.Tests/                  # xUnit unit tests
│   ├── BowlingGameScorerTests.cs
│   └── BowlingGame.Tests.csproj
├── BowlingWebApp/                      # ASP.NET Core Razor Pages web app
│   ├── Program.cs
│   ├── BowlingWebApp.csproj
│   └── Pages/
│       ├── _ViewImports.cshtml
│       ├── Index.cshtml
│       └── Index.cshtml.cs
├── BowlingWebApp.SeleniumTests/        # C# Selenium UI tests
│   ├── BowlingWebApp.SeleniumTests.csproj
│   ├── UiTests.cs
│   └── Pages/
│       └── BowlingPage.cs
├── .github/workflows/dotnet-tests.yml  # CI: unit tests + Selenium tests
├── .gitignore
└── README.md
```

Same UI element IDs as the Python/Flask version (`frame-0`..`frame-9`,
`calculate-btn`, `score-0`..`score-9`, `final-score`, `error-message`) —
if you've done the Python Selenium practice, the concepts transfer directly.

**A head start on flakiness:** the Page Object here already includes
explicit `WebDriverWait`s on every locator, and the resubmission test
already checks the new page's success signal before checking the old
error is gone. Both of those were bugs discovered and fixed the hard way
in the Python version — no need to rediscover them here.

---

## Running it locally (Windows 11)

**1. Install the .NET 8 SDK** if you don't have it:
```powershell
dotnet --version
```
If that errors, download and install from https://dotnet.microsoft.com/download
(get the SDK, not just the runtime), then reopen your terminal.

**2. Restore and build everything:**
```powershell
cd path\to\BowlingScorerCSharp
dotnet restore
dotnet build
```

**3. Run the unit tests first** (fast, no browser needed):
```powershell
dotnet test BowlingGame.Tests\BowlingGame.Tests.csproj
```
You're looking for a summary line like `Passed! - Failed: 0, Passed: 22`.

**4. Start the web app** (leave this terminal running):
```powershell
dotnet run --project BowlingWebApp\BowlingWebApp.csproj --urls "http://localhost:5000"
```
Open `http://localhost:5000` in a browser to confirm the form loads.

**5. In a second terminal**, run the Selenium tests:
```powershell
cd path\to\BowlingScorerCSharp
dotnet test BowlingWebApp.SeleniumTests\BowlingWebApp.SeleniumTests.csproj
```

By default these run **headless**. To actually watch Chrome click through
the form:
```powershell
$env:HEADLESS = "false"
dotnet test BowlingWebApp.SeleniumTests\BowlingWebApp.SeleniumTests.csproj
$env:HEADLESS = "true"   # reset afterward
```

You need Google Chrome installed. Selenium.WebDriver 4.6+ (pinned in the
`.csproj`) includes **Selenium Manager**, which auto-downloads the
matching chromedriver — no manual driver setup needed, same as the
Python version.

---

## Pushing to GitHub

Same process as your other repos, adapted for the extra project folders.

### Step 1: Create the repo
1. Go to **https://github.com** → **+** → **New repository**
2. Name it something like `bowling-scorer-csharp`
3. **Don't** initialize with a README (you already have one)
4. Click **Create repository**

### Step 2: Push from your terminal
```powershell
cd path\to\BowlingScorerCSharp
git init
git add .
git commit -m "Initial commit -- C# bowling scorer with xUnit and Selenium"
git branch -M main
git remote add origin https://github.com/YOUR-USERNAME/bowling-scorer-csharp.git
git push -u origin main
```

**Before you push, double check `.gitignore` is doing its job:** run
`git status` after `git add .` — you should **not** see any `bin/` or
`obj/` folders listed as staged. Those are regenerated automatically by
`dotnet build` and should never be committed; if you see them staged
anyway, confirm `.gitignore` was actually included in `git add .`
(it's a dotfile, same "hidden file" caveat as `.github` in the Python
project — make sure your file explorer shows hidden files if you're
checking manually).

**Also confirm `.github/workflows/dotnet-tests.yml` made it in** — same
hidden-folder caveat applies here too.

### Step 3: Watch the workflow run
1. Go to your repo → **Actions** tab
2. You should see a **"Dotnet Selenium UI Tests"** run kick off automatically
3. Click in to watch each step: checkout, .NET setup, restore, build, unit
   tests, Chrome setup, starting the web app, then the Selenium tests
4. Green ✅ means everything passed — both the xUnit unit tests **and**
   the C# Selenium UI tests — against a real headless Chrome instance in
   the cloud

If something fails, the same debugging approach from the Python project
applies: check which specific step failed, expand it, and look at the
actual error text near the top (import/build errors) or the test summary
near the bottom (assertion failures).

---

## xUnit vs. NUnit — quick reference

Since the JD lists both, here's the syntax mapping so you're not caught
off guard if asked about NUnit specifically:

| Concept | xUnit (used here) | NUnit |
|---|---|---|
| Test method attribute | `[Fact]` | `[Test]` |
| Parameterized test | `[Theory]` + `[InlineData(...)]` or `[MemberData(...)]` | `[TestCase(...)]` or `[TestCaseSource(...)]` |
| Setup before each test | Constructor | `[SetUp]` method |
| Teardown after each test | `IDisposable.Dispose()` | `[TearDown]` method |
| Assert equality | `Assert.Equal(expected, actual)` | `Assert.That(actual, Is.EqualTo(expected))` |
| Assert exception thrown | `Assert.Throws<T>(() => ...)` | `Assert.Throws<T>(() => ...)` (same) |
| Test class grouping | Plain class, no attribute needed | `[TestFixture]` on the class |

**The concepts underneath are identical either way** — fixtures/setup,
assertions, parameterization, exception testing. If asked in an
interview, this table is a good one to have skimmed once so you can
speak to the mapping confidently even without hands-on NUnit time.
