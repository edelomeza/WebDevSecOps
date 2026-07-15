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

public class ProductoUpdateTests
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
    public async Task Update_Get_ReturnsViewWithModel_WhenProductoFound()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.GetProductoByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidProducto);

        var result = await controller.Update(1, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ProductoUpdateViewModel>(viewResult.Model);
        Assert.Equal("Laptop Gamer", model.StrNombreProducto);
    }

    [Fact]
    public async Task Update_Get_RedirectsToIndex_WhenProductoNotFound()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.GetProductoByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(Producto));

        var result = await controller.Update(999, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/Producto/Index", redirectResult.Url);
        Assert.Equal("Producto no encontrado.", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Update_Post_ReturnsRedirectToIndex_WhenSuccess()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.UpdateProductoAsync(It.IsAny<int>(), It.IsAny<ProductoUpdateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Ok());

        var result = await controller.Update(ContractTestData.ValidProductoUpdateViewModel, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Producto actualizado exitosamente.", controller.TempData["Success"]);
    }

    [Fact]
    public async Task Update_Post_ReturnsViewWithModel_WhenModelInvalid()
    {
        var (controller, _, _) = CreateController();

        controller.ModelState.AddModelError("StrNombreProducto", "Required");

        var result = await controller.Update(ContractTestData.ValidProductoUpdateViewModel, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<ProductoUpdateViewModel>(viewResult.Model);
    }
}
