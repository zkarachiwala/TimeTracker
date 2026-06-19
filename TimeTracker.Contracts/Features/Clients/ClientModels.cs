using System.ComponentModel.DataAnnotations;

namespace TimeTracker.Contracts.Features.Clients;

public record ClientResponse(
    int Id,
    string Name,
    bool IsArchived,
    decimal? DefaultHourlyRate,
    decimal? AwardRate,
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
    public decimal? AwardRate { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}

public class ClientCreateRequest
{
    public required string Name { get; set; }
    public decimal? DefaultHourlyRate { get; set; }
    public decimal? AwardRate { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}

public class ClientUpdateRequest
{
    public required string Name { get; set; }
    public decimal? DefaultHourlyRate { get; set; }
    public decimal? AwardRate { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}

public record DeletedClientResponse(int Id, string Name, DateTime? DateDeleted, string? DeletedBy);
