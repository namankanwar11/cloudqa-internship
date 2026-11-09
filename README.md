# CloudQA resilient browser automation

Working C# / .NET 8 / Selenium WebDriver / Chrome / xUnit internship project.
Fills First Name, Gender and State and verifies each action. Does not submit the form.

## Installation and quick start

Project: D:\CloudQA-Internship. Local SDK: D:\CloudQA-Tools\dotnet (8.0.427).
Chrome is installed on this machine. Selenium Manager downloads matching ChromeDriver
on first use; internet access is required initially.

~~~powershell
Set-Location D:\CloudQA-Internship
. .\scripts\env.ps1
dotnet build CloudQA.sln -c Release
dotnet test CloudQA.sln -c Release --logger "trx;LogFileName=fixture-tests.trx" --results-directory artifacts/test-results
dotnet run --project src/CloudQA.Runner -c Release -- --headless false --first-name Ada --gender Male --state India
~~~

On a fresh machine install .NET 8 SDK and Chrome. The D-drive script is specific to this
machine; elsewhere use the system SDK and normal platform caches. CHROME_BIN overrides
Chrome's executable path. To install the SDK on D:

~~~powershell
New-Item -ItemType Directory -Force D:\CloudQA-Tools | Out-Null
$env:TEMP = 'D:\CloudQA-Tools'
$env:TMP = $env:TEMP
$env:DOTNET_CLI_HOME = 'D:\CloudQA-Tools\cli'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile D:\CloudQA-Tools\dotnet-install.ps1
& D:\CloudQA-Tools\dotnet-install.ps1 -Channel 8.0 -InstallDir D:\CloudQA-Tools\dotnet -NoPath
~~~

## Configuration

CLI accepts name/value pairs: --url, --first-name, --gender, --state, --headless.
CLOUDQA_URL supplies the default URL; CLI overrides it.
Defaults: https://app.cloudqa.io/home/AutomationPracticeForm, Naman, Male, India, true.
State uses visible option text (the live State list contains country names).

~~~powershell
dotnet run --project src/CloudQA.Runner -c Release -- --url "https://app.cloudqa.io/home/AutomationPracticeForm" --headless true
$fixture = ([Uri](Resolve-Path tests/CloudQA.Tests/Fixtures/mutated.html).Path).AbsoluteUri
dotnet run --project src/CloudQA.Runner -c Release -- --url $fixture --first-name Ada
~~~

Exit codes: 0 verified success; 1 browser/action failure; 2 configuration/startup error.
Logs: console and artifacts/<UTC timestamp>/run.log. Failure capture attempts screenshot
and HTML. Artifacts may contain entered data: review before sharing.

## Architecture

- src/CloudQA.Core: browser factory, semantic discovery, verified form workflow.
- src/CloudQA.Runner: configuration, lifecycle, logging and failure diagnostics.
- tests/CloudQA.Tests: real headless Chrome integration tests using local HTML.
- .github/workflows/tests.yml: GitHub Actions .NET test workflow and TRX upload.

Discovery.js enumerates generic input/select types, reading labels, aria-label,
aria-labelledby, placeholders and bounded nearby text. It also supports dynamic for
associations on spans used by the live page. IDs are read only to resolve semantic
relationships. No hardcoded ID, name, class, XPath, index or element order chooses targets.
Nonstandard for associations are scoped by enclosing form to tolerate duplicate IDs.

C# excludes hidden, disabled and readonly controls, then scores exact normalized names:
label/ARIA=100, placeholder=70, single-control nearby text=60. The strongest signal wins;
weak duplicate evidence is not added. Optional exact group context adds 20 and must match.
Minimum score=60; winner must exceed runner-up by 15. Otherwise discovery fails safely.
Normalization ignores case and punctuation. Exact matching prevents First Name confirmation
and Female from matching First Name and Male.

The uniquely discovered First Name anchors subsequent discovery to its enclosing form.
This handles similar controls in unrelated live-page forms without using their IDs.
Duplicate first-name fields fail as ambiguous. Radio intent uses its option's semantic name;
the discovery API also supports fieldset/radiogroup context when needed.

Explicit polling waits replace sleeps. Actions use normal WebDriver interactions, re-discover
controls, and verify value/selection. Stale elements, intercepted clicks and temporarily
non-interactable controls retry; ambiguity and missing dropdown options fail immediately.
No JavaScript click bypass is used.

## Tests and validation

Nine xUnit cases exercise five full workflows plus duplicate ambiguity, missing target,
missing option and radio group context. Fixtures cover renamed IDs/classes, reordered
fields, nested wrappers, similar-name/hidden/disabled decoys, ARIA names, nearby text,
delayed controls and live-style spans. The live-pattern fixture also has duplicate IDs
and unrelated controls in another form. Assertions verify results, unchanged decoys and
no mutation on ambiguity. Each test owns and disposes its browser.

On 2026-11-09 the local Release suite was executed, and a live headless Selenium run
verified First Name=Naman, Gender=Male, State=India with exit code 0.
Results are in ignored artifacts/test-results and timestamped run logs.
Re-run before the interview: the public website can change.
GitHub Actions passed on Ubuntu on 2026-11-09: all 9 fixture tests passed.
Validation run: https://github.com/namankanwar11/cloudqa-internship/actions/runs/42616630711

## Five-minute interview demonstration

1. 0:00-0:45: explain why fixed locators fail and how semantic scoring detects uncertainty.
2. 0:45-1:45: run dotnet test; show mutated.html and its wrappers, reordered fields and decoys.
3. 1:45-2:45: run the live command with --headless false; point out three VERIFIED logs.
   Chrome closes after verification, so keep terminal logs visible.
4. 2:45-4:00: show Discovery.js, SemanticDiscovery.cs scoring and PracticeForm.cs form scope.
5. 4:00-5:00: show the duplicate-label rejection test and explain limitations.
   Keep the local fixture command ready as a network-independent fallback.

## Limitations

Native text/radio/select controls in the current document are supported. No iframe/shadow-root
traversal or custom combobox support. Meaningful English semantic names must remain available;
major label changes need updated intent. Proximity is bounded to a single-control container.
Indistinguishable same-name controls need additional semantic context and fail safely otherwise.
All three controls are assumed to belong to one form. Entire form replacement can stale scope.
Nested discovery/action waits can exceed one nominal timeout. Verification checks DOM state,
not server persistence. Chrome profiles remain in TEMP and may be removed after sessions end.
Installed host binaries/Chrome already reside on C; project, local SDK, dependency caches,
temporary profiles and build outputs are on D.
The fixture suite has passed on Windows locally and Ubuntu in GitHub Actions. Live-site validation was performed on Windows.

## GitHub repository

https://github.com/namankanwar11/cloudqa-internship

The repository is private. Give reviewers access or make it public if required by the assignment.
Pushes and pull requests trigger the browser test workflow; TRX results are uploaded as artifacts.

To publish future changes:

~~~powershell
git add .
git commit -m "Describe your change"
git push
~~~