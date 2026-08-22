using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Extensions;

public static class DateTimeExtensions
{
    internal static readonly DateTime _midValidPersianDateTime = new DateTime(622, 3, 22);

    public static long AsUnixTimestamp(this DateTime time)
    {
        return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }

    private static readonly System.Globalization.PersianCalendar _calendar = new System.Globalization.PersianCalendar();

    public static bool IsAM(this DateTime dateTime)
    {
        return dateTime.ToString("tt").ToUpper() == "AM";
    }

    public static string GetPersianAmPm(this DateTime dateTime)
    {
        return dateTime.IsAM() ? "ق.ظ" : "ب.ظ";
    }

    public static string ToPersianAlphabeticDate(this DateTime dateTime, bool includeDayOfWeek = true, bool useFarsiNumbers = true)
    {
        //to avoid ArgumentOutOfRangeException
        if (dateTime < _midValidPersianDateTime)
            return _midValidPersianDateTime.ToPersianAlphabeticDate(includeDayOfWeek, useFarsiNumbers);

        var year = _calendar.GetYear(dateTime);

        var month = dateTime.GetPersianMonthName();

        var day = _calendar.GetDayOfMonth(dateTime);

        var result = includeDayOfWeek ?
            FormattableString.Invariant($"{dateTime.GetPersianDayOfWeekName()} {day:00} {month} {year:0000}") :
            FormattableString.Invariant($"{day:00} {month} {year:0000}");

        return useFarsiNumbers ? result.LatinNumbersToFarsiNumbers() : result;
    }

    public static string ToLongPersianDateTime(this DateTime dateTime, bool useFarsiNumbers = true)
    {
        //to avoid ArgumentOutOfRangeException
        if (dateTime < _midValidPersianDateTime)
            return _midValidPersianDateTime.ToLongPersianDateTime();

        var year = _calendar.GetYear(dateTime);

        var month = _calendar.GetMonth(dateTime);

        var day = _calendar.GetDayOfMonth(dateTime);

        var result = FormattableString.Invariant($" تاریخ {day:00}-{month:00}-{year:0000}  ساعت {dateTime.Hour:00}:{dateTime.Minute:00}  {dateTime.GetPersianAmPm()} ");

        return useFarsiNumbers ? result.LatinNumbersToFarsiNumbers() : result;

    }

    public static string ToLongPersianDateTimeSimple(this DateTime dateTime, bool useFarsiNumbers = true)
    {
        //to avoid ArgumentOutOfRangeException
        if (dateTime < _midValidPersianDateTime)
            return _midValidPersianDateTime.ToLongPersianDateTimeSimple();

        var result = FormattableString.Invariant(
            $"{_calendar.GetYear(dateTime):0000}/{_calendar.GetMonth(dateTime):00}/{_calendar.GetDayOfMonth(dateTime):00}-{_calendar.GetHour(dateTime):00}:{_calendar.GetMinute(dateTime):00}:{_calendar.GetSecond(dateTime):00}");

        return useFarsiNumbers ? result.LatinNumbersToFarsiNumbers() : result;
    }

    public static string ToPersianDate(this DateTime dateTime, bool useFarsiNumbers = true)
    {
        //to avoid ArgumentOutOfRangeException
        if (dateTime < _midValidPersianDateTime)
            return _midValidPersianDateTime.ToPersianDate();

        var result = FormattableString.Invariant($"{_calendar.GetYear(dateTime):0000}/{_calendar.GetMonth(dateTime):00}/{_calendar.GetDayOfMonth(dateTime):00}");

        return useFarsiNumbers ? result.LatinNumbersToFarsiNumbers() : result;
    }

    public static int GetPersianDayOfMonth(this DateTime dateTime)
    {
        return _calendar.GetDayOfMonth(dateTime);
    }

    public static int GetPersianMonth(this DateTime dateTime)
    {
        return _calendar.GetMonth(dateTime);
    }

    public static int GetPersianYear(this DateTime dateTime)
    {
        return _calendar.GetYear(dateTime);
    }

    public static string ToPersianYearMonth(this DateTime dateTime, bool useFarsiNumbers = true)
    {
        //to avoid ArgumentOutOfRangeException
        if (dateTime < _midValidPersianDateTime)
            return _midValidPersianDateTime.ToPersianYearMonth(useFarsiNumbers);

        var result = FormattableString.Invariant($"{dateTime.GetPersianMonthName()} {_calendar.GetYear(dateTime)}");

        return useFarsiNumbers ? result.LatinNumbersToFarsiNumbers() : result; ;
    }

    public static DateTime GetBeginningOfThePersianYear(this DateTime dateTime)
    {
        //to avoid ArgumentOutOfRangeException
        if (dateTime < _midValidPersianDateTime)
            return _midValidPersianDateTime.GetBeginningOfThePersianYear();

        var year = _calendar.GetYear(dateTime);

        return new DateTime(year, 1, 1, _calendar);
    }

    public static DateTime GetBeginningOfThePersianMonth(this DateTime dateTime)
    {
        //to avoid ArgumentOutOfRangeException
        if (dateTime < _midValidPersianDateTime)
            return _midValidPersianDateTime.GetBeginningOfThePersianMonth();

        var year = _calendar.GetYear(dateTime);

        var month = _calendar.GetMonth(dateTime);

        return new DateTime(year, month, 1, _calendar);
    }

