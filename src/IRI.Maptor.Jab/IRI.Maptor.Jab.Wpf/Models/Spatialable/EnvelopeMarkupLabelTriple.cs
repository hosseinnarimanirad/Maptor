using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Jab.Wpf.Models.MapExtentBookmarks;
using IRI.Maptor.Jab.Core;

namespace IRI.Maptor.Jab.Wpf.Models;

public class EnvelopeMarkupLabelTriple : Notifier
{
    public IriProvince93? Province { get; set; }

    //private string _base64EnvelopeWm;
    //public string Base64EnvelopeWm
    //{
    //    get { return _base64EnvelopeWm; }
    //    set
    //    {
    //        _base64EnvelopeWm = value;
    //        RaisePropertyChanged();
    //    }
    //}

    //public byte[] GetEnvelopeWkbWm()
    //{
    //    return Convert.FromBase64String(Base64EnvelopeWm);
    //}

    public Guid Id { get; set; } = Guid.Empty;

    private string? _pathMarkup;
    public string? PathMarkup
    {
        get { return _pathMarkup; }
        set
        {
            _pathMarkup = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HasPathMarkup));
        }
    }

    private BitmapSource? _thumbnail;
    public BitmapSource? Thumbnail
    {
        get { return _thumbnail; }
        set
        {
            _thumbnail = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(HasThumbnail));
        }
    }

    private string _label = string.Empty;
    public string Label
    {
        get { return _label; }
        set
        {
            _label = value;
            RaisePropertyChanged();
        }
    }

    public BoundingBox WebMercatorExtent { get; init; }

    public bool HasThumbnail => Thumbnail != null;

    public bool HasPathMarkup => PathMarkup != null;

    public bool IsUserDefined { get; set; }

    public EnvelopeMarkupLabelTriple(IriProvince93 province)
    {
        Province = province;

        Label = province.GetDescription();

        PathMarkup = province.GetPathMarkup();

        WebMercatorExtent = province.GetWebMercatorExtent();

        IsUserDefined = false;
    }

    public EnvelopeMarkupLabelTriple(MapExtentBookmark bookmark)
    {
        //this.Province = province;
        Id = bookmark.Id;

        Label = bookmark.Title;

        Thumbnail = bookmark.Thumbnail;

        WebMercatorExtent = bookmark.WebMercatorExtent;

        IsUserDefined = true;
    }

    public Action<EnvelopeMarkupLabelTriple>? RequestRaiseSelected { get; set; }

    private RelayCommand _selectedCommand;
    public RelayCommand SelectedCommand
    {
        get
        {
            if (_selectedCommand == null)
                _selectedCommand = new RelayCommand((param) => RequestRaiseSelected?.Invoke(this));

            return _selectedCommand;
        }
    }

    //public BoundingBox GetBoundingBox()
    //{
    //    //var geometry = SqlGeometry.STGeomFromWKB(new SqlBytes(value.GetEnvelopeWkbWm()), srid);
    //    var geometry = Geometry<Point>.FromWkb(GetEnvelopeWkbWm(), SridHelper.WebMercator);

    //    return geometry.GetBoundingBox();
    //}

    public static List<EnvelopeMarkupLabelTriple> GetProvinces93Wm(/*Action<EnvelopeMarkupLabelTriple> selectAction*/)
    {
        return new List<EnvelopeMarkupLabelTriple>() {
            new EnvelopeMarkupLabelTriple(IriProvince93.Alborz  )/*{ RequestRaiseSelected = selectAction }*/,
            new EnvelopeMarkupLabelTriple(IriProvince93.Ardabil)/*{ RequestRaiseSelected = selectAction }*/,
            new EnvelopeMarkupLabelTriple(IriProvince93.AzarbayejaneGarbi),
            new EnvelopeMarkupLabelTriple(IriProvince93.AzarbayejaneShargi),
            new EnvelopeMarkupLabelTriple(IriProvince93.Isfahan),
            new EnvelopeMarkupLabelTriple(IriProvince93.Booshehr),
            new EnvelopeMarkupLabelTriple(IriProvince93.ChaharmahalVaBakhtiari),
            new EnvelopeMarkupLabelTriple(IriProvince93.Fars),
            new EnvelopeMarkupLabelTriple(IriProvince93.Qazvin),
            new EnvelopeMarkupLabelTriple(IriProvince93.Gilan),
            new EnvelopeMarkupLabelTriple(IriProvince93.Golestan),
            new EnvelopeMarkupLabelTriple(IriProvince93.Hamadan),
            new EnvelopeMarkupLabelTriple(IriProvince93.Hormozgan),
            new EnvelopeMarkupLabelTriple(IriProvince93.Ilam),
            new EnvelopeMarkupLabelTriple(IriProvince93.Kerman),
            new EnvelopeMarkupLabelTriple(IriProvince93.Kermanshah),
            new EnvelopeMarkupLabelTriple(IriProvince93.KhorasanJonoobi),
            new EnvelopeMarkupLabelTriple(IriProvince93.KhorasanRazavi),
            new EnvelopeMarkupLabelTriple(IriProvince93.KhorasanShomali),
            new EnvelopeMarkupLabelTriple(IriProvince93.Khozestan),
            new EnvelopeMarkupLabelTriple(IriProvince93.KohgiluyehVaBoyerahmad),
            new EnvelopeMarkupLabelTriple(IriProvince93.Kordestan),
            new EnvelopeMarkupLabelTriple(IriProvince93.Lorestan),
            new EnvelopeMarkupLabelTriple(IriProvince93.Markazi),
            new EnvelopeMarkupLabelTriple(IriProvince93.Mazandaran),
            new EnvelopeMarkupLabelTriple(IriProvince93.Qom),
            new EnvelopeMarkupLabelTriple(IriProvince93.Semnan),
            new EnvelopeMarkupLabelTriple(IriProvince93.SistanVaBaluchestan),
            new EnvelopeMarkupLabelTriple(IriProvince93.Tehran),
            new EnvelopeMarkupLabelTriple(IriProvince93.Yazd),
            new EnvelopeMarkupLabelTriple(IriProvince93.Zanjan),
        }.OrderBy(i => i.Label).ToList();
        //return new List<EnvelopeMarkupLabelTriple>()
        //{
        //    new EnvelopeMarkupLabelTriple("البرز",IriProvinces93.Alborz,     IriProvinces93WmEnvelopes.Alborz){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("اردبیل",IriProvinces93.Ardabil,        IriProvinces93WmEnvelopes.Ardabil){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("آذربایجان غربی",IriProvinces93.AzarbayejaneGarbi,IriProvinces93WmEnvelopes.AzarbayejaneGarbi){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("آذربایجان شرقی",IriProvinces93.AzarbayejaneShargi,IriProvinces93WmEnvelopes.AzarbayejaneShargi){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("اصفهان",IriProvinces93.Esfahan,        IriProvinces93WmEnvelopes.Isfahan){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("بوشهر",IriProvinces93.Booshehr,         IriProvinces93WmEnvelopes.Booshehr){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("چهارمحال و...",IriProvinces93.Chaharmahal, IriProvinces93WmEnvelopes.Chaharmahal){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("فارس",IriProvinces93.Fars,          IriProvinces93WmEnvelopes.Fars){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("قزوین",IriProvinces93.Gazvin,         IriProvinces93WmEnvelopes.Qazvin){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("گیلان",IriProvinces93.Gilan,          IriProvinces93WmEnvelopes.Gilan){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("گلستان",IriProvinces93.Golestan,        IriProvinces93WmEnvelopes.Golestan){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("همدان",IriProvinces93.Hamedan,         IriProvinces93WmEnvelopes.Hamadan){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("هرمزگان",IriProvinces93.Hormozgan,       IriProvinces93WmEnvelopes.Hormozgan){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("ایلام",IriProvinces93.Ilam,          IriProvinces93WmEnvelopes.Ilam){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("کرمان",IriProvinces93.Kerman,         IriProvinces93WmEnvelopes.Kerman){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("کرمانشاه",IriProvinces93.Kermanshah,      IriProvinces93WmEnvelopes.Kermanshah){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("خراسان جنوبی",IriProvinces93.KhorasanJonoobi,  IriProvinces93WmEnvelopes.KhorasanJonoobi){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("خراسان رضوی",IriProvinces93.KhorasanRazavi,   IriProvinces93WmEnvelopes.KhorasanRazavi){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("خراسان شمالی",IriProvinces93.KhorasanShomali,  IriProvinces93WmEnvelopes.KhorasanShomali){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("خوزستان",IriProvinces93.Khozestan,       IriProvinces93WmEnvelopes.Khozestan){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("کهگیلویه و ...",IriProvinces93.Kohkiloye,IriProvinces93WmEnvelopes.Kohkiloye){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("کردستان",IriProvinces93.Kordestan,       IriProvinces93WmEnvelopes.Kordestan){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("لرستان",IriProvinces93.Lorestan,        IriProvinces93WmEnvelopes.Lorestan){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("مرکزی",IriProvinces93.Markazi,         IriProvinces93WmEnvelopes.Markazi){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("مازندران",IriProvinces93.Mazandaran,      IriProvinces93WmEnvelopes.Mazandaran){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("قم",IriProvinces93.Qom,            IriProvinces93WmEnvelopes.Qom){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("سمنان",IriProvinces93.Semnan,         IriProvinces93WmEnvelopes.Semnan){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("سیستان و ...",IriProvinces93.Sistan,  IriProvinces93WmEnvelopes.Sistan){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("تهران",IriProvinces93.Tehran,         IriProvinces93WmEnvelopes.Tehran){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("یزد",IriProvinces93.Yazd,           IriProvinces93WmEnvelopes.Yazd){ RequestRaiseSelected = selectAction },
        //    new EnvelopeMarkupLabelTriple("زنجان",IriProvinces93.Zanjan,         IriProvinces93WmEnvelopes.Zanjan){ RequestRaiseSelected = selectAction },
        //}.OrderBy(i => i.Label).ToList();
    }
}
