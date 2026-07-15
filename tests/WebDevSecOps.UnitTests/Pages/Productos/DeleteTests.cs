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

public class ProductoDeleteTests
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
    public async Task Delete_Get_ReturnsViewWithModel_WhenProductoFound()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.GetProductoByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidProducto);

        var result = await controller.Delete(1, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ProductoDeleteViewModel>(viewResult.Model);
        Assert.Equal("Laptop Gamer", model.StrNombreProducto);
    }

    [Fact]
    public async Task Delete_Get_RedirectsToIndex_WhenProductoNotFound()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.GetProductoByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Producto?)null);

        var result = await controller.Delete(999, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/Producto/Index", redirectResult.Url);
        Assert.Equal("Producto no encontrado.", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Delete_Post_ReturnsRedirectToIndex_WhenSuccess()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.DeleteProductoAsync(It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Ok());

        var model = new ProductoDeleteViewModel
        {
            Id = 1,
            RowVersion = [0x01, 0x02, 0x03, 0x04]
        };

        var result = await controller.Delete(model, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Producto eliminado exitosamente.", controller.TempData["Success"]);
    }

    [Fact]
    public async Task Delete_Post_ReturnsView_WhenServiceFails()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.DeleteProductoAsync(It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Fail("Error al eliminar el producto."));

        var model = new ProductoDeleteViewModel
        {
            Id = 1,
            RowVersion = [0x01, 0x02, 0x03, 0x04]
        };

        var result = await controller.Delete(model, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<ProductoDeleteViewModel>(viewResult.Model);
    }
}
