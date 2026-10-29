using IRI.Maptor.Core.Satellite.Elements;
using IRI.Maptor.Core.Satellite.Frames;

namespace IRI.Maptor.Core.Satellite.Propagation;

/// <summary>
/// SGP4: the propagator TLE and OMM elements are made for. A port of David Vallado's reference
/// implementation (version 2020-03-12), described in Vallado, Crawford, Hujsak and Kelso, "Revisiting
/// Spacetrack Report #3", AIAA 2006-6753 (https://celestrak.org/publications/AIAA/2006-6753/).
/// </summary>
/// <remarks>
/// <para>
/// The structure, the variable names and the order of operations follow the reference
/// (<c>initl</c>, <c>sgp4init</c>, <c>sgp4</c>, and for deep space <c>dscom</c>, <c>dpper</c>,
/// <c>dsinit</c>, <c>dspace</c>) so the port can be read side by side with it; the results match
/// Vallado's published verification output to well under a centimetre.
/// </para>
/// <para>
/// Both kinds of orbit are handled. Near-Earth orbits (period under 225 minutes: Earth observation,
/// the ISS, the large constellations) use SGP4 proper. Deep-space orbits (GPS, Galileo, Molniya,
/// geostationary) add the lunar-solar perturbations and the 12- and 24-hour resonances, once known as
/// SDP4; see <see cref="IsDeepSpace"/>. The <see cref="Sgp4OperationMode"/> matters for deep space only.
/// </para>
/// <para>One instance per satellite; it is immutable after creation and safe to share between threads.</para>
/// </remarks>
public sealed partial class Sgp4Propagator
{
    /// <summary>Periods from here up are deep space (the SDP4 terms).</summary>
    public const double DeepSpacePeriodMinutes = 225.0;

    private const double TwoPi = 2.0 * Math.PI;

    private const double DegreesToRadians = Math.PI / 180.0;

    private const double X2o3 = 2.0 / 3.0;

    private const double Temp4 = 1.5e-12;

    private readonly GravityConstants _g;

    // epoch elements, radians and radians/minute
    private readonly double _bstar, _ecco, _argpo, _inclo, _mo, _nodeo, _noUnkozai;

    // near-Earth coefficients set up by sgp4init
    private readonly bool _isimp;

    private readonly double _aycof, _con41, _cc1, _cc4, _cc5, _d2, _d3, _d4, _delmo, _eta, _argpdot,
        _omgcof, _sinmao, _t2cof, _t3cof, _t4cof, _t5cof, _x1mth2, _x7thm1, _mdot, _nodedot, _xlcof, _xmcof, _nodecf;

    // deep-space coefficients; null for a near-Earth orbit
    private readonly DeepSpaceTerms? _ds;

    public OrbitalElements Elements { get; }

    public Sgp4GravityModel GravityModel { get; }

    public Sgp4OperationMode OperationMode { get; }

    /// <summary>A deep-space orbit (period of 225 minutes or more): GPS, Galileo, Molniya, geostationary.</summary>
    public bool IsDeepSpace => _ds is not null;

    /// <summary>Un-Kozai'd ("Brouwer") mean motion at epoch, revolutions per day.</summary>
    public double BrouwerMeanMotion => _noUnkozai * 1440.0 / TwoPi;

    /// <summary>Orbital period from the un-Kozai'd mean motion, minutes: what decides near-Earth versus deep space.</summary>
    public double PeriodMinutes => TwoPi / _noUnkozai;

    /// <summary>A propagator for one satellite; near-Earth and deep-space orbits alike.</summary>
    /// <param name="elements">The satellite's mean elements.</param>
    /// <param name="gravityModel">WGS-72, the standard for published elements, unless they say otherwise.</param>
    /// <param name="operationMode">Improved, Vallado's recommendation, or AFSPC to reproduce the original code; deep space only.</param>
    public static Sgp4Propagator Create(OrbitalElements elements, Sgp4GravityModel gravityModel = Sgp4GravityModel.Wgs72,
        Sgp4OperationMode operationMode = Sgp4OperationMode.Improved)
    {
        if (elements is null)
            throw new ArgumentNullException(nameof(elements));

        return new Sgp4Propagator(elements, gravityModel, operationMode);
    }

