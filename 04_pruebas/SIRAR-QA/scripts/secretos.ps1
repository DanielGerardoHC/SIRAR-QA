# Secret scanning with Gitleaks: original history and corrected source
$repo = (Resolve-Path "$PSScriptRoot\..\..\..").Path
docker run --rm -v "${repo}:/repo" zricethezav/gitleaks:latest git /repo --log-opts="7156f04" --redact=0
docker run --rm -v "${repo}:/repo" --entrypoint sh zricethezav/gitleaks:latest -c "mkdir /tmp/app && cd /repo/04_pruebas/SIRAR-QA && cp -r Controllers Data Models Security Services Program.cs appsettings.json /tmp/app/ && gitleaks dir /tmp/app"
