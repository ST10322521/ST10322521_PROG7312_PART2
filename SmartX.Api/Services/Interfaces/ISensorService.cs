using SmartX.Api.Results;
using SmartX.Shared.DTOs;
using SmartX.Shared.Models;

namespace SmartX.Api.Services.Interfaces;

public interface ISensorService
{
    Task<IReadOnlyList<Sensor>> GetAllAsync();

    Task<Sensor?> GetByIdAsync(Guid id);

    Task<ServiceResult<Sensor>> CreateAsync(CreateSensorRequest request);

    Task<ServiceResult<Sensor>> UpdateAsync(Guid id, CreateSensorRequest request);

    Task<bool> DeleteAsync(Guid id);
}