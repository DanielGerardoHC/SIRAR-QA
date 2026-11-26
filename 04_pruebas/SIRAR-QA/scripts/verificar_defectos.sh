#!/usr/bin/env bash
# Runs the API test suite against the original audited code and against the corrected code
set -u
REPO=/repo
QA=$REPO/04_pruebas/SIRAR-QA
WORK=/tmp/sirar-verificacion
OUT=${OUT:-$QA/reportes/verificacion}
COMMIT_ORIGINAL=7156f04

git config --global --add safe.directory '*'
rm -rf "$WORK" && mkdir -p "$WORK/original" "$OUT"

git -C "$REPO" archive "$COMMIT_ORIGINAL" | tar -x -C "$WORK/original"
cp -r "$QA/SIRAR.Tests.Api" "$WORK/SIRAR.Tests.Api"
rm -rf "$WORK/SIRAR.Tests.Api/bin" "$WORK/SIRAR.Tests.Api/obj"
sed -i 's#\.\./SIRAR.Auditoria.csproj#../original/SIRAR.Auditoria.csproj#' "$WORK/SIRAR.Tests.Api/SIRAR.Tests.Api.csproj"

echo "== Version original $COMMIT_ORIGINAL"
dotnet test "$WORK/SIRAR.Tests.Api/SIRAR.Tests.Api.csproj" \
  --logger "console;verbosity=normal" --logger "trx;LogFileName=original.trx" --results-directory "$OUT" 2>&1 \
  | grep -E '^\s+(Passed|Failed) |Total tests|^\s+(Passed|Failed):' | tee "$OUT/resultado_version_original.txt"

echo "== Version corregida"
dotnet test "$QA/SIRAR.Tests.Api/SIRAR.Tests.Api.csproj" \
  --logger "console;verbosity=normal" --logger "trx;LogFileName=corregida.trx" --results-directory "$OUT" 2>&1 \
  | grep -E '^\s+(Passed|Failed) |Total tests|^\s+(Passed|Failed):' | tee "$OUT/resultado_version_corregida.txt"
