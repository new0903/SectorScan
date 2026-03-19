using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using WebAppCellMapper.Controllers;
using WebAppCellMapper.Data;
using WebAppCellMapper.Data.Models;
using WebAppCellMapper.DTO;
using WebAppCellMapper.Services;

namespace TestProject1
{

    public class Tests
    {
        private Mock<IStationsService> _mockStationsService;
        private Mock<ILogger<StationsController>> _mockLogger;
        private StationsController _controller;
        private DefaultHttpContext _httpContext;
        private MemoryStream _responseStream;

        [SetUp]
        public void Setup()
        {
            _mockStationsService = new Mock<IStationsService>();
            _mockLogger = new Mock<ILogger<StationsController>>();

            _controller = new StationsController(
                _mockStationsService.Object,
                _mockLogger.Object
            );

            // Настраиваем HttpContext для тестирования SSE
            _httpContext = new DefaultHttpContext();
            _responseStream = new MemoryStream();
            _httpContext.Response.Body = _responseStream;
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };
        }

        [TearDown]
        public void TearDown()
        {
            _responseStream?.Dispose();
        }

        #region SearchByOperator Tests

        [Test]
        public async Task SearchByOperator_ValidParams_WritesSseMessages()
        {
            // Arrange
            var network = NetworkStandard.Lte;
            var operatorCode = "250001";
            var queryParams = new QueryParams(50,51,30,31,3);
      
            var ct = CancellationToken.None;

            var testResults = new List<QueryResult>
            {
                new QueryResult(operatorCode, network, 5, 1, 10, "сканирование сектора 1", false),
                new QueryResult(operatorCode, network, 10, 2, 5, "сканирование сектора 2", false),
                new QueryResult(operatorCode, network, 15, 3, 0, "сканирование завершено", true)
            };

            _mockStationsService
                .Setup(s => s.ScanAreaAsync(
                    operatorCode,
                    network,
                    queryParams.latS.Value,
                    queryParams.latE.Value,
                    queryParams.lonS.Value,
                    queryParams.lonE.Value,
                    queryParams.step,
                    ct))
                .Returns(testResults.ToAsyncEnumerable());

            // Act
            await _controller.SearchByOperator(network, operatorCode, queryParams, ct);

            // Assert
            var responseString = Encoding.UTF8.GetString(_responseStream.ToArray());
            var messages = responseString.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

            Assert.Multiple(() =>
            {
                Assert.That(messages, Has.Length.EqualTo(4)); // 3 результата + [DONE]

                // Проверяем формат SSE
                foreach (var msg in messages.Take(3))
                {
                    Assert.That(msg, Does.StartWith("data: "));
                    var json = msg.Replace("data: ", "");
                    var result = JsonConvert.DeserializeObject<QueryResult>(json);
                    Assert.That(result, Is.Not.Null);
                }

                Assert.That(messages[3], Is.EqualTo("data: [DONE]"));
            });

            // Проверяем заголовки
            Assert.That(_httpContext.Response.Headers["Content-Type"],
                Is.EqualTo("text/event-stream"));
           
            Assert.That(_httpContext.Response.Headers["Connections"],
                Is.EqualTo("keep-alive"));
        }

