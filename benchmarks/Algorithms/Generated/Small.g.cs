namespace Tl.Algorithms;

public readonly struct SmallCode : IGenerated
{
    public static Fixture Create() => Fixture.Build(8, 256);

    public static void Tree<TSink>(int tick, Payload[] payloads, ref TSink sink)
        where TSink : struct, ISink
    {
        if (tick < 144)
        {
            if (tick < 56)
            {
                if (tick < 35)
                {
                    return;
                }
                else
                {
                    if (tick < 49)
                    {
                        sink.Sample(in payloads[0], 1f);
                        return;
                    }
                    else
                    {
                        return;
                    }
                }
            }
            else
            {
                if (tick < 98)
                {
                    if (tick < 89)
                    {
                        sink.Sample(in payloads[1], 1f);
                        return;
                    }
                    else
                    {
                        var factor = (float)(tick - 89) / 8f;
                        sink.Sample(in payloads[1], 1f - factor);
                        sink.Sample(in payloads[2], factor);
                        return;
                    }
                }
                else
                {
                    if (tick < 113)
                    {
                        sink.Sample(in payloads[2], 1f);
                        return;
                    }
                    else
                    {
                        sink.Sample(in payloads[3], 1f);
                        return;
                    }
                }
            }
        }
        else
        {
            if (tick < 203)
            {
                if (tick < 165)
                {
                    if (tick < 145)
                    {
                        var factor = 0.5f;
                        sink.Sample(in payloads[3], 1f - factor);
                        sink.Sample(in payloads[4], factor);
                        return;
                    }
                    else
                    {
                        sink.Sample(in payloads[4], 1f);
                        return;
                    }
                }
                else
                {
                    if (tick < 172)
                    {
                        return;
                    }
                    else
                    {
                        sink.Sample(in payloads[5], 1f);
                        return;
                    }
                }
            }
            else
            {
                if (tick < 227)
                {
                    if (tick < 212)
                    {
                        var factor = (float)(tick - 203) / 8f;
                        sink.Sample(in payloads[5], 1f - factor);
                        sink.Sample(in payloads[6], factor);
                        return;
                    }
                    else
                    {
                        sink.Sample(in payloads[6], 1f);
                        return;
                    }
                }
                else
                {
                    if (tick < 254)
                    {
                        sink.Sample(in payloads[7], 1f);
                        return;
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }
    }

    public static void State<TSink>(int tick, ref int state, Payload[] payloads, ref TSink sink)
        where TSink : struct, ISink
    {
        switch (state)
        {
            case 0:
            {
                if (tick >= 35)
                {
                    state = 1;
                    goto case 1;
                }
                return;
            }
            case 1:
            {
                if (tick >= 49)
                {
                    state = 2;
                    goto case 2;
                }
                sink.Sample(in payloads[0], 1f);
                return;
            }
            case 2:
            {
                if (tick >= 56)
                {
                    state = 3;
                    goto case 3;
                }
                return;
            }
            case 3:
            {
                if (tick >= 89)
                {
                    state = 4;
                    goto case 4;
                }
                sink.Sample(in payloads[1], 1f);
                return;
            }
            case 4:
            {
                if (tick >= 98)
                {
                    state = 5;
                    goto case 5;
                }
                var factor = (float)(tick - 89) / 8f;
                sink.Sample(in payloads[1], 1f - factor);
                sink.Sample(in payloads[2], factor);
                return;
            }
            case 5:
            {
                if (tick >= 113)
                {
                    state = 6;
                    goto case 6;
                }
                sink.Sample(in payloads[2], 1f);
                return;
            }
            case 6:
            {
                if (tick >= 144)
                {
                    state = 7;
                    goto case 7;
                }
                sink.Sample(in payloads[3], 1f);
                return;
            }
            case 7:
            {
                if (tick >= 145)
                {
                    state = 8;
                    goto case 8;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[3], 1f - factor);
                sink.Sample(in payloads[4], factor);
                return;
            }
            case 8:
            {
                if (tick >= 165)
                {
                    state = 9;
                    goto case 9;
                }
                sink.Sample(in payloads[4], 1f);
                return;
            }
            case 9:
            {
                if (tick >= 172)
                {
                    state = 10;
                    goto case 10;
                }
                return;
            }
            case 10:
            {
                if (tick >= 203)
                {
                    state = 11;
                    goto case 11;
                }
                sink.Sample(in payloads[5], 1f);
                return;
            }
            case 11:
            {
                if (tick >= 212)
                {
                    state = 12;
                    goto case 12;
                }
                var factor = (float)(tick - 203) / 8f;
                sink.Sample(in payloads[5], 1f - factor);
                sink.Sample(in payloads[6], factor);
                return;
            }
            case 12:
            {
                if (tick >= 227)
                {
                    state = 13;
                    goto case 13;
                }
                sink.Sample(in payloads[6], 1f);
                return;
            }
            case 13:
            {
                if (tick >= 254)
                {
                    state = 14;
                    goto case 14;
                }
                sink.Sample(in payloads[7], 1f);
                return;
            }
            case 14:
            {
                
                return;
            }
        }
    }
}