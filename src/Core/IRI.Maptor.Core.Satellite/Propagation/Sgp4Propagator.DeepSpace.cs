namespace IRI.Maptor.Core.Satellite.Propagation;

/// <summary>
/// The deep-space part of SGP4 (once called SDP4), for periods of 225 minutes or more: the lunar-solar
/// perturbations and the resonances of 12-hour and 24-hour orbits. A port of Vallado's <c>dscom</c>,
/// <c>dpper</c>, <c>dsinit</c>, <c>dspace</c> and <c>gstime</c> (version 2020-03-12), with the same
/// names and order of operations, so it can be read side by side with the reference.
/// </summary>
public sealed partial class Sgp4Propagator
{
    /// <summary>The deep-space coefficients set up once by <c>sgp4init</c>; never changed afterwards.</summary>
    private sealed class DeepSpaceTerms
    {
        public int Irez;

        public double D2201, D2211, D3210, D3222, D4410, D4422, D5220, D5232, D5421, D5433,
            Dedt, Del1, Del2, Del3, Didt, Dmdt, Dnodt, Domdt, E3, Ee2, Peo, Pgho, Pho, Pinco,
            Plo, Se2, Se3, Sgh2, Sgh3, Sgh4, Sh2, Sh3, Si2, Si3, Sl2, Sl3, Sl4, Gsto, Xfact,
            Xgh2, Xgh3, Xgh4, Xh2, Xh3, Xi2, Xi3, Xl2, Xl3, Xl4, Xlamo, Zmol, Zmos;
    }

    /// <summary>What <c>dscom</c> returns besides the coefficients it stores in <see cref="DeepSpaceTerms"/>.</summary>
    private struct DscomOutput
    {
        public double Snodm, Cnodm, Sinim, Cosim, Sinomm, Cosomm, Day, Em, Emsq, Gam, Rtemsq,
            S1, S2, S3, S4, S5, S6, S7, Ss1, Ss2, Ss3, Ss4, Ss5, Ss6, Ss7, Sz1, Sz2, Sz3,
            Sz11, Sz12, Sz13, Sz21, Sz22, Sz23, Sz31, Sz32, Sz33, Nm,
            Z1, Z2, Z3, Z11, Z12, Z13, Z21, Z22, Z23, Z31, Z32, Z33;
    }

