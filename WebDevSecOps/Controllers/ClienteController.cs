using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebDevSecOps.Models;
using WebDevSecOps.Services;

namespace WebDevSecOps.Controllers;

[Authorize]
public class ClienteController : Controller
{
    private readonly IClienteService _service;
    private readonly ILogger<ClienteController> _logger;

    public ClienteController(IClienteService service, ILogger<ClienteController> logger)
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
            ? await _service.SearchClientesAsync(texto, pageNumber, pageSize, ct)
            : await _service.GetClientesAsync(pageNumber, pageSize, ct);

        if (result is null)
        {
            _logger.LogWarning("Failed to load clientes — service returned null");
            TempData["Error"] = "No se pudieron cargar los clientes.";
        }

        return View(result);
    }

    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client, NoStore = false)]
    public async Task<IActionResult> Details(int id, CancellationToken ct, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var cliente = await _service.GetClienteByIdAsync(id, ct);

        if (cliente is null)
        {
            _logger.LogWarning("Cliente with id {Id} not found for details", id);
            TempData["Error"] = "El cliente no existe o no se pudo recuperar la informacion.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return Redirect("/Cliente/Index");
        }

        ViewData["ReturnUrl"] = returnUrl;

        var model = new ClienteDetailsViewModel
        {
            Id = cliente.Id,
            StrNombreCliente = cliente.StrNombreCliente,
            StrDireccionCliente = cliente.StrDireccionCliente,
            StrCorreoElectronico = cliente.StrCorreoElectronico,
            StrNumeroTelefono = cliente.StrNumeroTelefono,
            RowVersion = cliente.RowVersion ?? []
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
    public async Task<IActionResult> Create(ClienteCreateViewModel model, CancellationToken ct, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var result = await _service.CreateClienteAsync(model, ct);

        if (result.Success)
        {
            _logger.LogInformation("Cliente created successfully");
            TempData["Success"] = "Cliente creado exitosamente.";
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

        var cliente = await _service.GetClienteByIdAsync(id, ct);

        if (cliente is null)
        {
            _logger.LogWarning("Cliente with id {Id} not found for update", id);
            TempData["Error"] = "Cliente no encontrado.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return Redirect("/Cliente/Index");
        }

        ViewData["ReturnUrl"] = returnUrl;

        var model = new ClienteUpdateViewModel
        {
            Id = cliente.Id,
            StrNombreCliente = cliente.StrNombreCliente,
            StrDireccionCliente = cliente.StrDireccionCliente,
            StrCorreoElectronico = cliente.StrCorreoElectronico,
            StrNumeroTelefono = cliente.StrNumeroTelefono,
            RowVersion = cliente.RowVersion ?? []
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ClienteUpdateViewModel model, CancellationToken ct, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var result = await _service.UpdateClienteAsync(model.Id, model, ct);

        if (result.Success)
        {
            _logger.LogInformation("Cliente {Id} updated successfully", model.Id);
            TempData["Success"] = "Cliente actualizado exitosamente.";
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
                        "strNombreCliente" => nameof(ClienteUpdateViewModel.StrNombreCliente),
                        "strDireccionCliente" => nameof(ClienteUpdateViewModel.StrDireccionCliente),
                        "strCorreoElectronico" => nameof(ClienteUpdateViewModel.StrCorreoElectronico),
                        "strNumeroTelefono" => nameof(ClienteUpdateViewModel.StrNumeroTelefono),
                        "rowVersion" => nameof(ClienteUpdateViewModel.RowVersion),
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

        var cliente = await _service.GetClienteByIdAsync(id, ct);

        if (cliente is null)
        {
            _logger.LogWarning("Cliente with id {Id} not found for delete", id);
            TempData["Error"] = "Cliente no encontrado.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return Redirect("/Cliente/Index");
        }

        ViewData["ReturnUrl"] = returnUrl;

        var model = new ClienteDeleteViewModel
        {
            Id = cliente.Id,
            StrNombreCliente = cliente.StrNombreCliente,
            StrDireccionCliente = cliente.StrDireccionCliente,
            StrCorreoElectronico = cliente.StrCorreoElectronico,
            StrNumeroTelefono = cliente.StrNumeroTelefono,
            RowVersion = cliente.RowVersion ?? []
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(ClienteDeleteViewModel model, CancellationToken ct, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var result = await _service.DeleteClienteAsync(model.Id, model.RowVersion, ct);

        if (result.Success)
        {
            _logger.LogInformation("Cliente {Id} deleted successfully", model.Id);
            TempData["Success"] = "Cliente eliminado exitosamente.";
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
                        "id" => nameof(ClienteDeleteViewModel.Id),
                        "rowVersion" => nameof(ClienteDeleteViewModel.RowVersion),
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
