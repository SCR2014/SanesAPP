using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sanes.Application.AppUsers.DTOs;
using Sanes.Domain.Enums;
using Sanes.Web.Api;

namespace Sanes.Web.AppUsers;

public sealed class AppUsersWebService
    : IAppUsersWebService
{
    private readonly ISanesApiClient _apiClient;

    public AppUsersWebService(
        ISanesApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<List<AppUserResponse>> GetAllAsync(
        AppUserRole? role = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query =
            new List<string>();

        if (role.HasValue)
        {
            query.Add(
                $"role={(int)role.Value}");
        }

        if (includeInactive)
        {
            query.Add(
                "includeInactive=true");
        }

        var url =
            "api/app-users";

        if (query.Count > 0)
        {
            url +=
                "?" +
                string.Join(
                    "&",
                    query);
        }

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                url);

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.BadRequest)
        {
            throw new ArgumentException(
                TranslateApiMessage(
                    await ReadApiErrorAsync(
                        response,
                        cancellationToken)));
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<
                List<AppUserResponse>>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api devolvió una respuesta vacía al consultar los usuarios.");
    }

    public async Task<AppUserResponse?> GetByIdAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateAppUserId(
            appUserId);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/app-users/{appUserId:D}");

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<
                AppUserResponse>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api devolvió una respuesta vacía al consultar el usuario.");
    }

    public async Task<AppUserResponse> CreateAsync(
        CreateAppUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        using var message =
            new HttpRequestMessage(
                HttpMethod.Post,
                "api/app-users")
            {
                Content =
                    JsonContent.Create(
                        request)
            };

        using var response =
            await _apiClient.SendAsync(
                message,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.BadRequest)
        {
            throw new ArgumentException(
                TranslateApiMessage(
                    await ReadApiErrorAsync(
                        response,
                        cancellationToken)));
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<
                AppUserResponse>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api devolvió una respuesta vacía al crear el usuario.");
    }

    public async Task<AppUserResponse?> UpdateAsync(
        Guid appUserId,
        UpdateAppUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateAppUserId(
            appUserId);

        ArgumentNullException.ThrowIfNull(
            request);

        using var message =
            new HttpRequestMessage(
                HttpMethod.Put,
                $"api/app-users/{appUserId:D}")
            {
                Content =
                    JsonContent.Create(
                        request)
            };

        using var response =
            await _apiClient.SendAsync(
                message,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode ==
            HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException(
                TranslateApiMessage(
                    await ReadApiErrorAsync(
                        response,
                        cancellationToken)));
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<
                AppUserResponse>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api devolvió una respuesta vacía al actualizar el usuario.");
    }

    public async Task<bool> DeleteAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateAppUserId(
            appUserId);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"api/app-users/{appUserId:D}");

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return false;
        }

        if (response.StatusCode ==
            HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException(
                TranslateApiMessage(
                    await ReadApiErrorAsync(
                        response,
                        cancellationToken)));
        }

        response.EnsureSuccessStatusCode();

        return true;
    }

    public async Task<AppUserResponse?> ReactivateAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateAppUserId(
            appUserId);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Patch,
                $"api/app-users/{appUserId:D}/reactivate")
            {
                Content =
                    new StringContent(
                        string.Empty)
            };

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<
                AppUserResponse>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api devolvió una respuesta vacía al reactivar el usuario.");
    }

    public async Task<List<AppUserCollectionRouteResponse>?>
        GetCollectionRoutesAsync(
            Guid appUserId,
            CancellationToken cancellationToken = default)
    {
        ValidateAppUserId(
            appUserId);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                $"api/app-users/{appUserId:D}/collection-routes");

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<
                List<AppUserCollectionRouteResponse>>(
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Sanes.Api devolvió una respuesta vacía al consultar las rutas del usuario.");
    }

    public async Task<bool> AssignCollectionRouteAsync(
        Guid appUserId,
        Guid collectionRouteId,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifiers(
            appUserId,
            collectionRouteId);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                $"api/app-users/{appUserId:D}/collection-routes/{collectionRouteId:D}")
            {
                Content =
                    new StringContent(
                        string.Empty)
            };

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return false;
        }

        if (response.StatusCode ==
            HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException(
                TranslateApiMessage(
                    await ReadApiErrorAsync(
                        response,
                        cancellationToken)));
        }

        response.EnsureSuccessStatusCode();

        return true;
    }

    public async Task<bool> UnassignCollectionRouteAsync(
        Guid appUserId,
        Guid collectionRouteId,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifiers(
            appUserId,
            collectionRouteId);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"api/app-users/{appUserId:D}/collection-routes/{collectionRouteId:D}");

        using var response =
            await _apiClient.SendAsync(
                request,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();

        return true;
    }

    public async Task<bool> SetPasswordAsync(
        Guid appUserId,
        string password,
        CancellationToken cancellationToken = default)
    {
        ValidateAppUserId(
            appUserId);

        if (string.IsNullOrWhiteSpace(
                password))
        {
            throw new ArgumentException(
                "La contraseña es requerida.",
                nameof(password));
        }

        using var message =
            new HttpRequestMessage(
                HttpMethod.Patch,
                $"api/app-users/{appUserId:D}/password")
            {
                Content =
                    JsonContent.Create(
                        new SetAppUserPasswordRequest
                        {
                            Password =
                                password
                        })
            };

        using var response =
            await _apiClient.SendAsync(
                message,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return false;
        }

        if (response.StatusCode ==
            HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException(
                TranslateApiMessage(
                    await ReadApiErrorAsync(
                        response,
                        cancellationToken)));
        }

        response.EnsureSuccessStatusCode();

        return true;
    }

    private static void ValidateAppUserId(
        Guid appUserId)
    {
        if (appUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del usuario no es válido.",
                nameof(appUserId));
        }
    }

    private static void ValidateIdentifiers(
        Guid appUserId,
        Guid collectionRouteId)
    {
        ValidateAppUserId(
            appUserId);

        if (collectionRouteId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la ruta no es válido.",
                nameof(collectionRouteId));
        }
    }

    private static async Task<string> ReadApiErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var content =
            await response.Content
                .ReadAsStringAsync(
                    cancellationToken);

        if (string.IsNullOrWhiteSpace(
                content))
        {
            return "La operación solicitada no es válida.";
        }

        try
        {
            using var document =
                JsonDocument.Parse(
                    content);

            var root =
                document.RootElement;

            if (root.ValueKind ==
                JsonValueKind.String)
            {
                return root.GetString()
                    ?? "La operación solicitada no es válida.";
            }

            if (root.TryGetProperty(
                    "message",
                    out var message) &&
                message.ValueKind ==
                    JsonValueKind.String)
            {
                return message.GetString()
                    ?? "La operación solicitada no es válida.";
            }

            if (root.TryGetProperty(
                    "title",
                    out var title) &&
                title.ValueKind ==
                    JsonValueKind.String)
            {
                return title.GetString()
                    ?? "La operación solicitada no es válida.";
            }
        }
        catch (JsonException)
        {
            return content.Trim('"');
        }

        return content.Trim('"');
    }

    private static string TranslateApiMessage(
        string message)
    {
        return message switch
        {
            "Name is required." =>
                "El nombre es requerido.",

            "Name cannot exceed 150 characters." =>
                "El nombre no puede exceder 150 caracteres.",

            "Username is required." =>
                "El nombre de usuario es requerido.",

            "Username cannot exceed 100 characters." =>
                "El nombre de usuario no puede exceder 100 caracteres.",

            "Username already exists for this tenant." =>
                "Ya existe un usuario con ese nombre de usuario.",

            "Email cannot exceed 150 characters." =>
                "El correo electrónico no puede exceder 150 caracteres.",

            "Email format is invalid." =>
                "El correo electrónico no tiene un formato válido.",

            "Password is required." =>
                "La contraseña es requerida.",

            "Password must contain at least 8 characters." =>
                "La contraseña debe tener al menos 8 caracteres.",

            "Password cannot exceed 100 characters." =>
                "La contraseña no puede exceder 100 caracteres.",

            "Invalid app user role." =>
                "El rol seleccionado no es válido.",

            "The last active administrator cannot change role." =>
                "El último administrador activo no puede cambiar de rol.",

            "The last active administrator cannot be deactivated." =>
                "El último administrador activo no puede ser desactivado.",

            "A collector with assigned collection routes cannot change role. Unassign the routes first." =>
                "El cobrador tiene rutas asignadas. Debe desasignarlas antes de cambiar su rol.",

            "Only users with Collector role can be assigned collection routes." =>
                "Solo los usuarios con rol Cobrador pueden recibir rutas de cobro.",

            "Collection route does not exist, is inactive, or does not belong to this tenant." =>
                "La ruta no existe, está inactiva o no pertenece a la empresa actual.",

            "Collection route is already assigned to this user." =>
                "La ruta ya está asignada a este cobrador.",

            "Password cannot be changed for an inactive app user." =>
                "No se puede cambiar la contraseña de un usuario inactivo.",

            "Identifiers must be valid." =>
                "Los identificadores proporcionados no son válidos.",

            _ =>
                message
        };
    }
}