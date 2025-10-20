using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace MockSreWebApi.Services
{
    public class MetricsService : IDisposable
    {
        private readonly string _serviceName;
        private readonly string _environmentName;
        private readonly string _otlpEndpoint;
        private readonly Timer _metricsTimer;

        // metrics counters
        private int _requestCount = 0;
        private int _errorCount = 0;
        private readonly object _lock = new object();

        public MetricsService(string serviceName, string environmentName, string otlpEndpoint)
        {
            _serviceName = serviceName;
            _environmentName = environmentName;
            _otlpEndpoint = otlpEndpoint;

            // Export metrics every 30 seconds
            _metricsTimer = new Timer(ExportMetrics, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        public void RecordRequest(bool isError = false)
        {
            lock (_lock)
            {
                _requestCount++;
                if (isError) _errorCount++;
            }
        }

        private void ExportMetrics(object state)
        {
            try
            {
                int requestCount, errorCount;
                lock (_lock)
                {
                    requestCount = _requestCount;
                    errorCount = _errorCount;
                    // Reset counters after export
                    _requestCount = 0;
                    _errorCount = 0;
                }

                if (requestCount > 0)
                {
                    var metricsPayload = CreateMetricsPayload(requestCount, errorCount);
                    using (var client = new HttpClient())
                    {
                        var content = new StringContent(metricsPayload, Encoding.UTF8, "application/json");
                        client.PostAsync($"{_otlpEndpoint}/v1/metrics", content).GetAwaiter().GetResult();
                    }

                    Trace.TraceInformation($"Metrics exported: {requestCount} requests, {errorCount} errors");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"Metrics export failed: {ex.Message}");
            }
        }

        // creating opentelemetry input structure
        private string CreateMetricsPayload(int requestCount, int errorCount)
        {
            var timestamp = DateTime.UtcNow;
            var unixTimeNano = GetUnixNano(timestamp);

            return $@"{{
  ""resourceMetrics"": [
    {{
      ""resource"": {{
        ""attributes"": [
          {{ ""key"": ""service.name"", ""value"": {{ ""stringValue"": ""{_serviceName}"" }} }},
          {{ ""key"": ""deployment.environment"", ""value"": {{ ""stringValue"": ""{_environmentName}"" }} }}
        ]
      }},
      ""scopeMetrics"": [
        {{
          ""metrics"": [
            {{
              ""name"": ""http.server.requests"",
              ""unit"": ""count"",
              ""sum"": {{
                ""dataPoints"": [
                  {{
                    ""asInt"": ""{requestCount}"",
                    ""timeUnixNano"": ""{unixTimeNano}"",
                    ""attributes"": [
                      {{ ""key"": ""status"", ""value"": {{ ""stringValue"": ""total"" }} }}
                    ]
                  }}
                ],
                ""aggregationTemporality"": 2,
                ""isMonotonic"": true
              }}
            }},
            {{
              ""name"": ""http.server.errors"", 
              ""unit"": ""count"",
              ""sum"": {{
                ""dataPoints"": [
                  {{
                    ""asInt"": ""{errorCount}"",
                    ""timeUnixNano"": ""{unixTimeNano}""
                  }}
                ],
                ""aggregationTemporality"": 2,
                ""isMonotonic"": true
              }}
            }},
            {{
              ""name"": ""http.server.success_rate"",
              ""unit"": ""percent"",
              ""gauge"": {{
                ""dataPoints"": [
                  {{
                    ""asDouble"": ""{(requestCount > 0 ? (1.0 - (double)errorCount / requestCount) * 100 : 100.0)}"",
                    ""timeUnixNano"": ""{unixTimeNano}""
                  }}
                ]
              }}
            }}
          ]
        }}
      ]
    }}
  ]
}}";
        }

        private long GetUnixNano(DateTime dateTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long)(dateTime - epoch).TotalMilliseconds * 1000000;
        }

        public void Dispose()
        {
            _metricsTimer?.Dispose();
            // Export final metrics before disposal
            ExportMetrics(null);
        }
    }
}