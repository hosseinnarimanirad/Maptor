using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace IRI.Maptor.Presentation.Wpf.Helpers;

/// <summary>
/// A <see cref="ResourceDictionary"/> whose <see cref="Source"/> is parsed once per process and
/// shared by every view that merges it.
/// </summary>
/// <remarks>
/// WPF re-parses a dictionary from BAML every time a <c>ResourceDictionary Source="..."</c> is
/// encountered. Views that merge the library-wide style set (Controls.All.xaml, about 200 KB
/// across 26 files) therefore paid that parse on every window open. Merging through this class
/// keeps the view self-contained for hosts that do not merge the set at application level, while
/// making the second and later merges free.
/// At design time the dictionary falls back to the plain, uncached behaviour so the designer
/// keeps reloading edited dictionaries.
/// <para>
/// <b>Use it in views, not inside the library's own style dictionaries or a host's App.xaml.</b>
/// Each use adds a wrapper level: this object is itself a dictionary whose only child is the
/// cached one. Tried and measured on 2026-10-24 by converting the 59 internal merges of
/// Assets/: parsing a dictionary turned out to be cheap (about 5 ms for all of Maptor.All), while
/// the extra levels made every resource lookup from inside a dialog about 66% slower (120,000
/// lookups: 408 ms plain, 678 ms shared) with no gain in dialog open time. Reverted. See the
/// design-system PROGRESS.md section 15.
/// </para>
/// </remarks>
public class SharedResourceDictionary : ResourceDictionary
{
    private static readonly Dictionary<Uri, ResourceDictionary> _cache = new();
    private static readonly object _gate = new();

    private static readonly bool _isInDesignMode =
        (bool)DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue;

    private Uri? _source;

    /// <summary>
    /// The dictionary to load. Relative "/Assembly;component/..." URIs are normalised to the
    /// absolute pack form so both spellings share one cache entry.
    /// </summary>
    public new Uri? Source
    {
        get => _source;
        set
        {
            _source = value;

            if (value == null)
                return;

            if (_isInDesignMode)
            {
                base.Source = value;
                return;
            }

            var key = value.IsAbsoluteUri
                ? value
                : new Uri("pack://application:,,," + value.OriginalString, UriKind.Absolute);

            ResourceDictionary? shared;

            lock (_gate)
            {
                if (!_cache.TryGetValue(key, out shared))
                {
                    shared = new ResourceDictionary { Source = key };
                    _cache[key] = shared;
                }
            }

            MergedDictionaries.Add(shared);
        }
    }
}
