# Plan de Mejoras — Pipeline CI

## Cambios a realizar en `.github/workflows/ci.yml`

### 1. Actualizar `actions/setup-dotnet` (4 ocurrencias)

Encontrar `uses: actions/setup-dotnet@v4` y reemplazar por `uses: actions/setup-dotnet@v5` en:
- Línea 36 (job `build`)
- Línea 114 (job `codeql`)
- Línea 165 (job `sast`)
- Línea 200 (job `sca`)

### 2. Actualizar `actions/dependency-review-action`

Línea 148: `uses: actions/dependency-review-action@v4` → `uses: actions/dependency-review-action@v5`

### 3. Actualizar `zaproxy/action-baseline`

Línea 305: `uses: zaproxy/action-baseline@v0.14.0` → `uses: zaproxy/action-baseline@v0.15.0`

### 4. Agregar NuGet caching en 4 jobs

Insertar este bloque después del step `Setup .NET 10.0` en los jobs `build`, `codeql`, `sast`, `sca`:

```yaml
      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj', '**/*.props') }}
```

#### Ubicación exacta para cada job:

**Job `build`** — después de `Setup .NET 10.0`, antes de `Install SonarScanner`:
```
      - name: Setup .NET 10.0
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: 10.0.x

      - name: Cache NuGet packages          <-- INSERTAR AQUÍ
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj', '**/*.props') }}

      - name: Install SonarScanner
```

**Job `codeql`** — después de `Setup .NET 10.0`, antes de `Initialize CodeQL`:
```
      - name: Setup .NET 10.0
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: 10.0.x

      - name: Cache NuGet packages          <-- INSERTAR AQUÍ
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj', '**/*.props') }}

      - name: Initialize CodeQL
```

**Job `sast`** — después de `Setup .NET 10.0`, antes de `Restore`:
```
      - name: Setup .NET 10.0
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: 10.0.x

      - name: Cache NuGet packages          <-- INSERTAR AQUÍ
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj', '**/*.props') }}

      - name: Restore
```

**Job `sca`** — después de `Setup .NET 10.0`, antes de `Restore`:
```
      - name: Setup .NET 10.0
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: 10.0.x

      - name: Cache NuGet packages          <-- INSERTAR AQUÍ
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj', '**/*.props') }}

      - name: Restore
```

## Verificación

Después de aplicar los cambios, hacer push y revisar que el pipeline pase correctamente.
