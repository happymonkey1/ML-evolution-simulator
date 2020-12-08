using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NativeMethods : MonoBehaviour
{
    public static List<int> POS_NEG_LIST = new List<int>() {-1, 1};
    public static float GetRandomFloat(float min, float max, bool randNegation = true) {
        return (randNegation) ? UnityEngine.Random.Range(min, max) * POS_NEG_LIST[_rand.Next(POS_NEG_LIST.Count)] : UnityEngine.Random.Range(min, max);
    }

    public static float PowOneOverE(float b, float numerator = 1) { return Mathf.Pow(b, numerator * InverseE); }

    public static float InverseE = (float)(1 / System.Math.E);

    private static System.Random _rand = new System.Random();

    public static double RandomGaussian()
    {
        double u1 = 1.0 - UnityEngine.Random.value; //uniform(0,1] random doubles
        double u2 = 1.0 - UnityEngine.Random.value;
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
    }
}