    /// <summary>Vallado's dscom: deep-space common items for the secular and periodic terms.</summary>
    private static DscomOutput Dscom(double epoch, double ep, double argpp, double tc, double inclp, double nodep, double np, DeepSpaceTerms ds)
    {
        /* -------------------------- constants ------------------------- */
        const double zes = 0.01675;
        const double zel = 0.05490;
        const double c1ss = 2.9864797e-6;
        const double c1l = 4.7968065e-7;
        const double zsinis = 0.39785416;
        const double zcosis = 0.91744867;
        const double zcosgs = 0.1945905;
        const double zsings = -0.98088458;

        var o = new DscomOutput();

        o.Nm = np;
        o.Em = ep;
        o.Snodm = Math.Sin(nodep);
        o.Cnodm = Math.Cos(nodep);
        o.Sinomm = Math.Sin(argpp);
        o.Cosomm = Math.Cos(argpp);
        o.Sinim = Math.Sin(inclp);
        o.Cosim = Math.Cos(inclp);
        o.Emsq = o.Em * o.Em;
        var betasq = 1.0 - o.Emsq;
        o.Rtemsq = Math.Sqrt(betasq);

        /* ----------------- initialize lunar solar terms --------------- */
        ds.Peo = 0.0;
        ds.Pinco = 0.0;
        ds.Plo = 0.0;
        ds.Pgho = 0.0;
        ds.Pho = 0.0;
        o.Day = epoch + 18261.5 + tc / 1440.0;
        var xnodce = (4.5236020 - 9.2422029e-4 * o.Day) % TwoPi;
        var stem = Math.Sin(xnodce);
        var ctem = Math.Cos(xnodce);
        var zcosil = 0.91375164 - 0.03568096 * ctem;
        var zsinil = Math.Sqrt(1.0 - zcosil * zcosil);
        var zsinhl = 0.089683511 * stem / zsinil;
        var zcoshl = Math.Sqrt(1.0 - zsinhl * zsinhl);
        o.Gam = 5.8351514 + 0.0019443680 * o.Day;
        var zx = 0.39785416 * stem / zsinil;
        var zy = zcoshl * ctem + 0.91744867 * zsinhl * stem;
        zx = Math.Atan2(zx, zy);
        zx = o.Gam + zx - xnodce;
        var zcosgl = Math.Cos(zx);
        var zsingl = Math.Sin(zx);

        /* ------------------------- do solar terms --------------------- */
        var zcosg = zcosgs;
        var zsing = zsings;
        var zcosi = zcosis;
        var zsini = zsinis;
        var zcosh = o.Cnodm;
        var zsinh = o.Snodm;
        var cc = c1ss;
        var xnoi = 1.0 / o.Nm;

        for (var lsflg = 1; lsflg <= 2; lsflg++)
        {
            var a1 = zcosg * zcosh + zsing * zcosi * zsinh;
            var a3 = -zsing * zcosh + zcosg * zcosi * zsinh;
            var a7 = -zcosg * zsinh + zsing * zcosi * zcosh;
            var a8 = zsing * zsini;
            var a9 = zsing * zsinh + zcosg * zcosi * zcosh;
            var a10 = zcosg * zsini;
            var a2 = o.Cosim * a7 + o.Sinim * a8;
            var a4 = o.Cosim * a9 + o.Sinim * a10;
            var a5 = -o.Sinim * a7 + o.Cosim * a8;
            var a6 = -o.Sinim * a9 + o.Cosim * a10;

            var x1 = a1 * o.Cosomm + a2 * o.Sinomm;
            var x2 = a3 * o.Cosomm + a4 * o.Sinomm;
            var x3 = -a1 * o.Sinomm + a2 * o.Cosomm;
            var x4 = -a3 * o.Sinomm + a4 * o.Cosomm;
            var x5 = a5 * o.Sinomm;
            var x6 = a6 * o.Sinomm;
            var x7 = a5 * o.Cosomm;
            var x8 = a6 * o.Cosomm;

            o.Z31 = 12.0 * x1 * x1 - 3.0 * x3 * x3;
            o.Z32 = 24.0 * x1 * x2 - 6.0 * x3 * x4;
            o.Z33 = 12.0 * x2 * x2 - 3.0 * x4 * x4;
            o.Z1 = 3.0 * (a1 * a1 + a2 * a2) + o.Z31 * o.Emsq;
            o.Z2 = 6.0 * (a1 * a3 + a2 * a4) + o.Z32 * o.Emsq;
            o.Z3 = 3.0 * (a3 * a3 + a4 * a4) + o.Z33 * o.Emsq;
            o.Z11 = -6.0 * a1 * a5 + o.Emsq * (-24.0 * x1 * x7 - 6.0 * x3 * x5);
            o.Z12 = -6.0 * (a1 * a6 + a3 * a5) + o.Emsq *
                   (-24.0 * (x2 * x7 + x1 * x8) - 6.0 * (x3 * x6 + x4 * x5));
            o.Z13 = -6.0 * a3 * a6 + o.Emsq * (-24.0 * x2 * x8 - 6.0 * x4 * x6);
            o.Z21 = 6.0 * a2 * a5 + o.Emsq * (24.0 * x1 * x5 - 6.0 * x3 * x7);
            o.Z22 = 6.0 * (a4 * a5 + a2 * a6) + o.Emsq *
                   (24.0 * (x2 * x5 + x1 * x6) - 6.0 * (x4 * x7 + x3 * x8));
            o.Z23 = 6.0 * a4 * a6 + o.Emsq * (24.0 * x2 * x6 - 6.0 * x4 * x8);
            o.Z1 = o.Z1 + o.Z1 + betasq * o.Z31;
            o.Z2 = o.Z2 + o.Z2 + betasq * o.Z32;
            o.Z3 = o.Z3 + o.Z3 + betasq * o.Z33;
            o.S3 = cc * xnoi;
            o.S2 = -0.5 * o.S3 / o.Rtemsq;
            o.S4 = o.S3 * o.Rtemsq;
            o.S1 = -15.0 * o.Em * o.S4;
            o.S5 = x1 * x3 + x2 * x4;
            o.S6 = x2 * x3 + x1 * x4;
            o.S7 = x2 * x4 - x1 * x3;

            /* ----------------------- do lunar terms ------------------- */
            if (lsflg == 1)
            {
                o.Ss1 = o.S1;
                o.Ss2 = o.S2;
                o.Ss3 = o.S3;
                o.Ss4 = o.S4;
                o.Ss5 = o.S5;
                o.Ss6 = o.S6;
                o.Ss7 = o.S7;
                o.Sz1 = o.Z1;
                o.Sz2 = o.Z2;
                o.Sz3 = o.Z3;
                o.Sz11 = o.Z11;
                o.Sz12 = o.Z12;
                o.Sz13 = o.Z13;
                o.Sz21 = o.Z21;
                o.Sz22 = o.Z22;
                o.Sz23 = o.Z23;
                o.Sz31 = o.Z31;
                o.Sz32 = o.Z32;
                o.Sz33 = o.Z33;
                zcosg = zcosgl;
                zsing = zsingl;
                zcosi = zcosil;
                zsini = zsinil;
                zcosh = zcoshl * o.Cnodm + zsinhl * o.Snodm;
                zsinh = o.Snodm * zcoshl - o.Cnodm * zsinhl;
                cc = c1l;
            }
        }

        ds.Zmol = (4.7199672 + 0.22997150 * o.Day - o.Gam) % TwoPi;
        ds.Zmos = (6.2565837 + 0.017201977 * o.Day) % TwoPi;

        /* ------------------------ do solar terms ---------------------- */
        ds.Se2 = 2.0 * o.Ss1 * o.Ss6;
        ds.Se3 = 2.0 * o.Ss1 * o.Ss7;
        ds.Si2 = 2.0 * o.Ss2 * o.Sz12;
        ds.Si3 = 2.0 * o.Ss2 * (o.Sz13 - o.Sz11);
        ds.Sl2 = -2.0 * o.Ss3 * o.Sz2;
        ds.Sl3 = -2.0 * o.Ss3 * (o.Sz3 - o.Sz1);
        ds.Sl4 = -2.0 * o.Ss3 * (-21.0 - 9.0 * o.Emsq) * zes;
        ds.Sgh2 = 2.0 * o.Ss4 * o.Sz32;
        ds.Sgh3 = 2.0 * o.Ss4 * (o.Sz33 - o.Sz31);
        ds.Sgh4 = -18.0 * o.Ss4 * zes;
        ds.Sh2 = -2.0 * o.Ss2 * o.Sz22;
        ds.Sh3 = -2.0 * o.Ss2 * (o.Sz23 - o.Sz21);

        /* ------------------------ do lunar terms ---------------------- */
        ds.Ee2 = 2.0 * o.S1 * o.S6;
        ds.E3 = 2.0 * o.S1 * o.S7;
        ds.Xi2 = 2.0 * o.S2 * o.Z12;
        ds.Xi3 = 2.0 * o.S2 * (o.Z13 - o.Z11);
        ds.Xl2 = -2.0 * o.S3 * o.Z2;
        ds.Xl3 = -2.0 * o.S3 * (o.Z3 - o.Z1);
        ds.Xl4 = -2.0 * o.S3 * (-21.0 - 9.0 * o.Emsq) * zel;
        ds.Xgh2 = 2.0 * o.S4 * o.Z32;
        ds.Xgh3 = 2.0 * o.S4 * (o.Z33 - o.Z31);
        ds.Xgh4 = -18.0 * o.S4 * zel;
        ds.Xh2 = -2.0 * o.S2 * o.Z22;
        ds.Xh3 = -2.0 * o.S2 * (o.Z23 - o.Z21);

        return o;
    }

