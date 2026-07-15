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

public class ProductoIndexTests
{
    private static (ProductoController Controller, Mock<IProductoService> ServiceMock, Mock<ILogger<ProductoController>> LoggerMock) CreateController()
    {
        var serviceMock = new Mock<IProductoService>();
        var loggerMock = new Mock<ILogger<ProductoController>>();
        var controller = new ProductoController(serviceMock.Object, loggerMock.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>());

        return (controller, serviceMock, loggerMock);
    }

    [Fact]
    public async Task Index_ReturnsViewWithPaginatedResponse_WhenServiceReturnsData()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.GetProductosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidProductoPaginatedResponse);

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PaginatedResponse<Producto>>(viewResult.Model);
        Assert.Single(model.Items);
        Assert.Equal("Laptop Gamer", model.Items[0].StrNombreProducto);
        Assert.Null(controller.TempData["Error"]);
    }

    [Fact]
    public async Task Index_ReturnsViewWithNullModel_WhenServiceReturnsNull()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.GetProductosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaginatedResponse<Producto>?)null);

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.Model);
        Assert.Equal("No se pudieron cargar los productos.", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Index_UsesDefaultPagination_WhenNoParametersProvided()
    {
        var (controller, serviceMock, _) = CreateController();

        var capturedPageNumber = 0;
        var capturedPageSize = 0;

        serviceMock
            .Setup(x => x.GetProductosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<int, int, CancellationToken>((pn, ps, _) =>
            {
                capturedPageNumber = pn;
                capturedPageSize = ps;
            })
            .ReturnsAsync(new PaginatedResponse<Producto>());

        await controller.Index();

        Assert.Equal(1, capturedPageNumber);
        Assert.Equal(10, capturedPageSize);
    }

    [Fact]
    public async Task Index_CallsSearch_WhenTextoProvided()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.SearchProductosAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidProductoPaginatedResponse);

        await controller.Index(texto: "Laptop");

        serviceMock.Verify(
            x => x.SearchProductosAsync("Laptop", 1, 10, It.IsAny<CancellationToken>()),
            Times.Once);

        serviceMock.Verify(
            x => x.GetProductosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Index_CallsGetAll_WhenNoFiltersProvided()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.GetProductosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidProductoPaginatedResponse);

        await controller.Index();

        serviceMock.Verify(
            x => x.GetProductosAsync(1, 10, It.IsAny<CancellationToken>()),
            Times.Once);

        serviceMock.Verify(
            x => x.SearchProductosAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Index_LogsWarning_WhenServiceReturnsNull()
    {
        var (controller, serviceMock, loggerMock) = CreateController();

        serviceMock
            .Setup(x => x.GetProductosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaginatedResponse<Producto>?)null);

        await controller.Index();

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("Failed to load productos")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }
}
