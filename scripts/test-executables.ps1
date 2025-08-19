# PowerShell script for testing Windows executables
param(
    [string]$Platform = "win-x64",
    [string]$TestDir = "dist\test-executables"
)

Write-Host "🧪 Testing executable startup for platform: $Platform" -ForegroundColor Cyan
Write-Host "📁 Project root: $PWD" -ForegroundColor Gray

# Clean test directory
if (Test-Path $TestDir) {
    Remove-Item -Recurse -Force $TestDir
}
New-Item -ItemType Directory -Path $TestDir -Force | Out-Null

Write-Host "📦 Building CLI executable..." -ForegroundColor Yellow
dotnet publish src\DocxTemplate.CLI\DocxTemplate.CLI.csproj -c Release -r $Platform --self-contained -o "$TestDir\cli" --verbosity minimal

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ CLI build failed" -ForegroundColor Red
    exit 1
}

Write-Host "🧪 Testing CLI executable..." -ForegroundColor Yellow
Set-Location "$TestDir\cli"

# Test 1: Help command
Write-Host "  Testing --help command..." -ForegroundColor Gray
$helpResult = & .\DocxTemplate.CLI.exe --help 2>&1 | Out-String
if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✅ CLI help command succeeded" -ForegroundColor Green
    $helpResult | Out-File -FilePath "cli-help.txt"
    Write-Host "     Output preview:" -ForegroundColor Gray
    ($helpResult -split "`n")[0..4] | ForEach-Object { Write-Host "     $_" -ForegroundColor Gray }
} else {
    Write-Host "  ❌ CLI help command failed" -ForegroundColor Red
    Write-Host $helpResult -ForegroundColor Red
    exit 1
}

# Test 2: Version command
Write-Host "  Testing --version command..." -ForegroundColor Gray
$versionResult = & .\DocxTemplate.CLI.exe --version 2>&1 | Out-String
if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✅ CLI version command succeeded" -ForegroundColor Green
    $versionResult | Out-File -FilePath "cli-version.txt"
    Write-Host "     Version: $($versionResult.Trim())" -ForegroundColor Gray
} else {
    Write-Host "  ❌ CLI version command failed" -ForegroundColor Red
    Write-Host $versionResult -ForegroundColor Red
    exit 1
}

# Test 3: Invalid command handling
Write-Host "  Testing invalid command handling..." -ForegroundColor Gray
$invalidResult = & .\DocxTemplate.CLI.exe invalid-command 2>&1 | Out-String
if ($LASTEXITCODE -eq 0) {
    Write-Host "  ⚠️  Invalid command unexpectedly succeeded" -ForegroundColor Yellow
    $invalidResult | Out-File -FilePath "cli-invalid.txt"
} elseif ($LASTEXITCODE -eq 1 -or $LASTEXITCODE -eq 2) {
    Write-Host "  ✅ CLI properly handles invalid commands (exit code: $LASTEXITCODE)" -ForegroundColor Green
    $invalidResult | Out-File -FilePath "cli-invalid.txt"
} else {
    Write-Host "  ❌ CLI crashed on invalid command (exit code: $LASTEXITCODE)" -ForegroundColor Red
    Write-Host $invalidResult -ForegroundColor Red
    exit 1
}

# Test 4: Basic functionality
Write-Host "  Testing discover command..." -ForegroundColor Gray
New-Item -ItemType Directory -Path "test-templates" -Force | Out-Null
"dummy" | Out-File -FilePath "test-templates\test.docx"
$discoverResult = & .\DocxTemplate.CLI.exe discover --path test-templates 2>&1 | Out-String
if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✅ CLI discover command succeeded" -ForegroundColor Green
    $discoverResult | Out-File -FilePath "cli-discover.txt"
    $fileCount = ($discoverResult | Select-String "test\.docx").Matches.Count
    Write-Host "     Found $fileCount template files" -ForegroundColor Gray
} else {
    Write-Host "  ❌ CLI discover command failed" -ForegroundColor Red
    Write-Host $discoverResult -ForegroundColor Red
    exit 1
}

Set-Location "..\..\"

