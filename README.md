
## Answers:

1. **What specific changes did you make to export telemetry to an OpenTelemetry Collector?**  
- Added TraceService for batched span export traces
- Added MetricsService for metrics export metrics
- Enabled W3C Trace Context with Activity.DefaultIdFormat = W3C
- Set required attributes: service.name, http.method, http.status_code, request.id, deployment.environment

2. **How did you configure the OTLP exporter (protocol, endpoint, batching)?**  
- Protocol: HTTP with protobuf over HTTP (OTLP standard)
- Endpoint: http://localhost:4317/v1/traces and /v1/metrics ( read from Web.config)
- Batching: 10 spans or 5 seconds for traces, 30-second intervals for metrics


3. **Why use OpenTelemetry Collector in production rather than direct exporters?**  
- Centralized configuration and processing
- Multiple backend support (Jaeger, Prometheus, etc.)
- Load balancing and reliability

4. **What challenges did you face instrumenting the .NET framework application?**  
- OpenTelemetry SDK incompatibility with .NET 4.6.1
- Async limitations in ASP.NET context
- Dependency version conflicts


5. **How would you verify that telemetry successfully reaches the collector?**  
- Check collector console for Traces and Metrics logs
- Verify all required attributes appear in span data
- Monitor batches (check for multiple spans)

6. **If you needed to correlate logs and traces, what identifiers would you use?**
- TraceId: Activity.Current.TraceId - identifies entire request flow
- SpanId: Activity.Current.SpanId - identifies specific operation
- RequestId: Custom UUID for request correlation





**NOTE** - I was unable to run the provided initial code, likely because the folder paths changed when exporting the project to Slack. After spending some time trying to fix it, I decided to create a new empty .NET 4.6.1 Web API project and then added the provided code to it. Then completed the assignment.
