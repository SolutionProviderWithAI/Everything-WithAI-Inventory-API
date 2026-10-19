using InventoryOrderSystem.Core.DTOs;

namespace InventoryOrderSystem.Core.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<UserDto> GetUserByIdAsync(Guid userId, CancellationToken ct = default);
    Task<List<UserDto>> GetAllUsersAsync(CancellationToken ct = default);
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}

public interface IReportingService
{
    Task<SalesReportDto> GetSalesReportAsync(DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    Task<InventoryValuationDto> GetInventoryValuationAsync(CancellationToken ct = default);
    Task<byte[]> ExportSalesReportCsvAsync(DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    Task<byte[]> ExportInventoryCsvAsync(CancellationToken ct = default);
}

public interface IAuditService
{
    Task LogAsync(string entityName, string entityId, string action, object? oldValues = null, object? newValues = null, string? userId = null, string? userEmail = null, CancellationToken ct = default);
    Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(string? entityName, int page, int pageSize, CancellationToken ct = default);
}
