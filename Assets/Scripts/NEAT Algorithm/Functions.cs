using System;
using System.Collections.Generic;

// https://en.wikipedia.org/wiki/Activation_function
// https://stats.stackexchange.com/questions/115258/comprehensive-list-of-activation-functions-in-neural-networks-with-pros-cons
public class ACTIVATION
{
    public static double LOGISTIC(double x, bool derivate = false) {
        double y = 1.0 / (1.0 + Math.Pow(Math.E, x));
        if (!derivate)
            return y;
        else
            return y * (1 - y);
    }

    public static double SIGMOID(double x, bool derivate = false)
    {
        double y = 1.0 / (1.0 + Math.Pow(Math.E, -4.9 * x));
        if (!derivate)
            return y;
        else
            return y * (1 - y);
    }
    public static double TANH(double x, bool derivate = false)
    {
        if (derivate) return 1 - Math.Pow(Math.Tanh(x), 2);
        return Math.Tanh(x);
    }
    public static double IDENTITY(double x, bool derivate = false)
    {
        return (derivate) ? 1 : x;
    }
    public static double STEP(double x, bool derivate = false)
    {
        return (derivate) ? 0 : x > 0 ? 1 : 0;
    }
    public static double RELU(double x, bool derivate = false)
    {
        if (derivate) return x > 0 ? 1 : 0;
        return x > 0 ? x : 0;
    }
    public static double SOFTSIGN(double x, bool derivate = false)
    {
        var d = 1 + Math.Abs(x);
        if (derivate) return x / Math.Pow(d, 2);
        return x / d;
    }
    public static double SINUSOID(double x, bool derivate = false)
    {
        if (derivate) return Math.Cos(x);
        return Math.Sin(x);
    }
    public static double GAUSSIAN(double x, bool derivate = false)
    {
        var d = Math.Exp(-Math.Pow(x, 2));
        if (derivate) return -2 * x * d;
        return d;
    }
    public static double BENT_IDENTITY(double x, bool derivate = false)
    {
        var d = Math.Sqrt(Math.Pow(x, 2) + 1);
        if (derivate) return x / (2 * d) + 1;
        return (d - 1) / 2 + x;
    }
    public static double BIPOLAR(double x, bool derivate = false)
    {
        return derivate ? 0 : x > 0 ? 1 : -1;
    }
    public static double BIPOLAR_SIGMOID(double x, bool derivate = false)
    {
        var d = 2 / (1 + Math.Exp(-x)) - 1;
        if (derivate) return 1 / 2 * (1 + d) * (1 - d);
        return d;
    }
    public static double HARD_TANHfunction(double x, bool derivate = false)
    {
        if (derivate) return x > -1 && x < 1 ? 1 : 0;
        return Math.Max(-1, Math.Min(1, x));
    }
    public static double ABSOLUTE(double x, bool derivate = false)
    {
        if (derivate) return x < 0 ? -1 : 1;
        return Math.Abs(x);
    }
    public static double INVERSE(double x, bool derivate = false)
    {
        if (derivate) return -1;
        return 1 - x;
    }
  // https://arxiv.org/pdf/1706.02515.pdf
    public static double SELU(double x, bool derivate = false)
    {
        var alpha = 1.6732632423543772848170429916717;
        var scale = 1.0507009873554804934193349852946;
        var fx = x > 0 ? x : alpha * Math.Exp(x) - alpha;
        if (derivate) { return x > 0 ? scale : (fx + alpha) * scale; }
        return fx * scale;
    }

    
}


public class COST_FUNCTIONS
{
    public static double MEAN_SQUARED_ERROR(List<double> target, List<double> output)
    {
        double error = 0.0;
        for (int i = 0; i < output.Count; i++)
            error += Math.Pow(target[i] - output[i], 2);

        return error / output.Count;
    }
}

public class RATE_FUNCTIONS
{
    public static double FIXED(double baseRate, int iteration) { return baseRate; }
}