using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using WebDevSecOps.Controllers;
using WebDevSecOps.Models;
using WebDevSecOps.Services;

namespace WebDevSecOps.UnitTests;

public class CreateTests
{
    private static (UsuarioController Controller, Mock<IUsuarioService> ServiceMock, Mock<ILogger<UsuarioController>> LoggerMock) CreateController()
    {
        var serviceMock = new Mock<IUsuarioService>();
        var loggerMock = new Mock<ILogger<UsuarioController>>();
        var controller = new UsuarioController(serviceMock.Object, loggerMock.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>());

        return (controller, serviceMock, loggerMock);
    }

    private static UsuarioCreateViewModel ValidModel => new()
    {
        StrNombre = "Juan Perez",
        StrPWD = "Abc123!@",
        StrCorreoElectronico = "juan@test.com"
    };

    // ======================================================================
    // GET Create
    // ======================================================================

    [Fact]
    public void Create_Get_ReturnsView_WhenCalled()
    {
        var (controller, _, _) = CreateController();

        var result = controller.Create();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.Model);
    }

    [Fact]
    public void Create_Get_SetsReturnUrlInViewData_WhenReturnUrlProvided()
    {
        var (controller, _, _) = CreateController();

        var result = controller.Create(returnUrl: "/dashboard");

        Assert.IsType<ViewResult>(result);
        Assert.Equal("/dashboard", controller.ViewData["ReturnUrl"]);
    }

    // ======================================================================
    // POST Create — Controller with Mocks
    // ======================================================================

    [Fact]
    public async Task Create_Post_ReturnsView_WhenModelStateInvalid()
    {
        var (controller, serviceMock, _) = CreateController();
        controller.ModelState.AddModelError("StrNombre", "Required");

        var result = await controller.Create(ValidModel, default);

        Assert.IsType<ViewResult>(result);
        serviceMock.Verify(
            x => x.CreateUsuarioAsync(It.IsAny<UsuarioCreateViewModel>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Create_Post_RedirectsToIndex_WhenServiceSucceeds()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.CreateUsuarioAsync(It.IsAny<UsuarioCreateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUsuarioResult.Ok());

        var result = await controller.Create(ValidModel, default);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Usuario creado exitosamente.", controller.TempData["Success"]);
    }

    [Fact]
    public async Task Create_Post_RedirectsToReturnUrl_WhenLocalUrl()
    {
        var (controller, serviceMock, _) = CreateController();

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(x => x.IsLocalUrl("/dashboard")).Returns(true);
        controller.Url = urlHelper.Object;

        serviceMock
            .Setup(x => x.CreateUsuarioAsync(It.IsAny<UsuarioCreateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUsuarioResult.Ok());

        var result = await controller.Create(ValidModel, default, returnUrl: "/dashboard");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/dashboard", redirect.Url);
    }

    [Fact]
    public async Task Create_Post_RedirectsToIndex_WhenNonLocalUrl()
    {
        var (controller, serviceMock, _) = CreateController();

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(x => x.IsLocalUrl("https://evil.com")).Returns(false);
        controller.Url = urlHelper.Object;

        serviceMock
            .Setup(x => x.CreateUsuarioAsync(It.IsAny<UsuarioCreateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUsuarioResult.Ok());

        var result = await controller.Create(ValidModel, default, returnUrl: "https://evil.com");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task Create_Post_ReturnsViewWithFieldErrors_WhenServiceFails()
    {
        var (controller, serviceMock, _) = CreateController();

        var fieldErrors = new Dictionary<string, string[]>
        {
            { "strNombre", new[] { "El nombre ya existe." } }
        };

        serviceMock
            .Setup(x => x.CreateUsuarioAsync(It.IsAny<UsuarioCreateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUsuarioResult.Fail(fieldErrors));

        var result = await controller.Create(ValidModel, default);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey("strNombre"));
        Assert.Contains(controller.ModelState["strNombre"]!.Errors, e => e.ErrorMessage == "El nombre ya existe.");
    }

    [Fact]
    public async Task Create_Post_ReturnsViewWithGeneralError_WhenServiceFails()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.CreateUsuarioAsync(It.IsAny<UsuarioCreateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUsuarioResult.Fail("Error interno del servidor."));

        var result = await controller.Create(ValidModel, default);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(controller.ModelState[""]!.Errors, e => e.ErrorMessage == "Error interno del servidor.");
    }

    [Fact]
    public async Task Create_Post_ReturnsViewWithBothErrors_WhenServiceFailsWithFieldAndGeneral()
    {
        var (controller, serviceMock, _) = CreateController();

        var fieldErrors = new Dictionary<string, string[]>
        {
            { "strCorreoElectronico", new[] { "El correo ya está registrado." } }
        };

        serviceMock
            .Setup(x => x.CreateUsuarioAsync(It.IsAny<UsuarioCreateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUsuarioResult.Fail(fieldErrors, "Error de validación adicional."));

        var result = await controller.Create(ValidModel, default);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(controller.ModelState["strCorreoElectronico"]!.Errors, e => e.ErrorMessage == "El correo ya está registrado.");
        Assert.Contains(controller.ModelState[""]!.Errors, e => e.ErrorMessage == "Error de validación adicional.");
    }

    [Fact]
    public async Task Create_Post_LogsInformation_WhenServiceSucceeds()
    {
        var (controller, serviceMock, loggerMock) = CreateController();

        serviceMock
            .Setup(x => x.CreateUsuarioAsync(It.IsAny<UsuarioCreateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUsuarioResult.Ok());

        await controller.Create(ValidModel, default);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("Usuario created successfully")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    // ======================================================================
    // Model Validation — UsuarioCreateViewModel
    // ======================================================================

    private static List<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void CreateViewModel_Nombre_Required_Fails()
    {
        var model = new UsuarioCreateViewModel
        {
            StrNombre = "",
            StrPWD = "Abc123!@",
            StrCorreoElectronico = "juan@test.com"
        };

        var results = ValidateModel(model);

        Assert.Contains(results, r => r.MemberNames.Contains("StrNombre") && r.ErrorMessage!.Contains("obligatorio"));
    }

    [Fact]
    public void CreateViewModel_Nombre_MaxLength_Fails()
    {
        var model = new UsuarioCreateViewModel
        {
            StrNombre = new string('a', 51),
            StrPWD = "Abc123!@",
            StrCorreoElectronico = "juan@test.com"
        };

        var results = ValidateModel(model);

        Assert.Contains(results, r => r.MemberNames.Contains("StrNombre") && r.ErrorMessage!.Contains("exceder"));
    }

    [Fact]
    public void CreateViewModel_Nombre_InvalidCharacters_Fails()
    {
        var model = new UsuarioCreateViewModel
        {
            StrNombre = "Juan@#$",
            StrPWD = "Abc123!@",
            StrCorreoElectronico = "juan@test.com"
        };

        var results = ValidateModel(model);

        Assert.Contains(results, r => r.MemberNames.Contains("StrNombre") && r.ErrorMessage!.Contains("solo permite"));
    }

    [Fact]
    public void CreateViewModel_Password_MinLength_Fails()
    {
        var model = new UsuarioCreateViewModel
        {
            StrNombre = "Juan",
            StrPWD = "Ab1!@",
            StrCorreoElectronico = "juan@test.com"
        };

        var results = ValidateModel(model);

        Assert.Contains(results, r => r.MemberNames.Contains("StrPWD") && r.ErrorMessage!.Contains("al menos"));
    }

    [Fact]
    public void CreateViewModel_Password_Complexity_Fails()
    {
        var model = new UsuarioCreateViewModel
        {
            StrNombre = "Juan",
            StrPWD = "abcdefgh",
            StrCorreoElectronico = "juan@test.com"
        };

        var results = ValidateModel(model);

        Assert.Contains(results, r => r.MemberNames.Contains("StrPWD") && r.ErrorMessage!.Contains("mayúsculas"));
    }

    [Fact]
    public void CreateViewModel_Email_InvalidFormat_Fails()
    {
        var model = new UsuarioCreateViewModel
        {
            StrNombre = "Juan",
            StrPWD = "Abc123!@",
            StrCorreoElectronico = "invalido"
        };

        var results = ValidateModel(model);

        Assert.Contains(results, r => r.MemberNames.Contains("StrCorreoElectronico") && r.ErrorMessage!.Contains("formato"));
    }

    [Fact]
    public void CreateViewModel_ValidModel_PassesAll()
    {
        var results = ValidateModel(ValidModel);

        Assert.Empty(results);
    }
}
