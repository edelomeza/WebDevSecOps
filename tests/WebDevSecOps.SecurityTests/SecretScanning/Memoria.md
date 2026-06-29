# SecretScanning — Detección de Secretos

## Archivos
| Archivo | Propósito |
|---------|-----------|
| `SecretScanning/GitleaksScanTests.cs` | 7 pruebas de detección de secretos con Gitleaks |
| `SecretScanning/.gitleaks.toml` | Configuración personalizada de reglas + allowlist |

## Pruebas (7 activas) — OWASP A05:2021 + A08:2021

| Prueba | Qué verifica |
|--------|-------------|
| `GitleaksConfig_Exists` | El archivo `.gitleaks.toml` existe |
| `GitleaksConfig_HasCustomRules` | Tiene `[[rules]]`, `custom-hardcoded-secret`, `custom-private-key` y `[allowlist]` |
| `GitleaksConfig_ExtendsDefaultRules` | `useDefault = true` — extiende reglas base, no las reemplaza |
| `GitleaksCli_IsAvailable` | Detección condicional de Gitleaks CLI |
| `GitleaksScan_NoHighConfidenceLeaks` | `gitleaks detect --no-git` buscando secretos en todo el proyecto |
| `ProjectHasNoGitIgnoredSecrets` | `.gitignore` contiene `*.key`, `*.pem`, `secrets.*`, `appsettings.*.local` |
| `ConfigFiles_DoNotContainHardcodedCredentials` | Ningún `appsettings*.json` tiene `Password=` o `pwd=` |

## Configuración Gitleaks (`.gitleaks.toml`)

```toml
[extend]
useDefault = true

[[rules]]
id = "custom-hardcoded-secret"
regex = '''(?i)(?:secret|password|token|apikey|...).{0,5}[:=].{0,5}["'].{8,}["']'''

[[rules]]
id = "custom-private-key"
regex = '''-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----'''

[allowlist]
paths = [
    '''(?i).*\\be2e\\b.*''',
    '''(?i).*\\btests?\\b.*''',
    '''(?i).*\\bmock\\b.*''',
    '''\.git/''',
    '''_test\.cs$''',
    '''Tests\.cs$''',
]
```

## Buenas prácticas aplicadas

1. **Reglas personalizadas + defaults** — `useDefault = true` mantiene las 170+ reglas base de Gitleaks.
2. **Detección de credenciales hardcodeadas** — Regex para `password`, `secret`, `token`, `apikey`, `connectionstring`.
3. **Detección de llaves privadas** — Captura RSA, EC, OPENSSH.
4. **Allowlist controlado** — Exclusiones para tests, mocks, node_modules, archivos de configuración.
5. **Gitignore defensivo** — Validación de patrones anti-secretos en `.gitignore`.
6. **Archivos de configuración limpios** — Verificación de `appsettings.json` sin credenciales.
7. **Escaneo sin git** — `--no-git` para entorno CI sin historial.
8. **CLI condicional** — Pruebas saltan si Gitleaks no está instalado.
