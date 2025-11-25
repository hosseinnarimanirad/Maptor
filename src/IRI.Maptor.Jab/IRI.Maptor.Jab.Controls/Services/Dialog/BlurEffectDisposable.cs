using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Effects;

namespace IRI.Maptor.Jab.Controls.Services.Dialog;
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