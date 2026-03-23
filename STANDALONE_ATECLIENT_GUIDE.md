# Standalone ATE Client Guide (No WPF Required)

This document explains using `Ate.Engine` from any HTTP-capable client (CLI, web app, service, test harness).

## Can engine run without `Ate.Ui`?

Yes. `Ate.Engine` is independent and can run headless.

At startup, engine:

1. loads modules and configured wrappers,
2. builds capability metadata,
3. starts queue worker,
4. serves API endpoints.

`Ate.Ui` is optional.

---

## Minimum projects needed

- `Ate.Engine` (server runtime)
- `Ate.Contracts` (DTO/contracts, optional but recommended for typed clients)

---

## Device selection model

- Engine preloads configured devices from `engine-config.json`.
- Client chooses target device for each command by sending `deviceType` + `deviceName`.
- Resolution is exact key matching (`deviceType::deviceName`).

---

## API workflow

### 1) Discover capabilities

`GET /api/capabilities`

Use this to populate device/operation/parameter UI dynamically.

### 2) Submit command

`POST /api/command`

Body shape (`DeviceCommandRequest`):

```json
{
  "deviceType": "PSU",
  "deviceName": "PSU",
  "operation": "SetVoltage",
  "parameters": {
    "voltage": 5.0,
    "currentLimit": 1.0,
    "channel": 1
  },
  "clientRequestId": "my-op-0001"
}
```

For another configured instance:

```json
{
  "deviceType": "PSU",
  "deviceName": "PSU2",
  "operation": "SetVoltage",
  "parameters": {
    "voltage": 12.0,
    "currentLimit": 2.0,
    "channel": 1
  }
}
```

### 3) Poll status

`GET /api/status`

Returns queue depth, state, current command, last error, and loaded drivers.

### 4) Optional controls

- `POST /api/engine/pause`
- `POST /api/engine/resume`
- `POST /api/engine/clear`
- `POST /api/engine/abort-current`

---

## Contract notes for custom clients

1. Operation + parameter names must match capabilities exactly.
2. Missing required parameters are rejected during validation.
3. Known families (`DMM`, `PSU`) are contract-validated at startup against `KnownCapabilitiesCatalog`.
4. Server normalizes common JSON value representations (including numeric token coercions).

---

## Minimal curl sequence

```bash
curl http://localhost:9000/api/capabilities
curl -X POST http://localhost:9000/api/command -H "Content-Type: application/json" -d '{"deviceType":"DMM","deviceName":"DMM","operation":"MeasureVoltage","parameters":{"range":10.0,"channel":1},"clientRequestId":"demo-1"}'
curl http://localhost:9000/api/status
```