    /// <summary>
    /// Whether <see cref="Create"/> can propagate these elements. Since deep space is supported this is
    /// true for any elements <see cref="OrbitalElements"/> accepts; it stays for callers written before.
    /// To tell the two kinds of orbit apart, use <see cref="IsDeepSpaceOrbit"/>.
    /// </summary>
    public static bool IsSupported(OrbitalElements elements, Sgp4GravityModel gravityModel = Sgp4GravityModel.Wgs72)
    {
        if (elements is null)
            throw new ArgumentNullException(nameof(elements));

        return true;
    }

    /// <summary>Whether the elements describe a deep-space orbit (period of 225 minutes or more).</summary>
    public static bool IsDeepSpaceOrbit(OrbitalElements elements, Sgp4GravityModel gravityModel = Sgp4GravityModel.Wgs72)
    {
        if (elements is null)
            throw new ArgumentNullException(nameof(elements));

        var g = GravityConstants.For(gravityModel);

        Initl(g, elements.Eccentricity, elements.Inclination * DegreesToRadians, elements.MeanMotion * TwoPi / 1440.0, out var noUnkozai);

        return TwoPi / noUnkozai >= DeepSpacePeriodMinutes;
    }

    private Sgp4Propagator(OrbitalElements elements, Sgp4GravityModel gravityModel, Sgp4OperationMode operationMode)
    {
        Elements = elements;
        GravityModel = gravityModel;
        OperationMode = operationMode;

        _g = GravityConstants.For(gravityModel);

        // twoline2rv: published units to radians and radians per minute
        _bstar = elements.BStar;
        _ecco = elements.Eccentricity;
        _argpo = elements.ArgumentOfPerigee * DegreesToRadians;
        _inclo = elements.Inclination * DegreesToRadians;
        _mo = elements.MeanAnomaly * DegreesToRadians;
        _nodeo = elements.RightAscension * DegreesToRadians;

        var noKozai = elements.MeanMotion * TwoPi / 1440.0;

        // ------------------------ earth constants -----------------------
        var ss = 78.0 / _g.RadiusEarthKm + 1.0;
        var qzms2ttemp = (120.0 - 78.0) / _g.RadiusEarthKm;
        var qzms2t = qzms2ttemp * qzms2ttemp * qzms2ttemp * qzms2ttemp;

        var init = Initl(_g, _ecco, _inclo, noKozai, out _noUnkozai);

        var ao = init.Ao;
        var omeosq = init.Omeosq;
        var rteosq = init.Rteosq;
        var cosio = init.Cosio;
        var cosio2 = init.Cosio2;
        var sinio = init.Sinio;
        var con42 = init.Con42;
        _con41 = init.Con41;

        _isimp = false;

        if (init.Rp < 220.0 / _g.RadiusEarthKm + 1.0)
            _isimp = true;

        var sfour = ss;
        var qzms24 = qzms2t;
        var perige = (init.Rp - 1.0) * _g.RadiusEarthKm;

        // - for perigees below 156 km, s and qoms2t are altered -
        if (perige < 156.0)
        {
            sfour = perige - 78.0;

            if (perige < 98.0)
                sfour = 20.0;

            var qzms24temp = (120.0 - sfour) / _g.RadiusEarthKm;
            qzms24 = qzms24temp * qzms24temp * qzms24temp * qzms24temp;
            sfour = sfour / _g.RadiusEarthKm + 1.0;
        }

        var pinvsq = 1.0 / init.Posq;
        var tsi = 1.0 / (ao - sfour);
        _eta = ao * _ecco * tsi;
        var etasq = _eta * _eta;
        var eeta = _ecco * _eta;
        var psisq = Math.Abs(1.0 - etasq);
        var coef = qzms24 * Math.Pow(tsi, 4.0);
        var coef1 = coef / Math.Pow(psisq, 3.5);
        var cc2 = coef1 * _noUnkozai * (ao * (1.0 + 1.5 * etasq + eeta * (4.0 + etasq)) +
                  0.375 * _g.J2 * tsi / psisq * _con41 * (8.0 + 3.0 * etasq * (8.0 + etasq)));
        _cc1 = _bstar * cc2;

        var cc3 = 0.0;

        if (_ecco > 1.0e-4)
            cc3 = -2.0 * coef * tsi * _g.J3OverJ2 * _noUnkozai * sinio / _ecco;

        _x1mth2 = 1.0 - cosio2;
        _cc4 = 2.0 * _noUnkozai * coef1 * ao * omeosq *
               (_eta * (2.0 + 0.5 * etasq) + _ecco * (0.5 + 2.0 * etasq) - _g.J2 * tsi / (ao * psisq) *
               (-3.0 * _con41 * (1.0 - 2.0 * eeta + etasq * (1.5 - 0.5 * eeta)) +
               0.75 * _x1mth2 * (2.0 * etasq - eeta * (1.0 + etasq)) * Math.Cos(2.0 * _argpo)));
        _cc5 = 2.0 * coef1 * ao * omeosq * (1.0 + 2.75 * (etasq + eeta) + eeta * etasq);

        var cosio4 = cosio2 * cosio2;
        var temp1 = 1.5 * _g.J2 * pinvsq * _noUnkozai;
        var temp2 = 0.5 * temp1 * _g.J2 * pinvsq;
        var temp3 = -0.46875 * _g.J4 * pinvsq * pinvsq * _noUnkozai;

        _mdot = _noUnkozai + 0.5 * temp1 * rteosq * _con41 + 0.0625 * temp2 * rteosq * (13.0 - 78.0 * cosio2 + 137.0 * cosio4);
        _argpdot = -0.5 * temp1 * con42 + 0.0625 * temp2 * (7.0 - 114.0 * cosio2 + 395.0 * cosio4) +
                   temp3 * (3.0 - 36.0 * cosio2 + 49.0 * cosio4);

        var xhdot1 = -temp1 * cosio;
        _nodedot = xhdot1 + (0.5 * temp2 * (4.0 - 19.0 * cosio2) + 2.0 * temp3 * (3.0 - 7.0 * cosio2)) * cosio;
        var xpidot = _argpdot + _nodedot;

        _omgcof = _bstar * cc3 * Math.Cos(_argpo);
        _xmcof = 0.0;

        if (_ecco > 1.0e-4)
            _xmcof = -X2o3 * coef * _bstar / eeta;

        _nodecf = 3.5 * omeosq * xhdot1 * _cc1;
        _t2cof = 1.5 * _cc1;

        // sgp4fix for divide by zero with xinco = 180 deg
        _xlcof = Math.Abs(cosio + 1.0) > 1.5e-12
            ? -0.25 * _g.J3OverJ2 * sinio * (3.0 + 5.0 * cosio) / (1.0 + cosio)
            : -0.25 * _g.J3OverJ2 * sinio * (3.0 + 5.0 * cosio) / Temp4;

        _aycof = -0.5 * _g.J3OverJ2 * sinio;

        var delmotemp = 1.0 + _eta * Math.Cos(_mo);
        _delmo = delmotemp * delmotemp * delmotemp;
        _sinmao = Math.Sin(_mo);
        _x7thm1 = 7.0 * cosio2 - 1.0;

        // --------------- deep space initialization -------------
        if (TwoPi / _noUnkozai >= DeepSpacePeriodMinutes)
        {
            _isimp = true;
            var tc = 0.0;
            var inclm = _inclo;

            // days from 0 Jan 1950, as the reference's jdsatepoch + jdsatepochF - 2433281.5: whole days plus the day fraction
            var sinceRef = elements.Epoch - new DateTime(1949, 12, 31, 0, 0, 0, DateTimeKind.Utc);
            var epoch = (double)(sinceRef.Ticks / TimeSpan.TicksPerDay) + (double)(sinceRef.Ticks % TimeSpan.TicksPerDay) / TimeSpan.TicksPerDay;

            var ds = new DeepSpaceTerms { Gsto = SiderealAtEpoch(epoch, operationMode) };

            var c = Dscom(epoch, _ecco, _argpo, tc, _inclo, _nodeo, _noUnkozai, ds);

            // the reference's dpper call here (init = 'y') leaves the elements unchanged: the periodics are zero at epoch

            var em = c.Em;
            var argpm = 0.0;
            var nodem = 0.0;
            var mm = 0.0;
            var nm = c.Nm;

            Dsinit(ds, _g.Xke, c, _argpo, 0.0, tc, _mo, _mdot, _noUnkozai, _nodeo, _nodedot, xpidot, _ecco, init.Eccsq,
                ref em, ref argpm, ref inclm, ref mm, ref nm, ref nodem, out _);

            _ds = ds;
        }

        // ----------- set variables if not deep space -----------
        if (!_isimp)
        {
            var cc1sq = _cc1 * _cc1;
            _d2 = 4.0 * ao * tsi * cc1sq;
            var temp = _d2 * tsi * _cc1 / 3.0;
            _d3 = (17.0 * ao + sfour) * temp;
            _d4 = 0.5 * temp * ao * tsi * (221.0 * ao + 31.0 * sfour) * _cc1;
            _t3cof = _d2 + 2.0 * cc1sq;
            _t4cof = 0.25 * (3.0 * _d3 + _cc1 * (12.0 * _d2 + 10.0 * cc1sq));
            _t5cof = 0.2 * (3.0 * _d4 + 12.0 * _cc1 * _d3 + 6.0 * _d2 * _d2 + 15.0 * cc1sq * (2.0 * _d2 + cc1sq));
        }

    }

