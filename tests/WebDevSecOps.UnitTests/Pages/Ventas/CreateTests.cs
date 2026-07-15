using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using WebDevSecOps.Controllers;
using WebDevSecOps.Models;
using WebDevSecOps.Services;
using WebDevSecOps.UnitTests.Common;

namespace WebDevSecOps.UnitTests;

public class VentaCreateTests
{
    private static (VentaController Controller, Mock<IVentaService> ServiceMock, Mock<IEstadoVentaService> EstadoMock, Mock<IClienteService> ClienteMock, Mock<IUsuarioService> UsuarioMock, Mock<ILogger<VentaController>> LoggerMock) CreateController()
    {
        var serviceMock = new Mock<IVentaService>();
        var estadoMock = new Mock<IEstadoVentaService>();
        var clienteMock = new Mock<IClienteService>();
        var usuarioMock = new Mock<IUsuarioService>();
        var loggerMock = new Mock<ILogger<VentaController>>();
        var controller = new VentaController(serviceMock.Object, estadoMock.Object, clienteMock.Object, usuarioMock.Object, loggerMock.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>());

        return (controller, serviceMock, estadoMock, clienteMock, usuarioMock, loggerMock);
    }

    [Fact]
    public async Task Create_Get_ReturnsView()
    {
        var (controller, _, _, _, _, _) = CreateController();

        var result = await controller.Create();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.Model);
    }

    [Fact]
    public async Task Create_Post_ReturnsRedirectToIndex_WhenSuccess()
    {
        var (controller, serviceMock, _, _, _, _) = CreateController();

        serviceMock
            .Setup(x => x.CreateVentaAsync(It.IsAny<VentaCreateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Ok());

        var result = await controller.Create(ContractTestData.ValidVentaCreateViewModel, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Venta creada exitosamente.", controller.TempData["Success"]);
    }

    [Fact]
    public async Task Create_Post_ReturnsViewWithModel_WhenModelInvalid()
    {
        var (controller, _, _, _, _, _) = CreateController();

        controller.ModelState.AddModelError("IdCliCliente", "Required");

        var result = await controller.Create(ContractTestData.ValidVentaCreateViewModel, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<VentaCreateViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task Create_Post_ReturnsViewWithModel_WhenServiceFails()
    {
        var (controller, serviceMock, _, _, _, _) = CreateController();

        serviceMock
            .Setup(x => x.CreateVentaAsync(It.IsAny<VentaCreateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Fail("Error al crear la venta."));

        var result = await controller.Create(ContractTestData.ValidVentaCreateViewModel, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<VentaCreateViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task Create_Post_MapsFieldErrors_WhenServiceReturnsFieldErrors()
    {
        var (controller, serviceMock, _, _, _, _) = CreateController();

        var fieldErrors = new Dictionary<string, string[]>
        {
            ["idCliCliente"] = ["El cliente no existe."]
        };

        serviceMock
            .Setup(x => x.CreateVentaAsync(It.IsAny<VentaCreateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Fail(fieldErrors, "Validation Error"));

        var result = await controller.Create(ContractTestData.ValidVentaCreateViewModel, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<VentaCreateViewModel>(viewResult.Model);
        Assert.False(controller.ViewData.ModelState.IsValid);
        Assert.True(controller.ViewData.ModelState.ContainsKey(nameof(VentaCreateViewModel.IdCliCliente)));
    }

    [Fact]
    public async Task ClientesAutocomplete_ReturnsEmpty_WhenTextoLessThan2()
    {
        var (controller, _, _, _, _, _) = CreateController();

        var result = await controller.ClientesAutocomplete("a");

        var jsonResult = Assert.IsType<JsonResult>(result);
        var list = Assert.IsType<List<CliClienteAutocompleteDto>>(jsonResult.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task ClientesAutocomplete_ReturnsData_WhenTextoIsValid()
    {
        var (controller, _, _, clienteMock, _, _) = CreateController();

        clienteMock
            .Setup(x => x.AutocompleteClientesAsync("Maria", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidClienteAutocompleteList);

        var result = await controller.ClientesAutocomplete("Maria");

        var jsonResult = Assert.IsType<JsonResult>(result);
        var list = Assert.IsType<List<CliClienteAutocompleteDto>>(jsonResult.Value);
        Assert.Equal(2, list.Count);
        Assert.Equal("Maria Garcia", list[0].StrNombreCliente);
    }

    [Fact]
    public async Task UsuariosAutocomplete_ReturnsEmpty_WhenTextoLessThan2()
    {
        var (controller, _, _, _, _, _) = CreateController();

        var result = await controller.UsuariosAutocomplete("b");

        var jsonResult = Assert.IsType<JsonResult>(result);
        var list = Assert.IsType<List<SegUsuarioAutocompleteDto>>(jsonResult.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task UsuariosAutocomplete_ReturnsData_WhenTextoIsValid()
    {
        var (controller, _, _, _, usuarioMock, _) = CreateController();

        usuarioMock
            .Setup(x => x.AutocompleteUsuariosAsync("admin", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidUsuarioAutocompleteList);

        var result = await controller.UsuariosAutocomplete("admin");

        var jsonResult = Assert.IsType<JsonResult>(result);
        var list = Assert.IsType<List<SegUsuarioAutocompleteDto>>(jsonResult.Value);
        Assert.Equal(2, list.Count);
        Assert.Equal("admin", list[0].StrNombre);
    }

    [Fact]
    public async Task Create_Post_ReturnsViewWithGeneralError_WhenServiceErrorMessage()
    {
        var (controller, serviceMock, _, _, _, _) = CreateController();

        serviceMock
            .Setup(x => x.CreateVentaAsync(It.IsAny<VentaCreateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Fail("Error de conexión."));

        var result = await controller.Create(ContractTestData.ValidVentaCreateViewModel, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<VentaCreateViewModel>(viewResult.Model);
        Assert.False(controller.ViewData.ModelState.IsValid);
        Assert.True(controller.ViewData.ModelState.ContainsKey(string.Empty));
    }
}
