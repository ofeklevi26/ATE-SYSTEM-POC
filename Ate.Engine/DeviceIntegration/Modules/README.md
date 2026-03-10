# Driver Modules

`IDriverModule` is the per-driver-family DI seam.

Each module should register:
- hardware driver factory implementation(s),
- configured wrapper provider(s),
- any family-specific services needed by providers/wrappers.

## Rules
- Keep endpoint/connection parsing in provider(s), not in host/shared helpers.
- Keep settings keys provider-owned (no global enforced endpoint schema).
- Prefer one module per family (e.g., DMM, PSU, NI-DMM, NI-PSU).
