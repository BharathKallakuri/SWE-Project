# Smoke test: drives two ScreenShare.Demo windows with UI Automation (host + guest,
# both sharing the test pattern) and prints what each one receives.
# Build first (the solution or Visual Studio builds into bin\x64, the project alone into bin):
#   dotnet build CopProject.slnx   or   dotnet build ScreenShare.Demo/ScreenShare.Demo.csproj
param([string]$Exe = (Get-ChildItem -Path (Join-Path $PSScriptRoot 'bin') -Recurse -Filter 'ScreenShare.Demo.exe' |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName)

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$AE = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]

function Window-Of($proc) {
    for ($i = 0; $i -lt 50; $i++) {
        $proc.Refresh()
        if ($proc.MainWindowHandle -ne 0) { return $AE::FromHandle($proc.MainWindowHandle) }
        Start-Sleep -Milliseconds 200
    }
    throw "no window for process $($proc.Id)"
}

function All($root) { $root.FindAll($TS::Descendants, [System.Windows.Automation.Condition]::TrueCondition) }

function Find-ByName($root, $name) {
    foreach ($e in (All $root)) { if ($e.Current.Name -eq $name) { return $e } }
    throw "element '$name' not found"
}

function Find-NameLike($root, $pattern) {
    foreach ($e in (All $root)) { if ($e.Current.Name -like $pattern) { return $e } }
    return $null
}

function Invoke-El($e) { $e.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }
function Toggle-El($e) { $e.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Toggle() }
function Set-Value($e, $v) { $e.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($v) }

function Find-Edit($root, $header) {
    foreach ($e in (All $root)) {
        if ($e.Current.ControlType -eq [System.Windows.Automation.ControlType]::Edit -and $e.Current.Name -eq $header) { return $e }
    }
    throw "edit '$header' not found"
}

# the share button: the only toggle in the meeting view (the start checkbox is collapsed by then)
function Find-ShareToggle($root) {
    foreach ($e in (All $root)) {
        $ct = $e.Current.ControlType
        if (($ct -eq [System.Windows.Automation.ControlType]::Button -or $ct -eq [System.Windows.Automation.ControlType]::CheckBox) -and -not $e.Current.IsOffscreen) {
            $p = $null
            if ($e.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern, [ref]$p)) { return $e }
        }
    }
    throw "share toggle not found"
}

function Stats($root) {
    $s = Find-NameLike $root "*KB/s*"
    if ($s) { return $s.Current.Name } else { return "(no stats yet)" }
}

$a = Start-Process -FilePath $Exe -WorkingDirectory (Split-Path $Exe) -PassThru
$b = $null
try {
    $wa = Window-Of $a
    Set-Value (Find-Edit $wa "Your name") "Hosty"
    Toggle-El (Find-ByName $wa "Share a moving test pattern instead of my screen")
    Invoke-El (Find-ByName $wa "Host a meeting")
    Start-Sleep -Seconds 2
    $info = (Find-NameLike $wa "Hosting as*").Current.Name
    "A: $info"
    $endpoint = ($info -split "join with: ")[1].Trim()

    $b = Start-Process -FilePath $Exe -WorkingDirectory (Split-Path $Exe) -PassThru
    $wb = Window-Of $b
    Set-Value (Find-Edit $wb "Your name") "Guesty"
    Toggle-El (Find-ByName $wb "Share a moving test pattern instead of my screen")
    Set-Value (Find-Edit $wb "Or join a host (IP:port)") $endpoint
    Invoke-El (Find-ByName $wb "Join")
    Start-Sleep -Seconds 2
    "B: " + (Find-NameLike $wb "Joined*").Current.Name

    # host shares, guest watches
    Toggle-El (Find-ShareToggle $wa)
    Start-Sleep -Seconds 4
    "after host shares:"
    "  A stats: " + (Stats $wa)
    "  B stats: " + (Stats $wb)

    # guest shares too, host watches
    Toggle-El (Find-ShareToggle $wb)
    Start-Sleep -Seconds 4
    "after guest shares too:"
    "  A stats: " + (Stats $wa)
    "  B stats: " + (Stats $wb)

    # host stops sharing
    Toggle-El (Find-ShareToggle $wa)
    Start-Sleep -Seconds 3
    "after host stops:"
    "  A stats: " + (Stats $wa)
    "  B stats: " + (Stats $wb)

    $a.Refresh(); $b.Refresh()
    "A exited: $($a.HasExited)  B exited: $($b.HasExited)"
}
finally {
    foreach ($p in @($a, $b)) { if ($p -and -not $p.HasExited) { Stop-Process -Id $p.Id -Force } }
}
