// besmellahe rahmane rahim
// Allahoma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Metrics;


public static class UnitConversion
{
    //Angular Units
    public static double DegreeToGrade(double degreeValue)
    {

        return degreeValue * 200 / 180;

    }

    public static double DegreeToRadian(double degreeValue)
    {

        return degreeValue * Math.PI / 180;

    }

    public static double GradeToDegree(double gradeValue)
    {

        return gradeValue * 180 / 200;

    }

    public static double GradeToRadian(double gradeValue)
    {

        return gradeValue * Math.PI / 200;

    }

    public static double RadianToDegree(double radianValue)
    {

        return radianValue * 180 / Math.PI;

    }

    public static double RadianToGrade(double radianValue)
    {

        return radianValue * 200 / Math.PI;

    }

    //Linear Units
    public static double MeterToMile(double meterValue)
    {
        return meterValue * 3937 / (1200 * 5280);
    }

    public static double MeterToYard(double meterValue)
    {
        return meterValue * 3937 / (1200 * 3);
    }

    public static double MeterToFoot(double meterValue)
    {
        return meterValue * 3937 / 1200;
    }

    public static double MeterToInch(double meterValue)
    {
        return meterValue * (3937 * 12) / 1200;
    }

    public static double MeterToRod(double meterValue)
    {
        return meterValue * (3937 * 12) / (1200 * 198);
    }

    public static double MeterToChain(double meterValue)
    {
        return meterValue * (3937 * 12) / (1200 * 198 * 4);
    }

    public static double MileToMeter(double mileValue)
    {
        return mileValue * (5280 * 1200) / 3937;
    }

    public static double MileToYard(double mileValue)
    {
        return mileValue * 1760;
    }

    public static double MileToFoot(double mileValue)
    {
        return mileValue * 5280;
    }

    public static double MileToInch(double mileValue)
    {
        return mileValue * 5280 * 12;
    }

    public static double MileToRod(double mileValue)
    {
        return mileValue * 320;
    }

    public static double MileToChain(double mileValue)
    {
        return mileValue * 80;
    }

    public static double YardToMeter(double yardValue)
    {
        return yardValue * (3 * 1200) / 3937;
    }

    public static double YardToMile(double yardValue)
    {
        return yardValue / 1760;
    }

    public static double YardToFoot(double yardValue)
    {
        return yardValue * 3;
    }

    public static double YardToInch(double yardValue)
    {
        return yardValue * 36;
    }

    public static double YardToRod(double yardValue)
    {
        return yardValue * 36 / 198;
    }

    public static double YardToChain(double yardValue)
    {
        return yardValue * 36 / (198 * 4);
    }

    public static double FootToMeter(double footValue)
    {
        return footValue * 1200 / 3937;
    }

    public static double FootToMile(double footValue)
    {
        return footValue / 5280;
    }

    public static double FootToYard(double footValue)
    {
        return footValue / 3;
    }

    public static double FootToInch(double footValue)
    {
        return footValue * 12;
    }

    public static double FootToRod(double footValue)
    {
        return footValue * 12 / 198;
    }

    public static double FootToChain(double footValue)
    {
        return footValue * 12 / (198 * 4);
    }

    public static double InchToMeter(double inchValue)
    {
        return inchValue * 1200 / (3937 * 12);
    }

    public static double InchToMile(double inchValue)
    {
        return inchValue / (5280 * 12);
    }

    public static double InchToYard(double inchValue)
    {
        return inchValue / 36;
    }

    public static double InchToFoot(double inchValue)
    {
        return inchValue / 12;
    }

    public static double InchToRod(double inchValue)
    {
        return inchValue / 198;
    }

    public static double InchToChain(double inchValue)
    {
        return inchValue / (198 * 4);
    }

    public static double RodToMeter(double rodValue)
    {
        return rodValue * (1200 * 198) / (3937 * 12);
    }

    public static double RodToMile(double rodValue)
    {
        return rodValue / 320;
    }

    public static double RodToYard(double rodValue)
    {
        return rodValue * 198 / 36;
    }

    public static double RodToFoot(double rodValue)
    {
        return rodValue * 198 / 12;
    }

    public static double RodToInch(double rodValue)
    {
        return rodValue * 198;
    }

    public static double RodToChain(double rodValue)
    {
        return rodValue / 4;
    }

    public static double ChainToMeter(double chainValue)
    {
        return chainValue * (1200 * 198 * 4) / (3937 * 12);
    }

    public static double ChainToMile(double chainValue)
    {
        return chainValue / 80;
    }

    public static double ChainToYard(double chainValue)
    {
        return chainValue * 198 * 4 / 36;
    }

    public static double ChainToFoot(double chainValue)
    {
        return chainValue * 66;
    }

    public static double ChainToInch(double chainValue)
    {
        return chainValue * 198 * 4;
    }

    public static double ChainToRod(double chainValue)
    {
        return chainValue * 4;
    }

}

//public static DegreeMinuteSecond ToDegreeMinuteSecond(double degreeValue)
//{

//    short sexagesimalDegree = (short)Math.Floor(degreeValue);

//    byte sexagesimalMinute = (byte)Math.Floor((degreeValue - sexagesimalDegree) * 60);

//    byte sexagesimalSecond = (byte)Math.Floor(((degreeValue - sexagesimalDegree) * 60 - sexagesimalMinute) * 60);

//    return new DegreeMinuteSecond(sexagesimalDegree, sexagesimalMinute, sexagesimalSecond);

//}

///// <summary>
///// Calculate the equivalent decimal value
///// </summary>
///// <param name="degreeValue">must be positive</param>
///// <param name="minuteValue">must be positive</param>
///// <param name="secondValue">must be positive</param>
///// <returns></returns>
//public static double FromDegreeMinuteSecond(short degreeValue, byte minuteValue, byte secondValue)
//{
//    return (degreeValue + minuteValue / 60 + secondValue / 3600);
//}

//public static double FromDegreeMinuteSecond(DegreeMinuteSecond value)
//{
//    return (value.Sign * FromDegreeMinuteSecond(value.DegreeValue , value.MinuteValue , value.SecondValue));
//}

//public static GradeMinuteSecond ToGradeMinuteSecond(double gradeValue)
//{

//    short centesimalDegree = (short)Math.Floor(gradeValue);

//    byte centesimalMinute = (byte)Math.Floor((gradeValue - centesimalDegree) * 100);

//    byte centesimalSecond = (byte)Math.Floor(((gradeValue - centesimalDegree) * 100 - centesimalMinute) * 100);

//    return new GradeMinuteSecond(centesimalDegree, centesimalMinute, centesimalSecond);

//}

//public static double FromGradeMinuteSecond(GradeMinuteSecond value)
//{

//    return (value.GradeValue + value.MinuteValue / 100 + value.SecondValue / 10000);

//}
