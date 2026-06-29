# SCA — Software Composition Analysis

## Archivos
| Archivo | Propósito |
|---------|-----------|
| `SCA/SnykScanTests.cs` | 6 pruebas de integración con Snyk CLI |
| `SCA/NuGetAuditTests.cs` | 7 pruebas de auditoría NuGet y análisis de dependencias |
| `SCA/snyk-ignore.json` | Configuración de falsos positivos y exclusiones para Snyk |
| `Directory.Build.props` | Configuración global de NuGet Audit + analizadores |

## Pruebas

### SnykScanTests (6 activas) — OWASP A06:2021

| Prueba | Qué verifica |
|--------|-------------|
| `SnykIgnoreFile_Exists` | El archivo `snyk-ignore.json` existe |
| `SnykIgnoreFile_HasValidJson` | JSON válido (sin `{{` sin reemplazar) con `ignore`/`exclude` |
| `SnykIgnoreFile_ExpiryDatesAreInFuture` | Fechas de expiración futuras (evita ignorados perpetuos) |
| `SnykCli_IsAvailable` | Detección condicional de Snyk CLI |
| `SnykScan_NoHighSeverityVulnerabilities` | Escanea dependencias con `snyk test --severity-threshold=high` |
| `TransitiveDependencies_AreTracked` | `project.assets.json` existe con dependencias transitivas |

### NuGetAuditTests (7 activas) — OWASP A06:2021 + A08:2021

| Prueba | Qué verifica |
|--------|-------------|
| `DirectoryBuildProps_EnablesNuGetAudit` | `<NuGetAudit>true</NuGetAudit>` |
| `DirectoryBuildProps_AuditModeIsAll` | `<NuGetAuditMode>all</NuGetAuditMode>` |
| `DirectoryBuildProps_AuditLevelIsLow` | `<NuGetAuditLevel>low</NuGetAuditLevel>` |
| `NuGetAudit_ReportsNoVulnerabilities` | `dotnet list package --vulnerable --include-transitive` sin fallos |
| `AllTestProjects_ReferenceCoverletForCoverage` | Todos los tests incluyen `coverlet.collector` |
| `SolutionFile_IncludesAllProjects` | `WebDevSecOps.slnx` incluye todos los `.csproj` |
| `Project_UsesLatestPackageVersions` | Sin `Version="*"` (floating versions inseguras) |

## Configuración NuGet Audit (`Directory.Build.props`)

```xml
<NuGetAudit>true</NuGetAudit>
<NuGetAuditMode>all</NuGetAuditMode>
<NuGetAuditLevel>low</NuGetAuditLevel>
```

## Buenas prácticas aplicadas

1. **Auditoría automática en build** — NuGet Audit integrado, detecta vulnerabilidades en cada `dotnet build`.
2. **Cobertura completa** — `AuditMode=all` incluye transitivas, `AuditLevel=low` captura hasta baja severidad.
3. **Ignorados con expiración** — `snyk-ignore.json` requiere `expires` en cada regla, validado por prueba.
4. **Falsos positivos documentados** — Cada entrada incluye `reason`.
5. **Exclusiones controladas** — `docker` e `iac` excluidos explícitamente.
6. **CLI condicional** — Pruebas que requieren `snyk` detectan su ausencia sin fallar.
7. **Sin floating versions** — No se permite `Version="*"`.
8. **Cobertura de código** — `coverlet.collector` en todos los proyectos test.
9. **Analizadores estáticos** — `SecurityCodeScan.Vs2019` y `SonarAnalyzer.CSharp` globales.
