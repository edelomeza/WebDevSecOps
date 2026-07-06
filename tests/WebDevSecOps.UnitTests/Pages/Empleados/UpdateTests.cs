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

public class EmpleadoUpdateTests
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
    public async Task Update_Get_ReturnsViewWithModel_WhenEmpleadoFound()
    {
        var (controller, serviceMock, tipoMock, _) = CreateController();

        tipoMock
            .Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidTipoEmpleadoPaginatedResponse);

        serviceMock
            .Setup(x => x.GetEmpleadoByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidEmpleado);

        var result = await controller.Update(1, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<EmpleadoUpdateViewModel>(viewResult.Model);
        Assert.Equal("Carlos Lopez", model.StrNombre);
    }

    [Fact]
    public async Task Update_Get_RedirectsToIndex_WhenEmpleadoNotFound()
    {
        var (controller, serviceMock, _, _) = CreateController();

        serviceMock
            .Setup(x => x.GetEmpleadoByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(null);

        var result = await controller.Update(999, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/Empleado/Index", redirectResult.Url);
        Assert.Equal("Empleado no encontrado.", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Update_Post_ReturnsRedirectToIndex_WhenSuccess()
    {
        var (controller, serviceMock, _, _) = CreateController();

        serviceMock
            .Setup(x => x.UpdateEmpleadoAsync(It.IsAny<int>(), It.IsAny<EmpleadoUpdateViewModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult.Ok());

        var result = await controller.Update(ContractTestData.ValidEmpleadoUpdateViewModel, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Operación exitosa!!", controller.TempData["Success"]);
    }

    [Fact]
    public async Task Update_Post_ReturnsViewWithModel_WhenModelInvalid()
    {
        var (controller, _, tipoMock, _) = CreateController();

        tipoMock
            .Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContractTestData.ValidTipoEmpleadoPaginatedResponse);

        controller.ModelState.AddModelError("StrNombre", "Required");

        var result = await controller.Update(ContractTestData.ValidEmpleadoUpdateViewModel, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<EmpleadoUpdateViewModel>(viewResult.Model);
    }
}