    /// <summary>The state at a UTC instant.</summary>
    public PropagationResult Propagate(DateTime utc)
    {
        utc = utc.Kind == DateTimeKind.Local ? utc.ToUniversalTime() : DateTime.SpecifyKind(utc, DateTimeKind.Utc);

        return Propagate((utc - Elements.Epoch).TotalMinutes, utc);
    }

    /// <summary>The state <paramref name="minutesSinceEpoch"/> minutes after (or before, if negative) the epoch.</summary>
    public PropagationResult PropagateMinutes(double minutesSinceEpoch)
        => Propagate(minutesSinceEpoch, Elements.Epoch.AddTicks((long)Math.Round(minutesSinceEpoch * TimeSpan.TicksPerMinute)));

    private PropagationResult Propagate(double tsince, DateTime utc)
    {
        var status = Sgp4Status.Ok;

        var mrt = 0.0;

        var r = default(Vector3);

        var v = default(Vector3);

        var vkmpersec = _g.RadiusEarthKm * _g.Xke / 60.0;

        var t = tsince;

        // ------- update for secular gravity and atmospheric drag -----
        var xmdf = _mo + _mdot * t;
        var argpdf = _argpo + _argpdot * t;
        var nodedf = _nodeo + _nodedot * t;
        var argpm = argpdf;
        var mm = xmdf;
        var t2 = t * t;
        var nodem = nodedf + _nodecf * t2;
        var tempa = 1.0 - _cc1 * t;
        var tempe = _bstar * _cc4 * t;
        var templ = _t2cof * t2;

        if (!_isimp)
        {
            var delomg = _omgcof * t;
            var delmtemp = 1.0 + _eta * Math.Cos(xmdf);
            var delm = _xmcof * (delmtemp * delmtemp * delmtemp - _delmo);
            var temp = delomg + delm;
            mm = xmdf + temp;
            argpm = argpdf - temp;
            var t3 = t2 * t;
            var t4 = t3 * t;
            tempa = tempa - _d2 * t2 - _d3 * t3 - _d4 * t4;
            tempe = tempe + _bstar * _cc5 * (Math.Sin(mm) - _sinmao);
            templ = templ + _t3cof * t3 + t4 * (_t4cof + t * _t5cof);
        }

        var nm = _noUnkozai;
        var em = _ecco;
        var inclm = _inclo;

        if (_ds is not null)
        {
            var tc = t;
            Dspace(_ds, _argpo, _argpdot, t, tc, _noUnkozai, ref em, ref argpm, ref inclm, ref mm, ref nodem, out _, ref nm);
        }

        if (nm <= 0.0)
            status = Sgp4Status.MeanMotionNotPositive;

        var am = Math.Pow(_g.Xke / nm, X2o3) * tempa * tempa;
        nm = _g.Xke / Math.Pow(am, 1.5);
        em = em - tempe;

        // fix tolerance for error recognition
        if (em >= 1.0 || em < -0.001)
            status = Sgp4Status.MeanEccentricityOutOfRange;

        // avoid a division by zero
        if (em < 1.0e-6)
            em = 1.0e-6;

        mm = mm + _noUnkozai * templ;
        var xlm = mm + argpm + nodem;

        nodem %= TwoPi;
        argpm %= TwoPi;
        xlm %= TwoPi;
        mm = (xlm - argpm - nodem) % TwoPi;

        // ----------------- compute extra mean quantities -------------
        var sinim = Math.Sin(inclm);
        var cosim = Math.Cos(inclm);

        // -------------------- add lunar-solar periodics --------------
        var ep = em;
        var xincp = inclm;
        var argpp = argpm;
        var nodep = nodem;
        var mp = mm;
        var sinip = sinim;
        var cosip = cosim;

        // the reference stores these in satrec on every deep-space call; here they stay local
        var aycof = _aycof;
        var xlcof = _xlcof;
        var con41 = _con41;
        var x1mth2 = _x1mth2;
        var x7thm1 = _x7thm1;

        if (_ds is not null)
        {
            Dpper(_ds, t, OperationMode, ref ep, ref xincp, ref nodep, ref argpp, ref mp);

            if (xincp < 0.0)
            {
                xincp = -xincp;
                nodep = nodep + Math.PI;
                argpp = argpp - Math.PI;
            }

            if (ep < 0.0 || ep > 1.0)
                status = Sgp4Status.PerturbedEccentricityOutOfRange;

            // -------------------- long period periodics ------------------
            sinip = Math.Sin(xincp);
            cosip = Math.Cos(xincp);
            aycof = -0.5 * _g.J3OverJ2 * sinip;

            // sgp4fix for divide by zero for xincp = 180 deg
            xlcof = Math.Abs(cosip + 1.0) > 1.5e-12
                ? -0.25 * _g.J3OverJ2 * sinip * (3.0 + 5.0 * cosip) / (1.0 + cosip)
                : -0.25 * _g.J3OverJ2 * sinip * (3.0 + 5.0 * cosip) / Temp4;
        }

        var axnl = ep * Math.Cos(argpp);
        var tempLp = 1.0 / (am * (1.0 - ep * ep));
        var aynl = ep * Math.Sin(argpp) + tempLp * aycof;
        var xl = mp + argpp + nodep + tempLp * xlcof * axnl;

        // --------------------- solve kepler's equation ---------------
        var u = (xl - nodep) % TwoPi;
        var eo1 = u;
        var tem5 = 9999.9;
        var ktr = 1;
        var coseo1 = 0.0;
        var sineo1 = 0.0;

        // sgp4fix for kepler iteration: the following iteration needs better limits on corrections
        while (Math.Abs(tem5) >= 1.0e-12 && ktr <= 10)
        {
            sineo1 = Math.Sin(eo1);
            coseo1 = Math.Cos(eo1);
            tem5 = 1.0 - coseo1 * axnl - sineo1 * aynl;
            tem5 = (u - aynl * coseo1 + axnl * sineo1 - eo1) / tem5;

            if (Math.Abs(tem5) >= 0.95)
                tem5 = tem5 > 0.0 ? 0.95 : -0.95;

            eo1 += tem5;
            ktr++;
        }

        // ------------- short period preliminary quantities -----------
        var ecose = axnl * coseo1 + aynl * sineo1;
        var esine = axnl * sineo1 - aynl * coseo1;
        var el2 = axnl * axnl + aynl * aynl;
        var pl = am * (1.0 - el2);

        if (pl < 0.0)
        {
            status = Sgp4Status.SemiLatusRectumNegative;
        }
        else
        {
            var rl = am * (1.0 - ecose);
            var rdotl = Math.Sqrt(am) * esine / rl;
            var rvdotl = Math.Sqrt(pl) / rl;
            var betal = Math.Sqrt(1.0 - el2);
            var temp = esine / (1.0 + betal);
            var sinu = am / rl * (sineo1 - aynl - axnl * temp);
            var cosu = am / rl * (coseo1 - axnl + aynl * temp);
            var su = Math.Atan2(sinu, cosu);
            var sin2u = (cosu + cosu) * sinu;
            var cos2u = 1.0 - 2.0 * sinu * sinu;
            temp = 1.0 / pl;
            var temp1 = 0.5 * _g.J2 * temp;
            var temp2 = temp1 * temp;

            // -------------- update for short period periodics ------------
            if (_ds is not null)
            {
                var cosisq = cosip * cosip;
                con41 = 3.0 * cosisq - 1.0;
                x1mth2 = 1.0 - cosisq;
                x7thm1 = 7.0 * cosisq - 1.0;
            }

            mrt = rl * (1.0 - 1.5 * temp2 * betal * con41) + 0.5 * temp1 * x1mth2 * cos2u;
            su = su - 0.25 * temp2 * x7thm1 * sin2u;
            var xnode = nodep + 1.5 * temp2 * cosip * sin2u;
            var xinc = xincp + 1.5 * temp2 * cosip * sinip * cos2u;
            var mvt = rdotl - nm * temp1 * x1mth2 * sin2u / _g.Xke;
            var rvdot = rvdotl + nm * temp1 * (x1mth2 * cos2u + 1.5 * con41) / _g.Xke;

            // --------------------- orientation vectors -------------------
            var sinsu = Math.Sin(su);
            var cossu = Math.Cos(su);
            var snod = Math.Sin(xnode);
            var cnod = Math.Cos(xnode);
            var sini = Math.Sin(xinc);
            var cosi = Math.Cos(xinc);
            var xmx = -snod * cosi;
            var xmy = cnod * cosi;
            var ux = xmx * sinsu + cnod * cossu;
            var uy = xmy * sinsu + snod * cossu;
            var uz = sini * sinsu;
            var vx = xmx * cossu - cnod * sinsu;
            var vy = xmy * cossu - snod * sinsu;
            var vz = sini * cossu;

            // --------- position and velocity (in km and km/sec) ----------
            r = new Vector3(mrt * ux * _g.RadiusEarthKm, mrt * uy * _g.RadiusEarthKm, mrt * uz * _g.RadiusEarthKm);
            v = new Vector3((mvt * ux + rvdot * vx) * vkmpersec, (mvt * uy + rvdot * vy) * vkmpersec, (mvt * uz + rvdot * vz) * vkmpersec);
        }

        // sgp4fix for decaying satellites
        if (mrt < 1.0)
            status = Sgp4Status.Decayed;

        return new PropagationResult(utc, tsince, status, r, v);
    }

