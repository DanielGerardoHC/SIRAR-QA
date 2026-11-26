#!/usr/bin/env bash
# Collects coverage for unit and API tests and builds the HTML report
set -e
QA=/repo/04_pruebas/SIRAR-QA
OUT=$QA/reportes/cobertura
rm -rf "$OUT" "$QA/reportes/pruebas" /tmp/cov && mkdir -p "$OUT"
cd "$QA"
dotnet test SIRAR.sln --collect:"XPlat Code Coverage" --results-directory /tmp/cov --logger "trx" && mkdir -p $QA/reportes/pruebas && cp /tmp/cov/*.trx $QA/reportes/pruebas/ 2>&1 | grep -E "Passed!|Failed!" || true
dotnet tool install -g dotnet-reportgenerator-globaltool >/dev/null 2>&1 || true
export PATH="$PATH:/root/.dotnet/tools"
reportgenerator -reports:"/tmp/cov/**/coverage.cobertura.xml" -targetdir:"$OUT" -reporttypes:"Html;TextSummary;Cobertura" -assemblyfilters:"+SIRAR.Auditoria" >/dev/null
cat "$OUT/Summary.txt"
