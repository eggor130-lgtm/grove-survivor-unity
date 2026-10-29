$ErrorActionPreference = 'Stop'
Add-Type -Path "$PSHOME/Microsoft.CodeAnalysis.dll"
Add-Type -Path "$PSHOME/Microsoft.CodeAnalysis.CSharp.dll"
$sources = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'Assets') -Recurse -Filter '*.cs'
foreach ($source in $sources) {
    $tree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText($source.FullName))
    $errors = @($tree.GetDiagnostics() | Where-Object Severity -eq 'Error')
    if ($errors.Count) { throw "$($source.Name): $($errors -join [Environment]::NewLine)" }
}
Write-Output "PASS: C# syntax for $($sources.Count) files (Roslyn)."

# Only pure game rules are compiled here. These small shims do not validate Unity APIs.
$shim = @'
namespace UnityEngine {
    public static class Mathf {
        public static float Max(float a, float b) => System.Math.Max(a,b);
        public static float Min(float a, float b) => System.Math.Min(a,b);
        public static float Pow(float a, float b) => (float)System.Math.Pow(a,b);
        public static int RoundToInt(float a) => (int)System.Math.Round(a);
    }
    public static class Random {
        static readonly System.Random random = new System.Random(41);
        public static int Range(int a, int b) => random.Next(a,b);
    }
}
'@
$rules = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Assets/Scripts/GameRules.cs'))
Add-Type -TypeDefinition ($rules + [Environment]::NewLine + $shim)
function Assert-Rule([bool]$condition, [string]$message) { if (-not $condition) { throw $message } }
$hero = [GroveSurvivor.HeroStats]::new()
Assert-Rule ($hero.Health -eq 100 -and $hero.Damage -eq 24 -and $hero.Level -eq 1) 'Fresh hero defaults'
$hero.Experience = 17
Assert-Rule $hero.TryLevelUp() 'XP threshold'
Assert-Rule ($hero.Level -eq 2 -and $hero.Experience -eq 7 -and $hero.RequiredExperience -eq 22) 'Level scaling / XP overflow'
Assert-Rule (-not $hero.TryLevelUp()) 'No level without XP'
$choice = [GroveSurvivor.UpgradeChoice]::new()
$choice.Kind = [GroveSurvivor.UpgradeKind]::Damage; $choice.Value = 25
$hero.Apply($choice)
Assert-Rule ($hero.Damage -eq 30) 'Damage upgrade'
$choice.Kind = [GroveSurvivor.UpgradeKind]::AttackSpeed; $choice.Value = 18
1..100 | ForEach-Object { $hero.Apply($choice) }
Assert-Rule ([Math]::Abs($hero.AttackInterval - .16) -lt .0001) 'Attack interval minimum'
$hero.Health = 40
$choice.Kind = [GroveSurvivor.UpgradeKind]::Health; $choice.Value = 30
$hero.Apply($choice)
Assert-Rule ($hero.Health -eq 70 -and $hero.MaxHealth -eq 130) 'Health upgrade and healing'
1..100 | ForEach-Object {
    $choices = [GroveSurvivor.UpgradeChoice]::Roll()
    Assert-Rule ($choices.Count -eq 3) 'Three choices'
    Assert-Rule (@($choices.Kind | Sort-Object -Unique).Count -eq 3) 'Distinct choices'
    foreach ($upgrade in $choices) {
        if ($upgrade.Kind -eq [GroveSurvivor.UpgradeKind]::AttackSpeed) {
            Assert-Rule ($upgrade.Value -in 12,15,18) 'Speed roll bounds'
        } else { Assert-Rule ($upgrade.Value -in 20,25,30) 'Damage/health roll bounds' }
    }
}
$easy = [GroveSurvivor.DifficultyRules]::Get([GroveSurvivor.Difficulty]::Wanderer)
$normal = [GroveSurvivor.DifficultyRules]::Get([GroveSurvivor.Difficulty]::Survivor)
$hard = [GroveSurvivor.DifficultyRules]::Get([GroveSurvivor.Difficulty]::Nightmare)
Assert-Rule ($easy.Health -lt $normal.Health -and $hard.Health -gt $normal.Health) 'Difficulty HP scaling'
Assert-Rule ($easy.SpawnInterval -gt $normal.SpawnInterval -and $hard.SpawnInterval -lt $normal.SpawnInterval) 'Difficulty spawn scaling'
Assert-Rule ($easy.WaveSize -lt $normal.WaveSize -and $hard.WaveSize -gt $normal.WaveSize) 'Difficulty horde sizes'

$scene = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Assets/Scenes/Main.unity'))
$meta = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Assets/Scripts/GroveGame.cs.meta'))
$guid = [regex]::Match($meta, 'guid: ([0-9a-f]+)').Groups[1].Value
Assert-Rule ($scene.Contains($guid)) 'Scene bootstrap script reference'
$manifest = Get-Content (Join-Path $PSScriptRoot 'Packages/manifest.json') -Raw | ConvertFrom-Json
Assert-Rule ($manifest.dependencies.'com.unity.ugui' -eq '2.0.0') 'Unity UI dependency'
Write-Output 'PASS: hero reset, XP threshold / overflow, upgrade application, speed floor, 100 random choices, all difficulties, scene reference and package manifest.'
Write-Output 'LIMIT: this does not replace Unity import, compilation, Play Mode or a Windows build.'
