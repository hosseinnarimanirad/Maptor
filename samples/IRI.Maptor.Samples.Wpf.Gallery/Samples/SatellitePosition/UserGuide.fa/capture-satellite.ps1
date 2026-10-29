# Drives the gallery's Satellite positions sample on the terminal's monitor and saves screenshots of
# its states and parts into $OutDir. Full window is 1280x800; crops come from UIA bounding rectangles.
param(
    [Parameter(Mandatory = $true)] [string] $Exe,
    [Parameter(Mandatory = $true)] [string] $OutDir
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class W {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct MONITORINFO { public int cbSize; public RECT rcMonitor; public RECT rcWork; public uint dwFlags; }
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [DllImport("user32.dll")] public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO info);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, int dx, int dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out RECT rect, int size);
    public static RECT Frame(IntPtr hwnd) { RECT r; if (DwmGetWindowAttribute(hwnd, 9, out r, Marshal.SizeOf(typeof(RECT))) != 0) GetWindowRect(hwnd, out r); return r; }
    public static void Click(int x, int y) { SetCursorPos(x, y); System.Threading.Thread.Sleep(80); mouse_event(2, 0, 0, 0, UIntPtr.Zero); System.Threading.Thread.Sleep(60); mouse_event(4, 0, 0, 0, UIntPtr.Zero); }
    public static void Key(byte vk) { keybd_event(vk, 0, 0, UIntPtr.Zero); System.Threading.Thread.Sleep(40); keybd_event(vk, 0, 2, UIntPtr.Zero); }
}
'@
$A = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]
function ById($root, $id) {
    $c = New-Object System.Windows.Automation.PropertyCondition($A::AutomationIdProperty, $id)
    $e = $root.FindFirst($TS::Descendants, $c); if (-not $e) { throw "element '$id' not found" }; $e
}
function ByName($root, $name) {
    $c = New-Object System.Windows.Automation.PropertyCondition($A::NameProperty, $name)
    $e = $root.FindFirst($TS::Descendants, $c); if (-not $e) { throw "element named '$name' not found" }; $e
}
function Invoke-El($e) { $e.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }
function Select-El($e) { $e.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select() }
function Rect($e) { $r = $e.Current.BoundingRectangle; [int[]]@([int]$r.Left, [int]$r.Top, [int]$r.Width, [int]$r.Height) }
function Shot([int[]] $r, [string] $name, [int] $pad = 0) {
    $x = $r[0] - $pad; $y = $r[1] - $pad; $w = $r[2] + 2 * $pad; $h = $r[3] + 2 * $pad
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($x, $y, 0, 0, (New-Object System.Drawing.Size $w, $h))
    $g.Dispose()
    $out = Join-Path $OutDir ($name + '.png')
    $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
    Write-Host ("saved {0} ({1}x{2})" -f $name, $w, $h)
}
function ShotEl($e, [string] $name, [int] $pad = 8) { Shot (Rect $e) $name $pad }
# bounding box of several elements (panels themselves are not in the UIA tree)
function Union([object[]] $els, [int] $extraBottom = 0) {
    $l = 1000000; $t = 1000000; $r = -1; $b = -1
    foreach ($e in $els) { $x = Rect $e; if ($x[0] -lt $l) { $l = $x[0] }; if ($x[1] -lt $t) { $t = $x[1] }; if ($x[0] + $x[2] -gt $r) { $r = $x[0] + $x[2] }; if ($x[1] + $x[3] -gt $b) { $b = $x[1] + $x[3] } }
    [int[]]@($l, $t, ($r - $l), ($b - $t + $extraBottom))
}
function Frame() { $f = [W]::Frame($hwnd); [int[]]@($f.Left, $f.Top, ($f.Right - $f.Left), ($f.Bottom - $f.Top)) }

New-Item -ItemType Directory -Force $OutDir | Out-Null
$fg = [W]::GetForegroundWindow()
$mon = [W]::MonitorFromWindow($fg, 2)
$mi = New-Object W+MONITORINFO
$mi.cbSize = [Runtime.InteropServices.Marshal]::SizeOf($mi)
[void][W]::GetMonitorInfo($mon, [ref]$mi)
$originX = $mi.rcWork.Left + 40; $originY = $mi.rcWork.Top + 40

