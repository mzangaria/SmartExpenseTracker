using ExpenseTracker.Api.Dtos.Analytics;
using ExpenseTracker.Api.Dtos.Budgets;
using ExpenseTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("analytics")]
public class AnalyticsController(ICurrentUserService currentUserService, IAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet("monthly-summary")]
    public async Task<ActionResult<MonthlySummaryResponse>> GetMonthlySummary([FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var response = await analyticsService.GetMonthlySummaryAsync(userId, NormalizeYear(year), NormalizeMonth(month), cancellationToken);
        return Ok(response);
    }

    [HttpGet("category-breakdown")]
    public async Task<ActionResult<IReadOnlyList<CategoryBreakdownResponse>>> GetCategoryBreakdown([FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var response = await analyticsService.GetCategoryBreakdownAsync(userId, NormalizeYear(year), NormalizeMonth(month), cancellationToken);
        return Ok(response);
    }

    [HttpGet("trends")]
    public async Task<ActionResult<IReadOnlyList<TrendPointResponse>>> GetTrends([FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var response = await analyticsService.GetTrendsAsync(userId, NormalizeYear(year), NormalizeMonth(month), cancellationToken);
        return Ok(response);
    }

    [HttpGet("insights")]
    public async Task<ActionResult<IReadOnlyList<InsightResponse>>> GetInsights([FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var response = await analyticsService.GetInsightsAsync(userId, NormalizeYear(year), NormalizeMonth(month), cancellationToken);
        return Ok(response);
    }

    [HttpGet("budget-variance")]
    public async Task<ActionResult<IReadOnlyList<BudgetVarianceResponse>>> GetBudgetVariance([FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var response = await analyticsService.GetBudgetVarianceAsync(userId, NormalizeYear(year), NormalizeMonth(month), cancellationToken);
        return Ok(response);
    }

    private static int NormalizeYear(int year) => year is >= 2000 and <= 9999 ? year : DateTime.UtcNow.Year;

    private static int NormalizeMonth(int month) => month is >= 1 and <= 12 ? month : DateTime.UtcNow.Month;
}
