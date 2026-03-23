# Standalone ATE Client Guide (No WPF UI Required)

This document explains how to use `Ate.Engine` as a standalone HTTP server with any external client (CLI, service, web app, desktop app, test runner).

## Can the server run without `Ate.Ui`?

Yes.

`Ate.Engine` is an independent OWIN/Web API host and does not require `Ate.Ui`.

At startup the engine:

1. loads modules and configured wrappers,
2. builds capability metadata for registered wrappers,
3. starts queue worker,
4. serves API endpoints.

## Minimum projects needed for headless integration

- `Ate.Engine` (server runtime)
- `Ate.Contracts` (request/response DTOs and capability model)

`Ate.Ui` is optional.

## Who chooses the device?

Both, in different phases:

- Engine preloads available devices from `engine-config.json`.
- Client chooses the target per command by sending `deviceType` + `deviceName`.
- Engine resolves exact match from `DriverRegistry` key `deviceType::deviceName`.

## API workflow for another client

### 1) Discover capabilities

`GET /api/capabilities`

Use this first so your client can render selectable devices, operations, and parameter inputs.

### 2) Submit command

`POST /api/command`

Body shape (`DeviceCommandRequest`):

```json
{
  "deviceType": "PSU",
  "deviceName": "PSU2",
  "operation": "SetVoltage",
  "parameters": {
    "voltage": 12.0,
    "currentLimit": 2.0,
    "channel": 1
  },
  "clientRequestId": "my-op-0001"
}
```

Validation notes:

- `deviceType`, `deviceName`, and `operation` are required.
- Operation/parameter names must match capability metadata.
- Missing parameter values are rejected by runtime invocation validation.

### 3) Poll status

`GET /api/status`

Track queue length, state, current command, last error, and loaded driver keys.

### 4) Optional queue controls

- `POST /api/engine/pause`
- `POST /api/engine/resume`
- `POST /api/engine/clear`
- `POST /api/engine/abort-current`

## Contract notes for custom clients

1. Match operation + parameter names exactly as returned by `/api/capabilities`.
2. Send all operation parameters expected by the selected operation.
3. For known built-in families (`DMM`, `PSU`), wrapper signatures are checked against `KnownCapabilitiesCatalog` during startup.
4. The engine normalizes typical JSON values (`int`, `decimal`, `bool`, arrays/objects) before invocation.

## Minimal curl example

```bash
# 1) capabilities
curl http://localhost:9000/api/capabilities

# 2) command
curl -X POST http://localhost:9000/api/command \
  -H "Content-Type: application/json" \
  -d '{
    "deviceType":"DMM",
    "deviceName":"DMM",
    "operation":"MeasureVoltage",
    "parameters":{"range":10.0,"channel":1},
    "clientRequestId":"demo-1"
  }'

# 3) status
curl http://localhost:9000/api/status
```

## Recommendation for building another UI/client

- Treat `/api/capabilities` as runtime source of truth.
- Reference `Ate.Contracts` in your client for compile-time DTO safety.
- If you maintain known families in-engine, keep `KnownCapabilitiesCatalog` synchronized with wrapper signatures.
