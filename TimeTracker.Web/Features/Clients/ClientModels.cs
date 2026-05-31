using System.ComponentModel.DataAnnotations;
using TimeTracker.Shared.Entities;

namespace TimeTracker.Web.Features.Clients;

public record ClientResponse(
    int Id,
    string Name,
    bool IsArchived,
    decimal? DefaultHourlyRate,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone
);

public class ClientRequest
{
    public int Id { get; set; }
    public bool IsArchived { get; set; }

    [Required(ErrorMessage = "Please enter a client name.")]
    public required string Name { get; set; }

    public decimal? DefaultHourlyRate { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}

public class ClientCreateRequest
{
    public required string Name { get; set; }
    public decimal? DefaultHourlyRate { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}

public class ClientUpdateRequest
{
    public required string Name { get; set; }
    public decimal? DefaultHourlyRate { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}

public class ClientMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Client, ClientResponse>();
    }
}
