using System.Text.Json;
using Microsoft.JSInterop;

namespace IRI.Maptor.Jab.Blazor.Toc;

/// <summary>What the user changed about one TOC node, as opposed to what the server configured.</summary>
public sealed class TocPreference
{
    public bool IsVisible { get; set; }

    public double Opacity { get; set; } = 1;

    public bool IsExpanded { get; set; }
}

/// <summary>
/// Remembers a user's TOC edits — which layers are on, how transparent, which groups are open —
/// in browser localStorage, keyed by node id.
///
/// <para>Per-user and per-browser on purpose. The obvious alternative, writing back to the server's
/// layer configuration, is a shared row: one user switching a layer off would switch it off for
/// everyone. A roaming per-user store would need its own table and endpoints; this matches what the
/// WPF client already does by keeping the equivalent state in local user settings.</para>
///
/// <para>Preferences are advisory. An id the server no longer sends is ignored, and a layer with no
/// stored preference keeps the server's default — so adding or removing layers server-side never
/// needs the stored state to be migrated.</para>
/// </summary>
public sealed class TocPreferenceStore
{
    private readonly IJSRuntime _jsRuntime;
    private readonly string _storageKey;

    public TocPreferenceStore(IJSRuntime jsRuntime, string storageKey)
    {
        _jsRuntime = jsRuntime;
        _storageKey = storageKey;
    }

    /// <summary>
    /// Overlays stored preferences onto a freshly built tree. Assigns the properties directly
    /// rather than going through <see cref="TocLayerNode.SetVisibility"/>: that cascades to
    /// children, which would let a stored group value overwrite the stored values of its own
    /// leaves depending on the order nodes happen to be visited.
    /// </summary>
    public async Task ApplyAsync(IEnumerable<TocLayerNode> roots)
    {
        var preferences = await LoadAsync();

        if (preferences.Count == 0)
            return;

        foreach (var node in roots.SelectMany(root => root.SelfAndDescendants()))
        {
            if (!preferences.TryGetValue(node.Id, out var preference))
                continue;

            node.IsVisible = preference.IsVisible;
            node.Opacity = Math.Clamp(preference.Opacity, 0, 1);
            node.IsExpanded = preference.IsExpanded;
        }
    }

    public async Task SaveAsync(IEnumerable<TocLayerNode> roots)
    {
        var preferences = roots
            .SelectMany(root => root.SelfAndDescendants())
            .ToDictionary(
                node => node.Id,
                node => new TocPreference
                {
                    IsVisible = node.IsVisible,
                    Opacity = node.Opacity,
                    IsExpanded = node.IsExpanded,
                });

        var raw = JsonSerializer.Serialize(preferences, JsonSerializerOptions.Web);

        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", _storageKey, raw);
    }

    public async Task ClearAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", _storageKey);
    }

    private async Task<Dictionary<string, TocPreference>> LoadAsync()
    {
        var raw = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", _storageKey);

        if (string.IsNullOrWhiteSpace(raw))
            return [];

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, TocPreference>>(raw, JsonSerializerOptions.Web) ?? [];
        }
        catch (JsonException)
        {
            // State written by an older build (or hand-edited) must not brick the TOC; drop it and
            // fall back to the server's configuration.
            await ClearAsync();

            return [];
        }
    }
}
