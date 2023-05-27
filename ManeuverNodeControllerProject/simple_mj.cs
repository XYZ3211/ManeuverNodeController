using KSP.Sim;
using KSP.Sim.impl;
using System.Runtime.CompilerServices;

namespace ManeuverNodeController;

public static class OrbitExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3d OrbitNormal(this PatchedConicsOrbit o) // KS2: OrbitNormal // was: SwappedOrbitNormal
    {
        return o.referenceBody.transform.celestialFrame.ToLocalPosition(o.ReferenceFrame, -o.GetRelativeOrbitNormal().SwapYAndZ).normalized; // From KS2
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3d OrbitNormal(this IKeplerOrbit o) // KS2: OrbitNormal // was: SwappedOrbitNormal
    {
        return o.referenceBody.transform.celestialFrame.ToLocalPosition(o.ReferenceFrame, -o.GetRelativeOrbitNormal().SwapYAndZ).normalized; // From KS2
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3d WorldBCIPositionAtPeriapsis(this PatchedConicsOrbit o) // was: SwappedRelativePositionAtPeriapsis
    {
        Vector3d vectorToAN = QuaternionD.AngleAxis(-(float)o.longitudeOfAscendingNode, o.ReferenceFrame.up.vector) * o.ReferenceFrame.right.vector;
        Vector3d vectorToPe = QuaternionD.AngleAxis((float)o.argumentOfPeriapsis, o.OrbitNormal()) * vectorToAN;
        return o.Periapsis * vectorToPe;
    }

    //Returns the next time at which a will cross its ascending node with b.
    //For elliptical orbits this is a time between UT and UT + a.period.
    //For hyperbolic orbits this can be any time, including a time in the past if
    //the ascending node is in the past.
    //NOTE: this function will throw an ArgumentException if a is a hyperbolic orbit and the "ascending node"
    //occurs at a true anomaly that a does not actually ever attain
    public static double TimeOfAscendingNode(this PatchedConicsOrbit a, IKeplerOrbit b, double UT)
    {
        return a.TimeOfTrueAnomaly(a.AscendingNodeTrueAnomaly(b), UT);
    }

    //Returns the next time at which a will cross its descending node with b.
    //For elliptical orbits this is a time between UT and UT + a.period.
    //For hyperbolic orbits this can be any time, including a time in the past if
    //the descending node is in the past.
    //NOTE: this function will throw an ArgumentException if a is a hyperbolic orbit and the "descending node"
    //occurs at a true anomaly that a does not actually ever attain
    public static double TimeOfDescendingNode(this PatchedConicsOrbit a, IKeplerOrbit b, double UT)
    {
        return a.TimeOfTrueAnomaly(a.DescendingNodeTrueAnomaly(b), UT);
    }

    //Returns the next time at which the orbiting object will cross the equator
    //moving northward, if o is east-moving, or southward, if o is west-moving.
    //For elliptical orbits this is a time between UT and UT + o.period.
    //For hyperbolic orbits this can by any time, including a time in the past if the
    //ascending node is in the past.
    //NOTE: this function will throw an ArgumentException if o is a hyperbolic orbit and the
    //"ascending node" occurs at a true anomaly that o does not actually ever attain.
    public static double TimeOfAscendingNodeEquatorial(this PatchedConicsOrbit o, double UT)
    {
        return o.TimeOfTrueAnomaly(o.AscendingNodeEquatorialTrueAnomaly(), UT);
    }

    //Returns the next time at which the orbiting object will cross the equator
    //moving southward, if o is east-moving, or northward, if o is west-moving.
    //For elliptical orbits this is a time between UT and UT + o.period.
    //For hyperbolic orbits this can by any time, including a time in the past if the
    //descending node is in the past.
    //NOTE: this function will throw an ArgumentException if o is a hyperbolic orbit and the
    //"descending node" occurs at a true anomaly that o does not actually ever attain.
    public static double TimeOfDescendingNodeEquatorial(this PatchedConicsOrbit o, double UT)
    {
        return o.TimeOfTrueAnomaly(o.DescendingNodeEquatorialTrueAnomaly(), UT);
    }

    //TODO 1.1 changed trueAnomaly to rad but MJ ext stil uses deg. Should change for consistency

    //NOTE: this function can throw an ArgumentException, if o is a hyperbolic orbit with an eccentricity
    //large enough that it never attains the given true anomaly
    public static double TimeOfTrueAnomaly(this PatchedConicsOrbit o, double trueAnomalyRad, double UT)
    {
        // ManeuverNodeControllerMod.Logger.LogWarning($"OrbitExtensions.TimeOfTrueAnomaly: trueAnomaly: {trueAnomaly*UtilMath.Deg2Rad}");
        // return o.GetUTforTrueAnomaly(trueAnomaly * UtilMath.Deg2Rad, o.period);
        return o.UTAtMeanAnomaly(o.GetMeanAnomalyAtEccentricAnomaly(o.GetEccentricAnomalyAtTrueAnomaly(trueAnomalyRad)), UT);
    }

    //mean motion is rate of increase of the mean anomaly
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double MeanMotion(this PatchedConicsOrbit o)
    {
        if (o.eccentricity > 1)
        {
            return Math.Sqrt(o.referenceBody.gravParameter / Math.Abs(Math.Pow(o.semiMajorAxis, 3)));
        }

        // The above formula is wrong when using the RealSolarSystem mod, which messes with orbital periods.
        // This simpler formula should be foolproof for elliptical orbits:
        return 2 * Math.PI / o.period;
    }

    //The mean anomaly of the orbit.
    //For elliptical orbits, the value return is always between 0 and 2pi
    //For hyperbolic orbits, the value can be any number.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double MeanAnomalyAtUT(this PatchedConicsOrbit o, double UT)
    {
        // We use ObtAtEpoch and not meanAnomalyAtEpoch because somehow meanAnomalyAtEpoch
        // can be wrong when using the RealSolarSystem mod. ObtAtEpoch is always correct.
        double ret = (o.ObTAtEpoch + (UT - o.epoch)) * o.MeanMotion();
        if (o.eccentricity < 1) ret = ClampRadiansTwoPi(ret);
        return ret;
    }

    //The next time at which the orbiting object will reach the given mean anomaly.
    //For elliptical orbits, this will be a time between UT and UT + o.period
    //For hyperbolic orbits, this can be any time, including a time in the past, if
    //the given mean anomaly occurred in the past
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double UTAtMeanAnomaly(this PatchedConicsOrbit o, double meanAnomaly, double UT)
    {
        double currentMeanAnomaly = o.MeanAnomalyAtUT(UT);
        double meanDifference = meanAnomaly - currentMeanAnomaly;
        if (o.eccentricity < 1) meanDifference = ClampRadiansTwoPi(meanDifference);
        return UT + meanDifference / o.MeanMotion();
    }

    //Originally by Zool, revised by The_Duck
    //Converts a true anomaly into an eccentric anomaly.
    //For elliptical orbits this returns a value between 0 and 2pi
    //For hyperbolic orbits the returned value can be any number.
    //NOTE: For a hyperbolic orbit, if a true anomaly is requested that does not exist (a true anomaly
    //past the true anomaly of the asymptote) then an ArgumentException is thrown
    public static double GetEccentricAnomalyAtTrueAnomaly(this PatchedConicsOrbit o, double trueAnomalyRad)
    {
        double e = o.eccentricity;
        trueAnomalyRad = ClampRadiansTwoPi(trueAnomalyRad);
        // SAVEFORNOW trueAnomaly = MuUtils.ClampDegrees360(trueAnomaly);
        // SAVEFORNOW trueAnomaly = trueAnomaly * (UtilMath.Deg2Rad);

        if (e < 1) //elliptical orbits
        {
            double cosE = (e + Math.Cos(trueAnomalyRad)) / (1 + e * Math.Cos(trueAnomalyRad));
            double sinE = Math.Sqrt(1 - cosE * cosE);
            if (trueAnomalyRad > Math.PI) sinE *= -1;

            return ClampRadiansTwoPi(Math.Atan2(sinE, cosE));
        }

        //hyperbolic orbits
        double coshE = (e + Math.Cos(trueAnomalyRad)) / (1 + e * Math.Cos(trueAnomalyRad));
        if (coshE < 1)
            throw new ArgumentException("OrbitExtensions.GetEccentricAnomalyAtTrueAnomaly: True anomaly of " + trueAnomalyRad +
                                        " radians is not attained by orbit with eccentricity " + o.eccentricity);

        double E = Acosh(coshE);
        if (trueAnomalyRad > Math.PI) E *= -1;

        return E;
    }

    //Originally by Zool, revised by The_Duck
    //Converts an eccentric anomaly into a mean anomaly.
    //For an elliptical orbit, the returned value is between 0 and 2pi
    //For a hyperbolic orbit, the returned value is any number
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetMeanAnomalyAtEccentricAnomaly(this PatchedConicsOrbit o, double E)
    {
        double e = o.eccentricity;
        if (e < 1) //elliptical orbits
        {
            return ClampRadiansTwoPi(E - e * Math.Sin(E));
        }

        //hyperbolic orbits
        return e * Math.Sinh(E) - E;
    }

    //Gives the true anomaly (in a's orbit) at which a crosses its ascending node
    //with b's orbit.
    //The returned value is always between 0 and 2 * PI.
    public static double AscendingNodeTrueAnomaly(this PatchedConicsOrbit a, IKeplerOrbit b)
    {
        Vector3d vectorToAN = Vector3d.Cross(a.OrbitNormal(), b.OrbitNormal());
        return a.TrueAnomalyFromVector(vectorToAN);
    }

    //Gives the true anomaly (in a's orbit) at which a crosses its descending node
    //with b's orbit.
    //The returned value is always between 0 and 2 * PI.
    public static double DescendingNodeTrueAnomaly(this PatchedConicsOrbit a, IKeplerOrbit b)
    {
        return ClampRadiansTwoPi(a.AscendingNodeTrueAnomaly(b) + Math.PI);
    }

    //Gives the true anomaly at which o crosses the equator going northwards, if o is east-moving,
    //or southwards, if o is west-moving.
    //The returned value is always between 0 and 2 * PI.
    public static double AscendingNodeEquatorialTrueAnomaly(this PatchedConicsOrbit o)
    {
        Vector3d vectorToAN = Vector3d.Cross(o.referenceBody.transform.up.vector, o.OrbitNormal());
        return o.TrueAnomalyFromVector(vectorToAN);
    }

    //Gives the true anomaly at which o crosses the equator going southwards, if o is east-moving,
    //or northwards, if o is west-moving.
    //The returned value is always between 0 and 2 * PI.
    public static double DescendingNodeEquatorialTrueAnomaly(this PatchedConicsOrbit o)
    {
        return ClampRadiansTwoPi(o.AscendingNodeEquatorialTrueAnomaly() + Math.PI);
    }

    //TODO 1.1 changed trueAnomaly to rad but MJ ext stil uses deg. Should change for consistency

    //Converts a direction, specified by a Vector3d, into a true anomaly.
    //The vector is projected into the orbital plane and then the true anomaly is
    //computed as the angle this vector makes with the vector pointing to the periapsis.
    //The returned value is always between 0 and 360.
    public static double TrueAnomalyFromVector(this PatchedConicsOrbit o, Vector3d vec)
    {
        Vector3d oNormal = o.OrbitNormal();
        Vector3d projected = Vector3d.Exclude(oNormal, vec);
        Vector3d vectorToPe = o.WorldBCIPositionAtPeriapsis();
        double angleFromPe = Vector3d.Angle(vectorToPe, projected);

        //If the vector points to the infalling part of the orbit then we need to do 360 minus the
        //angle from Pe to get the true anomaly. Test this by taking the the cross product of the
        //orbit normal and vector to the periapsis. This gives a vector that points to center of the
        //outgoing side of the orbit. If vectorToAN is more than 90 degrees from this vector, it occurs
        //during the infalling part of the orbit.
        if (Math.Abs(Vector3d.Angle(projected, Vector3d.Cross(oNormal, vectorToPe))) < 90)
        {
            return angleFromPe * UtilMath.Deg2Rad;
        }
        else
        {
            return (360 - angleFromPe) * UtilMath.Deg2Rad;
        }
    }

    //keeps angles in the range 0 to 360
    //public static double ClampDegrees360(double angle)
    //{
    //    angle = angle % 360.0;
    //    if (angle < 0) return angle + 360.0;
    //    else return angle;
    //}

    //keeps angles in the range 0 to 2 PI
    public static double ClampRadiansTwoPi(double angle)
    {
        angle = angle % (2 * Math.PI);
        if (angle < 0) return angle + 2 * Math.PI;
        return angle;
    }

    //acosh(x) = log(x + sqrt(x^2 - 1))
    public static double Acosh(double x)
    {
        return Math.Log(x + Math.Sqrt(x * x - 1));
    }
}

public static class MathExtensions
{
    public static Vector3d Reorder(this Vector3d vector, int order)
    {
        switch (order)
        {
            case 123:
                return new Vector3d(vector.x, vector.y, vector.z);
            case 132:
                return new Vector3d(vector.x, vector.z, vector.y);
            case 213:
                return new Vector3d(vector.y, vector.x, vector.z);
            case 231:
                return new Vector3d(vector.y, vector.z, vector.x);
            case 312:
                return new Vector3d(vector.z, vector.x, vector.y);
            case 321:
                return new Vector3d(vector.z, vector.y, vector.x);
        }
        throw new ArgumentException("Invalid order", "order");
    }
}