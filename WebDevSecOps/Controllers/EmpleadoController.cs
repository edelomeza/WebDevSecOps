using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebDevSecOps.Models;
using WebDevSecOps.Services;

namespace WebDevSecOps.Controllers;

[Authorize]
public class EmpleadoController : Controller
{
    private readonly IEmpleadoService _service;
    private readonly ITipoEmpleadoService _tipoEmpleadoService;
    private readonly ILogger<EmpleadoController> _logger;

    public EmpleadoController(IEmpleadoService service, ITipoEmpleadoService tipoEmpleadoService, ILogger<EmpleadoController> logger)
    {
        _service = service;
        _tipoEmpleadoService = tipoEmpleadoService;
        _logger = logger;
    }

    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client, NoStore = false)]
    public async Task<IActionResult> Index(string? texto = null, int? idTipoEmpleado = null, int pageNumber = 1, int pageSize = 10, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await CargarTipoEmpleadosAsync(ct);

        var result = (!string.IsNullOrEmpty(texto) || idTipoEmpleado.HasValue)
            ? await _service.SearchEmpleadosAsync(texto, idTipoEmpleado, pageNumber, pageSize, ct)
            : await _service.GetEmpleadosAsync(pageNumber, pageSize, ct);

        if (result is null)
        {
            _logger.LogWarning("Failed to load empleados — service returned null");
            TempData["Error"] = "No se pudieron cargar los empleados.";
        }

        return View(result);
    }

    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client, NoStore = false)]
    public async Task<IActionResult> Details(int id, CancellationToken ct, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var empleado = await _service.GetEmpleadoByIdAsync(id, ct);

        if (empleado is null)
        {
            _logger.LogWarning("Empleado with id {Id} not found for details", id);
            TempData["Error"] = "El empleado no existe o no se pudo recuperar la informacion.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return Redirect("/Empleado/Index");
        }

        var strValorTipoEmpleado = await ObtenerValorTipoEmpleado(empleado.IdEmpCatTipoEmpleado, ct);

        ViewData["ReturnUrl"] = returnUrl;

        var model = new EmpleadoDetailsViewModel
        {
            Id = empleado.Id,
            StrNombre = empleado.StrNombre,
            StrAPaterno = empleado.StrAPaterno,
            StrAMaterno = empleado.StrAMaterno,
            StrCURP = empleado.StrCURP,
            StrValorTipoEmpleado = strValorTipoEmpleado,
            RowVersion = empleado.RowVersion ?? []
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(string? returnUrl = null, CancellationToken ct = default)
    {
        await CargarTipoEmpleadosAsync(ct);
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmpleadoCreateViewModel model, CancellationToken ct, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            await CargarTipoEmpleadosAsync(ct);
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var result = await _service.CreateEmpleadoAsync(model, ct);

        if (result.Success)
        {
            _logger.LogInformation("Empleado created successfully");
            TempData["Success"] = "Empleado creado exitosamente.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index");
        }

        if (result.FieldErrors is not null)
        {
            foreach (var kvp in result.FieldErrors)
            {
                foreach (var msg in kvp.Value)
                    ModelState.AddModelError(kvp.Key, msg);
            }
        }

        if (result.ErrorMessage is not null)
            ModelState.AddModelError(string.Empty, result.ErrorMessage);

        await CargarTipoEmpleadosAsync(ct);
        ViewData["ReturnUrl"] = returnUrl;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Update(int id, CancellationToken ct, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var empleado = await _service.GetEmpleadoByIdAsync(id, ct);

        if (empleado is null)
        {
            _logger.LogWarning("Empleado with id {Id} not found for update", id);
            TempData["Error"] = "Empleado no encontrado.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return Redirect("/Empleado/Index");
        }

        await CargarTipoEmpleadosAsync(ct);
        ViewData["ReturnUrl"] = returnUrl;

        var model = new EmpleadoUpdateViewModel
        {
            Id = empleado.Id,
            StrNombre = empleado.StrNombre,
            StrAPaterno = empleado.StrAPaterno,
            StrAMaterno = empleado.StrAMaterno,
            StrCURP = empleado.StrCURP,
            IdEmpCatTipoEmpleado = empleado.IdEmpCatTipoEmpleado,
            RowVersion = empleado.RowVersion ?? []
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(EmpleadoUpdateViewModel model, CancellationToken ct, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            await CargarTipoEmpleadosAsync(ct);
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var result = await _service.UpdateEmpleadoAsync(model.Id, model, ct);

        if (result.Success)
        {
            _logger.LogInformation("Empleado {Id} updated successfully", model.Id);
            TempData["Success"] = "Empleado actualizado exitosamente.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index");
        }

        if (result.FieldErrors is not null)
        {
            foreach (var kvp in result.FieldErrors)
            {
                foreach (var msg in kvp.Value)
                {
                    var key = kvp.Key switch
                    {
                        "strNombre" => nameof(EmpleadoUpdateViewModel.StrNombre),
                        "strAPaterno" => nameof(EmpleadoUpdateViewModel.StrAPaterno),
                        "strAMaterno" => nameof(EmpleadoUpdateViewModel.StrAMaterno),
                        "strCURP" => nameof(EmpleadoUpdateViewModel.StrCURP),
                        "idEmpCatTipoEmpleado" => nameof(EmpleadoUpdateViewModel.IdEmpCatTipoEmpleado),
                        "rowVersion" => nameof(EmpleadoUpdateViewModel.RowVersion),
                        _ => kvp.Key
                    };
                    ModelState.AddModelError(key, msg);
                }
            }
        }

        if (result.ErrorMessage is not null)
            ModelState.AddModelError(string.Empty, result.ErrorMessage);

        await CargarTipoEmpleadosAsync(ct);
        ViewData["ReturnUrl"] = returnUrl;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var empleado = await _service.GetEmpleadoByIdAsync(id, ct);

        if (empleado is null)
        {
            _logger.LogWarning("Empleado with id {Id} not found for delete", id);
            TempData["Error"] = "Empleado no encontrado.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return Redirect("/Empleado/Index");
        }

        var strValorTipoEmpleado = await ObtenerValorTipoEmpleado(empleado.IdEmpCatTipoEmpleado, ct);

        ViewData["ReturnUrl"] = returnUrl;

        var model = new EmpleadoDeleteViewModel
        {
            Id = empleado.Id,
            StrNombre = empleado.StrNombre,
            StrAPaterno = empleado.StrAPaterno,
            StrAMaterno = empleado.StrAMaterno,
            StrCURP = empleado.StrCURP,
            StrValorTipoEmpleado = strValorTipoEmpleado,
            RowVersion = empleado.RowVersion ?? []
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(EmpleadoDeleteViewModel model, CancellationToken ct, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var result = await _service.DeleteEmpleadoAsync(model.Id, model.RowVersion, ct);

        if (result.Success)
        {
            _logger.LogInformation("Empleado {Id} deleted successfully", model.Id);
            TempData["Success"] = "Empleado eliminado exitosamente.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index");
        }

        if (result.FieldErrors is not null)
        {
            foreach (var kvp in result.FieldErrors)
            {
                foreach (var msg in kvp.Value)
                {
                    var key = kvp.Key switch
                    {
                        "id" => nameof(EmpleadoDeleteViewModel.Id),
                        "rowVersion" => nameof(EmpleadoDeleteViewModel.RowVersion),
                        _ => kvp.Key
                    };
                    ModelState.AddModelError(key, msg);
                }
            }
        }

        if (result.ErrorMessage is not null)
            ModelState.AddModelError(string.Empty, result.ErrorMessage);

        ViewData["ReturnUrl"] = returnUrl;
        return View(model);
    }

    private async Task CargarTipoEmpleadosAsync(CancellationToken ct = default)
    {
        var tipos = await _tipoEmpleadoService.GetAllAsync(ct: ct);
        ViewBag.TipoEmpleadoList = new SelectList(tipos?.Items ?? [], "Id", "StrValor");
        ViewBag.TipoEmpleadoDict = tipos?.Items?.ToDictionary(t => t.Id, t => t.StrValor) ?? [];
    }

    private async Task<string?> ObtenerValorTipoEmpleado(int? idTipoEmpleado, CancellationToken ct = default)
    {
        if (!idTipoEmpleado.HasValue)
            return null;

        var tipo = await _tipoEmpleadoService.GetByIdAsync(idTipoEmpleado.Value, ct);
        return tipo?.StrValor;
    }
}
