using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebDevSecOps.Models;
using WebDevSecOps.Services;

namespace WebDevSecOps.Controllers;

[Authorize]
public class VentaController : Controller
{
    private readonly IVentaService _service;
    private readonly IEstadoVentaService _estadoVentaService;
    private readonly IClienteService _clienteService;
    private readonly IUsuarioService _usuarioService;
    private readonly ILogger<VentaController> _logger;

    public VentaController(IVentaService service, IEstadoVentaService estadoVentaService, IClienteService clienteService, IUsuarioService usuarioService, ILogger<VentaController> logger)
    {
        _service = service;
        _estadoVentaService = estadoVentaService;
        _clienteService = clienteService;
        _usuarioService = usuarioService;
        _logger = logger;
    }

    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client, NoStore = false)]
    public async Task<IActionResult> Index(string? texto = null, DateTime? dteFechaInicio = null, DateTime? dteFechaFin = null, int pageNumber = 1, int pageSize = 10, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await CargarEstadoVentasAsync(ct);

        var result = (!string.IsNullOrEmpty(texto) || dteFechaInicio.HasValue || dteFechaFin.HasValue)
            ? await _service.SearchVentasAsync(texto, dteFechaInicio, dteFechaFin, pageNumber, pageSize, ct)
            : await _service.GetVentasAsync(pageNumber, pageSize, ct);

        if (result is null)
        {
            _logger.LogWarning("Failed to load ventas — service returned null");
            TempData["Error"] = "No se pudieron cargar las ventas.";
        }

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Create(string? returnUrl = null, CancellationToken ct = default)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VentaCreateViewModel model, CancellationToken ct, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var result = await _service.CreateVentaAsync(model, ct);

        if (result.Success)
        {
            _logger.LogInformation("Venta created successfully");
            TempData["Success"] = "Venta creada exitosamente.";
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
                        "idCliCliente" => nameof(VentaCreateViewModel.IdCliCliente),
                        "idSegUsuario" => nameof(VentaCreateViewModel.IdSegUsuario),
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

    [HttpGet]
    public async Task<JsonResult> ClientesAutocomplete(string texto, int maxResultados = 10, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return Json(new List<CliClienteAutocompleteDto>());

        if (string.IsNullOrWhiteSpace(texto) || texto.Length < 2)
            return Json(new List<CliClienteAutocompleteDto>());

        var result = await _clienteService.AutocompleteClientesAsync(texto, maxResultados, ct);
        return Json(result ?? []);
    }

    [HttpGet]
    public async Task<JsonResult> UsuariosAutocomplete(string texto, int maxResultados = 10, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return Json(new List<SegUsuarioAutocompleteDto>());

        if (string.IsNullOrWhiteSpace(texto) || texto.Length < 2)
            return Json(new List<SegUsuarioAutocompleteDto>());

        var result = await _usuarioService.AutocompleteUsuariosAsync(texto, maxResultados, ct);
        return Json(result ?? []);
    }

    private async Task CargarEstadoVentasAsync(CancellationToken ct = default)
    {
        var estados = await _estadoVentaService.GetAllAsync(ct: ct);
        ViewBag.EstadoVentaDict = estados?.Items?.ToDictionary(t => t.Id, t => t.StrValor) ?? [];
    }
}
