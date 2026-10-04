# SanesApp - Azure Staging Configuration

Este documento define la configuración requerida para desplegar SanesApp
en un ambiente de staging sobre Azure App Service.

No deben almacenarse secretos reales en este archivo ni en archivos
`appsettings*.json` versionados.

La configuración del ambiente debe proporcionarse mediante Application
Settings / Environment Variables de Azure App Service.

---

## Environment

### Sanes.Api

```text
ASPNETCORE_ENVIRONMENT=Staging
```

### Sanes.Web

```text
ASPNETCORE_ENVIRONMENT=Staging
```

---

# Sanes.Api

## PostgreSQL

### Requerido

```text
ConnectionStrings__DefaultConnection
```

Debe contener la connection string de Azure Database for PostgreSQL.

La API falla durante el arranque en ambientes distintos de `Testing`
si esta configuración está ausente o vacía.

No almacenar la connection string en el repositorio.

---

## JWT

### Requeridos

```text
Jwt__Issuer
Jwt__Audience
Jwt__ExpirationMinutes
Jwt__Key
```

Reglas de startup:

- `Jwt__Key` no puede estar vacío.
- `Jwt__Issuer` no puede estar vacío.
- `Jwt__Audience` no puede estar vacío.
- `Jwt__ExpirationMinutes` debe ser mayor que cero.

`Jwt__Key` es un secreto y debe mantenerse únicamente en la
configuración segura del ambiente.

---

## Tenant Provisioning

### Opcional

```text
Provisioning__Key
```

Si no se configura una clave, el endpoint de provisioning permanece
efectivamente deshabilitado porque las solicitudes no pueden superar
la validación de `X-Provisioning-Key`.

Si se habilita, la clave debe almacenarse como secreto del ambiente.

---

## File Storage

Para staging en Azure se utilizará Azure Blob Storage.

### Requeridos para Azure Blob

```text
FileStorage__Provider=AzureBlob
FileStorage__AccountName=<storage-account-name>
FileStorage__ContainerName=<container-name>
```

No se utiliza una Storage Account Key dentro de SanesApp.

La autenticación hacia Blob Storage se realiza mediante
`DefaultAzureCredential`, por lo que el App Service deberá utilizar
Managed Identity y recibir los permisos RBAC correspondientes sobre
el Storage Account / container.

`FileStorage__RootPath` corresponde únicamente al provider `Local`
y no debe utilizarse como almacenamiento persistente de staging.

---

## Application Insights / Azure Monitor

### Recomendado

```text
APPLICATIONINSIGHTS_CONNECTION_STRING
```

Si esta variable está configurada, Sanes.Api habilita Azure Monitor /
Application Insights.

Si no está configurada, la API puede iniciar sin telemetría de Azure.

---

## Health Checks

Sanes.Api expone:

```text
/health/live
/health/ready
```

Uso esperado:

- `/health/live`: liveness del proceso.
- `/health/ready`: readiness incluyendo conectividad con PostgreSQL.

Estos endpoints pueden utilizarse para health checks de Azure App
Service y monitoreo operativo.

---

# Sanes.Web

## Sanes.Api Base URL

### Requerido

```text
Api__BaseUrl
```

Debe contener la URL absoluta HTTPS de Sanes.Api en staging.

Ejemplo de formato:

```text
https://<sanes-api-staging-host>/
```

Sanes.Web valida esta configuración durante el arranque mediante
`ValidateOnStart()`.

No utilizar la URL localhost definida en `appsettings.Development.json`
para staging.

---

# Reverse Proxy / Forwarded Headers

Sanes.Api y Sanes.Web tienen soporte para:

```text
X-Forwarded-For
X-Forwarded-Proto
```

La aplicación procesa forwarded headers antes de HTTPS redirection y,
en Sanes.Web, antes de HSTS.

No se requiere duplicar esta configuración mediante
`ASPNETCORE_FORWARDEDHEADERS_ENABLED`.

---

# Azure Resources Expected

La arquitectura inicial de staging contempla:

```text
Sanes.Web
    -> Azure App Service

Sanes.Api
    -> Azure App Service

PostgreSQL
    -> Azure Database for PostgreSQL Flexible Server

Guarantee attachments
    -> Azure Blob Storage

Observability
    -> Azure Monitor / Application Insights
```

No se requiere inicialmente:

- AKS / Kubernetes
- Redis
- Service Bus
- API Management
- custom containers

---

# Staging Configuration Checklist

## Sanes.Api App Service

```text
ASPNETCORE_ENVIRONMENT
ConnectionStrings__DefaultConnection
Jwt__Issuer
Jwt__Audience
Jwt__ExpirationMinutes
Jwt__Key
FileStorage__Provider
FileStorage__AccountName
FileStorage__ContainerName
APPLICATIONINSIGHTS_CONNECTION_STRING
```

Opcional:

```text
Provisioning__Key
```

## Sanes.Web App Service

```text
ASPNETCORE_ENVIRONMENT
Api__BaseUrl
```

---

# Secrets

Los siguientes valores deben tratarse como secretos:

```text
ConnectionStrings__DefaultConnection
Jwt__Key
Provisioning__Key
APPLICATIONINSIGHTS_CONNECTION_STRING
```

No deben:

- incluirse en commits;
- escribirse en `appsettings.json`;
- escribirse en `appsettings.Staging.json`;
- aparecer en documentación con sus valores reales;
- imprimirse en logs.

---

# Current Staging Policy

La configuración específica del ambiente será administrada desde Azure
App Service.

No se utilizará `appsettings.Staging.json` para almacenar secretos ni
endpoints reales de staging.

La aplicación debe poder desplegarse desde el mismo artefacto y recibir
su configuración exclusivamente desde el ambiente.
