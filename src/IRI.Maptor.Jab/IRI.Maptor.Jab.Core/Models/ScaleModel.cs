using IRI.Maptor.Jab.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Core.Models;

public class ScaleModel : Notifier
{
    private static readonly List<ScaleModel> _scales;
    public static List<ScaleModel> Scales => _scales;


    private double _inverseScale;
    public double InverseScale
    {
        get { return _inverseScale; }
        set
        {
            _inverseScale = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(Scale));
        }
    }

    public double Scale => 1.0 / InverseScale;

    public ScaleModel(double inverseScale)
    {
        InverseScale = inverseScale;
    }

    static ScaleModel()
    {
        _scales = new List<double>()
        {
            1_000_000,
            500_000,
            250_000,
            100_000,
            50_000,
            25_000,
            10_000,
            5_000,
            2_000,
            1_000,
            500
        }.Select(d => new ScaleModel(d)).ToList();
    }
}