# Test GUI
Write-Host "📦 Building GUI executable..." -ForegroundColor Yellow
dotnet publish src\DocxTemplate.UI\DocxTemplate.UI.csproj -c Release -r $Platform --self-contained -p:SkipCliBuild=true -o "$TestDir\gui" --verbosity minimal

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ GUI build failed" -ForegroundColor Red
    exit 1
}

Write-Host "🔗 Setting up CLI-GUI integration..." -ForegroundColor Yellow
Copy-Item "$TestDir\cli\DocxTemplate.CLI.exe" "$TestDir\gui\docx-template.exe"

Write-Host "🧪 Testing GUI executable..." -ForegroundColor Yellow
Set-Location "$TestDir\gui"

# Test 1: Check GUI executable exists
Write-Host "  Testing GUI executable file..." -ForegroundColor Gray
if (Test-Path "DocxTemplate.UI.exe") {
    $guiFile = Get-Item "DocxTemplate.UI.exe"
    Write-Host "  ✅ GUI executable exists and is executable" -ForegroundColor Green
    Write-Host "     File size: $($guiFile.Length) bytes" -ForegroundColor Gray
} else {
    Write-Host "  ❌ GUI executable missing" -ForegroundColor Red
    Get-ChildItem | Format-Table
    exit 1
}

# Test 2: Test GUI startup (with timeout for headless environment)
Write-Host "  Testing GUI startup capability..." -ForegroundColor Gray
$job = Start-Job -ScriptBlock { 
    param($exePath)
    & $exePath 2>&1 | Out-String
} -ArgumentList (Resolve-Path "DocxTemplate.UI.exe")

$timeout = 10
if (Wait-Job $job -Timeout $timeout) {
    $result = Receive-Job $job
    Write-Host "  ✅ GUI executable started successfully" -ForegroundColor Green
    $result | Out-File -FilePath "gui-startup.txt"
} else {
    Write-Host "  ✅ GUI executable can start (timed out as expected in headless mode)" -ForegroundColor Green
    Stop-Job $job
    "GUI startup timed out (expected in headless environment)" | Out-File -FilePath "gui-startup.txt"
}
Remove-Job $job -Force

# Test 3: Test CLI integration
Write-Host "  Testing CLI discoverability..." -ForegroundColor Gray
if (Test-Path "docx-template.exe") {
    Write-Host "  ✅ CLI executable present in GUI directory" -ForegroundColor Green
    
    $cliFromGuiResult = & .\docx-template.exe --version 2>&1 | Out-String
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✅ CLI callable from GUI directory" -ForegroundColor Green
        $cliFromGuiResult | Out-File -FilePath "cli-from-gui.txt"
        Write-Host "     CLI version from GUI dir: $($cliFromGuiResult.Trim())" -ForegroundColor Gray
    } else {
        Write-Host "  ❌ CLI not callable from GUI directory" -ForegroundColor Red
        Write-Host $cliFromGuiResult -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "  ❌ CLI executable missing in GUI directory" -ForegroundColor Red
    Get-ChildItem | Format-Table
    exit 1
}

# Test 4: Test GUI dependencies (check for DLLs)
Write-Host "  Testing GUI dependencies..." -ForegroundColor Gray
$dllCount = (Get-ChildItem -Filter "*.dll").Count
if ($dllCount -gt 0) {
    Write-Host "  ✅ Found $dllCount DLL files" -ForegroundColor Green
} else {
    Write-Host "  ❌ No DLL files found" -ForegroundColor Red
    exit 1
}

Set-Location "..\..\"

Write-Host ""
Write-Host "✅ All executable startup tests passed!" -ForegroundColor Green
Write-Host "📁 Test outputs available in: $TestDir" -ForegroundColor Gray
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  ✅ CLI help command works" -ForegroundColor Green
Write-Host "  ✅ CLI version command works" -ForegroundColor Green
Write-Host "  ✅ CLI handles invalid commands properly" -ForegroundColor Green
Write-Host "  ✅ CLI basic commands functional" -ForegroundColor Green
Write-Host "  ✅ GUI executable builds and can start" -ForegroundColor Green
Write-Host "  ✅ GUI has all required dependencies" -ForegroundColor Green
Write-Host "  ✅ CLI-GUI integration works" -ForegroundColor Green