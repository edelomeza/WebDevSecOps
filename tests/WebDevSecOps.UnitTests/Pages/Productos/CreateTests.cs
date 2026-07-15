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

public class ProductoCreateTests
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
    public async Task Create_Get_ReturnsView()
    {
        var (controller, _, _) = CreateController();

        var result = await controller.Create();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.Model);
    }

    [Fact]
    public async Task Create_Post_ReturnsRedirectToIndex_WhenSuccess()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.CreateProductoAsync(It.IsAny<ProductoCreateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Ok());

        var result = await controller.Create(ContractTestData.ValidProductoCreateViewModel, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Producto creado exitosamente.", controller.TempData["Success"]);
    }

    [Fact]
    public async Task Create_Post_ReturnsViewWithModel_WhenModelInvalid()
    {
        var (controller, _, _) = CreateController();

        controller.ModelState.AddModelError("StrNombreProducto", "Required");

        var result = await controller.Create(ContractTestData.ValidProductoCreateViewModel, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<ProductoCreateViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task Create_Post_ReturnsViewWithModel_WhenServiceFails()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.CreateProductoAsync(It.IsAny<ProductoCreateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Fail("Error al crear el producto."));

        var result = await controller.Create(ContractTestData.ValidProductoCreateViewModel, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<ProductoCreateViewModel>(viewResult.Model);
    }
}
