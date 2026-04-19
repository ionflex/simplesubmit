# Azure naming conventions

Aligned with the Mobitech proposed standard.

## Pattern

```
system[-customer]-environment-region[-subsystem]-component-resourcetype
```

| Part | Required | Example | Notes |
|---|---|---|---|
| `system` | yes | `iptyphet` | Overall business area, product, or owner prefix. |
| `customer` | no | `fj1` | Skip when the resource isn't customer-specific. |
| `environment` | yes | `p` | `d` dev, `t` test, `s` staging, `p` prod. |
| `region` | yes | `we` | Two-letter: `we` West Europe, `ne` North Europe, `cc` Canada Central, `ce` Canada East. |
| `subsystem` | no | `apc` | Use when a system is split into subsystems that each contain several resources. |
| `component` | yes | `simsub` | Short, lowercase, no separators. |
| `resourcetype` | yes | `stapp` | See table below. |

## Resource-type abbreviations (subset used in this project)

| Resource | Abbr |
|---|---|
| Resource group | `rg` |
| Static Web App | `stapp` |
| Function App | `func` |
| App Service / Web App | `app` |
| App Service Plan | `asp` |
| Storage account | `st` |
| Application Insights | `appi` |
| Log Analytics workspace | `log` |
| Key Vault | `kv` |
| Service Bus namespace | `sbns` |
| Service Bus topic | `sbt` |
| Azure SQL Database Server | `sql` |
| Azure SQL Database | `sqldb` |

Full list: https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-abbreviations

## Storage account exception

Storage-account names must be 3–24 lowercase alphanumeric characters with no separators. Drop the hyphens and keep the `component` part short:

- `orionpwecommonst` — general storage in `orion-p-we-common-rg`
- `orcafj1pwest` — general storage in `orca-fj1-p-we-rg`
- `orionnlepweingestionst` — storage for `orion-nle-p-we-ingestion-func`

## Names used by this project

System = `iptyphet`, component = `simsub`, env = `p`, region = `we`. No customer or subsystem.

| Resource | Name |
|---|---|
| Resource group | `iptyphet-p-we-simsub-rg` |
| Static Web App | `iptyphet-p-we-simsub-stapp` |
| Application Insights | `iptyphet-p-we-simsub-appi` |
| Log Analytics workspace | `iptyphet-p-we-simsub-log` |
| Storage account | `iptyphetpwesimsubst` |
