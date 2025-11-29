//using IRI.Maptor.Jab.Common;
//using IRI.Maptor.Jab.Common.Assets.Commands;
//using System;
//using Geometry = IRI.Maptor.Sta.Spatial.Primitives.Geometry<IRI.Maptor.Sta.Common.Primitives.Point>;

//namespace IRI.Maptor.Jab.Common.Models.CoordinateEditor;

//public class CoordinateEditor : Notifier
//{
//    private int _srid;

//    public int Srid
//    {
//        get { return _srid; }
//        set
//        {
//            _srid = value;
//            RaisePropertyChanged();
//        }
//    }


//    public Action<CoordinateEditor> RequestPan;

//    public Action<CoordinateEditor> RequestZoom;

//    public Action<CoordinateEditor> RequestCheckValidity;

//    public Action<CoordinateEditor> RequestDelete;

//    private void Zoom()
//    {
//        this.RequestZoom?.Invoke(this);
//    }

//    private void CheckValidity()
//    {
//        this.RequestCheckValidity?.Invoke(this);
//    }

//    public virtual Geometry GetGeometry()
//    {
//        return null;
//    }

//    #region Command

//    private RelayCommand _zoomCommand;

//    public RelayCommand ZoomCommand
//    {
//        get
//        {
//            if (_zoomCommand == null)
//            {
//                _zoomCommand = new RelayCommand(param => Zoom());
//            }

//            return _zoomCommand;
//        }
//    }

//    private RelayCommand _deleteCommand;

//    public RelayCommand DeleteCommand
//    {
//        get
//        {
//            if (_deleteCommand == null)
//            {
//                _deleteCommand = new RelayCommand(param => this.RequestDelete?.Invoke(this));
//            }

//            return _deleteCommand;
//        }
//    }

//    private RelayCommand _checkValidityCommand;

//    public RelayCommand CheckValidityCommand
//    {
//        get
//        {
//            if (_checkValidityCommand == null)
//            {
//                _checkValidityCommand = new RelayCommand(param => CheckValidity());
//            }

//            return _checkValidityCommand;
//        }
//    }

//    #endregion
//}
