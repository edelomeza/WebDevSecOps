using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebDevSecOps.Models;
using WebDevSecOps.Services;

namespace WebDevSecOps.Controllers;

[Authorize]
public class ProductoController : Controller
{
    private readonly IProductoService _service;
    private readonly ILogger<ProductoController> _logger;

    public ProductoController(IProductoService service, ILogger<ProductoController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client, NoStore = false)]
    public async Task<IActionResult> Index(string? texto = null, int pageNumber = 1, int pageSize = 6, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = !string.IsNullOrEmpty(texto)
            ? await _service.SearchProductosAsync(texto, pageNumber, pageSize, ct)
            : await _service.GetProductosAsync(pageNumber, pageSize, ct);

        if (result is null)
        {
            _logger.LogWarning("Failed to load productos — service returned null");
            TempData["Error"] = "No se pudieron cargar los productos.";
        }

        return View(result);
    }

    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client, NoStore = false)]
    public async Task<IActionResult> Details(int id, CancellationToken ct, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var producto = await _service.GetProductoByIdAsync(id, ct);

        if (producto is null)
        {
            _logger.LogWarning("Producto with id {Id} not found for details", id);
            TempData["Error"] = "El producto no existe o no se pudo recuperar la informacion.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return Redirect("/Producto/Index");
        }

        ViewData["ReturnUrl"] = returnUrl;

        var model = new ProductoDetailsViewModel
        {
            Id = producto.Id,
            StrNombreProducto = producto.StrNombreProducto,
            StrURLImagen = producto.StrURLImagen,
            StrDescripcion = producto.StrDescripcion,
            IntNumeroExistencia = producto.IntNumeroExistencia,
            DecPrecio = producto.DecPrecio,
            RowVersion = producto.RowVersion ?? []
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(string? returnUrl = null, CancellationToken ct = default)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductoCreateViewModel model, CancellationToken ct, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var result = await _service.CreateProductoAsync(model, ct);

        if (result.Success)
        {
            _logger.LogInformation("Producto created successfully");
            TempData["Success"] = "Producto creado exitosamente.";
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

        ViewData["ReturnUrl"] = returnUrl;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Update(int id, CancellationToken ct, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var producto = await _service.GetProductoByIdAsync(id, ct);

        if (producto is null)
        {
            _logger.LogWarning("Producto with id {Id} not found for update", id);
            TempData["Error"] = "Producto no encontrado.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return Redirect("/Producto/Index");
        }

        ViewData["ReturnUrl"] = returnUrl;

        var model = new ProductoUpdateViewModel
        {
            Id = producto.Id,
            StrNombreProducto = producto.StrNombreProducto,
            StrURLImagen = producto.StrURLImagen,
            StrDescripcion = producto.StrDescripcion,
            IntNumeroExistencia = producto.IntNumeroExistencia,
            DecPrecio = producto.DecPrecio,
            RowVersion = producto.RowVersion ?? []
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProductoUpdateViewModel model, CancellationToken ct, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var result = await _service.UpdateProductoAsync(model.Id, model, ct);

        if (result.Success)
        {
            _logger.LogInformation("Producto {Id} updated successfully", model.Id);
            TempData["Success"] = "Producto actualizado exitosamente.";
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
                        "strNombreProducto" => nameof(ProductoUpdateViewModel.StrNombreProducto),
                        "strURLImagen" => nameof(ProductoUpdateViewModel.StrURLImagen),
                        "strDescripcion" => nameof(ProductoUpdateViewModel.StrDescripcion),
                        "intNumeroExistencia" => nameof(ProductoUpdateViewModel.IntNumeroExistencia),
                        "decPrecio" => nameof(ProductoUpdateViewModel.DecPrecio),
                        "rowVersion" => nameof(ProductoUpdateViewModel.RowVersion),
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
    public async Task<IActionResult> Delete(int id, CancellationToken ct, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var producto = await _service.GetProductoByIdAsync(id, ct);

        if (producto is null)
        {
            _logger.LogWarning("Producto with id {Id} not found for delete", id);
            TempData["Error"] = "Producto no encontrado.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return Redirect("/Producto/Index");
        }

        ViewData["ReturnUrl"] = returnUrl;

        var model = new ProductoDeleteViewModel
        {
            Id = producto.Id,
            StrNombreProducto = producto.StrNombreProducto,
            StrDescripcion = producto.StrDescripcion,
            IntNumeroExistencia = producto.IntNumeroExistencia,
            DecPrecio = producto.DecPrecio,
            RowVersion = producto.RowVersion ?? []
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(ProductoDeleteViewModel model, CancellationToken ct, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var result = await _service.DeleteProductoAsync(model.Id, model.RowVersion, ct);

        if (result.Success)
        {
            _logger.LogInformation("Producto {Id} deleted successfully", model.Id);
            TempData["Success"] = "Producto eliminado exitosamente.";
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
                        "id" => nameof(ProductoDeleteViewModel.Id),
                        "rowVersion" => nameof(ProductoDeleteViewModel.RowVersion),
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
}
