# Auditoría Forense, Informática y Plan de QA del Sistema SIRAR

Examen de Suficiencia Integral · Gestión de Calidad de Software COIDS3904 · ITCA-FEPADE

Daniel Gerardo Hernández Calderón · 066823

## Contenido

| Carpeta | Contenido |
|---|---|
| `01_cadena_custodia` | Hashes SHA-256 de la evidencia |
| `02_forense` | Scripts y resultados del análisis forense |
| `04_pruebas/SIRAR-QA` | Sistema auditado con sus correcciones, suite de pruebas automatizadas y reportes |

## Ejecución

Requiere Docker.

```powershell
docker compose up -d --build
docker compose run --rm pruebas
docker compose down
```

| Servicio | Descripción |
|---|---|
| `api-original` | Versión auditada, commit `7156f04`, en `http://localhost:5080` |
| `api-corregida` | Versión corregida, construida con su Dockerfile, en `http://localhost:5081` |
| `pruebas` | Suite automatizada contra ambas versiones |
| `zap-original` y `zap-corregida` | Escaneo OWASP ZAP de cada versión |
