using System;
using System.Web;
using System.Web.Http;
using System.Configuration;
using System.Diagnostics;
using MockSreWebApi.Services;

namespace MockSreWebApi
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        private static readonly string ServiceName = ConfigurationManager.AppSettings["ServiceName"] ?? "MockSreWebApi";
        private static readonly string EnvironmentName = ConfigurationManager.AppSettings["DeploymentEnvironment"] ?? "development";
        private static readonly string OtlpEndpoint = ConfigurationManager.AppSettings["OtlpEndpoint"] ?? "http://localhost:4317";

        private static TraceService _traceService;
        private static MetricsService _metricsService;

        protected void Application_Start()
        {
            // Initialize opentelemetry services
            _traceService = new TraceService(ServiceName, EnvironmentName, OtlpEndpoint);
            _metricsService = new MetricsService(ServiceName, EnvironmentName, OtlpEndpoint);

            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            GlobalConfiguration.Configure(WebApiConfig.Register);

            Trace.TraceInformation("Application started with OpenTelemetry instrumentation");
        }

        protected void Application_End()
        {
            _traceService?.Dispose();
            _metricsService?.Dispose();
        }

        protected void Application_BeginRequest()
        {
            var activity = new Activity($"HTTP {Context.Request.HttpMethod}");

            // Set ALL required opentelemetry attributes
            activity.SetTag("http.method", Context.Request.HttpMethod);
            activity.SetTag("http.url", Context.Request.Url.ToString());
            activity.SetTag("service.name", ServiceName);
            activity.SetTag("deployment.environment", EnvironmentName);
            activity.SetTag("request.id", Guid.NewGuid().ToString());

            activity.Start();
            Context.Items["CurrentActivity"] = activity;
        }

        protected void Application_EndRequest()
        {
            var activity = Context.Items["CurrentActivity"] as Activity;
            if (activity != null)
            {
                activity.SetTag("http.status_code", Context.Response.StatusCode.ToString());
                bool isError = Context.Response.StatusCode >= 400;
                if (isError) activity.SetTag("error", "true");

                activity.Stop();
                _traceService.AddToBatch(activity);
                _metricsService.RecordRequest(isError);
            }
        }

        protected void Application_Error()
        {
            var activity = Context.Items["CurrentActivity"] as Activity;
            var exception = Server.GetLastError();

            if (activity != null && exception != null)
            {
                activity.SetTag("error", "true");
                activity.SetTag("exception.message", exception.Message);
                _traceService.AddToBatch(activity);
                _metricsService.RecordRequest(true);
            }
        }
    }
}