    /// <summary>
    /// Vallado's dpper: the lunar-solar long-period periodics, applied to the elements (the "n" call;
    /// at epoch they are zero by design, so the reference's initial "y" call changes nothing and is omitted).
    /// </summary>
    private static void Dpper(DeepSpaceTerms ds, double t, Sgp4OperationMode opsmode,
        ref double ep, ref double inclp, ref double nodep, ref double argpp, ref double mp)
    {
        /* ---------------------- constants ----------------------------- */
        const double zns = 1.19459e-5;
        const double zes = 0.01675;
        const double znl = 1.5835218e-4;
        const double zel = 0.05490;

        /* --------------- calculate time varying periodics ----------- */
        var zm = ds.Zmos + zns * t;
        var zf = zm + 2.0 * zes * Math.Sin(zm);
        var sinzf = Math.Sin(zf);
        var f2 = 0.5 * sinzf * sinzf - 0.25;
        var f3 = -0.5 * sinzf * Math.Cos(zf);
        var ses = ds.Se2 * f2 + ds.Se3 * f3;
        var sis = ds.Si2 * f2 + ds.Si3 * f3;
        var sls = ds.Sl2 * f2 + ds.Sl3 * f3 + ds.Sl4 * sinzf;
        var sghs = ds.Sgh2 * f2 + ds.Sgh3 * f3 + ds.Sgh4 * sinzf;
        var shs = ds.Sh2 * f2 + ds.Sh3 * f3;
        zm = ds.Zmol + znl * t;
        zf = zm + 2.0 * zel * Math.Sin(zm);
        sinzf = Math.Sin(zf);
        f2 = 0.5 * sinzf * sinzf - 0.25;
        f3 = -0.5 * sinzf * Math.Cos(zf);
        var sel = ds.Ee2 * f2 + ds.E3 * f3;
        var sil = ds.Xi2 * f2 + ds.Xi3 * f3;
        var sll = ds.Xl2 * f2 + ds.Xl3 * f3 + ds.Xl4 * sinzf;
        var sghl = ds.Xgh2 * f2 + ds.Xgh3 * f3 + ds.Xgh4 * sinzf;
        var shll = ds.Xh2 * f2 + ds.Xh3 * f3;
        var pe = ses + sel;
        var pinc = sis + sil;
        var pl = sls + sll;
        var pgh = sghs + sghl;
        var ph = shs + shll;

        pe = pe - ds.Peo;
        pinc = pinc - ds.Pinco;
        pl = pl - ds.Plo;
        pgh = pgh - ds.Pgho;
        ph = ph - ds.Pho;
        inclp = inclp + pinc;
        ep = ep + pe;
        var sinip = Math.Sin(inclp);
        var cosip = Math.Cos(inclp);

        /* ----------------- apply periodics directly ------------ */
        //  sgp4fix for lyddane choice: the gsfc version, with the perturbed inclination
        if (inclp >= 0.2)
        {
            ph = ph / sinip;
            pgh = pgh - cosip * ph;
            argpp = argpp + pgh;
            nodep = nodep + ph;
            mp = mp + pl;
        }
        else
        {
            /* ---- apply periodics with lyddane modification ---- */
            var sinop = Math.Sin(nodep);
            var cosop = Math.Cos(nodep);
            var alfdp = sinip * sinop;
            var betdp = sinip * cosop;
            var dalf = ph * cosop + pinc * cosip * sinop;
            var dbet = -ph * sinop + pinc * cosip * cosop;
            alfdp = alfdp + dalf;
            betdp = betdp + dbet;
            nodep = nodep % TwoPi;
            //  sgp4fix for afspc written intrinsic functions
            // nodep used without a trigonometric function ahead
            if (nodep < 0.0 && opsmode == Sgp4OperationMode.Afspc)
                nodep = nodep + TwoPi;
            var xls = mp + argpp + cosip * nodep;
            var dls = pl + pgh - pinc * nodep * sinip;
            xls = xls + dls;
            var xnoh = nodep;
            nodep = Math.Atan2(alfdp, betdp);
            //  sgp4fix for afspc written intrinsic functions
            // nodep used without a trigonometric function ahead
            if (nodep < 0.0 && opsmode == Sgp4OperationMode.Afspc)
                nodep = nodep + TwoPi;
            if (Math.Abs(xnoh - nodep) > Math.PI)
            {
                if (nodep < xnoh)
                    nodep = nodep + TwoPi;
                else
                    nodep = nodep - TwoPi;
            }
            mp = mp + pl;
            argpp = xls - mp - cosip * nodep;
        }
    }

