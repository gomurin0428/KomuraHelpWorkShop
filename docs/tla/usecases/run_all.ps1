$ErrorActionPreference = 'Stop'
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Root = (Resolve-Path (Join-Path $ScriptDir '..\..\..')).Path
python (Join-Path $Root 'tools\run_tla_usecase_models.py')
