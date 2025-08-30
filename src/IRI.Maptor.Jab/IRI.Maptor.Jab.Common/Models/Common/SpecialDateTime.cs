using IRI.Maptor.Extensions;
using IRI.Maptor.Jab.Common.Events;
using System;
using System.Text.Json.Serialization;

namespace IRI.Maptor.Jab.Common.Models;

public class SpecialDateTime : Notifier
{
    //public SpecialDateTime(DateTime? dateTime, Action<SpecialDateTime> onChangeAction)
    //{
    //    this.ADDate = dateTime;

    //    this.ChangeAction = onChangeAction;
    //}

    public SpecialDateTime(DateTime? dateTime, Action<SpecialDateTime> onChangeAction = null)
    {
        ADDate = dateTime;

        ChangeAction = onChangeAction;
    }

    private DateTime? _adDate;

    public DateTime? ADDate
    {
        get { return _adDate; }
        set
        {
            if (value == _adDate)
                return;

            _adDate = value;

            RaisePropertyChanged();

            FullPersianAlphabeticDate = value?.ToPersianAlphabeticDate() ?? string.Empty;

            PersianDate = value?.ToPersianDate() ?? string.Empty; ;

            LongPersianDateTime = value?.ToLongPersianDateTime() ?? string.Empty;

            PersianDay = value?.GetPersianDayOfMonth() ?? 0;

            PersianMonth = value?.GetPersianMonth() ?? 0;

            RaisePropertyChanged(nameof(Ticks));

            ChangeAction?.Invoke(this);
        }
    }

    private string _fullPersianAlphabeticDate;

    public string FullPersianAlphabeticDate
    {
        get
        {
            return _fullPersianAlphabeticDate;
        }
        private set
        {
            _fullPersianAlphabeticDate = value;
            RaisePropertyChanged();
        }
    }


    private string _persianDate;

    public string PersianDate
    {
        get
        {
            return _persianDate;
        }
        private set
        {
            _persianDate = value;
            RaisePropertyChanged();
        }
    }

    private int _persianDay;

    public int PersianDay
    {
        get { return _persianDay; }
        set
        {
            _persianDay = value;
            RaisePropertyChanged();
        }
    }

    private int _persianMonth;

    public int PersianMonth
    {
        get { return _persianMonth; }
        set
        {
            _persianMonth = value;
            RaisePropertyChanged();
        }
    }



    private string _longPersianDateTime;

    public string LongPersianDateTime
    {
        get
        {
            return _longPersianDateTime;
        }
        private set
        {
            _longPersianDateTime = value;
            RaisePropertyChanged();
        }
    }


    public long Ticks
    {
        get
        {
            return ADDate?.Ticks ?? -1;
        }
    }

    public bool HasValue
    {
        get { return ADDate.HasValue; }
    }

    public override int GetHashCode()
    {
        return ADDate.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        var item = obj as SpecialDateTime;

        if (item == null)
        {
            return false;
        }

        return ADDate.Equals(item.ADDate);
    }

    public override string ToString()
    {
        return LongPersianDateTime;
    }

    [JsonIgnore]
    public Action<SpecialDateTime> ChangeAction;

    public event EventHandler<CustomEventArgs<SpecialDateTime>> OnChanged;
}
