# SanesApp - Azure Staging Deployment

Este documento define el flujo de despliegue de SanesApp hacia staging en Azure.

El deployment está integrado en:

```text
.github/workflows/ci.yml
```

## Activation

El job de staging permanece deshabilitado mientras la variable:

```text
STAGING_DEPLOYMENT_ENABLED
```

no tenga exactamente el valor `true`.

Esto permite versionar y validar el pipeline antes de crear los recursos reales de Azure.

## Deployment flow

```text
push / merge -> develop
        |
        v
Build and Integration Tests
        |
        | success
        v
Deploy to Azure Staging
        |
        +--> Publish Sanes.Api
        +--> Publish Sanes.Web
        +--> Build EF migration bundle (linux-x64)
        +--> Azure login using OIDC
        +--> Apply database migrations once
        +--> Deploy Sanes.Api
        +--> Check /health/ready
        +--> Deploy Sanes.Web
```

El deployment no se ejecuta para feature branches, pull requests, fallos de tests o mientras STAGING_DEPLOYMENT_ENABLED no sea true.

## GitHub Repository Variable

La bandera de activación debe configurarse como Repository Variable porque GitHub evalúa el `if` del job antes de cargar las variables del environment.

Variable requerida:

- `STAGING_DEPLOYMENT_ENABLED`

Debe permanecer ausente o con valor distinto de `true` hasta que staging esté listo.

## GitHub Environment

Debe existir un GitHub Environment llamado `staging`.

Variables requeridas dentro del environment `staging`:

- `AZURE_API_APP_NAME`
- `AZURE_WEB_APP_NAME`

Secrets requeridos:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `STAGING_DATABASE_CONNECTION_STRING`

No se utiliza Azure client secret. La autenticación del pipeline utiliza OIDC.

## Azure OIDC

El job utiliza `azure/login@v2` y permiso `id-token: write`.

La identidad de Azure deberá tener una Federated Identity Credential asociada al repositorio y al environment `staging`.

## Runtime configuration

La configuración runtime de Sanes.Api y Sanes.Web está documentada en:

```text
infrastructure/azure-staging-configuration.md
```

Los secrets del pipeline no sustituyen los Application Settings requeridos por ambos App Services.

## EF Core migrations

Las migraciones no se ejecutan durante el startup de Sanes.Api.

El pipeline genera un migration bundle `linux-x64` y `self-contained` usando la versión de dotnet-ef fijada en `dotnet-tools.json`.

El bundle se ejecuta una sola vez antes de desplegar la nueva versión de Sanes.Api.

Esto evita que varias instancias de App Service intenten ejecutar migraciones simultáneamente.

## Database network requirement

Inicialmente el migration bundle se ejecuta desde un GitHub-hosted runner.

Ese runner debe poder alcanzar Azure Database for PostgreSQL.

Si PostgreSQL utiliza acceso privado mediante VNet, deberá usarse un mecanismo que ejecute las migraciones dentro de esa red, por ejemplo un self-hosted runner.

No se debe abrir PostgreSQL indiscriminadamente a Internet solo para permitir las migraciones.

## API readiness

Después de desplegar Sanes.Api, el pipeline consulta:

```text
https://<AZURE_API_APP_NAME>.azurewebsites.net/health/ready
```

Sanes.Web solamente se despliega cuando Sanes.Api reporta readiness correctamente.

## Deployment concurrency

El deployment utiliza:

```text
group: sanesapp-staging
cancel-in-progress: false
```

Esto evita deployments simultáneos de staging y evita cancelar una ejecución que pueda estar aplicando migraciones.

## Activation checklist

Antes de activar STAGING_DEPLOYMENT_ENABLED=true deben estar listos:

1. App Service de Sanes.Api.
2. App Service de Sanes.Web.
3. Azure Database for PostgreSQL.
4. Azure Blob Storage.
5. Managed Identity y permisos RBAC de Blob para Sanes.Api.
6. Application Settings de Sanes.Api.
7. Application Settings de Sanes.Web.
8. GitHub Environment staging.
9. OIDC / Federated Identity Credential.
10. Repository Variable `STAGING_DEPLOYMENT_ENABLED` configurada cuando corresponda.
11. Variables y secrets del environment.
12. Conectividad del runner hacia PostgreSQL.
13. Endpoint /health/ready operativo.

## Current policy

Actualmente el deployment debe permanecer deshabilitado.

Este feature prepara el mecanismo de CD, pero no crea recursos Azure ni realiza deployments reales.
