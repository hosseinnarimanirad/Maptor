using System;
using System.Linq;
using System.Collections.Generic;

using IRI.Maptor.Sta.Common.Enums;
using IRI.Maptor.Jab.Common.Assets.Data;
using IRI.Maptor.Jab.Common.Assets.Commands;
using IRI.Maptor.Jab.Common.Assets.ShapeStrings;

namespace IRI.Maptor.Jab.Common.Models.Spatialable;

public class EnvelopeMarkupLabelTriple : Notifier
{
    public IrProvince93 Province { get; set; }

    private string _base64EnvelopeWm;

    public string Base64EnvelopeWm
    {
        get { return _base64EnvelopeWm; }
        set
        {
            _base64EnvelopeWm = value;
            RaisePropertyChanged();
        }
    }

    //public byte[] GetEnvelopeWkbWm()
    //{
    //    return Convert.FromBase64String(Base64EnvelopeWm);
    //}

    private string _pathMarkup;

    public string PathMarkup
    {
        get { return _pathMarkup; }
        set
        {
            _pathMarkup = value;
            RaisePropertyChanged();
        }
    }

    private string _label;

    public string Label
    {
        get { return _label; }
        set
        {
            _label = value;
            RaisePropertyChanged();
        }
    }

    public EnvelopeMarkupLabelTriple(IrProvince93 province, string label, string markup, string base64EnvelopeWm)
    {
        this.Province = province;

        this.Label = label;

        this.PathMarkup = markup;

        this.Base64EnvelopeWm = base64EnvelopeWm;
    }

    public Action<EnvelopeMarkupLabelTriple> RequestRaiseSelected { get; set; }

    private RelayCommand _selectedCommand;

    public RelayCommand SelectedCommand
    {
        get
        {
            if (_selectedCommand == null)
            {
                _selectedCommand = new RelayCommand((param) => this.RequestRaiseSelected?.Invoke(this));
            }
            return _selectedCommand;
        }
    }

    //public BoundingBox GetBoundingBox()
    //{
    //    //var geometry = SqlGeometry.STGeomFromWKB(new SqlBytes(value.GetEnvelopeWkbWm()), srid);
    //    var geometry = Geometry<Point>.FromWkb(GetEnvelopeWkbWm(), SridHelper.WebMercator);

    //    return geometry.GetBoundingBox();
    //}

    public static List<EnvelopeMarkupLabelTriple> GetProvinces93Wm(Action<EnvelopeMarkupLabelTriple> selectAction)
    {
        return new List<EnvelopeMarkupLabelTriple>()
        {
            new EnvelopeMarkupLabelTriple(IrProvince93.Alborz                 ,"البرز",         IriProvinces93.Alborz, IriProvinces93WmEnvelopes.Alborz){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.Ardabil                ,"اردبیل",        IriProvinces93.Ardabil,IriProvinces93WmEnvelopes.Ardabil){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.AzarbayejaneGarbi        ,"آذربایجان غربی",IriProvinces93.AzarbayejaneGarbi,  IriProvinces93WmEnvelopes.AzarbayejaneGarbi){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.AzarbayejaneShargi       ,"آذربایجان شرقی",IriProvinces93.AzarbayejaneShargi,IriProvinces93WmEnvelopes.AzarbayejaneShargi){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.Isfahan                ,"اصفهان",        IriProvinces93.Esfahan,IriProvinces93WmEnvelopes.Isfahan){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.Booshehr                ,"بوشهر",         IriProvinces93.Booshehr,IriProvinces93WmEnvelopes.Booshehr){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.ChaharmahalVaBakhtiari ,"چهارمحال و...", IriProvinces93.Chaharmahal,IriProvinces93WmEnvelopes.Chaharmahal){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.Fars                   ,"فارس",          IriProvinces93.Fars,IriProvinces93WmEnvelopes.Fars){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.Qazvin                 ,"قزوین",         IriProvinces93.Gazvin,IriProvinces93WmEnvelopes.Qazvin){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.Gilan                  ,"گیلان",          IriProvinces93.Gilan,IriProvinces93WmEnvelopes.Gilan){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.Golestan               ,"گلستان",        IriProvinces93.Golestan,IriProvinces93WmEnvelopes.Golestan){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.Hamadan                ,"همدان",         IriProvinces93.Hamedan,IriProvinces93WmEnvelopes.Hamadan){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.Hormozgan              ,"هرمزگان",       IriProvinces93.Hormozgan,IriProvinces93WmEnvelopes.Hormozgan){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.Ilam                   ,"ایلام",          IriProvinces93.Ilam,IriProvinces93WmEnvelopes.Ilam){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.Kerman                 ,"کرمان",         IriProvinces93.Kerman,IriProvinces93WmEnvelopes.Kerman){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.Kermanshah             ,"کرمانشاه",      IriProvinces93.Kermanshah,IriProvinces93WmEnvelopes.Kermanshah){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.KhorasanJonoobi         ,"خراسان جنوبی",  IriProvinces93.KhorasanJonoobi,IriProvinces93WmEnvelopes.KhorasanJonoobi){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.KhorasanRazavi         ,"خراسان رضوی",   IriProvinces93.KhorasanRazavi,IriProvinces93WmEnvelopes.KhorasanRazavi){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.KhorasanShomali        ,"خراسان شمالی",  IriProvinces93.KhorasanShomali,IriProvinces93WmEnvelopes.KhorasanShomali){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.Khozestan              ,"خوزستان",       IriProvinces93.Khozestan,IriProvinces93WmEnvelopes.Khozestan){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.KohgiluyehVaBoyerahmad ,"کهگیلویه و ...",IriProvinces93.Kohkiloye,IriProvinces93WmEnvelopes.Kohkiloye){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.Kordestan              ,"کردستان",       IriProvinces93.Kordestan,IriProvinces93WmEnvelopes.Kordestan){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.Lorestan               ,"لرستان",        IriProvinces93.Lorestan,IriProvinces93WmEnvelopes.Lorestan){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.Markazi                ,"مرکزی",         IriProvinces93.Markazi,IriProvinces93WmEnvelopes.Markazi){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.Mazandaran             ,"مازندران",      IriProvinces93.Mazandaran,IriProvinces93WmEnvelopes.Mazandaran){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.Qom                    ,"قم",            IriProvinces93.Qom,IriProvinces93WmEnvelopes.Qom){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.Semnan                 ,"سمنان",         IriProvinces93.Semnan,IriProvinces93WmEnvelopes.Semnan){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.SistanVaBaluchestan    ,"سیستان و ...",  IriProvinces93.Sistan,IriProvinces93WmEnvelopes.Sistan){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.Tehran                 ,"تهران",         IriProvinces93.Tehran,IriProvinces93WmEnvelopes.Tehran){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.Yazd                   ,"یزد",           IriProvinces93.Yazd,IriProvinces93WmEnvelopes.Yazd){ RequestRaiseSelected = selectAction },
            new EnvelopeMarkupLabelTriple(IrProvince93.Zanjan                 ,"زنجان",         IriProvinces93.Zanjan,IriProvinces93WmEnvelopes.Zanjan){ RequestRaiseSelected = selectAction },
        }.OrderBy(i => i.Label).ToList();
    }
}
