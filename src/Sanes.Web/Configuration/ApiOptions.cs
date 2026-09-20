using System.ComponentModel.DataAnnotations;

namespace Sanes.Web.Configuration;

public sealed class ApiOptions
{
    public const string SectionName = "Api";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;
}