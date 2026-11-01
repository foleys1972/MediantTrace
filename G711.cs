namespace MediantTrace;

/// <summary>G.711 A-law and µ-law, encode and decode (ITU-T G.711 reference algorithm).</summary>
public static class G711
{
    public static byte LinearToAlaw(short pcm)
    {
        int sign = (pcm >> 8) & 0x80;
        int value = sign != 0 ? -pcm - 1 : pcm;
        if (value > 32635) value = 32635;
        int exponent, mantissa;
        if (value >= 256)
        {
            exponent = 7;
            for (int mask = 0x4000; (value & mask) == 0 && exponent > 1; mask >>= 1) exponent--;
            mantissa = (value >> (exponent + 3)) & 0x0F;
        }
        else
        {
            exponent = 0;
            mantissa = value >> 4;
        }
        int alaw = (exponent << 4) | mantissa;
        return (byte)((alaw ^ (sign == 0 ? 0xD5 : 0x55)) & 0xFF);
    }

    public static short AlawToLinear(byte alaw)
    {
        alaw ^= 0x55;
        int sign = alaw & 0x80;
        int exponent = (alaw & 0x70) >> 4;
        int value = (alaw & 0x0F) << 4;
        value += exponent == 0 ? 8 : 0x108;
        if (exponent > 1) value <<= exponent - 1;
        return (short)(sign != 0 ? value : -value);
    }

    private const int Bias = 0x84, Clip = 32635;

    public static byte LinearToUlaw(short pcm)
    {
        int sign = (pcm >> 8) & 0x80;
        int value = sign != 0 ? -pcm : pcm;
        if (value > Clip) value = Clip;
        value += Bias;
        int exponent = 7;
        for (int mask = 0x4000; (value & mask) == 0 && exponent > 0; mask >>= 1) exponent--;
        int mantissa = (value >> (exponent + 3)) & 0x0F;
        return (byte)~(sign | (exponent << 4) | mantissa);
    }

    public static short UlawToLinear(byte ulaw)
    {
        ulaw = (byte)~ulaw;
        int sign = ulaw & 0x80;
        int exponent = (ulaw >> 4) & 0x07;
        int mantissa = ulaw & 0x0F;
        int value = ((mantissa << 3) + Bias) << exponent;
        value -= Bias;
        return (short)(sign != 0 ? -value : value);
    }
}