    public static DateTime GetEndOfThePersianMonth(this DateTime dateTime)
    {
        //to avoid ArgumentOutOfRangeException
        if (dateTime < _midValidPersianDateTime)
            return _midValidPersianDateTime.GetBeginningOfThePersianMonth();

        var year = _calendar.GetYear(dateTime);

        var month = _calendar.GetMonth(dateTime);

        var daysInMonth = _calendar.GetDaysInMonth(year, month);

        return new DateTime(year, month, 1, _calendar).AddDays(daysInMonth);
    }

    public static DateTime GetBeginningOfThePersianWeek(this DateTime dateTime)
    {
        //to avoid ArgumentOutOfRangeException
        if (dateTime < _midValidPersianDateTime)
            return _midValidPersianDateTime.GetBeginningOfThePersianWeek();

        var dayOfWeek = ((int)_calendar.GetDayOfWeek(dateTime) + 1) % 7;

        return dateTime.Date.AddDays(-dayOfWeek);
    }

    public static string GetPersianMonthName(this DateTime dateTime)
    {
        //to avoid ArgumentOutOfRangeException
        if (dateTime < _midValidPersianDateTime)
            return _midValidPersianDateTime.GetPersianMonthName();

        var monthIndex = _calendar.GetMonth(dateTime) - 1;

        return persianMonths[monthIndex];
    }

    public static string GetPersianDayOfWeekName(this DateTime dateTime)
    {
        //to avoid ArgumentOutOfRangeException
        if (dateTime < _midValidPersianDateTime)
            return _midValidPersianDateTime.GetPersianDayOfWeekName();

        var dayOfWeek = _calendar.GetDayOfWeek(dateTime);

        return GetPersianDayOfWeekName(dayOfWeek);
    }

    public static string GetPersianDayOfWeekName(DayOfWeek dayOfWeek)
    {
        return persianDaysOfWeek[((int)dayOfWeek + 1) % 7];
    }

    static readonly string[] persianMonths = new string[]
    {
        "فروردین",
        "اردیبهشت",
        "خرداد",
        "تیر",
        "مرداد",
        "شهریور",
        "مهر",
        "آبان",
        "آذر",
        "دی",
        "بهمن",
        "اسفند",
    };

    static readonly string[] persianDaysOfWeek = new string[]
    {
        "شنبه",
        "یک‌شنبه",
        "دوشنبه",
        "سه‌شنبه",
        "چهارشنبه",
        "پنج‌شنبه",
        "جمعه",
    };

    public static readonly DateTime JulianDate = new DateTime(1970, 1, 1, 0, 0, 0);

    public static string GetPersianElapsedDateCoarse(this DateTime dateTime)
    {
        var interval = DateTime.Now - dateTime;

        var result = string.Empty;

        if (interval.TotalSeconds > 0)
        {
            if (interval.Days == 0)
            {
                return "امروز";
            }
            else if (interval.Days == 1)
            {
                return "دیروز";
            }
            if (interval.Days < 7)
            {
                result = $"۷ روز گذشته";
            }
            else if (interval.Days < 30)
            {
                result = $"یک ماه گذشته";
            }
            else if (interval.Days < 366)
            {
                result = $"یک سال گذشته";
            }
            else
            {
                result = $"مدت‌ها پیش";
            }
        }
        else
        {
            // 1400.02.14
            interval = interval.Negate();

            if (interval.Days == 0) { result = "امروز"; }
            else if (interval.Days == 1) { result = "فردا"; }
            else if (interval.Days == 2) { result = "پس فردا"; }
            else if (interval.Days < 7) { result = "۷ روز آتی"; }
            else if (interval.Days < 30) { result = "ماه آینده"; }
            else { result = "آینده"; }
        }
        return result.LatinNumbersToFarsiNumbers();
    }

    public static string GetPersianElapsedDateNormal(this DateTime dateTime)
    {
        var interval = DateTime.Now - dateTime;

        var result = string.Empty;

        // 1400.02.14
        if (interval.TotalSeconds < 0)
            return "آینده";

        if (interval.Days == 0)
        {
            return "امروز";
        }
        else if (interval.Days == 1)
        {
            return "دیروز";
        }
        if (interval.Days < 7)
        {
            result = $"{interval.Days} روز پیش";
        }
        else if (interval.Days < 30)
        {
            result = $"{interval.Days / 7} هفته پیش";
        }
        else if (interval.Days < 366)
        {
            result = $"{interval.Days / 30} ماه پیش";
        }
        else
        {
            result = $"{interval.Days / 360} سال پیش";
        }

        return result.LatinNumbersToFarsiNumbers();
    }

    public static string GetPersianElapsedTimeFine(this DateTime dateTime)
    {
        var duration = DateTime.Now - dateTime;

        // 1400.02.14
        if (duration.TotalSeconds < 0)
            return "آینده";

        var result = string.Empty;

        if (duration < TimeSpan.FromSeconds(60))
        {
            result = $"{duration.Seconds} ثانیه پیش";
        }
        else if (duration < TimeSpan.FromMinutes(60))
        {
            result = $"{duration.Minutes} دقیقه پیش";
        }
        else if (duration < TimeSpan.FromHours(24))
        {
            result = $"{duration.Hours} ساعت پیش";
        }
        else if (duration < TimeSpan.FromDays(7))
        {
            result = $"هفتهٔ گذشته";
        }
        else if (duration < TimeSpan.FromDays(30))
        {
            result = $"{duration.Days} روز پیش";
        }
        else if (duration < TimeSpan.FromMinutes(365))
        {
            result = $"{duration.Days / 30} ماه پیش";
        }
        else
        {
            result = $"{duration.Days / 365} سال پیش";
        }

        return result.LatinNumbersToFarsiNumbers();
    }
}
