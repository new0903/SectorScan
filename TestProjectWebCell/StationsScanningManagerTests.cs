using Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WebAppCellMapper.Data.Models;
using WebAppCellMapper.Data.Repositories;
using WebAppCellMapper.DTO;
using WebAppCellMapper.Services;

namespace TestProjectWebCell
{
    public class StationsScanningManagerTests
    {
        private readonly Mock<IServiceProvider> serviceProviderMock;
        private readonly Mock<ILogger<StationsScanningManager>> loggerMock;
        private readonly Mock<IServiceScope> serviceScopeMock;
        private readonly Mock<IRuntimeRepository> runtimeRepoMock;
        private readonly Mock<IStationsService> stationsServiceMock;
        private readonly Mock<IProgressRepository> progressRepoMock;
        private readonly StationsScanningManager manager;

        public StationsScanningManagerTests()
        {
            serviceProviderMock = new Mock<IServiceProvider>();
            loggerMock = new Mock<ILogger<StationsScanningManager>>();
            runtimeRepoMock = new Mock<IRuntimeRepository>();
            stationsServiceMock = new Mock<IStationsService>();
            progressRepoMock = new Mock<IProgressRepository>();

            serviceProviderMock.Setup(x => x.GetService(typeof(IRuntimeRepository)))
                .Returns(runtimeRepoMock.Object);
            serviceProviderMock.Setup(x => x.GetService(typeof(IStationsService)))
                .Returns(stationsServiceMock.Object);
            serviceProviderMock.Setup(x => x.GetService(typeof(IProgressRepository)))
                .Returns(progressRepoMock.Object);

            var serviceScopeMock = new Mock<IServiceScope>();
            serviceScopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);

            serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
                .Returns(Mock.Of<IServiceScopeFactory>(f =>
                    f.CreateScope() == serviceScopeMock.Object));

            manager = new StationsScanningManager(serviceProviderMock.Object, loggerMock.Object);
        }



        [Fact]
        public async Task StopCurrentProccess_ShouldCancelRunningTask()
        {
            // Arrange
            var items = new List<QueryResult>
            {
                new QueryResult("2500001", NetworkStandard.Gsm, 10, 5, 100, "over", false)
            };

            stationsServiceMock.Setup(x => x.SyncStationsAllAsync(It.IsAny<CancellationToken>()))
                .Returns(items.ToAsyncEnumerable());

            runtimeRepoMock.Setup(x => x.IsRunning()).ReturnsAsync(true);

            manager.StartFullScan(true);
            await Task.Delay(100);

            await manager.StopCurrentProccess();

            Assert.False(manager.IsWorking);
        }

        [Fact]
        public async Task CanceledProccess_ShouldReturnFailedProgressCount()
        {
            int expectedFailedCount = 42;
            progressRepoMock.Setup(x => x.FailedProgress(default)).ReturnsAsync(expectedFailedCount);
            runtimeRepoMock.Setup(x => x.CancelRuntime(default)).Returns(Task.CompletedTask);

            var result = await manager.CanceledProccess();

            Assert.Equal(expectedFailedCount, result);
            runtimeRepoMock.Verify(x => x.CancelRuntime(default), Times.Once);
        }

        [Fact]
        public async Task FullScan_ShouldUpdateResult_WhenItemsReceived()
        {
            var expectedResult = new QueryResult(string.Empty, NetworkStandard.Gsm, 10, 5, 100, "progress", false);
            var items = new List<QueryResult> { expectedResult };
            stationsServiceMock.Setup(x => x.SyncStationsAllAsync(It.IsAny<CancellationToken>()))
                .Returns(items.ToAsyncEnumerable());
            runtimeRepoMock.Setup(x => x.IsRunning()).ReturnsAsync(true);

            manager.StartFullScan(true);

            Assert.Equal(expectedResult.OperatorCode, manager.GetCurrentProcess.OperatorCode);
        }

        [Fact]
        public async Task StartFullScan_ShouldHandleCancellation()
        {
            var cts = new CancellationTokenSource();
            stationsServiceMock.Setup(x => x.SyncStationsAllAsync(It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());
            runtimeRepoMock.Setup(x => x.IsRunning()).ReturnsAsync(true);

            manager.StartFullScan(true);
            await Task.Delay(500);

            Assert.False(manager.IsWorking);
        }

      
    }
}
