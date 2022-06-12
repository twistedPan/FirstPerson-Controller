using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class _Utilities : MonoBehaviour
{
    // Create range from value n which acts in range start1 to stop1 to new range
    public static float mapRange(float n, float start1, float stop1, float start2, float stop2) 
    {
        float newval = (n - start1) / (stop1 - start1) * (stop2 - start2) + start2;
        //if (newval != ) {return newval;}
        if (start2 < stop2) 
        {
            return Mathf.Clamp(newval, start2, stop2);
        } 
        else 
        {
            return Mathf.Clamp(newval, stop2, start2);
        }
    }

    public static float roundTo(float v, float dec) 
    {
        return Mathf.Floor(v*Mathf.Pow(10,dec))/Mathf.Pow(10,dec);
    }

    public static Vector3 addVec3(Vector3 v, Vector3 adding) {
        return new Vector3(v.x + adding.x, v.y + adding.y, v.z + adding.z);
    }

    public static Vector3 subVec3(Vector3 v, Vector3 substrac)
    {
        return new Vector3(v.x - substrac.x, v.y - substrac.y, v.z - substrac.z);
    }


}