    private struct InitlResult
    {
        public double Ao, Con41, Con42, Cosio, Cosio2, Eccsq, Omeosq, Posq, Rp, Rteosq, Sinio;
    }

    /// <summary>Vallado's initl: epoch quantities and the un-Kozai'd mean motion (the deep-space sidereal angle is <see cref="SiderealAtEpoch"/>).</summary>
    private static InitlResult Initl(GravityConstants g, double ecco, double inclo, double noKozai, out double noUnkozai)
    {
        // ------------- calculate auxillary epoch quantities ----------
        var eccsq = ecco * ecco;
        var omeosq = 1.0 - eccsq;
        var rteosq = Math.Sqrt(omeosq);
        var cosio = Math.Cos(inclo);
        var cosio2 = cosio * cosio;

        // ------------------ un-kozai the mean motion -----------------
        var ak = Math.Pow(g.Xke / noKozai, X2o3);
        var d1 = 0.75 * g.J2 * (3.0 * cosio2 - 1.0) / (rteosq * omeosq);
        var del = d1 / (ak * ak);
        var adel = ak * (1.0 - del * del - del * (1.0 / 3.0 + 134.0 * del * del / 81.0));
        del = d1 / (adel * adel);
        noUnkozai = noKozai / (1.0 + del);

        var ao = Math.Pow(g.Xke / noUnkozai, X2o3);
        var sinio = Math.Sin(inclo);
        var po = ao * omeosq;
        var con42 = 1.0 - 5.0 * cosio2;

        return new InitlResult
        {
            Ao = ao,
            Con41 = -con42 - cosio2 - cosio2,
            Con42 = con42,
            Cosio = cosio,
            Cosio2 = cosio2,
            Eccsq = eccsq,
            Omeosq = omeosq,
            Posq = po * po,
            Rp = ao * (1.0 - ecco),
            Rteosq = rteosq,
            Sinio = sinio,
        };
    }
}
