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

public class UpdateTests
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

    private static UsuarioUpdateViewModel ValidModel => new()
    {
        Id = 1,
        StrNombre = "Juan Perez",
        StrPWD = "Abc123!@",
        StrCorreoElectronico = "juan@test.com",
        RowVersion = [1, 2, 3]
    };

    // ======================================================================
    // GET Update
    // ======================================================================

    [Fact]
    public async Task Update_Get_ReturnsViewWithModel_WhenUserFound()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.GetUsuarioByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingUser);

        var result = await controller.Update(1, default);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<UsuarioUpdateViewModel>(viewResult.Model);
        Assert.Equal(ExistingUser.Id, model.Id);
        Assert.Equal(ExistingUser.StrNombre, model.StrNombre);
        Assert.Equal(ExistingUser.StrCorreoElectronico, model.StrCorreoElectronico);
        Assert.Equal(ExistingUser.RowVersion, model.RowVersion);
    }

    [Fact]
    public async Task Update_Get_RedirectsToIndex_WhenUserNotFound()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.GetUsuarioByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var result = await controller.Update(999, default);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/Usuario/Index", redirect.Url);
        Assert.Equal("Usuario no encontrado.", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Update_Get_RedirectsToReturnUrl_WhenUserNotFoundWithLocalUrl()
    {
        var (controller, serviceMock, _) = CreateController();

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(x => x.IsLocalUrl("/dashboard")).Returns(true);
        controller.Url = urlHelper.Object;

        serviceMock
            .Setup(x => x.GetUsuarioByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var result = await controller.Update(999, default, returnUrl: "/dashboard");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/dashboard", redirect.Url);
    }

    [Fact]
    public async Task Update_Get_RedirectsToIndex_WhenUserNotFoundWithNonLocalUrl()
    {
        var (controller, serviceMock, _) = CreateController();

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(x => x.IsLocalUrl("https://evil.com")).Returns(false);
        controller.Url = urlHelper.Object;

        serviceMock
            .Setup(x => x.GetUsuarioByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var result = await controller.Update(999, default, returnUrl: "https://evil.com");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/Usuario/Index", redirect.Url);
    }

    [Fact]
    public async Task Update_Get_LogsWarning_WhenUserNotFound()
    {
        var (controller, serviceMock, loggerMock) = CreateController();

        serviceMock
            .Setup(x => x.GetUsuarioByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        await controller.Update(999, default);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("not found for update")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task Update_Get_SetsReturnUrlInViewData_WhenUserFound()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.GetUsuarioByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingUser);

        var result = await controller.Update(1, default, returnUrl: "/dashboard");

        Assert.IsType<ViewResult>(result);
        Assert.Equal("/dashboard", controller.ViewData["ReturnUrl"]);
    }

    // ======================================================================
    // POST Update — Controller with Mocks
    // ======================================================================

    [Fact]
    public async Task Update_Post_ReturnsView_WhenModelStateInvalid()
    {
        var (controller, serviceMock, _) = CreateController();
        controller.ModelState.AddModelError("StrNombre", "Required");

        var result = await controller.Update(ValidModel, default);

        Assert.IsType<ViewResult>(result);
        serviceMock.Verify(
            x => x.UpdateUsuarioAsync(It.IsAny<int>(), It.IsAny<UsuarioUpdateViewModel>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Update_Post_RedirectsToIndex_WhenServiceSucceeds()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.UpdateUsuarioAsync(It.IsAny<int>(), It.IsAny<UsuarioUpdateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUsuarioResult.Ok());

        var result = await controller.Update(ValidModel, default);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Usuario actualizado exitosamente.", controller.TempData["Success"]);
    }

    [Fact]
    public async Task Update_Post_RedirectsToReturnUrl_WhenLocalUrl()
    {
        var (controller, serviceMock, _) = CreateController();

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(x => x.IsLocalUrl("/dashboard")).Returns(true);
        controller.Url = urlHelper.Object;

        serviceMock
            .Setup(x => x.UpdateUsuarioAsync(It.IsAny<int>(), It.IsAny<UsuarioUpdateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUsuarioResult.Ok());

        var result = await controller.Update(ValidModel, default, returnUrl: "/dashboard");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/dashboard", redirect.Url);
    }

    [Fact]
    public async Task Update_Post_RedirectsToIndex_WhenNonLocalUrl()
    {
        var (controller, serviceMock, _) = CreateController();

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(x => x.IsLocalUrl("https://evil.com")).Returns(false);
        controller.Url = urlHelper.Object;

        serviceMock
            .Setup(x => x.UpdateUsuarioAsync(It.IsAny<int>(), It.IsAny<UsuarioUpdateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUsuarioResult.Ok());

        var result = await controller.Update(ValidModel, default, returnUrl: "https://evil.com");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task Update_Post_ReturnsViewWithMappedFieldErrors_WhenServiceFails()
    {
        var (controller, serviceMock, _) = CreateController();

        var fieldErrors = new Dictionary<string, string[]>
        {
            { "strNombre", new[] { "El nombre ya existe." } }
        };

        serviceMock
            .Setup(x => x.UpdateUsuarioAsync(It.IsAny<int>(), It.IsAny<UsuarioUpdateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUsuarioResult.Fail(fieldErrors));

        var result = await controller.Update(ValidModel, default);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey("StrNombre"));
        Assert.Contains(controller.ModelState["StrNombre"]!.Errors, e => e.ErrorMessage == "El nombre ya existe.");
    }

    [Fact]
    public async Task Update_Post_ReturnsViewWithMappedErrors_ForAllFieldKeys()
    {
        var (controller, serviceMock, _) = CreateController();

        var fieldErrors = new Dictionary<string, string[]>
        {
            { "strNombre", new[] { "Error nombre" } },
            { "strPWD", new[] { "Error password" } },
            { "strCorreoElectronico", new[] { "Error email" } },
            { "rowVersion", new[] { "Error version" } }
        };

        serviceMock
            .Setup(x => x.UpdateUsuarioAsync(It.IsAny<int>(), It.IsAny<UsuarioUpdateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUsuarioResult.Fail(fieldErrors));

        await controller.Update(ValidModel, default);

        Assert.True(controller.ModelState.ContainsKey("StrNombre"));
        Assert.True(controller.ModelState.ContainsKey("StrPWD"));
        Assert.True(controller.ModelState.ContainsKey("StrCorreoElectronico"));
        Assert.True(controller.ModelState.ContainsKey("RowVersion"));
        Assert.Contains(controller.ModelState["StrPWD"]!.Errors, e => e.ErrorMessage == "Error password");
    }

    [Fact]
    public async Task Update_Post_ReturnsViewWithGeneralError_WhenServiceFails()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.UpdateUsuarioAsync(It.IsAny<int>(), It.IsAny<UsuarioUpdateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUsuarioResult.Fail("Error interno del servidor."));

        var result = await controller.Update(ValidModel, default);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(controller.ModelState[""]!.Errors, e => e.ErrorMessage == "Error interno del servidor.");
    }

    [Fact]
    public async Task Update_Post_LogsInformation_WhenServiceSucceeds()
    {
        var (controller, serviceMock, loggerMock) = CreateController();

        serviceMock
            .Setup(x => x.UpdateUsuarioAsync(It.IsAny<int>(), It.IsAny<UsuarioUpdateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUsuarioResult.Ok());

        await controller.Update(ValidModel, default);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("updated successfully")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    // ======================================================================
    // Model Validation — UsuarioUpdateViewModel
    // ======================================================================

    private static List<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void UpdateViewModel_Nombre_Required_Fails()
    {
        var model = new UsuarioUpdateViewModel
        {
            Id = 1,
            StrNombre = "",
            StrPWD = "Abc123!@",
            StrCorreoElectronico = "juan@test.com",
            RowVersion = [1, 2, 3]
        };

        var results = ValidateModel(model);

        Assert.Contains(results, r => r.MemberNames.Contains("StrNombre") && r.ErrorMessage!.Contains("obligatorio"));
    }

    [Fact]
    public void UpdateViewModel_Nombre_MaxLength_Fails()
    {
        var model = new UsuarioUpdateViewModel
        {
            Id = 1,
            StrNombre = new string('a', 51),
            StrPWD = "Abc123!@",
            StrCorreoElectronico = "juan@test.com",
            RowVersion = [1, 2, 3]
        };

        var results = ValidateModel(model);

        Assert.Contains(results, r => r.MemberNames.Contains("StrNombre") && r.ErrorMessage!.Contains("exceder"));
    }

    [Fact]
    public void UpdateViewModel_Nombre_InvalidCharacters_Fails()
    {
        var model = new UsuarioUpdateViewModel
        {
            Id = 1,
            StrNombre = "Juan@#$",
            StrPWD = "Abc123!@",
            StrCorreoElectronico = "juan@test.com",
            RowVersion = [1, 2, 3]
        };

        var results = ValidateModel(model);

        Assert.Contains(results, r => r.MemberNames.Contains("StrNombre") && r.ErrorMessage!.Contains("solo permite"));
    }

    [Fact]
    public void UpdateViewModel_Password_MinLength_Fails()
    {
        var model = new UsuarioUpdateViewModel
        {
            Id = 1,
            StrNombre = "Juan",
            StrPWD = "Ab1!@",
            StrCorreoElectronico = "juan@test.com",
            RowVersion = [1, 2, 3]
        };

        var results = ValidateModel(model);

        Assert.Contains(results, r => r.MemberNames.Contains("StrPWD") && r.ErrorMessage!.Contains("al menos"));
    }

    [Fact]
    public void UpdateViewModel_Email_InvalidFormat_Fails()
    {
        var model = new UsuarioUpdateViewModel
        {
            Id = 1,
            StrNombre = "Juan",
            StrPWD = "Abc123!@",
            StrCorreoElectronico = "invalido",
            RowVersion = [1, 2, 3]
        };

        var results = ValidateModel(model);

        Assert.Contains(results, r => r.MemberNames.Contains("StrCorreoElectronico") && r.ErrorMessage!.Contains("formato"));
    }

    [Fact]
    public void UpdateViewModel_ValidModel_PassesAll()
    {
        var results = ValidateModel(ValidModel);

        Assert.Empty(results);
    }
}
