using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using WebDevSecOps.Controllers;
using WebDevSecOps.Models;
using WebDevSecOps.Services;
using WebDevSecOps.UnitTests.Common;

namespace WebDevSecOps.UnitTests;

public class EmpleadoIndexTests
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
    public async Task Index_ReturnsViewWithPaginatedResponse_WhenServiceReturnsData()
    {
        var (controller, serviceMock, tipoMock, _) = CreateController();

        tipoMock
            .Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidTipoEmpleadoPaginatedResponse);

        serviceMock
            .Setup(x => x.GetEmpleadosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidEmpleadoPaginatedResponse);

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PaginatedResponse<Empleado>>(viewResult.Model);
        Assert.Single(model.Items);
        Assert.Equal("Carlos Lopez", model.Items[0].StrNombre);
        Assert.Null(controller.TempData["Error"]);
    }

    [Fact]
    public async Task Index_ReturnsViewWithNullModel_WhenServiceReturnsNull()
    {
        var (controller, serviceMock, tipoMock, _) = CreateController();

        tipoMock
            .Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidTipoEmpleadoPaginatedResponse);

        serviceMock
            .Setup(x => x.GetEmpleadosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaginatedResponse<Empleado>?)null);

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.Model);
        Assert.Equal("No se pudieron cargar los empleados.", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Index_UsesDefaultPagination_WhenNoParametersProvided()
    {
        var (controller, serviceMock, tipoMock, _) = CreateController();

        tipoMock
            .Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidTipoEmpleadoPaginatedResponse);

        var capturedPageNumber = 0;
        var capturedPageSize = 0;

        serviceMock
            .Setup(x => x.GetEmpleadosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<int, int, CancellationToken>((pn, ps, _) =>
            {
                capturedPageNumber = pn;
                capturedPageSize = ps;
            })
            .ReturnsAsync(new PaginatedResponse<Empleado>());

        await controller.Index();

        Assert.Equal(1, capturedPageNumber);
        Assert.Equal(6, capturedPageSize);
    }

    [Fact]
    public async Task Index_CallsSearch_WhenTextoProvided()
    {
        var (controller, serviceMock, tipoMock, _) = CreateController();

        tipoMock
            .Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidTipoEmpleadoPaginatedResponse);

        serviceMock
            .Setup(x => x.SearchEmpleadosAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidEmpleadoPaginatedResponse);

        await controller.Index(texto: "Carlos");

        serviceMock.Verify(
            x => x.SearchEmpleadosAsync("Carlos", null, 1, 6, It.IsAny<CancellationToken>()),
            Times.Once);

        serviceMock.Verify(
            x => x.GetEmpleadosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Index_CallsSearch_WhenIdTipoEmpleadoProvided()
    {
        var (controller, serviceMock, tipoMock, _) = CreateController();

        tipoMock
            .Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidTipoEmpleadoPaginatedResponse);

        serviceMock
            .Setup(x => x.SearchEmpleadosAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidEmpleadoPaginatedResponse);

        await controller.Index(idTipoEmpleado: 2);

        serviceMock.Verify(
            x => x.SearchEmpleadosAsync(null, 2, 1, 6, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Index_CallsGetAll_WhenNoFiltersProvided()
    {
        var (controller, serviceMock, tipoMock, _) = CreateController();

        tipoMock
            .Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidTipoEmpleadoPaginatedResponse);

        serviceMock
            .Setup(x => x.GetEmpleadosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidEmpleadoPaginatedResponse);

        await controller.Index();

        serviceMock.Verify(
            x => x.GetEmpleadosAsync(1, 6, It.IsAny<CancellationToken>()),
            Times.Once);

        serviceMock.Verify(
            x => x.SearchEmpleadosAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Index_LogsWarning_WhenServiceReturnsNull()
    {
        var (controller, serviceMock, tipoMock, loggerMock) = CreateController();

        tipoMock
            .Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidTipoEmpleadoPaginatedResponse);

        serviceMock
            .Setup(x => x.GetEmpleadosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaginatedResponse<Empleado>?)null);

        await controller.Index();

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("Failed to load empleados")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task Index_LoadsTipoEmpleadoList()
    {
        var (controller, serviceMock, tipoMock, _) = CreateController();

        tipoMock
            .Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidTipoEmpleadoPaginatedResponse);

        serviceMock
            .Setup(x => x.GetEmpleadosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidEmpleadoPaginatedResponse);

        await controller.Index();

        var tipoList = controller.ViewBag.TipoEmpleadoList as SelectList;
        Assert.NotNull(tipoList);
        Assert.Single(tipoList);
    }
}
