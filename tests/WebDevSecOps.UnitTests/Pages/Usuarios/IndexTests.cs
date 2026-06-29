using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using WebDevSecOps.Controllers;
using WebDevSecOps.Models;
using WebDevSecOps.Services;

namespace WebDevSecOps.UnitTests;

public class IndexTests
{
    private static (UsuarioController Controller, Mock<IUsuarioService> ServiceMock, Mock<ILogger<UsuarioController>> LoggerMock) CreateController()
    {
        var serviceMock = new Mock<IUsuarioService>();
        var loggerMock = new Mock<ILogger<UsuarioController>>();
        var controller = new UsuarioController(serviceMock.Object, loggerMock.Object);

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

        var usuarios = new List<Usuario>
        {
            new() { Id = 1, StrNombre = "Juan Pérez", StrCorreoElectronico = "juan@test.com", DteFechaRegistro = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc) }
        };
        var expectedResponse = new PaginatedResponse<Usuario>
        {
            Items = usuarios,
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10,
            TotalPages = 1
        };

        serviceMock
            .Setup(x => x.GetUsuariosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PaginatedResponse<Usuario>>(viewResult.Model);
        Assert.Single(model.Items);
        Assert.Equal(1, model.TotalCount);
        Assert.Equal(1, model.PageNumber);
        Assert.Equal(10, model.PageSize);
        Assert.Equal(1, model.TotalPages);
        Assert.Equal("Juan Pérez", model.Items[0].StrNombre);
        Assert.Null(controller.TempData["Error"]);
    }

    [Fact]
    public async Task Index_ReturnsViewWithNullModel_WhenServiceReturnsNull()
    {
        var (controller, serviceMock, _) = CreateController();

        serviceMock
            .Setup(x => x.GetUsuariosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaginatedResponse<Usuario>?)null);

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.Model);
        Assert.Equal("No se pudieron cargar los usuarios.", controller.TempData["Error"]);
    }

    [Fact]
    public async Task Index_ReturnsViewWithEmptyList_WhenServiceReturnsNoData()
    {
        var (controller, serviceMock, _) = CreateController();

        var expectedResponse = new PaginatedResponse<Usuario>
        {
            Items = [],
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10,
            TotalPages = 0
        };

        serviceMock
            .Setup(x => x.GetUsuariosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PaginatedResponse<Usuario>>(viewResult.Model);
        Assert.Empty(model.Items);
        Assert.Equal(0, model.TotalCount);
        Assert.Null(controller.TempData["Error"]);
    }

    [Fact]
    public async Task Index_UsesDefaultPagination_WhenNoParametersProvided()
    {
        var (controller, serviceMock, _) = CreateController();

        var capturedPageNumber = 0;
        var capturedPageSize = 0;

        serviceMock
            .Setup(x => x.GetUsuariosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<int, int, CancellationToken>((pn, ps, _) =>
            {
                capturedPageNumber = pn;
                capturedPageSize = ps;
            })
            .ReturnsAsync(new PaginatedResponse<Usuario>());

        await controller.Index();

        Assert.Equal(1, capturedPageNumber);
        Assert.Equal(10, capturedPageSize);
    }

    [Fact]
    public async Task Index_PassesCustomPaginationToService()
    {
        var (controller, serviceMock, _) = CreateController();

        var capturedPageNumber = 0;
        var capturedPageSize = 0;

        serviceMock
            .Setup(x => x.GetUsuariosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<int, int, CancellationToken>((pn, ps, _) =>
            {
                capturedPageNumber = pn;
                capturedPageSize = ps;
            })
            .ReturnsAsync(new PaginatedResponse<Usuario>());

        await controller.Index(pageNumber: 3, pageSize: 25);

        Assert.Equal(3, capturedPageNumber);
        Assert.Equal(25, capturedPageSize);
    }

    [Fact]
    public async Task Index_LogsWarning_WhenServiceReturnsNull()
    {
        var (controller, serviceMock, loggerMock) = CreateController();

        serviceMock
            .Setup(x => x.GetUsuariosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaginatedResponse<Usuario>?)null);

        await controller.Index();

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("Failed to load usuarios")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task Index_DoesNotLogWarning_WhenServiceReturnsData()
    {
        var (controller, serviceMock, loggerMock) = CreateController();

        serviceMock
            .Setup(x => x.GetUsuariosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResponse<Usuario>());

        await controller.Index();

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
