using IRI.Maptor.Jab.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Jab.Common.Models
{
    public class DegreeMinuteSecondModel : Notifier
    {
        private bool _triggerOnValueChanged = true;

        private double _degree;

        public double Degree
        {
            get { return _degree; }
            set
            {
                if (_degree == value)
                    return;

                _degree = value;

                RaisePropertyChanged();

                UpdateDecimalValue();
            }
        }


        private double _minute;

        public double Minute
        {
            get { return _minute; }
            set
            {
                if (_minute == value)
                    return;

                _minute = value;

                RaisePropertyChanged();

                UpdateDecimalValue();
            }
        }


        private double _second;

        public double Second
        {
            get { return _second; }
            set
            {
                if (_second == value)
                    return;

                _second = value;

                RaisePropertyChanged();

                UpdateDecimalValue();
            }
        }


        private double _value;

        public double Value
        {
            get { return _value; }
            set
            {
                _value = value;
                RaisePropertyChanged();

                UpdateDms();
            }
        }


        public DegreeMinuteSecondModel() : this(0)
        {

        }

        public DegreeMinuteSecondModel(double deciamalDegree)
        {
            Value = deciamalDegree;
        }

        public double GetDegreeValue()
        {
            return Degree + Minute / 60.0 + Second / 3600.0;
        }

        public event EventHandler OnValueChanged;


        private void UpdateDms()
        {
            int degree, minute;

            double second;

            IRI.Maptor.Sta.Common.Helpers.DegreeHelper.ToDms(Value, true, out degree, out minute, out second);

            _triggerOnValueChanged = false;

            this.Degree = degree;

            this.Minute = minute;

            this.Second = second;

            _triggerOnValueChanged = true;

            this.OnValueChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateDecimalValue()
        {
            if (!_triggerOnValueChanged)
                return;

            _value = Degree + Minute / 60 + Second / 3600;

            RaisePropertyChanged(nameof(Value));

            this.OnValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
