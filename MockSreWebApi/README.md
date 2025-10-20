# MockSreWebApi (.NET Framework 4.6.1)

This is a mock ASP.NET Web API application designed for **SRE instrumentation exercises**.

## 🚀 Setup & Run

1. Open `MockSreWebApi.sln` in **Visual Studio 2022**
2. Set `MockSreWebApi` as the startup project
3. Run using **IIS Express**
4. Navigate to [http://localhost:port/api/health](http://localhost:port/api/health)

## 📡 API Endpoints

| Endpoint | Description |
|-----------|-------------|
| `/api/health` | Returns `200 OK` with `"Healthy"` |
| `/api/data` | Simulates fake DB call (100–500ms delay) |
| `/api/external` | Simulates external API (1–2s delay, 20% 5xx chance) |
| `/api/random-error` | Randomly throws 500 (30% chance) |
| `/api/work` | Simulates computation work |

## 🔁 Background Worker

- Runs every 10 seconds
- Prints execution logs
- Randomly fails (10% chance)

No telemetry is pre-configured — you must add it locally.

---
