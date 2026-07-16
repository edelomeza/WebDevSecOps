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

public class VentaIndexTests
{
    private static (VentaController Controller, Mock<IVentaService> ServiceMock, Mock<IEstadoVentaService> EstadoMock, Mock<ILogger<VentaController>> LoggerMock) CreateController()
    {
        var serviceMock = new Mock<IVentaService>();
        var estadoMock = new Mock<IEstadoVentaService>();
        var clienteMock = new Mock<IClienteService>();
        var usuarioMock = new Mock<IUsuarioService>();
        var productoMock = new Mock<IProductoService>();
        var loggerMock = new Mock<ILogger<VentaController>>();
        var controller = new VentaController(serviceMock.Object, estadoMock.Object, clienteMock.Object, usuarioMock.Object, productoMock.Object, loggerMock.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>());

        return (controller, serviceMock, estadoMock, loggerMock);
    }

    [Fact]
    public async Task Index_ReturnsViewWithPaginatedResponse_WhenServiceReturnsData()
    {
        var (controller, serviceMock, estadoMock, _) = CreateController();

        estadoMock
            .Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidVenCatEstadoPaginatedResponse);

        serviceMock
            .Setup(x => x.GetVentasAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidVentaPaginatedResponse);

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PaginatedResponse<Venta>>(viewResult.Model);
        Assert.Single(model.Items);
        Assert.Equal("V-000001", model.Items[0].StrClaveVenta);
        Assert.Null(controller.TempData["Error"]);
    }

    [Fact]
    public async Task Index_ReturnsViewWithNullModel_WhenServiceReturnsNull()
    {
        var (controller, serviceMock, estadoMock, _) = CreateController();

        estadoMock
            .Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidVenCatEstadoPaginatedResponse);

        serviceMock
            .Setup(x => x.GetVentasAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaginatedResponse<Venta>?)null);

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.Model);
        Assert.Equal("No se pudieron cargar las ventas.", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Index_UsesDefaultPagination_WhenNoParametersProvided()
    {
        var (controller, serviceMock, estadoMock, _) = CreateController();

        estadoMock
            .Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidVenCatEstadoPaginatedResponse);

        var capturedPageNumber = 0;
        var capturedPageSize = 0;

        serviceMock
            .Setup(x => x.GetVentasAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<int, int, CancellationToken>((pn, ps, _) =>
            {
                capturedPageNumber = pn;
                capturedPageSize = ps;
            })
            .ReturnsAsync(new PaginatedResponse<Venta>());

        await controller.Index();

        Assert.Equal(1, capturedPageNumber);
        Assert.Equal(10, capturedPageSize);
    }

    [Fact]
    public async Task Index_CallsSearch_WhenTextoProvided()
    {
        var (controller, serviceMock, estadoMock, _) = CreateController();

        estadoMock
            .Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidVenCatEstadoPaginatedResponse);

        serviceMock
            .Setup(x => x.SearchVentasAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidVentaPaginatedResponse);

        await controller.Index(texto: "V-000001");

        serviceMock.Verify(
            x => x.SearchVentasAsync("V-000001", null, null, 1, 10, It.IsAny<CancellationToken>()),
            Times.Once);

        serviceMock.Verify(
            x => x.GetVentasAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Index_CallsSearch_WhenFechasProvided()
    {
        var (controller, serviceMock, estadoMock, _) = CreateController();

        estadoMock
            .Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidVenCatEstadoPaginatedResponse);

        serviceMock
            .Setup(x => x.SearchVentasAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidVentaPaginatedResponse);

        var desde = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var hasta = new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc);

        await controller.Index(dteFechaInicio: desde, dteFechaFin: hasta);

        serviceMock.Verify(
            x => x.SearchVentasAsync(null, desde, hasta, 1, 10, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Index_CallsGetAll_WhenNoFiltersProvided()
    {
        var (controller, serviceMock, estadoMock, _) = CreateController();

        estadoMock
            .Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidVenCatEstadoPaginatedResponse);

        serviceMock
            .Setup(x => x.GetVentasAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidVentaPaginatedResponse);

        await controller.Index();

        serviceMock.Verify(
            x => x.GetVentasAsync(1, 10, It.IsAny<CancellationToken>()),
            Times.Once);

        serviceMock.Verify(
            x => x.SearchVentasAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Index_LogsWarning_WhenServiceReturnsNull()
    {
        var (controller, serviceMock, estadoMock, loggerMock) = CreateController();

        estadoMock
            .Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidVenCatEstadoPaginatedResponse);

        serviceMock
            .Setup(x => x.GetVentasAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaginatedResponse<Venta>?)null);

        await controller.Index();

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("Failed to load ventas")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task Index_LoadsEstadoVentaDict()
    {
        var (controller, serviceMock, estadoMock, _) = CreateController();

        estadoMock
            .Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidVenCatEstadoPaginatedResponse);

        serviceMock
            .Setup(x => x.GetVentasAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidVentaPaginatedResponse);

        await controller.Index();

        var dict = controller.ViewBag.EstadoVentaDict as Dictionary<int, string>;
        Assert.NotNull(dict);
        Assert.Single(dict);
        Assert.Equal("Activa", dict[1]);
    }
}