    /// <summary>Vallado's dsinit: the deep-space secular rates, and the resonance terms of half-day and one-day orbits.</summary>
    private static void Dsinit(DeepSpaceTerms ds, double xke, in DscomOutput c, double argpo, double t, double tc,
        double mo, double mdot, double no, double nodeo, double nodedot, double xpidot, double ecco, double eccsq,
        ref double em, ref double argpm, ref double inclm, ref double mm, ref double nm, ref double nodem, out double dndt)
    {
        const double q22 = 1.7891679e-6;
        const double q31 = 2.1460748e-6;
        const double q33 = 2.2123015e-7;
        const double root22 = 1.7891679e-6;
        const double root44 = 7.3636953e-9;
        const double root54 = 2.1765803e-9;
        const double rptim = 4.37526908801129966e-3; // this equates to 7.29211514668855e-5 rad/sec
        const double root32 = 3.7393792e-7;
        const double root52 = 1.1428639e-7;
        const double znl = 1.5835218e-4;
        const double zns = 1.19459e-5;

        var cosim = c.Cosim;
        var sinim = c.Sinim;
        var emsq = c.Emsq;

        /* -------------------- deep space initialization ------------ */
        ds.Irez = 0;
        if (nm < 0.0052359877 && nm > 0.0034906585)
            ds.Irez = 1;
        if (nm >= 8.26e-3 && nm <= 9.24e-3 && em >= 0.5)
            ds.Irez = 2;

        /* ------------------------ do solar terms ------------------- */
        var ses = c.Ss1 * zns * c.Ss5;
        var sis = c.Ss2 * zns * (c.Sz11 + c.Sz13);
        var sls = -zns * c.Ss3 * (c.Sz1 + c.Sz3 - 14.0 - 6.0 * emsq);
        var sghs = c.Ss4 * zns * (c.Sz31 + c.Sz33 - 6.0);
        var shs = -zns * c.Ss2 * (c.Sz21 + c.Sz23);
        // sgp4fix for 180 deg incl
        if (inclm < 5.2359877e-2 || inclm > Math.PI - 5.2359877e-2)
            shs = 0.0;
        if (sinim != 0.0)
            shs = shs / sinim;
        var sgs = sghs - cosim * shs;

        /* ------------------------- do lunar terms ------------------ */
        ds.Dedt = ses + c.S1 * znl * c.S5;
        ds.Didt = sis + c.S2 * znl * (c.Z11 + c.Z13);
        ds.Dmdt = sls - znl * c.S3 * (c.Z1 + c.Z3 - 14.0 - 6.0 * emsq);
        var sghl = c.S4 * znl * (c.Z31 + c.Z33 - 6.0);
        var shll = -znl * c.S2 * (c.Z21 + c.Z23);
        // sgp4fix for 180 deg incl
        if (inclm < 5.2359877e-2 || inclm > Math.PI - 5.2359877e-2)
            shll = 0.0;
        ds.Domdt = sgs + sghl;
        ds.Dnodt = shs;
        if (sinim != 0.0)
        {
            ds.Domdt = ds.Domdt - cosim / sinim * shll;
            ds.Dnodt = ds.Dnodt + shll / sinim;
        }

        /* ----------- calculate deep space resonance effects -------- */
        dndt = 0.0;
        var theta = (ds.Gsto + tc * rptim) % TwoPi;
        em = em + ds.Dedt * t;
        inclm = inclm + ds.Didt * t;
        argpm = argpm + ds.Domdt * t;
        nodem = nodem + ds.Dnodt * t;
        mm = mm + ds.Dmdt * t;

        /* -------------- initialize the resonance terms ------------- */
        if (ds.Irez != 0)
        {
            var aonv = Math.Pow(nm / xke, X2o3);

            /* ---------- geopotential resonance for 12 hour orbits ------ */
            if (ds.Irez == 2)
            {
                var cosisq = cosim * cosim;
                var emo = em;
                em = ecco;
                var emsqo = emsq;
                emsq = eccsq;
                var eoc = em * emsq;
                var g201 = -0.306 - (em - 0.64) * 0.440;

                double g211, g310, g322, g410, g422, g520, g521, g532, g533;

                if (em <= 0.65)
                {
                    g211 = 3.616 - 13.2470 * em + 16.2900 * emsq;
                    g310 = -19.302 + 117.3900 * em - 228.4190 * emsq + 156.5910 * eoc;
                    g322 = -18.9068 + 109.7927 * em - 214.6334 * emsq + 146.5816 * eoc;
                    g410 = -41.122 + 242.6940 * em - 471.0940 * emsq + 313.9530 * eoc;
                    g422 = -146.407 + 841.8800 * em - 1629.014 * emsq + 1083.4350 * eoc;
                    g520 = -532.114 + 3017.977 * em - 5740.032 * emsq + 3708.2760 * eoc;
                }
                else
                {
                    g211 = -72.099 + 331.819 * em - 508.738 * emsq + 266.724 * eoc;
                    g310 = -346.844 + 1582.851 * em - 2415.925 * emsq + 1246.113 * eoc;
                    g322 = -342.585 + 1554.908 * em - 2366.899 * emsq + 1215.972 * eoc;
                    g410 = -1052.797 + 4758.686 * em - 7193.992 * emsq + 3651.957 * eoc;
                    g422 = -3581.690 + 16178.110 * em - 24462.770 * emsq + 12422.520 * eoc;
                    if (em > 0.715)
                        g520 = -5149.66 + 29936.92 * em - 54087.36 * emsq + 31324.56 * eoc;
                    else
                        g520 = 1464.74 - 4664.75 * em + 3763.64 * emsq;
                }
                if (em < 0.7)
                {
                    g533 = -919.22770 + 4988.6100 * em - 9064.7700 * emsq + 5542.21 * eoc;
                    g521 = -822.71072 + 4568.6173 * em - 8491.4146 * emsq + 5337.524 * eoc;
                    g532 = -853.66600 + 4690.2500 * em - 8624.7700 * emsq + 5341.4 * eoc;
                }
                else
                {
                    g533 = -37995.780 + 161616.52 * em - 229838.20 * emsq + 109377.94 * eoc;
                    g521 = -51752.104 + 218913.95 * em - 309468.16 * emsq + 146349.42 * eoc;
                    g532 = -40023.880 + 170470.89 * em - 242699.48 * emsq + 115605.82 * eoc;
                }

                var sini2 = sinim * sinim;
                var f220 = 0.75 * (1.0 + 2.0 * cosim + cosisq);
                var f221 = 1.5 * sini2;
                var f321 = 1.875 * sinim * (1.0 - 2.0 * cosim - 3.0 * cosisq);
                var f322 = -1.875 * sinim * (1.0 + 2.0 * cosim - 3.0 * cosisq);
                var f441 = 35.0 * sini2 * f220;
                var f442 = 39.3750 * sini2 * sini2;
                var f522 = 9.84375 * sinim * (sini2 * (1.0 - 2.0 * cosim - 5.0 * cosisq) +
                        0.33333333 * (-2.0 + 4.0 * cosim + 6.0 * cosisq));
                var f523 = sinim * (4.92187512 * sini2 * (-2.0 - 4.0 * cosim +
                       10.0 * cosisq) + 6.56250012 * (1.0 + 2.0 * cosim - 3.0 * cosisq));
                var f542 = 29.53125 * sinim * (2.0 - 8.0 * cosim + cosisq *
                       (-12.0 + 8.0 * cosim + 10.0 * cosisq));
                var f543 = 29.53125 * sinim * (-2.0 - 8.0 * cosim + cosisq *
                       (12.0 + 8.0 * cosim - 10.0 * cosisq));
                var xno2 = nm * nm;
                var ainv2 = aonv * aonv;
                var temp1 = 3.0 * xno2 * ainv2;
                var temp = temp1 * root22;
                ds.D2201 = temp * f220 * g201;
                ds.D2211 = temp * f221 * g211;
                temp1 = temp1 * aonv;
                temp = temp1 * root32;
                ds.D3210 = temp * f321 * g310;
                ds.D3222 = temp * f322 * g322;
                temp1 = temp1 * aonv;
                temp = 2.0 * temp1 * root44;
                ds.D4410 = temp * f441 * g410;
                ds.D4422 = temp * f442 * g422;
                temp1 = temp1 * aonv;
                temp = temp1 * root52;
                ds.D5220 = temp * f522 * g520;
                ds.D5232 = temp * f523 * g532;
                temp = 2.0 * temp1 * root54;
                ds.D5421 = temp * f542 * g521;
                ds.D5433 = temp * f543 * g533;
                ds.Xlamo = (mo + nodeo + nodeo - theta - theta) % TwoPi;
                ds.Xfact = mdot + ds.Dmdt + 2.0 * (nodedot + ds.Dnodt - rptim) - no;
                em = emo;
                emsq = emsqo;
            }

            /* ---------------- synchronous resonance terms -------------- */
            if (ds.Irez == 1)
            {
                var g200 = 1.0 + emsq * (-2.5 + 0.8125 * emsq);
                var g310 = 1.0 + 2.0 * emsq;
                var g300 = 1.0 + emsq * (-6.0 + 6.60937 * emsq);
                var f220 = 0.75 * (1.0 + cosim) * (1.0 + cosim);
                var f311 = 0.9375 * sinim * sinim * (1.0 + 3.0 * cosim) - 0.75 * (1.0 + cosim);
                var f330 = 1.0 + cosim;
                f330 = 1.875 * f330 * f330 * f330;
                ds.Del1 = 3.0 * nm * nm * aonv * aonv;
                ds.Del2 = 2.0 * ds.Del1 * f220 * g200 * q22;
                ds.Del3 = 3.0 * ds.Del1 * f330 * g300 * q33 * aonv;
                ds.Del1 = ds.Del1 * f311 * g310 * q31 * aonv;
                ds.Xlamo = (mo + nodeo + argpo - theta) % TwoPi;
                ds.Xfact = mdot + xpidot - rptim + ds.Dmdt + ds.Domdt + ds.Dnodt - no;
            }

            /* ------------ for sgp4, initialize the integrator ---------- */
            // xli = xlamo, xni = no, atime = 0: kept per call in Dspace, not stored
            nm = no + dndt;
        }
    }

