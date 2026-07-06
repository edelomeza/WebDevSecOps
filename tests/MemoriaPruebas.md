# Memoria de Pruebas — Lecciones Aprendidas

## 1. Errores Comunes de Compilación

### CS0121: Ambigüedad en Moq ReturnsAsync
- **Causa**: `.ReturnsAsync(null)` o `.ReturnsAsync((Type?)null)` no resuelve
  la sobrecarga entre `ReturnsAsync(TResult)` y `ReturnsAsync(Func<TResult>)`
- **Fix correcto**: `.ReturnsAsync(default(Type))` o `.ReturnsAsync(ItExpr.IsNull<T>())`
- **Fix incorrecto**: `.ReturnsAsync(null)` — sigue ambiguo
- **Archivo de ejemplo**: `UpdateTests.cs:60`

### S1481: Variable no usada (SonarQube)
- **Causa**: Variable desestructura de tupla pero no se usa en el test
- **Fix**: Reemplazar con `_` en la desestructuración
- **Archivo de ejemplo**: `CreateTests.cs:65`

## 2. Aserciones de Mensajes TempData

### Patrón correcto
Los tests deben usar los mensajes **exactos** del controlador:
- Create: `"Empleado creado exitosamente."`
- Update: `"Empleado actualizado exitosamente."`
- Delete: `"Empleado eliminado exitosamente."`

### Error común
Usar mensajes genéricos como `"Operación exitosa!!"` que no coinciden
con el controlador.

## 3. Checklist Pre-Push

- [ ] `dotnet build --configuration Release --warnaserror` → 0 errores, 0 warnings
- [ ] `dotnet test tests/WebDevSecOps.UnitTests` → todos pasan
- [ ] Verificar que los mensajes en tests coinciden con el controlador
- [ ] Verificar que no hay variables no usadas (S1481)

## 4. Pipeline CI/CD

### Checks que deben pasar
| Check | Requisito |
|-------|-----------|
| build | Compilación limpia + tests unitarios |
| codeql | Análisis estático de código |
| sast | Análisis de seguridad estático |
| secret-scanning | Sin secrets expuestos |
| dependency-review | Dependencias sin vulnerabilidades |
| sca | Análisis de composición de software |

### Checks opcionales (pueden skippear)
- trivy: Escaneo de contenedores
- zap-dast: Pruebas de seguridad dinámicas

## 5. Patrones de Fix para Moq

### Retornar null de forma segura
```csharp
// CORRECTO
.ReturnsAsync(default(Empleado));
.ReturnsAsync((Empleado?)null!);

// INCORRECTO (ambiguo)
.ReturnsAsync(null);
.ReturnsAsync((Empleado?)null);
```

### Mock de servicio no usado
```csharp
// CORRECTO
var (controller, serviceMock, _, _) = CreateController();

// INCORRECTO (warning S1481)
var (controller, serviceMock, tipoMock, _) = CreateController();
```

## 6. Commits de Corrección

### Mensajes de commit recomendados
- `fix: resolve Moq ambiguity with default(Type)`
- `fix: remove unused variable in tests`
- `fix: align TempData assertions with controller messages`

### Orden de fixes
1. Primero corregir errores de compilación
2. Luego corregir errores de tests
3. Verificar localmente antes de push
