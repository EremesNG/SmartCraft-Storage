[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ValheimManagedDir,
    [string] $PluginAssembly,
    [string] $CecilAssembly = (Join-Path $env:USERPROFILE '.nuget/packages/mono.cecil/0.11.5/lib/netstandard2.0/Mono.Cecil.dll')
)

# Read metadata only: this catches ambiguous or renamed Harmony targets without
# attempting to initialize Unity inside PowerShell. It is not an in-game test.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ([string]::IsNullOrWhiteSpace($PluginAssembly)) {
    $PluginAssembly = Join-Path (Split-Path -Parent $PSScriptRoot) 'bin/Release/net48/SmartCraftStorage.dll'
}
Add-Type -Path $CecilAssembly
$types = @{}
$assemblies = [System.Collections.Generic.List[Mono.Cecil.AssemblyDefinition]]::new()

function Get-AllTypes($items) {
    foreach ($item in $items) {
        $item
        if ($item.HasNestedTypes) { Get-AllTypes $item.NestedTypes }
    }
}

try {
    foreach ($name in @('assembly_valheim.dll', 'assembly_utils.dll', 'assembly_guiutils.dll', 'UnityEngine.UI.dll', 'UnityEngine.CoreModule.dll')) {
        $assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $ValheimManagedDir $name))
        $assemblies.Add($assembly)
        foreach ($type in (Get-AllTypes $assembly.MainModule.Types)) { $types[$type.FullName] = $type }
    }
    $plugin = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($PluginAssembly)
    $assemblies.Add($plugin)
    $checked = 0
    $lifecycleChecked = 0
    $failures = [System.Collections.Generic.List[string]]::new()
    foreach ($patch in (Get-AllTypes $plugin.MainModule.Types)) {
        if ($null -ne $patch.BaseType -and $patch.BaseType.FullName -eq 'UnityEngine.MonoBehaviour') {
            foreach ($message in @($patch.Methods | Where-Object Name -in @('Awake', 'Start', 'Update', 'LateUpdate', 'FixedUpdate', 'OnEnable', 'OnDisable', 'OnDestroy'))) {
                $lifecycleChecked++
                if ($message.Parameters.Count -ne 0 -or $message.IsStatic) {
                    $failures.Add("$($patch.FullName).$($message.Name): Unity lifecycle messages must be parameterless instance methods")
                }
            }
        }
        foreach ($attribute in $patch.CustomAttributes) {
            if ($attribute.AttributeType.FullName -ne 'HarmonyLib.HarmonyPatch') { continue }
            $arguments = $attribute.ConstructorArguments
            if ($arguments.Count -lt 2 -or $arguments[0].Type.FullName -ne 'System.Type' -or $arguments[1].Type.FullName -ne 'System.String') { continue }
            $typeName = $arguments[0].Value.FullName
            $methodName = [string]$arguments[1].Value
            $expected = $null
            if ($arguments.Count -ge 3 -and $arguments[2].Type.FullName -eq 'System.Type[]') {
                $expected = @($arguments[2].Value | ForEach-Object { $_.Value.FullName }) -join ','
            }
            $target = $types[$typeName]
            $methods = @()
            while ($null -ne $target) {
                $methods = @($target.Methods | Where-Object {
                    $_.Name -eq $methodName -and ($null -eq $expected -or (($_.Parameters | ForEach-Object { $_.ParameterType.FullName }) -join ',') -eq $expected)
                })
                if ($methods.Count -gt 0 -or $null -eq $target.BaseType) { break }
                $target = $types[$target.BaseType.FullName]
            }
            $checked++
            if ($methods.Count -ne 1) {
                $failures.Add("$($patch.FullName): $typeName.$methodName resolved to $($methods.Count) methods")
                continue
            }
            $names = @($methods[0].Parameters | ForEach-Object Name)
            foreach ($hook in @($patch.Methods | Where-Object Name -in @('Prefix', 'Postfix', 'Finalizer'))) {
                foreach ($parameter in $hook.Parameters) {
                    if ($parameter.Name.StartsWith('__') -or @($parameter.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'HarmonyLib.HarmonyArgument' }).Count -gt 0) { continue }
                    if ($parameter.Name -notin $names) { $failures.Add("$($patch.FullName).$($hook.Name): target has no '$($parameter.Name)' parameter") }
                }
            }
        }
    }
    if ($failures.Count -gt 0) { throw ($failures -join [Environment]::NewLine) }
    Write-Output "PASS $checked explicit Harmony targets and named hook parameters resolve in the local game references."
    Write-Output "PASS $lifecycleChecked Unity lifecycle method signatures."
}
finally {
    foreach ($assembly in $assemblies) { $assembly.Dispose() }
}
