using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebAppCellMapper.Controllers;
using WebAppCellMapper.Data.Models;
using WebAppCellMapper.DTO;
using WebAppCellMapper.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TestProjectWebCell
{
    public class StationsControllerTests
    {
        private readonly Mock<IStationsScanningManager> scanningManagerMock;
        private readonly Mock<ILogger<StationsController>> loggerMock;
        private readonly StationsController controller;

        public StationsControllerTests()
        {
            scanningManagerMock = new Mock<IStationsScanningManager>();
            loggerMock = new Mock<ILogger<StationsController>>();
            controller = new StationsController(scanningManagerMock.Object, loggerMock.Object);
        }

        [Fact]
        public async Task FullScan_ShouldReturnOk_WhenScanStarts()
        {
            var result = await controller.FullScan();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("started", okResult.Value);
            scanningManagerMock.Verify(x => x.StartFullScan(false), Times.Once);
        }

        [Fact]
        public async Task FullScan_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            // Arrange
            var errorMessage = "Scan failed";
            scanningManagerMock.Setup(x => x.StartFullScan(false))
                .Throws(new Exception(errorMessage));

            // Act
            var result = await controller.FullScan();

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(errorMessage, badRequestResult.Value);
        }

        [Fact]
        public async Task Stats_ShouldReturnSerializedStats()
        {
            var expectedStats = new QueryResult("250001", NetworkStandard.Gsm, 0, 0, 0, "задачи нет", true);
            scanningManagerMock.Setup(x => x.GetCurrentProcess)
                .Returns(expectedStats);

            var result = await controller.Stats();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = Assert.IsType<string>(okResult.Value);
            Assert.Contains("CountAdded", json);
        }

        [Fact]
        public async Task StopProccess_ShouldReturnOk_WhenStopped()
        {
            var result = await controller.StopProccess();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("stopped", okResult.Value);
            scanningManagerMock.Verify(x => x.StopCurrentProccess(), Times.Once);
        }

        [Fact]
        public async Task CanceledProccess_ShouldReturnCanceledStatus()
        {
            scanningManagerMock.Setup(x => x.CanceledProccess())
                .ReturnsAsync(1);

            var result = await controller.CanceledProccess();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(1, okResult.Value);
        }
    }
}
