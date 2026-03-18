# PoLinks Azure Compliance Baseline

This folder defines the deployment baseline used to satisfy the app rules.

## Naming
- Solution prefix: `PoLinks`
- Shared prefix: `PoShared`
- Resource groups must follow:
  - `rg-polinks-*` for app-specific resources
  - `rg-poshared-*` for shared resources

## Secrets and Key Vault
- All secrets must be sourced from `kv-poshared`.
- Do not commit secrets to any appsettings JSON file.
- Secret naming policy:
  - App-specific secrets: prefix with app name (`polinks-*`)
  - Shared secrets: no app prefix

## Managed Identity
- Web app resources are configured with system-assigned managed identity.
- Use managed identity + RBAC for data plane access.

## Cost controls
- App Service plan SKU is restricted to low-cost options (`F1` or `B1`).

## Table Storage scoping
- Table Storage for PoLinks must be provisioned in the app resource group.
- Do not use shared resource group storage accounts for app table data.

## Subscription
- Compliance target subscription: `Punkouter26` (`Bbb8dfbe-9169-432f-9b7a-fbf861b51037`).