    /// <summary>
    /// Vallado's dspace: the deep-space secular terms and the resonance integration at time <paramref name="t"/>.
    /// </summary>
    /// <remarks>
    /// The reference keeps the integrator's state (atime, xli, xni) between calls to resume where it
    /// stopped. Here every call starts from the epoch: the integrator takes fixed 720-minute steps, so
    /// it repeats exactly the same arithmetic and gives the same numbers, and the propagator stays
    /// immutable and safe to share between threads.
    /// </remarks>
    private static void Dspace(DeepSpaceTerms ds, double argpo, double argpdot, double t, double tc, double no,
        ref double em, ref double argpm, ref double inclm, ref double mm, ref double nodem, out double dndt, ref double nm)
    {
        const double fasx2 = 0.13130908;
        const double fasx4 = 2.8843198;
        const double fasx6 = 0.37448087;
        const double g22 = 5.7686396;
        const double g32 = 0.95240898;
        const double g44 = 1.8014998;
        const double g52 = 1.0508330;
        const double g54 = 4.4108898;
        const double rptim = 4.37526908801129966e-3; // this equates to 7.29211514668855e-5 rad/sec
        const double stepp = 720.0;
        const double stepn = -720.0;
        const double step2 = 259200.0;

        /* ----------- calculate deep space resonance effects ----------- */
        dndt = 0.0;
        var theta = (ds.Gsto + tc * rptim) % TwoPi;
        em = em + ds.Dedt * t;

        inclm = inclm + ds.Didt * t;
        argpm = argpm + ds.Domdt * t;
        nodem = nodem + ds.Dnodt * t;
        mm = mm + ds.Dmdt * t;

        /* - update resonances : numerical (euler-maclaurin) integration - */
        /* ------------------------- epoch restart ----------------------  */
        var xndt = 0.0;
        var xnddt = 0.0;
        var xldot = 0.0;

        var ft = 0.0;
        if (ds.Irez != 0)
        {
            // the reference's restart, taken on every call (see remarks)
            var atime = 0.0;
            var xni = no;
            var xli = ds.Xlamo;

            var delt = t > 0.0 ? stepp : stepn;

            var iretn = 381; // added for do loop
            while (iretn == 381)
            {
                /* ------------------- dot terms calculated ------------- */
                /* ----------- near - synchronous resonance terms ------- */
                if (ds.Irez != 2)
                {
                    xndt = ds.Del1 * Math.Sin(xli - fasx2) + ds.Del2 * Math.Sin(2.0 * (xli - fasx4)) +
                            ds.Del3 * Math.Sin(3.0 * (xli - fasx6));
                    xldot = xni + ds.Xfact;
                    xnddt = ds.Del1 * Math.Cos(xli - fasx2) +
                            2.0 * ds.Del2 * Math.Cos(2.0 * (xli - fasx4)) +
                            3.0 * ds.Del3 * Math.Cos(3.0 * (xli - fasx6));
                    xnddt = xnddt * xldot;
                }
                else
                {
                    /* --------- near - half-day resonance terms -------- */
                    var xomi = argpo + argpdot * atime;
                    var x2omi = xomi + xomi;
                    var x2li = xli + xli;
                    xndt = ds.D2201 * Math.Sin(x2omi + xli - g22) + ds.D2211 * Math.Sin(xli - g22) +
                          ds.D3210 * Math.Sin(xomi + xli - g32) + ds.D3222 * Math.Sin(-xomi + xli - g32) +
                          ds.D4410 * Math.Sin(x2omi + x2li - g44) + ds.D4422 * Math.Sin(x2li - g44) +
                          ds.D5220 * Math.Sin(xomi + xli - g52) + ds.D5232 * Math.Sin(-xomi + xli - g52) +
                          ds.D5421 * Math.Sin(xomi + x2li - g54) + ds.D5433 * Math.Sin(-xomi + x2li - g54);
                    xldot = xni + ds.Xfact;
                    xnddt = ds.D2201 * Math.Cos(x2omi + xli - g22) + ds.D2211 * Math.Cos(xli - g22) +
                          ds.D3210 * Math.Cos(xomi + xli - g32) + ds.D3222 * Math.Cos(-xomi + xli - g32) +
                          ds.D5220 * Math.Cos(xomi + xli - g52) + ds.D5232 * Math.Cos(-xomi + xli - g52) +
                          2.0 * (ds.D4410 * Math.Cos(x2omi + x2li - g44) +
                          ds.D4422 * Math.Cos(x2li - g44) + ds.D5421 * Math.Cos(xomi + x2li - g54) +
                          ds.D5433 * Math.Cos(-xomi + x2li - g54));
                    xnddt = xnddt * xldot;
                }

                /* ----------------------- integrator ------------------- */
                if (Math.Abs(t - atime) >= stepp)
                {
                    iretn = 381;
                }
                else // exit here
                {
                    ft = t - atime;
                    iretn = 0;
                }

                if (iretn == 381)
                {
                    xli = xli + xldot * delt + xndt * step2;
                    xni = xni + xndt * delt + xnddt * step2;
                    atime = atime + delt;
                }
            }

            nm = xni + xndt * ft + xnddt * ft * ft * 0.5;
            var xl = xli + xldot * ft + xndt * ft * ft * 0.5;
            if (ds.Irez != 1)
            {
                mm = xl - 2.0 * nodem + 2.0 * theta;
                dndt = nm - no;
            }
            else
            {
                mm = xl - nodem - argpm + theta;
                dndt = nm - no;
            }
            nm = no + dndt;
        }
    }

