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
    private readonly IProductoService _productoService;
    private readonly ILogger<VentaController> _logger;

    public VentaController(IVentaService service, IEstadoVentaService estadoVentaService, IClienteService clienteService, IUsuarioService usuarioService, IProductoService productoService, ILogger<VentaController> logger)
    {
        _service = service;
        _estadoVentaService = estadoVentaService;
        _clienteService = clienteService;
        _usuarioService = usuarioService;
        _productoService = productoService;
        _logger = logger;
    }

    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client, NoStore = false)]
    public async Task<IActionResult> Index(string? texto = null, DateTime? dteFechaInicio = null, DateTime? dteFechaFin = null, int pageNumber = 1, int pageSize = 6, CancellationToken ct = default)
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

    [HttpGet]
    public async Task<IActionResult> Productos(int id, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var venta = await _service.GetVentaByIdAsync(id, ct);
        if (venta is null)
        {
            _logger.LogWarning("Venta with id {Id} not found", id);
            TempData["Error"] = "La venta no existe o no se pudo recuperar la informacion.";
            return RedirectToAction("Index");
        }

        await CargarEstadoVentasAsync(ct);

        var detalles = await _service.GetVentaDetallesAsync(id, ct) ?? [];

        var model = new VentaProductosViewModel
        {
            Id = venta.Id,
            DteFechaHoraCompra = venta.DteFechaHoraCompra,
            StrClaveVenta = venta.StrClaveVenta,
            StrNombreCliente = venta.StrNombreCliente,
            StrNombreUsuario = venta.StrNombreUsuario,
            IdVenCatEstado = venta.IdVenCatEstado,
            RowVersion = venta.RowVersion,
            Detalles = detalles
        };

        return View(model);
    }

    [HttpGet]
    public async Task<JsonResult> ProductosAutocomplete(string texto, int maxResultados = 10, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return Json(new List<ProProductoAutocompleteDto>());

        if (string.IsNullOrWhiteSpace(texto) || texto.Length < 2)
            return Json(new List<ProProductoAutocompleteDto>());

        var result = await _productoService.AutocompleteProductosAsync(texto, maxResultados, ct);
        return Json(result ?? []);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarProducto(VentaAgregarProductoViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Verifique los datos del producto.";
            return RedirectToAction("Productos", new { id = model.IdVenVenta });
        }

        var producto = await _productoService.GetProductoByIdAsync(model.IdProProducto, ct);
        if (producto is null)
        {
            _logger.LogWarning("Producto with id {IdProProducto} not found", model.IdProProducto);
            TempData["Error"] = "El producto seleccionado no existe.";
            return RedirectToAction("Productos", new { id = model.IdVenVenta });
        }

        if (producto.IntNumeroExistencia <= 0)
        {
            _logger.LogWarning("Producto {IdProProducto} has no stock", model.IdProProducto);
            TempData["Error"] = "El producto no tiene existencia disponible.";
            return RedirectToAction("Productos", new { id = model.IdVenVenta });
        }

        var result = await _service.CreateVentaDetalleAsync(model, ct);

        if (result.Success)
        {
            _logger.LogInformation("VentaDetalle created for Venta {Id}", model.IdVenVenta);
            TempData["Success"] = "Producto agregado exitosamente.";
        }
        else
        {
            TempData["Error"] = result.ErrorMessage ?? "Error al agregar el producto.";
        }

        return RedirectToAction("Productos", new { id = model.IdVenVenta });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(int id, int idVenCatEstado, byte[] rowVersion, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Datos invalidos para guardar la venta.";
            return RedirectToAction("Productos", new { id });
        }

        var venta = await _service.GetVentaByIdAsync(id, ct);
        if (venta is null)
        {
            TempData["Error"] = "Venta no encontrada.";
            return RedirectToAction("Index");
        }

        venta.IdVenCatEstado = idVenCatEstado;

        var result = await _service.UpdateVentaAsync(id, venta, ct);

        if (result.Success)
        {
            _logger.LogInformation("Venta {Id} guardada exitosamente", id);
            TempData["Success"] = "Venta guardada exitosamente.";
        }
        else
        {
            TempData["Error"] = result.ErrorMessage ?? "Error al guardar la venta.";
            return RedirectToAction("Productos", new { id });
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarProducto(int id, byte[] rowVersion, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = "Datos invalidos." });

        var result = await _service.DeleteVentaDetalleAsync(id, rowVersion, ct);

        if (result.Success)
            return Json(new { success = true });

        return Json(new { success = false, message = result.ErrorMessage ?? "Error al eliminar el producto." });
    }

    private async Task CargarEstadoVentasAsync(CancellationToken ct = default)
    {
        var estados = await _estadoVentaService.GetAllAsync(ct: ct);
        ViewBag.EstadoVentaDict = estados?.Items?.ToDictionary(t => t.Id, t => t.StrValor) ?? [];
    }
}
