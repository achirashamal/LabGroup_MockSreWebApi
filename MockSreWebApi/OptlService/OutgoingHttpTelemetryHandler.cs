using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MockSreWebApi.Handlers
{
    public class OutgoingHttpTelemetryHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var activity = new Activity($"HTTP OUT {request.Method.Method}");

            // Set OpenTelemetry attributes for outgoing call
            activity.SetTag("http.method", request.Method.Method);
            activity.SetTag("http.url", request.RequestUri.ToString());
            activity.SetTag("service.name", "MockSreWebApi");
            activity.SetTag("span.kind", "client");

            activity.Start();

            try
            {
                var response = await base.SendAsync(request, cancellationToken);
                activity.SetTag("http.status_code", ((int)response.StatusCode).ToString());
                return response;
            }
            catch (Exception ex)
            {
                activity.SetTag("error", "true");
                activity.SetTag("exception.message", ex.Message);
                throw;
            }
            finally
            {
                activity.Stop();
            }
        }
    }
}