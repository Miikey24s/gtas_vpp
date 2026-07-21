using System.Security.Claims;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gtas_vpp_be.Controllers;

[ApiController]
[Authorize]
[Route("api/business-data/{entityType}/{entityId:guid}/localization")]
public sealed class BusinessDataLocalizationController : ControllerBase
{
    private readonly IBusinessDataLocalizationService _localizationService;

    public BusinessDataLocalizationController(IBusinessDataLocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.LibraryView)]
    [ProducesResponseType<BusinessDataTranslationBundleResDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BusinessDataTranslationBundleResDTO>> Get(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        var result = await _localizationService.GetBundleAsync(
            entityType,
            entityId,
            cancellationToken: cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("translations/{languageCode}")]
    [Authorize(Policy = Permissions.LibraryManage)]
    [ProducesResponseType<BusinessDataTranslationBundleResDTO>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BusinessDataTranslationBundleResDTO>> Upsert(
        string entityType,
        Guid entityId,
        string languageCode,
        [FromBody] BusinessDataTranslationUpsertReqDTO request,
        CancellationToken cancellationToken = default)
        => Ok(await _localizationService.UpsertTranslationAsync(
            entityType,
            entityId,
            languageCode,
            request,
            CurrentUserId,
            cancellationToken));

    [HttpDelete("translations/{languageCode}")]
    [Authorize(Policy = Permissions.LibraryManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        string entityType,
        Guid entityId,
        string languageCode,
        CancellationToken cancellationToken = default)
        => await _localizationService.DeleteTranslationAsync(
            entityType,
            entityId,
            languageCode,
            CurrentUserId,
            cancellationToken)
            ? NoContent()
            : NotFound();

    [HttpPut("original-language")]
    [Authorize(Policy = Permissions.LibraryManage)]
    [ProducesResponseType<BusinessDataTranslationBundleResDTO>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BusinessDataTranslationBundleResDTO>> UpdateOriginalLanguage(
        string entityType,
        Guid entityId,
        [FromBody] BusinessDataOriginalLanguageUpdateReqDTO request,
        CancellationToken cancellationToken = default)
        => Ok(await _localizationService.UpdateOriginalLanguageAsync(
            entityType,
            entityId,
            request.LanguageCode,
            CurrentUserId,
            cancellationToken));

    private int CurrentUserId => int.TryParse(User.FindFirstValue("UserID"), out var userId) ? userId : 0;
}
