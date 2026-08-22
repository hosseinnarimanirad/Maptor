using System;
using System.Windows;
using System.Windows.Media.Effects;

namespace IRI.Maptor.Presentation.Wpf.Models.Themes;
/// <summary>
/// Disposable helper class that restores the original window effect when disposed.
/// </summary>
internal class BlurEffectDisposable : IDisposable
{
    private readonly Window _owner;
    private readonly Effect? _originalEffect;
    private bool _disposed = false;

    public BlurEffectDisposable(Window owner, Effect? originalEffect)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _originalEffect = originalEffect;
    }

    public void Dispose()
    {
        if (!_disposed && _owner != null)
        {
            _owner.Effect = _originalEffect;
            _disposed = true;
        }
    }
}