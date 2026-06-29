using System.Net;
using System.Net.Http.Json;

namespace WebDevSecOps.UnitTests.Common
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly object? _responseContent;
        private readonly HttpStatusCode _statusCode;

        public HttpRequestMessage? LastRequest { get; private set; }

        public string? LastRequestBody { get; private set; }

        public MockHttpMessageHandler(object? responseContent, HttpStatusCode statusCode)
        {
            _responseContent = responseContent;
            _statusCode = statusCode;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;

            if (request.Content is not null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            var response = new HttpResponseMessage(_statusCode);

            if (_responseContent is not null)
            {
                response.Content = JsonContent.Create(_responseContent);
            }

            return response;
        }
    }
}
