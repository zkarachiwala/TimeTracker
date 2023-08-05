using TimeTracker.Shared.Models.Identity;

namespace TimeTracker.API.Services;

public interface IClientConfigurationManager
{
    ClientConfiguration GetClientConfiguration();
}