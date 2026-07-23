using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebDevSecOps.Models;
using WebDevSecOps.Services;

namespace WebDevSecOps.Controllers;

[Authorize]
public class UsuarioController : Controller
{
    private readonly IUsuarioService _usuarioService;
    private readonly ILogger<UsuarioController> _logger;

    public UsuarioController(IUsuarioService usuarioService, ILogger<UsuarioController> logger)
    {
        _usuarioService = usuarioService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchString = null, int pageNumber = 1, int pageSize = 6)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        ViewData["SearchString"] = searchString;

        var result = string.IsNullOrWhiteSpace(searchString)
            ? await _usuarioService.GetUsuariosAsync(pageNumber, pageSize)
            : await _usuarioService.BuscarUsuariosAsync(searchString, pageNumber, pageSize);

        if (result is null)
        {
            _logger.LogWarning("Failed to load usuarios — service returned null");
            TempData["Error"] = "No se pudieron cargar los usuarios.";
        }

        return View(result);
    }

    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client, NoStore = false)]
    public async Task<IActionResult> Details(int id, CancellationToken ct, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var usuario = await _usuarioService.GetUsuarioByIdAsync(id, ct);

        if (usuario is null)
        {
            _logger.LogWarning("Usuario with id {Id} not found for details", id);
            TempData["Error"] = "El usuario no existe o no se pudo recuperar la información.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return Redirect("/Usuario/Index");
        }

        ViewData["ReturnUrl"] = returnUrl;

        var model = new UsuarioDetailsViewModel
        {
            Id = usuario.Id,
            StrNombre = usuario.StrNombre,
            StrCorreoElectronico = usuario.StrCorreoElectronico,
            DteFechaRegistro = usuario.DteFechaRegistro,
            RowVersion = usuario.RowVersion ?? []
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UsuarioCreateViewModel model, CancellationToken ct, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var result = await _usuarioService.CreateUsuarioAsync(model, ct);

        if (result.Success)
        {
            _logger.LogInformation("Usuario created successfully");
            TempData["Success"] = "Usuario creado exitosamente.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index");
        }

        if (result.FieldErrors is not null)
        {
            foreach (var kvp in result.FieldErrors)
            {
                foreach (var msg in kvp.Value)
                {
                    ModelState.AddModelError(kvp.Key, msg);
                }
            }
        }

        if (result.ErrorMessage is not null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Update(int id, CancellationToken ct, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var usuario = await _usuarioService.GetUsuarioByIdAsync(id, ct);

        if (usuario is null)
        {
            _logger.LogWarning("Usuario with id {Id} not found for update", id);
            TempData["Error"] = "Usuario no encontrado.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return Redirect("/Usuario/Index");
        }

        ViewData["ReturnUrl"] = returnUrl;

        var model = new UsuarioUpdateViewModel
        {
            Id = usuario.Id,
            StrNombre = usuario.StrNombre,
            StrCorreoElectronico = usuario.StrCorreoElectronico,
            RowVersion = usuario.RowVersion ?? []
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(UsuarioUpdateViewModel model, CancellationToken ct, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var result = await _usuarioService.UpdateUsuarioAsync(model.Id, model, ct);

        if (result.Success)
        {
            _logger.LogInformation("Usuario {Id} updated successfully", model.Id);
            TempData["Success"] = "Usuario actualizado exitosamente.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
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
                        "strNombre" => nameof(UsuarioUpdateViewModel.StrNombre),
                        "strPWD" => nameof(UsuarioUpdateViewModel.StrPWD),
                        "strCorreoElectronico" => nameof(UsuarioUpdateViewModel.StrCorreoElectronico),
                        "rowVersion" => nameof(UsuarioUpdateViewModel.RowVersion),
                        _ => kvp.Key
                    };
                    ModelState.AddModelError(key, msg);
                }
            }
        }

        if (result.ErrorMessage is not null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var usuario = await _usuarioService.GetUsuarioByIdAsync(id, ct);

        if (usuario is null)
        {
            _logger.LogWarning("Usuario with id {Id} not found for delete", id);
            TempData["Error"] = "Usuario no encontrado.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return Redirect("/Usuario/Index");
        }

        ViewData["ReturnUrl"] = returnUrl;

        var model = new UsuarioDeleteViewModel
        {
            Id = usuario.Id,
            StrNombre = usuario.StrNombre,
            StrCorreoElectronico = usuario.StrCorreoElectronico,
            RowVersion = usuario.RowVersion ?? []
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(UsuarioDeleteViewModel model, CancellationToken ct, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var result = await _usuarioService.DeleteUsuarioAsync(model.Id, model.RowVersion, ct);

        if (result.Success)
        {
            _logger.LogInformation("Usuario {Id} deleted successfully", model.Id);
            TempData["Success"] = "Usuario eliminado exitosamente.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
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
                        "id" => nameof(UsuarioDeleteViewModel.Id),
                        "rowVersion" => nameof(UsuarioDeleteViewModel.RowVersion),
                        _ => kvp.Key
                    };
                    ModelState.AddModelError(key, msg);
                }
            }
        }

        if (result.ErrorMessage is not null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(model);
    }
}
