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

public class EmpleadoCreateTests
{
    private static (EmpleadoController Controller, Mock<IEmpleadoService> ServiceMock, Mock<ITipoEmpleadoService> TipoMock, Mock<ILogger<EmpleadoController>> LoggerMock) CreateController()
    {
        var serviceMock = new Mock<IEmpleadoService>();
        var tipoMock = new Mock<ITipoEmpleadoService>();
        var loggerMock = new Mock<ILogger<EmpleadoController>>();
        var controller = new EmpleadoController(serviceMock.Object, tipoMock.Object, loggerMock.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>());

        return (controller, serviceMock, tipoMock, loggerMock);
    }

    [Fact]
    public async Task Create_Get_ReturnsView()
    {
        var (controller, _, tipoMock, _) = CreateController();

        tipoMock
            .Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidTipoEmpleadoPaginatedResponse);

        var result = await controller.Create();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.Model);
    }

    [Fact]
    public async Task Create_Get_LoadsTipoEmpleadoList()
    {
        var (controller, _, tipoMock, _) = CreateController();

        tipoMock
            .Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidTipoEmpleadoPaginatedResponse);

        await controller.Create();

        Assert.NotNull(controller.ViewBag.TipoEmpleadoList);
    }

    [Fact]
    public async Task Create_Post_ReturnsRedirectToIndex_WhenSuccess()
    {
        var (controller, serviceMock, tipoMock, _) = CreateController();

        serviceMock
            .Setup(x => x.CreateEmpleadoAsync(It.IsAny<EmpleadoCreateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Ok());

        var result = await controller.Create(ContractTestData.ValidEmpleadoCreateViewModel, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Operación exitosa!!", controller.TempData["Success"]);
    }

    [Fact]
    public async Task Create_Post_ReturnsViewWithModel_WhenModelInvalid()
    {
        var (controller, _, tipoMock, _) = CreateController();

        tipoMock
            .Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidTipoEmpleadoPaginatedResponse);

        controller.ModelState.AddModelError("StrNombre", "Required");

        var result = await controller.Create(ContractTestData.ValidEmpleadoCreateViewModel, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<EmpleadoCreateViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task Create_Post_ReturnsViewWithModel_WhenServiceFails()
    {
        var (controller, serviceMock, tipoMock, _) = CreateController();

        tipoMock
            .Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidTipoEmpleadoPaginatedResponse);

        serviceMock
            .Setup(x => x.CreateEmpleadoAsync(It.IsAny<EmpleadoCreateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Fail("Error al crear el empleado."));

        var result = await controller.Create(ContractTestData.ValidEmpleadoCreateViewModel, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<EmpleadoCreateViewModel>(viewResult.Model);
    }
}
