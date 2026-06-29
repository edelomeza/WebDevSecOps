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

public class DeleteTests
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

    private static Usuario ExistingUser => new()
    {
        Id = 1,
        StrNombre = "Juan Perez",
        StrCorreoElectronico = "juan@test.com",
        DteFechaRegistro = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
        RowVersion = [1, 2, 3]
    };

    private static UsuarioDeleteViewModel ValidModel => new()
    {
        Id = 1,
        StrNombre = "Juan Perez",
        StrCorreoElectronico = "juan@test.com",
        RowVersion = [1, 2, 3]
    };

    // ======================================================================
    // GET Delete
    // ======================================================================

    [Fact]
    public async Task Delete_Get_ReturnsViewWithModel_WhenUserFound()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.GetUsuarioByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingUser);

        var result = await controller.Delete(1, default);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<UsuarioDeleteViewModel>(viewResult.Model);
        Assert.Equal(ExistingUser.Id, model.Id);
        Assert.Equal(ExistingUser.StrNombre, model.StrNombre);
        Assert.Equal(ExistingUser.StrCorreoElectronico, model.StrCorreoElectronico);
        Assert.Equal(ExistingUser.RowVersion, model.RowVersion);
    }

    [Fact]
    public async Task Delete_Get_RedirectsToIndex_WhenUserNotFound()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.GetUsuarioByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var result = await controller.Delete(999, default);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/Usuario/Index", redirect.Url);
        Assert.Equal("Usuario no encontrado.", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Delete_Get_RedirectsToReturnUrl_WhenUserNotFoundWithLocalUrl()
    {
        var (controller, serviceMock, _) = CreateController();

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(x => x.IsLocalUrl("/dashboard")).Returns(true);
        controller.Url = urlHelper.Object;

        serviceMock
            .Setup(x => x.GetUsuarioByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var result = await controller.Delete(999, default, returnUrl: "/dashboard");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/dashboard", redirect.Url);
    }

    [Fact]
    public async Task Delete_Get_RedirectsToIndex_WhenUserNotFoundWithNonLocalUrl()
    {
        var (controller, serviceMock, _) = CreateController();

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(x => x.IsLocalUrl("https://evil.com")).Returns(false);
        controller.Url = urlHelper.Object;

        serviceMock
            .Setup(x => x.GetUsuarioByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var result = await controller.Delete(999, default, returnUrl: "https://evil.com");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/Usuario/Index", redirect.Url);
    }

    [Fact]
    public async Task Delete_Get_LogsWarning_WhenUserNotFound()
    {
        var (controller, serviceMock, loggerMock) = CreateController();

        serviceMock
            .Setup(x => x.GetUsuarioByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        await controller.Delete(999, default);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("not found for delete")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task Delete_Get_SetsReturnUrlInViewData_WhenUserFound()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.GetUsuarioByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingUser);

        var result = await controller.Delete(1, default, returnUrl: "/dashboard");

        Assert.IsType<ViewResult>(result);
        Assert.Equal("/dashboard", controller.ViewData["ReturnUrl"]);
    }

    // ======================================================================
    // POST Delete — Controller with Mocks
    // ======================================================================

    [Fact]
    public async Task Delete_Post_ReturnsView_WhenModelStateInvalid()
    {
        var (controller, serviceMock, _) = CreateController();
        controller.ModelState.AddModelError("Id", "Required");

        var result = await controller.Delete(ValidModel, default);

        Assert.IsType<ViewResult>(result);
        serviceMock.Verify(
            x => x.DeleteUsuarioAsync(It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Delete_Post_RedirectsToIndex_WhenServiceSucceeds()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.DeleteUsuarioAsync(It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUsuarioResult.Ok());

        var result = await controller.Delete(ValidModel, default);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Usuario eliminado exitosamente.", controller.TempData["Success"]);
    }

    [Fact]
    public async Task Delete_Post_RedirectsToReturnUrl_WhenLocalUrl()
    {
        var (controller, serviceMock, _) = CreateController();

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(x => x.IsLocalUrl("/dashboard")).Returns(true);
        controller.Url = urlHelper.Object;

        serviceMock
            .Setup(x => x.DeleteUsuarioAsync(It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUsuarioResult.Ok());

        var result = await controller.Delete(ValidModel, default, returnUrl: "/dashboard");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/dashboard", redirect.Url);
    }

    [Fact]
    public async Task Delete_Post_RedirectsToIndex_WhenNonLocalUrl()
    {
        var (controller, serviceMock, _) = CreateController();

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(x => x.IsLocalUrl("https://evil.com")).Returns(false);
        controller.Url = urlHelper.Object;

        serviceMock
            .Setup(x => x.DeleteUsuarioAsync(It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUsuarioResult.Ok());

        var result = await controller.Delete(ValidModel, default, returnUrl: "https://evil.com");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task Delete_Post_ReturnsViewWithMappedFieldErrors_WhenServiceFails()
    {
        var (controller, serviceMock, _) = CreateController();

        var fieldErrors = new Dictionary<string, string[]>
        {
            { "id", new[] { "El usuario no existe." } },
            { "rowVersion", new[] { "El registro fue modificado por otro usuario." } }
        };

        serviceMock
            .Setup(x => x.DeleteUsuarioAsync(It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUsuarioResult.Fail(fieldErrors));

        var result = await controller.Delete(ValidModel, default);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey("Id"));
        Assert.True(controller.ModelState.ContainsKey("RowVersion"));
        Assert.Contains(controller.ModelState["Id"]!.Errors, e => e.ErrorMessage == "El usuario no existe.");
        Assert.Contains(controller.ModelState["RowVersion"]!.Errors, e => e.ErrorMessage == "El registro fue modificado por otro usuario.");
    }

    [Fact]
    public async Task Delete_Post_ReturnsViewWithGeneralError_WhenServiceFails()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.DeleteUsuarioAsync(It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUsuarioResult.Fail("Error interno del servidor."));

        var result = await controller.Delete(ValidModel, default);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(controller.ModelState[""]!.Errors, e => e.ErrorMessage == "Error interno del servidor.");
    }

    [Fact]
    public async Task Delete_Post_LogsInformation_WhenServiceSucceeds()
    {
        var (controller, serviceMock, loggerMock) = CreateController();

        serviceMock
            .Setup(x => x.DeleteUsuarioAsync(It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUsuarioResult.Ok());

        await controller.Delete(ValidModel, default);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("deleted successfully")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    // ======================================================================
    // Model Validation — UsuarioDeleteViewModel
    // ======================================================================

    private static List<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void DeleteViewModel_ValidModel_PassesAll()
    {
        var results = ValidateModel(ValidModel);

        Assert.Empty(results);
    }

    [Fact]
    public void DeleteViewModel_RowVersion_Required_Fails_WhenNull()
    {
        var model = new UsuarioDeleteViewModel
        {
            Id = 1,
            StrNombre = "Juan Perez",
            StrCorreoElectronico = "juan@test.com",
            RowVersion = null!
        };

        var results = ValidateModel(model);

        Assert.Contains(results, r => r.MemberNames.Contains("RowVersion"));
    }
}
