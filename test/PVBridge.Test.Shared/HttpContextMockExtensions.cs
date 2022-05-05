using Moq;
using Moq.Language.Flow;
using Moq.Protected;

namespace PVBridge.Test.Shared
{
    public static class HttpMessageHandlerMockExtensions
    {
        public static IReturnsResult<THandler> MockResponse<THandler>(this Mock<THandler> httpHandlerMock, Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> func)
            where THandler : HttpMessageHandler
        {
            return httpHandlerMock.Protected()
                                  .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                                  .ReturnsAsync(func);
        }
    }
}
