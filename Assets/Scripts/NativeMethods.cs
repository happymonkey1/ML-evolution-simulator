using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NativeMethods : MonoBehaviour
{
    public static List<int> POS_NEG_LIST = new List<int>() {-1, 1};
    public static float GetRandomFloat(float min, float max, bool randNegation = true) {
        return (randNegation) ? Random.Range(min, max) * POS_NEG_LIST[new System.Random().Next(POS_NEG_LIST.Count)] : Random.Range(min, max);
    }

    public static float PowOneOverE(float b) { return Mathf.Pow(b, InverseE); }

    public static float InverseE = (float)(1 / System.Math.E); 

    
}
