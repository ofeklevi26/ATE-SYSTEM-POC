# Standalone ATE Client Guide (No WPF UI Required)

This document explains how to use `Ate.Engine` as a standalone HTTP server with any external client (CLI, web UI, desktop UI, service worker, test runner), as long as the client can send HTTP requests.

## Can the server run without `Ate.Ui`?

Yes.

`Ate.Engine` is an independent OWIN/Web API host and does not require `Ate.Ui` to start or process commands. The UI is only one optional consumer of the HTTP API.

At startup, the engine:
1. loads modules and configured wrappers,
2. builds capabilities metadata,
3. starts queue worker,
4. serves API endpoints (`/api/capabilities`, `/api/command`, `/api/status`, etc.).

So any custom `AteClient` implementation can work as long as it uses the contracts/payload shapes correctly.

---

## Minimum projects you need

For a headless integration you typically need:
- `Ate.Engine` (server runtime)
- `Ate.Contracts` (DTO/contracts package)

`Ate.Ui` is optional.

---

## API workflow for another client

### 1) Discover capabilities

`GET /api/capabilities`

Use this first so your client can render/build operation forms dynamically.

For known built-in families (e.g. DMM/PSU), capabilities come from `KnownCapabilitiesCatalog` (contract-first).
For unknown/plugin families, capabilities come from reflection fallback on wrapper methods.

### 2) Submit command

`POST /api/command`

Body shape (`DeviceCommandRequest`):

```json
{
  "deviceType": "PSU",
  "driverId": "default",
  "operation": "SetVoltage",
  "parameters": {
    "voltage": 5.0,
    "currentLimit": 1.0,
    "channel": 1
  },
  "clientRequestId": "my-op-0001"
}
```

### 3) Poll status

`GET /api/status`

Track queue length, current command, state, and last error.

### 4) Optional engine controls

- `POST /api/engine/pause`
- `POST /api/engine/resume`
- `POST /api/engine/clear`
- `POST /api/engine/abort-current`

---

## Important contract notes for custom clients

1. **Operation + parameter names must match capability metadata exactly.**
2. **Required params**: if a wrapper parameter has no default value, server treats it as required and rejects missing values.
3. **Known-family drift protection**: for device types backed by `KnownCapabilitiesCatalog`, startup validates wrapper signatures vs catalog and fails fast on mismatch.
4. **Type conversion**: server converts common string/number representations to target CLR types (`int`, `decimal`, `bool`, etc.).

---

## Example: minimal curl-based standalone client

```bash
# 1) capabilities
curl http://localhost:9000/api/capabilities

# 2) command
curl -X POST http://localhost:9000/api/command \
  -H "Content-Type: application/json" \
  -d '{
    "deviceType":"DMM",
    "driverId":"default",
    "operation":"MeasureVoltage",
    "parameters":{"range":10.0,"channel":1},
    "clientRequestId":"demo-1"
  }'

# 3) status
curl http://localhost:9000/api/status
```

---

## Recommendation for building another UI/client

- Use `/api/capabilities` as runtime source of truth for selectable devices/operations.
- Also reference `Ate.Contracts` directly in your client for compile-time DTO safety.
- If you add a built-in family, keep `KnownCapabilitiesCatalog` synchronized with wrapper signatures.