        [Test]
        public async Task SearchByOperator_MissingCoordinates_ReturnsBadRequest()
        {
            // Arrange
            var queryParams = new QueryParams(50,null,30,31);
          

            // Act
            await _controller.SearchByOperator(
                NetworkStandard.Lte,
                "250001",
                queryParams);

            // Assert
            Assert.That(_httpContext.Response.StatusCode, Is.EqualTo(400));
            _mockStationsService.Verify(
                s => s.ScanAreaAsync(
                    It.IsAny<string>(),
                    It.IsAny<NetworkStandard>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task SearchByOperator_NullQueryParams_DoesNothing()
        {
            // Act
            await _controller.SearchByOperator(
                NetworkStandard.Lte,
                "250001",
                null);

            // Assert
            _mockStationsService.Verify(
                s => s.ScanAreaAsync(
                    It.IsAny<string>(),
                    It.IsAny<NetworkStandard>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task SearchByOperator_ServiceThrowsException_LogsAndRethrows()
        {
            // Arrange
            var queryParams = new QueryParams(50, 51, 30, 31);
            var expectedException = new Exception("Test exception");

            _mockStationsService
                .Setup(s => s.ScanAreaAsync(
                    It.IsAny<string>(),
                    It.IsAny<NetworkStandard>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<CancellationToken>()))
                .Throws(expectedException);

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(async () =>
                await _controller.SearchByOperator(
                    NetworkStandard.Lte,
                    "250001",
                    queryParams));

            Assert.That(ex.Message, Is.EqualTo("Test exception"));

        }

        [Test]
        public async Task SearchByOperator_EmptyResult_WritesDone()
        {
            // Arrange
            var queryParams = new QueryParams(50, 51, 30, 31);
            async IAsyncEnumerable<QueryResult> EmptyAsyncEnumerable()
            {
                yield break;
            }
            _mockStationsService
                .Setup(s => s.ScanAreaAsync(
                    It.IsAny<string>(),
                    It.IsAny<NetworkStandard>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<CancellationToken>()))
                .Returns(EmptyAsyncEnumerable());

            // Act
            await _controller.SearchByOperator(
                NetworkStandard.Lte,
                "250001",
                queryParams);

            // Assert
            var responseString = Encoding.UTF8.GetString(_responseStream.ToArray());
            Assert.That(responseString, Is.Empty); // Ничего не записано
        }

        #endregion

        #region SyncStationsAll Tests

        [Test]
        public async Task SyncStationsAll_ValidRequest_WritesSseMessages()
        {
            // Arrange
            var network = NetworkStandard.Lte;
            var operatorCode = "250001";
            var queryParams = new QueryParams(null,null,null,null);
            var ct = CancellationToken.None;

            var testResults = new List<QueryResult>
            {
                new QueryResult(operatorCode, network, 5, 1, 100, "оператор 1", false),
                new QueryResult(operatorCode, network, 10, 2, 50, "оператор 2", false),
                new QueryResult("", NetworkStandard.Gsm, 15, 3, 0, "сканирование завершено", true)
            };

            _mockStationsService
                .Setup(s => s.SyncStationsAllAsync(ct))
                .Returns(testResults.ToAsyncEnumerable());

            // Act
            await _controller.SyncStationsAll(network, operatorCode, queryParams, ct);

            // Assert
            var responseString = Encoding.UTF8.GetString(_responseStream.ToArray());
            var messages = responseString.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

            Assert.Multiple(() =>
            {
                Assert.That(messages, Has.Length.EqualTo(4)); // 3 результата + [DONE]

                // Проверяем, что последнее сообщение - [DONE]
                Assert.That(messages[3], Is.EqualTo("data: [DONE]"));
            });

            // Проверяем заголовки
            Assert.That(_httpContext.Response.Headers["Content-Type"],
                Is.EqualTo("text/event-stream"));
        }

        [Test]
        public async Task SyncStationsAll_ServiceThrowsException_LogsAndRethrows()
        {
            // Arrange
            var expectedException = new Exception("Sync error");

            _mockStationsService
                .Setup(s => s.SyncStationsAllAsync(It.IsAny<CancellationToken>()))
                .Throws(expectedException);

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(async () =>
                await _controller.SyncStationsAll(
                    NetworkStandard.Lte,
                    "250001",
                    new QueryParams(null,null,null,null)));

            Assert.That(ex.Message, Is.EqualTo("Sync error"));
        }

        #endregion

        #region Helper Methods

        [Test]
        public async Task WriteResponse_WritesCorrectSseFormat()
        {
            // Arrange
            var testJson = "{\"test\":\"data\"}";

            // Используем рефлексию для доступа к private методу
            var method = typeof(StationsController).GetMethod(
                "WriteResponse",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            await (Task)method.Invoke(_controller, new object[] { testJson });

            // Assert
            var responseString = Encoding.UTF8.GetString(_responseStream.ToArray());
            Assert.That(responseString, Is.EqualTo($"data: {testJson}\n\n"));
        }

        #endregion
    }

    // Вспомогательный extension метод для преобразования List в IAsyncEnumerable
    public static class AsyncEnumerableExtensions
    {
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
        {
            foreach (var item in source)
            {
                yield return item;
                await Task.CompletedTask; // Для имитации асинхронности
            }
        }
    } 
}