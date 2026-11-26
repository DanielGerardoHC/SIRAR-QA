#!/usr/bin/env bash
# Static analysis with the .NET analyzers and Security Code Scan on the original and corrected code
set -u
REPO=/repo
QA=$REPO/04_pruebas/SIRAR-QA
OUT=$QA/reportes/analisis_estatico
WORK=/tmp/sirar-estatico
git config --global --add safe.directory '*'
rm -rf "$WORK" "$OUT" && mkdir -p "$WORK/original" "$WORK/corregida" "$OUT"

git -C "$REPO" archive 7156f04 | tar -x -C "$WORK/original"
cp -r "$QA"/{Controllers,Data,Models,Security,Services,Program.cs,appsettings.json,SIRAR.Auditoria.csproj,.editorconfig} "$WORK/corregida/"
cp "$QA/.editorconfig" "$WORK/original/"

cat > "$WORK/Directory.Build.props" << 'PROPS'
<Project>
  <PropertyGroup>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest-all</AnalysisLevel>
    <AnalysisModeSecurity>All</AnalysisModeSecurity>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="SecurityCodeScan.VS2019" Version="5.6.7" PrivateAssets="all" />
  </ItemGroup>
</Project>
PROPS

for v in original corregida; do
  dotnet build "$WORK/$v/SIRAR.Auditoria.csproj" -c Release -t:Rebuild -p:ErrorLog="$OUT/sarif_$v.json" 2>&1 \
    | grep -E "warning [A-Z]+[0-9]+" | sed -E 's#^/tmp/sirar-estatico/[a-z]+/##; s# \[/tmp.*##' | sort -u > "$OUT/advertencias_$v.txt"
  echo "== $v: $(wc -l < "$OUT/advertencias_$v.txt") advertencias"
  grep -oE "warning [A-Z]+[0-9]+" "$OUT/advertencias_$v.txt" | sort | uniq -c | sort -rn
done

for v in original corregida; do
  echo "== Paquetes con vulnerabilidades conocidas: $v"
  dotnet list "$WORK/$v/SIRAR.Auditoria.csproj" package --vulnerable --include-transitive 2>&1 | grep -E "^\s+>|no vulnerable" | tee "$OUT/paquetes_vulnerables_$v.txt"
done
