using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryOrderSystem.Core.DTOs;
using InventoryOrderSystem.Core.Entities;
using InventoryOrderSystem.Core.Exceptions;
using InventoryOrderSystem.Core.Interfaces;
using InventoryOrderSystem.Infrastructure.Data;

namespace InventoryOrderSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportingService _reportingService;

    public ReportsController(IReportingService reportingService)
    {
        _reportingService = reportingService;
    }

    [HttpGet("sales")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(SalesReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSalesReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken ct)
    {
        var report = await _reportingService.GetSalesReportAsync(startDate, endDate, ct);
        return Ok(report);
    }

    [HttpGet("inventory")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(InventoryValuationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventoryValuation(CancellationToken ct)
    {
        var valuation = await _reportingService.GetInventoryValuationAsync(ct);
        return Ok(valuation);
    }

    [HttpGet("export/sales-csv")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> ExportSalesCsv(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken ct)
    {
        var csvBytes = await _reportingService.ExportSalesReportCsvAsync(startDate, endDate, ct);
        return File(csvBytes, "text/csv", $"sales_report_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet("export/inventory-csv")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> ExportInventoryCsv(CancellationToken ct)
    {
        var csvBytes = await _reportingService.ExportInventoryCsvAsync(ct);
        return File(csvBytes, "text/csv", $"inventory_report_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}

[ApiController]
[Route("api/[controller]")]
public class ChatbotController : ControllerBase
{
    private readonly AppDbContext _context;

    public ChatbotController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("conversations")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(List<ChatbotConversationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversations(CancellationToken ct)
    {
        var convs = await _context.ChatbotConversations
            .Include(c => c.Customer)
            .Include(c => c.Messages)
            .OrderByDescending(c => c.LastMessageAt)
            .Take(100)
            .Select(c => new ChatbotConversationDto
            {
                Id = c.Id,
                SessionId = c.SessionId,
                CustomerId = c.CustomerId,
                CustomerName = c.Customer != null ? c.Customer.Name : "Anonymous Guest",
                Channel = c.Channel,
                StartedAt = c.StartedAt,
                LastMessageAt = c.LastMessageAt,
                MessageCount = c.Messages.Count
            })
            .ToListAsync(ct);

        return Ok(convs);
    }

    [HttpGet("conversations/{sessionId}/messages")]
    [ProducesResponseType(typeof(List<ChatbotMessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessages(string sessionId, CancellationToken ct)
    {
        var conv = await _context.ChatbotConversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId, ct);

        if (conv == null)
        {
            return Ok(new List<ChatbotMessageDto>());
        }

        var messages = conv.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatbotMessageDto
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                Sender = m.Sender,
                MessageText = m.MessageText,
                IntentDetected = m.IntentDetected,
                StructuredData = m.StructuredData,
                CreatedAt = m.CreatedAt
            })
            .ToList();

        return Ok(messages);
    }

    [HttpPost("log")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> LogMessage([FromBody] ChatbotLogRequest request, CancellationToken ct)
    {
        var conv = await _context.ChatbotConversations
            .FirstOrDefaultAsync(c => c.SessionId == request.SessionId, ct);

        if (conv == null)
        {
            conv = new ChatbotConversation
            {
                Id = Guid.NewGuid(),
                SessionId = request.SessionId,
                CustomerId = request.CustomerId,
                Channel = "WebChat",
                StartedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            };
            await _context.ChatbotConversations.AddAsync(conv, ct);
        }
        else
        {
            conv.LastMessageAt = DateTime.UtcNow;
            if (request.CustomerId.HasValue && !conv.CustomerId.HasValue)
            {
                conv.CustomerId = request.CustomerId;
            }
        }

        var msg = new ChatbotMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conv.Id,
            Sender = request.Sender,
            MessageText = request.MessageText,
            IntentDetected = request.IntentDetected,
            StructuredData = request.StructuredData,
            CreatedAt = DateTime.UtcNow
        };
        await _context.ChatbotMessages.AddAsync(msg, ct);
        await _context.SaveChangesAsync(ct);

        return Ok(new { message = "Logged successfully" });
    }
}

[ApiController]
[Route("api/[controller]")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditLogsController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? entityName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var logs = await _auditService.GetAuditLogsAsync(entityName, page, pageSize, ct);
        return Ok(logs);
    }
}

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SettingsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<SettingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings(CancellationToken ct)
    {
        var settings = await _context.ApplicationSettings
            .OrderBy(s => s.Category)
            .ThenBy(s => s.SettingKey)
            .Select(s => new SettingDto
            {
                Id = s.Id,
                SettingKey = s.SettingKey,
                SettingValue = s.SettingValue,
                Description = s.Description,
                Category = s.Category,
                UpdatedAt = s.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(settings);
    }

    [HttpPut("{key}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SettingDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingDto dto, CancellationToken ct)
    {
        var setting = await _context.ApplicationSettings
            .FirstOrDefaultAsync(s => s.SettingKey.ToLower() == key.ToLower(), ct)
            ?? throw new NotFoundException(nameof(ApplicationSetting), key);

        setting.SettingValue = dto.SettingValue;
        if (!string.IsNullOrWhiteSpace(dto.Description))
        {
            setting.Description = dto.Description;
        }
        setting.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return Ok(new SettingDto
        {
            Id = setting.Id,
            SettingKey = setting.SettingKey,
            SettingValue = setting.SettingValue,
            Description = setting.Description,
            Category = setting.Category,
            UpdatedAt = setting.UpdatedAt
        });
    }
}
