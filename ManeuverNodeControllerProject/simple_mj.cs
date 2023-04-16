using KSP.Game;
using KSP.Sim;
using KSP.Sim.impl;

namespace ManeuverNodeController;

public static class OrbitExtensions
{
    //can probably be replaced with Vector3d.xzy?
    public static Vector3d SwapYZ(Vector3d v)
    {
        return v.Reorder(132);
    }

    //normalized vector perpendicular to the orbital plane
    //convention: as you look down along the orbit normal, the satellite revolves counterclockwise
    public static Vector3d SwappedOrbitNormal(this PatchedConicsOrbit o)
    {
        return -SwapYZ(o.GetRelativeOrbitNormal()).normalized;
    }

    //normalized vector perpendicular to the orbital plane
    //convention: as you look down along the orbit normal, the satellite revolves counterclockwise
    public static Vector3d SwappedOrbitNormal(this IKeplerOrbit o)
    {
        return -SwapYZ(o.GetRelativeOrbitNormal()).normalized;
    }

    //Returns the vector from the primary to the orbiting body at periapsis
    //Better than using PatchedConicsOrbit.eccVec because that is zero for circular orbits
    public static Vector3d SwappedRelativePositionAtPeriapsis(this PatchedConicsOrbit o)
    {
        // was: (float)o.LAN -> longitudeOfAscendingNode
        // was: Planetarium.up -> o.ReferenceFrame.up.vector
        // was: Planetarium.right -> o.ReferenceFrame.right.vector
        Vector3d vectorToAN = QuaternionD.AngleAxis(-(float)o.longitudeOfAscendingNode, o.ReferenceFrame.up.vector) * o.ReferenceFrame.right.vector;
        Vector3d vectorToPe = QuaternionD.AngleAxis((float)o.argumentOfPeriapsis, o.SwappedOrbitNormal()) * vectorToAN;
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
    public static double TimeOfTrueAnomaly(this PatchedConicsOrbit o, double trueAnomaly, double UT)
    {
        // ManeuverNodeControllerMod.Logger.LogWarning($"OrbitExtensions.TimeOfTrueAnomaly: trueAnomaly: {trueAnomaly*UtilMath.Deg2Rad}");
        return o.GetUTforTrueAnomaly(trueAnomaly * UtilMath.Deg2Rad, o.period);
        //return o.UTAtMeanAnomaly(o.GetMeanAnomalyAtEccentricAnomaly(o.GetEccentricAnomalyAtTrueAnomaly(trueAnomaly)), UT);
    }

    //Gives the true anomaly (in a's orbit) at which a crosses its ascending node
    //with b's orbit.
    //The returned value is always between 0 and 2 * PI.
    public static double AscendingNodeTrueAnomaly(this PatchedConicsOrbit a, IKeplerOrbit b)
    {
        Vector3d vectorToAN = Vector3d.Cross(a.SwappedOrbitNormal(), b.SwappedOrbitNormal()); // tried: GetRelativeOrbitNormal()
        return a.TrueAnomalyFromVector(vectorToAN);
    }

    //Gives the true anomaly (in a's orbit) at which a crosses its descending node
    //with b's orbit.
    //The returned value is always between 0 and 2 * PI.
    public static double DescendingNodeTrueAnomaly(this PatchedConicsOrbit a, IKeplerOrbit b)
    {
        return ClampDegrees360(a.AscendingNodeTrueAnomaly(b) + 180.0);
    }

    //Gives the true anomaly at which o crosses the equator going northwards, if o is east-moving,
    //or southwards, if o is west-moving.
    //The returned value is always between 0 and 2 * PI.
    public static double AscendingNodeEquatorialTrueAnomaly(this PatchedConicsOrbit o)
    {
        // was: o.referenceBody.transform.up -> o.referenceBody.transform.up.vector
        Vector3d vectorToAN = Vector3d.Cross(o.referenceBody.transform.up.vector, o.SwappedOrbitNormal()); // tried: GetRelativeOrbitNormal()
        return o.TrueAnomalyFromVector(vectorToAN);
    }

    //Gives the true anomaly at which o crosses the equator going southwards, if o is east-moving,
    //or northwards, if o is west-moving.
    //The returned value is always between 0 and 2 * PI.
    public static double DescendingNodeEquatorialTrueAnomaly(this PatchedConicsOrbit o)
    {
        return ClampDegrees360(o.AscendingNodeEquatorialTrueAnomaly() + 180);
    }

    //TODO 1.1 changed trueAnomaly to rad but MJ ext stil uses deg. Should change for consistency

    //Converts a direction, specified by a Vector3d, into a true anomaly.
    //The vector is projected into the orbital plane and then the true anomaly is
    //computed as the angle this vector makes with the vector pointing to the periapsis.
    //The returned value is always between 0 and 360.
    public static double TrueAnomalyFromVector(this PatchedConicsOrbit o, Vector3d vec)
    {
        Vector3d oNormal = o.SwappedOrbitNormal(); // tried: GetRelativeOrbitNormal()
        Vector3d projected = Vector3d.Exclude(oNormal, vec);
        Vector3d vectorToPe = o.SwappedRelativePositionAtPeriapsis();
        double angleFromPe = Vector3d.Angle(vectorToPe, projected);

        //If the vector points to the infalling part of the orbit then we need to do 360 minus the
        //angle from Pe to get the true anomaly. Test this by taking the the cross product of the
        //orbit normal and vector to the periapsis. This gives a vector that points to center of the
        //outgoing side of the orbit. If vectorToAN is more than 90 degrees from this vector, it occurs
        //during the infalling part of the orbit.
        if (Math.Abs(Vector3d.Angle(projected, Vector3d.Cross(oNormal, vectorToPe))) < 90)
        {
            return angleFromPe;
        }
        else
        {
            return 360 - angleFromPe;
        }
    }

    //keeps angles in the range 0 to 360
    public static double ClampDegrees360(double angle)
    {
        angle = angle % 360.0;
        if (angle < 0) return angle + 360.0;
        else return angle;
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