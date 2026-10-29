using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using IRI.Maptor.Core.Spatial.Dtos;
using IRI.Maptor.Core.Spatial.Importing;

namespace IRI.Maptor.Presentation.Wpf.Services;

/// <summary>
/// The server behind the layer-settings admin window (<c>LayerSetting/Admin/*</c>). Each application
/// implements it over its own HTTP client and error format; the window only sees these results.
/// </summary>
public interface ILayerSettingsAdminService
{
    Task<LayerSettingsAdminResponse<LayerSettingAdminListDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<LayerSettingsAdminResponse<LayerSettingAdminDto>> UpdateAsync(int id, LayerSettingUpdateDto setting, CancellationToken cancellationToken = default);

    Task<LayerSettingsAdminResponse<LayerSettingAdminDto>> CreateGroupAsync(LayerGroupCreateDto group, CancellationToken cancellationToken = default);

    Task<LayerSettingsAdminResponse<bool>> DeleteGroupAsync(int id, CancellationToken cancellationToken = default);

    Task<LayerSettingsAdminResponse<LayerSettingAdminListDto>> UpdateOrderAsync(LayerSettingOrderDto order, CancellationToken cancellationToken = default);

    /// <summary>The data model of every active layer, as the file an import tool reads (<c>LayerSetting/Admin/ImportModel</c>).</summary>
    Task<LayerSettingsAdminResponse<ImportModelDto>> GetImportModelAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// One call's outcome. <see cref="Status"/> is the server's answer; null means the call never got one
/// (network, sign-in, unexpected status) and <see cref="ErrorMessage"/> says why, already localized.
/// </summary>
public class LayerSettingsAdminResponse<T>
{
    public LayerSettingAdminStatus? Status { get; init; }

    public T? Value { get; init; }

    /// <summary>The broken rules when <see cref="Status"/> is Invalid.</summary>
    public IReadOnlyList<LayerSettingError> Errors { get; init; } = [];

    public string? ErrorMessage { get; init; }

    public bool IsSuccess => Status == LayerSettingAdminStatus.Ok;

    public static LayerSettingsAdminResponse<T> Ok(T value) => new() { Status = LayerSettingAdminStatus.Ok, Value = value };

    public static LayerSettingsAdminResponse<T> Refused(LayerSettingAdminStatus status, IReadOnlyList<LayerSettingError>? errors = null) =>
        new() { Status = status, Errors = errors ?? [] };

    public static LayerSettingsAdminResponse<T> Failed(string errorMessage) => new() { ErrorMessage = errorMessage };
}