$proc = Start-Process -FilePath $Exe -PassThru
try {
    $hwnd = [IntPtr]::Zero
    for ($i = 0; $i -lt 60 -and $hwnd -eq [IntPtr]::Zero; $i++) { Start-Sleep -Milliseconds 500; $proc.Refresh(); $hwnd = $proc.MainWindowHandle }
    if ($hwnd -eq [IntPtr]::Zero) { throw 'gallery window did not appear' }
    Start-Sleep -Seconds 2
    $WW = 1600; $WH = 980
    [void][W]::SetWindowPos($hwnd, [IntPtr]::Zero, $originX, $originY, $WW, $WH, 0x0040)
    Start-Sleep -Milliseconds 500
    $f = [W]::Frame($hwnd)
    $dw = $WW - ($f.Right - $f.Left); $dh = $WH - ($f.Bottom - $f.Top)
    $ox = $originX - ($f.Left - $originX); $oy = $originY - ($f.Top - $originY)
    [void][W]::SetWindowPos($hwnd, [IntPtr](-1), $ox, $oy, $WW + $dw, $WH + $dh, 0x0040)
    Start-Sleep -Milliseconds 500
    [void][W]::SetForegroundWindow($hwnd)

    $root = $A::FromHandle($hwnd)
    $search = ById $root 'searchBox'
    $v = $search.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    $v.SetValue('Satellite positions'); Start-Sleep -Milliseconds 800; $v.SetValue(''); Start-Sleep -Seconds 6

    # 1. whole sample with the start state
    Shot (Frame) '01-overview'
    $map = ById $root 'map'
    $satTab = ById $root 'satellitesTab'
    $panelRect = Union @($satTab, (ById $root 'satelliteList'), (ById $root 'intervalCombo'))
    Shot $panelRect '02-satellites-panel' 10
    ShotEl (ById $root 'timeBar') '03-time-bar' 6

    # 2. choose Landsat 8 from the list: the read-out and the overlays on the map
    $list = ById $root 'satelliteList'
    $items = $list.FindAll($TS::Children, (New-Object System.Windows.Automation.PropertyCondition($A::ControlTypeProperty, [System.Windows.Automation.ControlType]::ListItem)))
    $target = $null
    foreach ($it in $items) {
        $t = $it.FindFirst($TS::Descendants, (New-Object System.Windows.Automation.PropertyCondition($A::NameProperty, 'LANDSAT 8')))
        if ($t) { $target = $it; break }
    }
    if (-not $target) { throw 'LANDSAT 8 row not found' }
    Select-El $target; Start-Sleep -Seconds 3
    Shot (Union @((ById $root 'readoutName'), (ById $root 'timeValue'), (ById $root 'sensorValue'), (ById $root 'ageText'))) '04-readout' 16
    ShotEl $map '05-map-landsat' 0
    Shot (Frame) '06-overview-landsat'

    # 3. the Load a group menu
    Invoke-El (ById $root 'groupButton'); Start-Sleep -Milliseconds 900
    $gm = Union @($satTab, (ById $root 'groupButton')) 300
    Shot @($gm[0], $gm[1], ($gm[2] + 170), $gm[3]) '07-group-menu' 10
    [W]::Key(0x1B); Start-Sleep -Milliseconds 500   # Esc closes the menu

    # 4. passes tab: empty, then a point region, then results
    Select-El (ById $root 'passesTab'); Start-Sleep -Seconds 1
    function PassesRect() { Union @((ById $root 'passesTab'), (ById $root 'findPassesButton'), (ById $root 'passesList')) }
    function PassesTop() { Union @((ById $root 'passesTab'), (ById $root 'findPassesButton')) 50 }
    Shot (PassesTop) '08-passes-empty' 10
    $pt = Rect (ByName $root 'Point')
    [W]::Click([int]($pt[0] + $pt[2] / 2), [int]($pt[1] + $pt[3] / 2)); Start-Sleep -Milliseconds 600
    $m = Rect $map
    [W]::Click([int]($m[0] + $m[2] * 0.55), [int]($m[1] + $m[3] * 0.42)); Start-Sleep -Seconds 2
    Shot (PassesTop) '09-passes-region' 10
    Invoke-El (ById $root 'findPassesButton')
    $cancel = ById $root 'cancelPassesButton'
    for ($i = 0; $i -lt 120; $i++) { Start-Sleep -Milliseconds 500; if ($cancel.Current.IsOffscreen) { break } }
    Start-Sleep -Seconds 2
    Shot (PassesRect) '10-passes-results' 10
    $plist = ById $root 'passesList'
    $prow = $plist.FindFirst($TS::Children, (New-Object System.Windows.Automation.PropertyCondition($A::ControlTypeProperty, [System.Windows.Automation.ControlType]::ListItem)))
    if ($prow) { Select-El $prow; Start-Sleep -Seconds 3; Shot (Frame) '11-overview-pass'; ShotEl $map '12-map-pass' 0 }

    # 5. imaged passes
    Select-El (ById $root 'imagedRadio'); Start-Sleep -Milliseconds 800
    Invoke-El (ById $root 'findPassesButton')
    for ($i = 0; $i -lt 120; $i++) { Start-Sleep -Milliseconds 500; if ($cancel.Current.IsOffscreen) { break } }
    Start-Sleep -Seconds 2
    Shot (PassesRect) '13-passes-imaged' 10
}
finally {
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
}
