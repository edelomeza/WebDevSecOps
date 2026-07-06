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

public class EmpleadoDeleteTests
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
    public async Task Delete_Get_ReturnsViewWithModel_WhenEmpleadoFound()
    {
        var (controller, serviceMock, tipoMock, _) = CreateController();

        tipoMock
            .Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidTipoEmpleado);

        serviceMock
            .Setup(x => x.GetEmpleadoByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidEmpleado);

        var result = await controller.Delete(1, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<EmpleadoDeleteViewModel>(viewResult.Model);
        Assert.Equal("Carlos Lopez", model.StrNombre);
        Assert.Equal("Administrativo", model.StrValorTipoEmpleado);
    }

    [Fact]
    public async Task Delete_Get_RedirectsToIndex_WhenEmpleadoNotFound()
    {
        var (controller, serviceMock, _, _) = CreateController();

        serviceMock
            .Setup(x => x.GetEmpleadoByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Empleado?)null);

        var result = await controller.Delete(999, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/Empleado/Index", redirectResult.Url);
        Assert.Equal("Empleado no encontrado.", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Delete_Post_ReturnsRedirectToIndex_WhenSuccess()
    {
        var (controller, serviceMock, _, _) = CreateController();

        serviceMock
            .Setup(x => x.DeleteEmpleadoAsync(It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Ok());

        var model = new EmpleadoDeleteViewModel
        {
            Id = 1,
            RowVersion = [0x01, 0x02, 0x03, 0x04]
        };

        var result = await controller.Delete(model, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Operación exitosa!!", controller.TempData["Success"]);
    }

    [Fact]
    public async Task Delete_Post_ReturnsView_WhenServiceFails()
    {
        var (controller, serviceMock, _, _) = CreateController();

        serviceMock
            .Setup(x => x.DeleteEmpleadoAsync(It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Fail("Error al eliminar el empleado."));

        var model = new EmpleadoDeleteViewModel
        {
            Id = 1,
            RowVersion = [0x01, 0x02, 0x03, 0x04]
        };

        var result = await controller.Delete(model, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<EmpleadoDeleteViewModel>(viewResult.Model);
    }
}
