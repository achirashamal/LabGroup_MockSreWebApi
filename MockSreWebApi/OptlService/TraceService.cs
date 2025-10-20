using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace MockSreWebApi.Services
{
    public class TraceService : IDisposable
    {
        private readonly string _serviceName;
        private readonly string _environmentName;
        private readonly string _otlpEndpoint;
        private readonly List<Activity> _batch;
        private readonly object _lock = new object();
        private readonly Timer _batchTimer;
        private readonly int _batchSize = 10;
        private readonly TimeSpan _batchInterval = TimeSpan.FromSeconds(5);

        public TraceService(string serviceName, string environmentName, string otlpEndpoint)
        {
            _serviceName = serviceName;
            _environmentName = environmentName;
            _otlpEndpoint = otlpEndpoint;
            _batch = new List<Activity>();

            // Timer to send batch periodically
            _batchTimer = new Timer(FlushBatch, null, _batchInterval, _batchInterval);
        }

        public void AddToBatch(Activity activity)
        {
            lock (_lock)
            {
                _batch.Add(activity);

                // Send if batch size reached
                if (_batch.Count >= _batchSize)
                {
                    FlushBatch(null);
                }
            }
        }

        private void FlushBatch(object state)
        {
            List<Activity> batchToSend;

            lock (_lock)
            {
                if (_batch.Count == 0) return;

                batchToSend = new List<Activity>(_batch);
                _batch.Clear();
            }

            if (batchToSend.Count > 0)
            {
                SendBatch(batchToSend);
            }
        }

        private void SendBatch(List<Activity> activities)
        {
            try
            {
                var batchPayload = CreateBatchOtlpPayload(activities);
                using (var client = new HttpClient())
                {
                    var content = new StringContent(batchPayload, Encoding.UTF8, "application/json");
                    client.PostAsync($"{_otlpEndpoint}/v1/traces", content).GetAwaiter().GetResult();

                    Trace.TraceInformation($"Trace batch sent: {activities.Count} spans");
                }
            }
            catch (Exception ex)
            {
                // Re-add activities to batch on failure
                lock (_lock)
                {
                    _batch.AddRange(activities);
                }
                Trace.TraceWarning($" ! Trace batch failed, re-adding {activities.Count} spans: {ex.Message}");
            }
        }


        // creating opentelemetry input structure
        private string CreateBatchOtlpPayload(List<Activity> activities)
        {
            var spansJson = new List<string>();

            foreach (var activity in activities)
            {
                spansJson.Add($@"
            {{
              ""traceId"": ""{activity.TraceId.ToHexString().Replace("-", "")}"",
              ""spanId"": ""{activity.SpanId.ToHexString().Replace("-", "")}"",
              ""name"": ""{activity.OperationName.Replace("\"", "\\\"")}"",
              ""kind"": 2,
              ""startTimeUnixNano"": ""{GetUnixNano(activity.StartTimeUtc)}"",
              ""endTimeUnixNano"": ""{GetUnixNano(activity.StartTimeUtc + activity.Duration)}"",
              ""attributes"": [
                {{ ""key"": ""service.name"", ""value"": {{ ""stringValue"": ""{_serviceName}"" }} }},
                {{ ""key"": ""http.method"", ""value"": {{ ""stringValue"": ""{GetTag(activity, "http.method")}"" }} }},
                {{ ""key"": ""http.status_code"", ""value"": {{ ""intValue"": ""{GetTag(activity, "http.status_code")}"" }} }},
                {{ ""key"": ""request.id"", ""value"": {{ ""stringValue"": ""{GetTag(activity, "request.id")}"" }} }},
                {{ ""key"": ""deployment.environment"", ""value"": {{ ""stringValue"": ""{_environmentName}"" }} }},
                {{ ""key"": ""http.url"", ""value"": {{ ""stringValue"": ""{GetTag(activity, "http.url").Replace("\"", "\\\"")}"" }} }}
              ]
            }}");
            }

            return $@"{{
  ""resourceSpans"": [
    {{
      ""resource"": {{
        ""attributes"": [
          {{ ""key"": ""service.name"", ""value"": {{ ""stringValue"": ""{_serviceName}"" }} }},
          {{ ""key"": ""deployment.environment"", ""value"": {{ ""stringValue"": ""{_environmentName}"" }} }}
        ]
      }},
      ""scopeSpans"": [
        {{
          ""spans"": [
            {string.Join(",", spansJson)}
          ]
        }}
      ]
    }}
  ]
}}";
        }

        private string GetTag(Activity activity, string key)
        {
            return activity.GetTagItem(key)?.ToString() ?? "";
        }

        private long GetUnixNano(DateTime dateTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long)(dateTime - epoch).TotalMilliseconds * 1000000;
        }

        public void Dispose()
        {
            _batchTimer?.Dispose();
            // Flush any remaining activities
            FlushBatch(null);
        }
    }
}