    /// <summary>Vallado's gstime: Greenwich mean sidereal time (IAU 1982), radians, for a UT1 Julian date.</summary>
    private static double Gstime(double jdut1)
    {
        const double deg2rad = Math.PI / 180.0;

        var tut1 = (jdut1 - 2451545.0) / 36525.0;
        var temp = -6.2e-6 * tut1 * tut1 * tut1 + 0.093104 * tut1 * tut1 +
                (876600.0 * 3600 + 8640184.812866) * tut1 + 67310.54841;  // sec
        temp = (temp * deg2rad / 240.0) % TwoPi; //360/86400 = 1/240, to deg, to rad

        // ------------------------ check quadrants ---------------------
        if (temp < 0.0)
            temp += TwoPi;

        return temp;
    }

    /// <summary>Vallado's gsto in initl: the sidereal angle at epoch, by the mode's formula.</summary>
    private static double SiderealAtEpoch(double epoch, Sgp4OperationMode opsmode)
    {
        if (opsmode == Sgp4OperationMode.Afspc)
        {
            // sgp4fix use old way of finding gst
            // count integer number of days from 0 jan 1970
            var ts70 = epoch - 7305.0;
            var ds70 = Math.Floor(ts70 + 1.0e-8);
            var tfrac = ts70 - ds70;
            // find greenwich location at epoch
            const double c1 = 1.72027916940703639e-2;
            const double thgr70 = 1.7321343856509374;
            const double fk5r = 5.07551419432269442e-15;
            const double c1p2p = c1 + TwoPi;
            var gsto = (thgr70 + c1 * ds70 + c1p2p * tfrac + ts70 * ts70 * fk5r) % TwoPi;
            if (gsto < 0.0)
                gsto = gsto + TwoPi;

            return gsto;
        }

        return Gstime(epoch + 2433281.5);
    }
}
