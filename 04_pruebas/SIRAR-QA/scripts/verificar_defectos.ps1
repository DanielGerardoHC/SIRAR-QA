# Live demo: same test suite against the original code and the corrected code
$repo = (Resolve-Path "$PSScriptRoot\..\..\..").Path
docker run --rm -v "${repo}:/repo" -v nuget8:/root/.nuget/packages -e OUT=/tmp/salida `
  mcr.microsoft.com/dotnet/sdk:8.0 bash /repo/04_pruebas/SIRAR-QA/scripts/verificar_defectos.sh
