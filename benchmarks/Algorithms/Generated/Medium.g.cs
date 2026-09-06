namespace Tl.Algorithms;

public readonly struct MediumCode : IGenerated
{
    public static Fixture Create() => Fixture.Build(64, 4096);

    public static void Tree<TSink>(int tick, Payload[] payloads, ref TSink sink)
        where TSink : struct, ISink
    {
        if (tick < 2098)
        {
            if (tick < 1085)
            {
                if (tick < 568)
                {
                    if (tick < 322)
                    {
                        if (tick < 126)
                        {
                            if (tick < 73)
                            {
                                return;
                            }
                            else
                            {
                                if (tick < 111)
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
                            if (tick < 230)
                            {
                                if (tick < 209)
                                {
                                    sink.Sample(in payloads[1], 1f);
                                    return;
                                }
                                else
                                {
                                    var factor = (float)(tick - 209) / 20f;
                                    sink.Sample(in payloads[1], 1f - factor);
                                    sink.Sample(in payloads[2], factor);
                                    return;
                                }
                            }
                            else
                            {
                                if (tick < 265)
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
                        if (tick < 379)
                        {
                            if (tick < 323)
                            {
                                var factor = 0.5f;
                                sink.Sample(in payloads[3], 1f - factor);
                                sink.Sample(in payloads[4], factor);
                                return;
                            }
                            else
                            {
                                if (tick < 364)
                                {
                                    sink.Sample(in payloads[4], 1f);
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
                            if (tick < 481)
                            {
                                if (tick < 460)
                                {
                                    sink.Sample(in payloads[5], 1f);
                                    return;
                                }
                                else
                                {
                                    var factor = (float)(tick - 460) / 20f;
                                    sink.Sample(in payloads[5], 1f - factor);
                                    sink.Sample(in payloads[6], factor);
                                    return;
                                }
                            }
                            else
                            {
                                if (tick < 507)
                                {
                                    sink.Sample(in payloads[6], 1f);
                                    return;
                                }
                                else
                                {
                                    sink.Sample(in payloads[7], 1f);
                                    return;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (tick < 819)
                    {
                        if (tick < 641)
                        {
                            if (tick < 569)
                            {
                                var factor = 0.5f;
                                sink.Sample(in payloads[7], 1f - factor);
                                sink.Sample(in payloads[8], factor);
                                return;
                            }
                            else
                            {
                                if (tick < 626)
                                {
                                    sink.Sample(in payloads[8], 1f);
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
                            if (tick < 731)
                            {
                                if (tick < 710)
                                {
                                    sink.Sample(in payloads[9], 1f);
                                    return;
                                }
                                else
                                {
                                    var factor = (float)(tick - 710) / 20f;
                                    sink.Sample(in payloads[9], 1f - factor);
                                    sink.Sample(in payloads[10], factor);
                                    return;
                                }
                            }
                            else
                            {
                                if (tick < 766)
                                {
                                    sink.Sample(in payloads[10], 1f);
                                    return;
                                }
                                else
                                {
                                    sink.Sample(in payloads[11], 1f);
                                    return;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (tick < 897)
                        {
                            if (tick < 820)
                            {
                                var factor = 0.5f;
                                sink.Sample(in payloads[11], 1f - factor);
                                sink.Sample(in payloads[12], factor);
                                return;
                            }
                            else
                            {
                                if (tick < 882)
                                {
                                    sink.Sample(in payloads[12], 1f);
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
                            if (tick < 973)
                            {
                                if (tick < 952)
                                {
                                    sink.Sample(in payloads[13], 1f);
                                    return;
                                }
                                else
                                {
                                    var factor = (float)(tick - 952) / 20f;
                                    sink.Sample(in payloads[13], 1f - factor);
                                    sink.Sample(in payloads[14], factor);
                                    return;
                                }
                            }
                            else
                            {
                                if (tick < 1023)
                                {
                                    sink.Sample(in payloads[14], 1f);
                                    return;
                                }
                                else
                                {
                                    sink.Sample(in payloads[15], 1f);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (tick < 1585)
                {
                    if (tick < 1338)
                    {
                        if (tick < 1154)
                        {
                            if (tick < 1086)
                            {
                                var factor = 0.5f;
                                sink.Sample(in payloads[15], 1f - factor);
                                sink.Sample(in payloads[16], factor);
                                return;
                            }
                            else
                            {
                                if (tick < 1139)
                                {
                                    sink.Sample(in payloads[16], 1f);
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
                            if (tick < 1224)
                            {
                                if (tick < 1203)
                                {
                                    sink.Sample(in payloads[17], 1f);
                                    return;
                                }
                                else
                                {
                                    var factor = (float)(tick - 1203) / 20f;
                                    sink.Sample(in payloads[17], 1f - factor);
                                    sink.Sample(in payloads[18], factor);
                                    return;
                                }
                            }
                            else
                            {
                                if (tick < 1270)
                                {
                                    sink.Sample(in payloads[18], 1f);
                                    return;
                                }
                                else
                                {
                                    sink.Sample(in payloads[19], 1f);
                                    return;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (tick < 1400)
                        {
                            if (tick < 1339)
                            {
                                var factor = 0.5f;
                                sink.Sample(in payloads[19], 1f - factor);
                                sink.Sample(in payloads[20], factor);
                                return;
                            }
                            else
                            {
                                if (tick < 1385)
                                {
                                    sink.Sample(in payloads[20], 1f);
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
                            if (tick < 1475)
                            {
                                if (tick < 1454)
                                {
                                    sink.Sample(in payloads[21], 1f);
                                    return;
                                }
                                else
                                {
                                    var factor = (float)(tick - 1454) / 20f;
                                    sink.Sample(in payloads[21], 1f - factor);
                                    sink.Sample(in payloads[22], factor);
                                    return;
                                }
                            }
                            else
                            {
                                if (tick < 1514)
                                {
                                    sink.Sample(in payloads[22], 1f);
                                    return;
                                }
                                else
                                {
                                    sink.Sample(in payloads[23], 1f);
                                    return;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (tick < 1847)
                    {
                        if (tick < 1640)
                        {
                            if (tick < 1586)
                            {
                                var factor = 0.5f;
                                sink.Sample(in payloads[23], 1f - factor);
                                sink.Sample(in payloads[24], factor);
                                return;
                            }
                            else
                            {
                                if (tick < 1625)
                                {
                                    sink.Sample(in payloads[24], 1f);
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
                            if (tick < 1725)
                            {
                                if (tick < 1704)
                                {
                                    sink.Sample(in payloads[25], 1f);
                                    return;
                                }
                                else
                                {
                                    var factor = (float)(tick - 1704) / 20f;
                                    sink.Sample(in payloads[25], 1f - factor);
                                    sink.Sample(in payloads[26], factor);
                                    return;
                                }
                            }
                            else
                            {
                                if (tick < 1780)
                                {
                                    sink.Sample(in payloads[26], 1f);
                                    return;
                                }
                                else
                                {
                                    sink.Sample(in payloads[27], 1f);
                                    return;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (tick < 1906)
                        {
                            if (tick < 1848)
                            {
                                var factor = 0.5f;
                                sink.Sample(in payloads[27], 1f - factor);
                                sink.Sample(in payloads[28], factor);
                                return;
                            }
                            else
                            {
                                if (tick < 1891)
                                {
                                    sink.Sample(in payloads[28], 1f);
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
                            if (tick < 1986)
                            {
                                if (tick < 1965)
                                {
                                    sink.Sample(in payloads[29], 1f);
                                    return;
                                }
                                else
                                {
                                    var factor = (float)(tick - 1965) / 20f;
                                    sink.Sample(in payloads[29], 1f - factor);
                                    sink.Sample(in payloads[30], factor);
                                    return;
                                }
                            }
                            else
                            {
                                if (tick < 2019)
                                {
                                    sink.Sample(in payloads[30], 1f);
                                    return;
                                }
                                else
                                {
                                    sink.Sample(in payloads[31], 1f);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }
        else
        {
            if (tick < 3087)
            {
                if (tick < 2584)
                {
                    if (tick < 2333)
                    {
                        if (tick < 2161)
                        {
                            if (tick < 2099)
                            {
                                var factor = 0.5f;
                                sink.Sample(in payloads[31], 1f - factor);
                                sink.Sample(in payloads[32], factor);
                                return;
                            }
                            else
                            {
                                if (tick < 2146)
                                {
                                    sink.Sample(in payloads[32], 1f);
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
                            if (tick < 2232)
                            {
                                if (tick < 2211)
                                {
                                    sink.Sample(in payloads[33], 1f);
                                    return;
                                }
                                else
                                {
                                    var factor = (float)(tick - 2211) / 20f;
                                    sink.Sample(in payloads[33], 1f - factor);
                                    sink.Sample(in payloads[34], factor);
                                    return;
                                }
                            }
                            else
                            {
                                if (tick < 2281)
                                {
                                    sink.Sample(in payloads[34], 1f);
                                    return;
                                }
                                else
                                {
                                    sink.Sample(in payloads[35], 1f);
                                    return;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (tick < 2404)
                        {
                            if (tick < 2334)
                            {
                                var factor = 0.5f;
                                sink.Sample(in payloads[35], 1f - factor);
                                sink.Sample(in payloads[36], factor);
                                return;
                            }
                            else
                            {
                                if (tick < 2389)
                                {
                                    sink.Sample(in payloads[36], 1f);
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
                            if (tick < 2496)
                            {
                                if (tick < 2475)
                                {
                                    sink.Sample(in payloads[37], 1f);
                                    return;
                                }
                                else
                                {
                                    var factor = (float)(tick - 2475) / 20f;
                                    sink.Sample(in payloads[37], 1f - factor);
                                    sink.Sample(in payloads[38], factor);
                                    return;
                                }
                            }
                            else
                            {
                                if (tick < 2529)
                                {
                                    sink.Sample(in payloads[38], 1f);
                                    return;
                                }
                                else
                                {
                                    sink.Sample(in payloads[39], 1f);
                                    return;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (tick < 2836)
                    {
                        if (tick < 2647)
                        {
                            if (tick < 2585)
                            {
                                var factor = 0.5f;
                                sink.Sample(in payloads[39], 1f - factor);
                                sink.Sample(in payloads[40], factor);
                                return;
                            }
                            else
                            {
                                if (tick < 2632)
                                {
                                    sink.Sample(in payloads[40], 1f);
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
                            if (tick < 2738)
                            {
                                if (tick < 2717)
                                {
                                    sink.Sample(in payloads[41], 1f);
                                    return;
                                }
                                else
                                {
                                    var factor = (float)(tick - 2717) / 20f;
                                    sink.Sample(in payloads[41], 1f - factor);
                                    sink.Sample(in payloads[42], factor);
                                    return;
                                }
                            }
                            else
                            {
                                if (tick < 2775)
                                {
                                    sink.Sample(in payloads[42], 1f);
                                    return;
                                }
                                else
                                {
                                    sink.Sample(in payloads[43], 1f);
                                    return;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (tick < 2911)
                        {
                            if (tick < 2837)
                            {
                                var factor = 0.5f;
                                sink.Sample(in payloads[43], 1f - factor);
                                sink.Sample(in payloads[44], factor);
                                return;
                            }
                            else
                            {
                                if (tick < 2896)
                                {
                                    sink.Sample(in payloads[44], 1f);
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
                            if (tick < 3000)
                            {
                                if (tick < 2979)
                                {
                                    sink.Sample(in payloads[45], 1f);
                                    return;
                                }
                                else
                                {
                                    var factor = (float)(tick - 2979) / 20f;
                                    sink.Sample(in payloads[45], 1f - factor);
                                    sink.Sample(in payloads[46], factor);
                                    return;
                                }
                            }
                            else
                            {
                                if (tick < 3027)
                                {
                                    sink.Sample(in payloads[46], 1f);
                                    return;
                                }
                                else
                                {
                                    sink.Sample(in payloads[47], 1f);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (tick < 3605)
                {
                    if (tick < 3348)
                    {
                        if (tick < 3158)
                        {
                            if (tick < 3088)
                            {
                                var factor = 0.5f;
                                sink.Sample(in payloads[47], 1f - factor);
                                sink.Sample(in payloads[48], factor);
                                return;
                            }
                            else
                            {
                                if (tick < 3143)
                                {
                                    sink.Sample(in payloads[48], 1f);
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
                            if (tick < 3249)
                            {
                                if (tick < 3228)
                                {
                                    sink.Sample(in payloads[49], 1f);
                                    return;
                                }
                                else
                                {
                                    var factor = (float)(tick - 3228) / 20f;
                                    sink.Sample(in payloads[49], 1f - factor);
                                    sink.Sample(in payloads[50], factor);
                                    return;
                                }
                            }
                            else
                            {
                                if (tick < 3276)
                                {
                                    sink.Sample(in payloads[50], 1f);
                                    return;
                                }
                                else
                                {
                                    sink.Sample(in payloads[51], 1f);
                                    return;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (tick < 3404)
                        {
                            if (tick < 3349)
                            {
                                var factor = 0.5f;
                                sink.Sample(in payloads[51], 1f - factor);
                                sink.Sample(in payloads[52], factor);
                                return;
                            }
                            else
                            {
                                if (tick < 3389)
                                {
                                    sink.Sample(in payloads[52], 1f);
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
                            if (tick < 3499)
                            {
                                if (tick < 3478)
                                {
                                    sink.Sample(in payloads[53], 1f);
                                    return;
                                }
                                else
                                {
                                    var factor = (float)(tick - 3478) / 20f;
                                    sink.Sample(in payloads[53], 1f - factor);
                                    sink.Sample(in payloads[54], factor);
                                    return;
                                }
                            }
                            else
                            {
                                if (tick < 3540)
                                {
                                    sink.Sample(in payloads[54], 1f);
                                    return;
                                }
                                else
                                {
                                    sink.Sample(in payloads[55], 1f);
                                    return;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (tick < 3862)
                    {
                        if (tick < 3656)
                        {
                            if (tick < 3606)
                            {
                                var factor = 0.5f;
                                sink.Sample(in payloads[55], 1f - factor);
                                sink.Sample(in payloads[56], factor);
                                return;
                            }
                            else
                            {
                                if (tick < 3641)
                                {
                                    sink.Sample(in payloads[56], 1f);
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
                            if (tick < 3749)
                            {
                                if (tick < 3728)
                                {
                                    sink.Sample(in payloads[57], 1f);
                                    return;
                                }
                                else
                                {
                                    var factor = (float)(tick - 3728) / 20f;
                                    sink.Sample(in payloads[57], 1f - factor);
                                    sink.Sample(in payloads[58], factor);
                                    return;
                                }
                            }
                            else
                            {
                                if (tick < 3799)
                                {
                                    sink.Sample(in payloads[58], 1f);
                                    return;
                                }
                                else
                                {
                                    sink.Sample(in payloads[59], 1f);
                                    return;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (tick < 3973)
                        {
                            if (tick < 3911)
                            {
                                if (tick < 3863)
                                {
                                    var factor = 0.5f;
                                    sink.Sample(in payloads[59], 1f - factor);
                                    sink.Sample(in payloads[60], factor);
                                    return;
                                }
                                else
                                {
                                    sink.Sample(in payloads[60], 1f);
                                    return;
                                }
                            }
                            else
                            {
                                if (tick < 3926)
                                {
                                    return;
                                }
                                else
                                {
                                    sink.Sample(in payloads[61], 1f);
                                    return;
                                }
                            }
                        }
                        else
                        {
                            if (tick < 4044)
                            {
                                if (tick < 3994)
                                {
                                    var factor = (float)(tick - 3973) / 20f;
                                    sink.Sample(in payloads[61], 1f - factor);
                                    sink.Sample(in payloads[62], factor);
                                    return;
                                }
                                else
                                {
                                    sink.Sample(in payloads[62], 1f);
                                    return;
                                }
                            }
                            else
                            {
                                if (tick < 4094)
                                {
                                    sink.Sample(in payloads[63], 1f);
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
                if (tick >= 73)
                {
                    state = 1;
                    goto case 1;
                }
                return;
            }
            case 1:
            {
                if (tick >= 111)
                {
                    state = 2;
                    goto case 2;
                }
                sink.Sample(in payloads[0], 1f);
                return;
            }
            case 2:
            {
                if (tick >= 126)
                {
                    state = 3;
                    goto case 3;
                }
                return;
            }
            case 3:
            {
                if (tick >= 209)
                {
                    state = 4;
                    goto case 4;
                }
                sink.Sample(in payloads[1], 1f);
                return;
            }
            case 4:
            {
                if (tick >= 230)
                {
                    state = 5;
                    goto case 5;
                }
                var factor = (float)(tick - 209) / 20f;
                sink.Sample(in payloads[1], 1f - factor);
                sink.Sample(in payloads[2], factor);
                return;
            }
            case 5:
            {
                if (tick >= 265)
                {
                    state = 6;
                    goto case 6;
                }
                sink.Sample(in payloads[2], 1f);
                return;
            }
            case 6:
            {
                if (tick >= 322)
                {
                    state = 7;
                    goto case 7;
                }
                sink.Sample(in payloads[3], 1f);
                return;
            }
            case 7:
            {
                if (tick >= 323)
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
                if (tick >= 364)
                {
                    state = 9;
                    goto case 9;
                }
                sink.Sample(in payloads[4], 1f);
                return;
            }
            case 9:
            {
                if (tick >= 379)
                {
                    state = 10;
                    goto case 10;
                }
                return;
            }
            case 10:
            {
                if (tick >= 460)
                {
                    state = 11;
                    goto case 11;
                }
                sink.Sample(in payloads[5], 1f);
                return;
            }
            case 11:
            {
                if (tick >= 481)
                {
                    state = 12;
                    goto case 12;
                }
                var factor = (float)(tick - 460) / 20f;
                sink.Sample(in payloads[5], 1f - factor);
                sink.Sample(in payloads[6], factor);
                return;
            }
            case 12:
            {
                if (tick >= 507)
                {
                    state = 13;
                    goto case 13;
                }
                sink.Sample(in payloads[6], 1f);
                return;
            }
            case 13:
            {
                if (tick >= 568)
                {
                    state = 14;
                    goto case 14;
                }
                sink.Sample(in payloads[7], 1f);
                return;
            }
            case 14:
            {
                if (tick >= 569)
                {
                    state = 15;
                    goto case 15;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[7], 1f - factor);
                sink.Sample(in payloads[8], factor);
                return;
            }
            case 15:
            {
                if (tick >= 626)
                {
                    state = 16;
                    goto case 16;
                }
                sink.Sample(in payloads[8], 1f);
                return;
            }
            case 16:
            {
                if (tick >= 641)
                {
                    state = 17;
                    goto case 17;
                }
                return;
            }
            case 17:
            {
                if (tick >= 710)
                {
                    state = 18;
                    goto case 18;
                }
                sink.Sample(in payloads[9], 1f);
                return;
            }
            case 18:
            {
                if (tick >= 731)
                {
                    state = 19;
                    goto case 19;
                }
                var factor = (float)(tick - 710) / 20f;
                sink.Sample(in payloads[9], 1f - factor);
                sink.Sample(in payloads[10], factor);
                return;
            }
            case 19:
            {
                if (tick >= 766)
                {
                    state = 20;
                    goto case 20;
                }
                sink.Sample(in payloads[10], 1f);
                return;
            }
            case 20:
            {
                if (tick >= 819)
                {
                    state = 21;
                    goto case 21;
                }
                sink.Sample(in payloads[11], 1f);
                return;
            }
            case 21:
            {
                if (tick >= 820)
                {
                    state = 22;
                    goto case 22;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[11], 1f - factor);
                sink.Sample(in payloads[12], factor);
                return;
            }
            case 22:
            {
                if (tick >= 882)
                {
                    state = 23;
                    goto case 23;
                }
                sink.Sample(in payloads[12], 1f);
                return;
            }
            case 23:
            {
                if (tick >= 897)
                {
                    state = 24;
                    goto case 24;
                }
                return;
            }
            case 24:
            {
                if (tick >= 952)
                {
                    state = 25;
                    goto case 25;
                }
                sink.Sample(in payloads[13], 1f);
                return;
            }
            case 25:
            {
                if (tick >= 973)
                {
                    state = 26;
                    goto case 26;
                }
                var factor = (float)(tick - 952) / 20f;
                sink.Sample(in payloads[13], 1f - factor);
                sink.Sample(in payloads[14], factor);
                return;
            }
            case 26:
            {
                if (tick >= 1023)
                {
                    state = 27;
                    goto case 27;
                }
                sink.Sample(in payloads[14], 1f);
                return;
            }
            case 27:
            {
                if (tick >= 1085)
                {
                    state = 28;
                    goto case 28;
                }
                sink.Sample(in payloads[15], 1f);
                return;
            }
            case 28:
            {
                if (tick >= 1086)
                {
                    state = 29;
                    goto case 29;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[15], 1f - factor);
                sink.Sample(in payloads[16], factor);
                return;
            }
            case 29:
            {
                if (tick >= 1139)
                {
                    state = 30;
                    goto case 30;
                }
                sink.Sample(in payloads[16], 1f);
                return;
            }
            case 30:
            {
                if (tick >= 1154)
                {
                    state = 31;
                    goto case 31;
                }
                return;
            }
            case 31:
            {
                if (tick >= 1203)
                {
                    state = 32;
                    goto case 32;
                }
                sink.Sample(in payloads[17], 1f);
                return;
            }
            case 32:
            {
                if (tick >= 1224)
                {
                    state = 33;
                    goto case 33;
                }
                var factor = (float)(tick - 1203) / 20f;
                sink.Sample(in payloads[17], 1f - factor);
                sink.Sample(in payloads[18], factor);
                return;
            }
            case 33:
            {
                if (tick >= 1270)
                {
                    state = 34;
                    goto case 34;
                }
                sink.Sample(in payloads[18], 1f);
                return;
            }
            case 34:
            {
                if (tick >= 1338)
                {
                    state = 35;
                    goto case 35;
                }
                sink.Sample(in payloads[19], 1f);
                return;
            }
            case 35:
            {
                if (tick >= 1339)
                {
                    state = 36;
                    goto case 36;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[19], 1f - factor);
                sink.Sample(in payloads[20], factor);
                return;
            }
            case 36:
            {
                if (tick >= 1385)
                {
                    state = 37;
                    goto case 37;
                }
                sink.Sample(in payloads[20], 1f);
                return;
            }
            case 37:
            {
                if (tick >= 1400)
                {
                    state = 38;
                    goto case 38;
                }
                return;
            }
            case 38:
            {
                if (tick >= 1454)
                {
                    state = 39;
                    goto case 39;
                }
                sink.Sample(in payloads[21], 1f);
                return;
            }
            case 39:
            {
                if (tick >= 1475)
                {
                    state = 40;
                    goto case 40;
                }
                var factor = (float)(tick - 1454) / 20f;
                sink.Sample(in payloads[21], 1f - factor);
                sink.Sample(in payloads[22], factor);
                return;
            }
            case 40:
            {
                if (tick >= 1514)
                {
                    state = 41;
                    goto case 41;
                }
                sink.Sample(in payloads[22], 1f);
                return;
            }
            case 41:
            {
                if (tick >= 1585)
                {
                    state = 42;
                    goto case 42;
                }
                sink.Sample(in payloads[23], 1f);
                return;
            }
            case 42:
            {
                if (tick >= 1586)
                {
                    state = 43;
                    goto case 43;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[23], 1f - factor);
                sink.Sample(in payloads[24], factor);
                return;
            }
            case 43:
            {
                if (tick >= 1625)
                {
                    state = 44;
                    goto case 44;
                }
                sink.Sample(in payloads[24], 1f);
                return;
            }
            case 44:
            {
                if (tick >= 1640)
                {
                    state = 45;
                    goto case 45;
                }
                return;
            }
            case 45:
            {
                if (tick >= 1704)
                {
                    state = 46;
                    goto case 46;
                }
                sink.Sample(in payloads[25], 1f);
                return;
            }
            case 46:
            {
                if (tick >= 1725)
                {
                    state = 47;
                    goto case 47;
                }
                var factor = (float)(tick - 1704) / 20f;
                sink.Sample(in payloads[25], 1f - factor);
                sink.Sample(in payloads[26], factor);
                return;
            }
            case 47:
            {
                if (tick >= 1780)
                {
                    state = 48;
                    goto case 48;
                }
                sink.Sample(in payloads[26], 1f);
                return;
            }
            case 48:
            {
                if (tick >= 1847)
                {
                    state = 49;
                    goto case 49;
                }
                sink.Sample(in payloads[27], 1f);
                return;
            }
            case 49:
            {
                if (tick >= 1848)
                {
                    state = 50;
                    goto case 50;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[27], 1f - factor);
                sink.Sample(in payloads[28], factor);
                return;
            }
            case 50:
            {
                if (tick >= 1891)
                {
                    state = 51;
                    goto case 51;
                }
                sink.Sample(in payloads[28], 1f);
                return;
            }
            case 51:
            {
                if (tick >= 1906)
                {
                    state = 52;
                    goto case 52;
                }
                return;
            }
            case 52:
            {
                if (tick >= 1965)
                {
                    state = 53;
                    goto case 53;
                }
                sink.Sample(in payloads[29], 1f);
                return;
            }
            case 53:
            {
                if (tick >= 1986)
                {
                    state = 54;
                    goto case 54;
                }
                var factor = (float)(tick - 1965) / 20f;
                sink.Sample(in payloads[29], 1f - factor);
                sink.Sample(in payloads[30], factor);
                return;
            }
            case 54:
            {
                if (tick >= 2019)
                {
                    state = 55;
                    goto case 55;
                }
                sink.Sample(in payloads[30], 1f);
                return;
            }
            case 55:
            {
                if (tick >= 2098)
                {
                    state = 56;
                    goto case 56;
                }
                sink.Sample(in payloads[31], 1f);
                return;
            }
            case 56:
            {
                if (tick >= 2099)
                {
                    state = 57;
                    goto case 57;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[31], 1f - factor);
                sink.Sample(in payloads[32], factor);
                return;
            }
            case 57:
            {
                if (tick >= 2146)
                {
                    state = 58;
                    goto case 58;
                }
                sink.Sample(in payloads[32], 1f);
                return;
            }
            case 58:
            {
                if (tick >= 2161)
                {
                    state = 59;
                    goto case 59;
                }
                return;
            }
            case 59:
            {
                if (tick >= 2211)
                {
                    state = 60;
                    goto case 60;
                }
                sink.Sample(in payloads[33], 1f);
                return;
            }
            case 60:
            {
                if (tick >= 2232)
                {
                    state = 61;
                    goto case 61;
                }
                var factor = (float)(tick - 2211) / 20f;
                sink.Sample(in payloads[33], 1f - factor);
                sink.Sample(in payloads[34], factor);
                return;
            }
            case 61:
            {
                if (tick >= 2281)
                {
                    state = 62;
                    goto case 62;
                }
                sink.Sample(in payloads[34], 1f);
                return;
            }
            case 62:
            {
                if (tick >= 2333)
                {
                    state = 63;
                    goto case 63;
                }
                sink.Sample(in payloads[35], 1f);
                return;
            }
            case 63:
            {
                if (tick >= 2334)
                {
                    state = 64;
                    goto case 64;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[35], 1f - factor);
                sink.Sample(in payloads[36], factor);
                return;
            }
            case 64:
            {
                if (tick >= 2389)
                {
                    state = 65;
                    goto case 65;
                }
                sink.Sample(in payloads[36], 1f);
                return;
            }
            case 65:
            {
                if (tick >= 2404)
                {
                    state = 66;
                    goto case 66;
                }
                return;
            }
            case 66:
            {
                if (tick >= 2475)
                {
                    state = 67;
                    goto case 67;
                }
                sink.Sample(in payloads[37], 1f);
                return;
            }
            case 67:
            {
                if (tick >= 2496)
                {
                    state = 68;
                    goto case 68;
                }
                var factor = (float)(tick - 2475) / 20f;
                sink.Sample(in payloads[37], 1f - factor);
                sink.Sample(in payloads[38], factor);
                return;
            }
            case 68:
            {
                if (tick >= 2529)
                {
                    state = 69;
                    goto case 69;
                }
                sink.Sample(in payloads[38], 1f);
                return;
            }
            case 69:
            {
                if (tick >= 2584)
                {
                    state = 70;
                    goto case 70;
                }
                sink.Sample(in payloads[39], 1f);
                return;
            }
            case 70:
            {
                if (tick >= 2585)
                {
                    state = 71;
                    goto case 71;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[39], 1f - factor);
                sink.Sample(in payloads[40], factor);
                return;
            }
            case 71:
            {
                if (tick >= 2632)
                {
                    state = 72;
                    goto case 72;
                }
                sink.Sample(in payloads[40], 1f);
                return;
            }
            case 72:
            {
                if (tick >= 2647)
                {
                    state = 73;
                    goto case 73;
                }
                return;
            }
            case 73:
            {
                if (tick >= 2717)
                {
                    state = 74;
                    goto case 74;
                }
                sink.Sample(in payloads[41], 1f);
                return;
            }
            case 74:
            {
                if (tick >= 2738)
                {
                    state = 75;
                    goto case 75;
                }
                var factor = (float)(tick - 2717) / 20f;
                sink.Sample(in payloads[41], 1f - factor);
                sink.Sample(in payloads[42], factor);
                return;
            }
            case 75:
            {
                if (tick >= 2775)
                {
                    state = 76;
                    goto case 76;
                }
                sink.Sample(in payloads[42], 1f);
                return;
            }
            case 76:
            {
                if (tick >= 2836)
                {
                    state = 77;
                    goto case 77;
                }
                sink.Sample(in payloads[43], 1f);
                return;
            }
            case 77:
            {
                if (tick >= 2837)
                {
                    state = 78;
                    goto case 78;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[43], 1f - factor);
                sink.Sample(in payloads[44], factor);
                return;
            }
            case 78:
            {
                if (tick >= 2896)
                {
                    state = 79;
                    goto case 79;
                }
                sink.Sample(in payloads[44], 1f);
                return;
            }
            case 79:
            {
                if (tick >= 2911)
                {
                    state = 80;
                    goto case 80;
                }
                return;
            }
            case 80:
            {
                if (tick >= 2979)
                {
                    state = 81;
                    goto case 81;
                }
                sink.Sample(in payloads[45], 1f);
                return;
            }
            case 81:
            {
                if (tick >= 3000)
                {
                    state = 82;
                    goto case 82;
                }
                var factor = (float)(tick - 2979) / 20f;
                sink.Sample(in payloads[45], 1f - factor);
                sink.Sample(in payloads[46], factor);
                return;
            }
            case 82:
            {
                if (tick >= 3027)
                {
                    state = 83;
                    goto case 83;
                }
                sink.Sample(in payloads[46], 1f);
                return;
            }
            case 83:
            {
                if (tick >= 3087)
                {
                    state = 84;
                    goto case 84;
                }
                sink.Sample(in payloads[47], 1f);
                return;
            }
            case 84:
            {
                if (tick >= 3088)
                {
                    state = 85;
                    goto case 85;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[47], 1f - factor);
                sink.Sample(in payloads[48], factor);
                return;
            }
            case 85:
            {
                if (tick >= 3143)
                {
                    state = 86;
                    goto case 86;
                }
                sink.Sample(in payloads[48], 1f);
                return;
            }
            case 86:
            {
                if (tick >= 3158)
                {
                    state = 87;
                    goto case 87;
                }
                return;
            }
            case 87:
            {
                if (tick >= 3228)
                {
                    state = 88;
                    goto case 88;
                }
                sink.Sample(in payloads[49], 1f);
                return;
            }
            case 88:
            {
                if (tick >= 3249)
                {
                    state = 89;
                    goto case 89;
                }
                var factor = (float)(tick - 3228) / 20f;
                sink.Sample(in payloads[49], 1f - factor);
                sink.Sample(in payloads[50], factor);
                return;
            }
            case 89:
            {
                if (tick >= 3276)
                {
                    state = 90;
                    goto case 90;
                }
                sink.Sample(in payloads[50], 1f);
                return;
            }
            case 90:
            {
                if (tick >= 3348)
                {
                    state = 91;
                    goto case 91;
                }
                sink.Sample(in payloads[51], 1f);
                return;
            }
            case 91:
            {
                if (tick >= 3349)
                {
                    state = 92;
                    goto case 92;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[51], 1f - factor);
                sink.Sample(in payloads[52], factor);
                return;
            }
            case 92:
            {
                if (tick >= 3389)
                {
                    state = 93;
                    goto case 93;
                }
                sink.Sample(in payloads[52], 1f);
                return;
            }
            case 93:
            {
                if (tick >= 3404)
                {
                    state = 94;
                    goto case 94;
                }
                return;
            }
            case 94:
            {
                if (tick >= 3478)
                {
                    state = 95;
                    goto case 95;
                }
                sink.Sample(in payloads[53], 1f);
                return;
            }
            case 95:
            {
                if (tick >= 3499)
                {
                    state = 96;
                    goto case 96;
                }
                var factor = (float)(tick - 3478) / 20f;
                sink.Sample(in payloads[53], 1f - factor);
                sink.Sample(in payloads[54], factor);
                return;
            }
            case 96:
            {
                if (tick >= 3540)
                {
                    state = 97;
                    goto case 97;
                }
                sink.Sample(in payloads[54], 1f);
                return;
            }
            case 97:
            {
                if (tick >= 3605)
                {
                    state = 98;
                    goto case 98;
                }
                sink.Sample(in payloads[55], 1f);
                return;
            }
            case 98:
            {
                if (tick >= 3606)
                {
                    state = 99;
                    goto case 99;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[55], 1f - factor);
                sink.Sample(in payloads[56], factor);
                return;
            }
            case 99:
            {
                if (tick >= 3641)
                {
                    state = 100;
                    goto case 100;
                }
                sink.Sample(in payloads[56], 1f);
                return;
            }
            case 100:
            {
                if (tick >= 3656)
                {
                    state = 101;
                    goto case 101;
                }
                return;
            }
            case 101:
            {
                if (tick >= 3728)
                {
                    state = 102;
                    goto case 102;
                }
                sink.Sample(in payloads[57], 1f);
                return;
            }
            case 102:
            {
                if (tick >= 3749)
                {
                    state = 103;
                    goto case 103;
                }
                var factor = (float)(tick - 3728) / 20f;
                sink.Sample(in payloads[57], 1f - factor);
                sink.Sample(in payloads[58], factor);
                return;
            }
            case 103:
            {
                if (tick >= 3799)
                {
                    state = 104;
                    goto case 104;
                }
                sink.Sample(in payloads[58], 1f);
                return;
            }
            case 104:
            {
                if (tick >= 3862)
                {
                    state = 105;
                    goto case 105;
                }
                sink.Sample(in payloads[59], 1f);
                return;
            }
            case 105:
            {
                if (tick >= 3863)
                {
                    state = 106;
                    goto case 106;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[59], 1f - factor);
                sink.Sample(in payloads[60], factor);
                return;
            }
            case 106:
            {
                if (tick >= 3911)
                {
                    state = 107;
                    goto case 107;
                }
                sink.Sample(in payloads[60], 1f);
                return;
            }
            case 107:
            {
                if (tick >= 3926)
                {
                    state = 108;
                    goto case 108;
                }
                return;
            }
            case 108:
            {
                if (tick >= 3973)
                {
                    state = 109;
                    goto case 109;
                }
                sink.Sample(in payloads[61], 1f);
                return;
            }
            case 109:
            {
                if (tick >= 3994)
                {
                    state = 110;
                    goto case 110;
                }
                var factor = (float)(tick - 3973) / 20f;
                sink.Sample(in payloads[61], 1f - factor);
                sink.Sample(in payloads[62], factor);
                return;
            }
            case 110:
            {
                if (tick >= 4044)
                {
                    state = 111;
                    goto case 111;
                }
                sink.Sample(in payloads[62], 1f);
                return;
            }
            case 111:
            {
                if (tick >= 4094)
                {
                    state = 112;
                    goto case 112;
                }
                sink.Sample(in payloads[63], 1f);
                return;
            }
            case 112:
            {
                
                return;
            }
        }
    }
}