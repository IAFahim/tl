namespace Tl.Algorithms;

public readonly struct LargeCode : IGenerated
{
    public static Fixture Create() => Fixture.Build(512, 65536);

    public static void Tree<TSink>(int tick, Payload[] payloads, ref TSink sink)
        where TSink : struct, ISink
    {
        if (tick < 32667)
        {
            if (tick < 16420)
            {
                if (tick < 8294)
                {
                    if (tick < 4210)
                    {
                        if (tick < 2173)
                        {
                            if (tick < 1165)
                            {
                                if (tick < 642)
                                {
                                    if (tick < 254)
                                    {
                                        if (tick < 137)
                                        {
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 223)
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
                                        if (tick < 443)
                                        {
                                            if (tick < 401)
                                            {
                                                sink.Sample(in payloads[1], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 401) / 41f;
                                                sink.Sample(in payloads[1], 1f - factor);
                                                sink.Sample(in payloads[2], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 521)
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
                                    if (tick < 763)
                                    {
                                        if (tick < 643)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[3], 1f - factor);
                                            sink.Sample(in payloads[4], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 732)
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
                                        if (tick < 971)
                                        {
                                            if (tick < 929)
                                            {
                                                sink.Sample(in payloads[5], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 929) / 41f;
                                                sink.Sample(in payloads[5], 1f - factor);
                                                sink.Sample(in payloads[6], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 1040)
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
                                if (tick < 1672)
                                {
                                    if (tick < 1302)
                                    {
                                        if (tick < 1166)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[7], 1f - factor);
                                            sink.Sample(in payloads[8], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 1271)
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
                                        if (tick < 1456)
                                        {
                                            if (tick < 1414)
                                            {
                                                sink.Sample(in payloads[9], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 1414) / 41f;
                                                sink.Sample(in payloads[9], 1f - factor);
                                                sink.Sample(in payloads[10], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 1555)
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
                                    if (tick < 1793)
                                    {
                                        if (tick < 1673)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[11], 1f - factor);
                                            sink.Sample(in payloads[12], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 1762)
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
                                        if (tick < 1954)
                                        {
                                            if (tick < 1912)
                                            {
                                                sink.Sample(in payloads[13], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 1912) / 41f;
                                                sink.Sample(in payloads[13], 1f - factor);
                                                sink.Sample(in payloads[14], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 2068)
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
                            if (tick < 3206)
                            {
                                if (tick < 2682)
                                {
                                    if (tick < 2327)
                                    {
                                        if (tick < 2174)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[15], 1f - factor);
                                            sink.Sample(in payloads[16], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 2296)
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
                                        if (tick < 2482)
                                        {
                                            if (tick < 2440)
                                            {
                                                sink.Sample(in payloads[17], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 2440) / 41f;
                                                sink.Sample(in payloads[17], 1f - factor);
                                                sink.Sample(in payloads[18], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 2571)
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
                                    if (tick < 2808)
                                    {
                                        if (tick < 2683)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[19], 1f - factor);
                                            sink.Sample(in payloads[20], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 2777)
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
                                        if (tick < 2989)
                                        {
                                            if (tick < 2947)
                                            {
                                                sink.Sample(in payloads[21], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 2947) / 41f;
                                                sink.Sample(in payloads[21], 1f - factor);
                                                sink.Sample(in payloads[22], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 3071)
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
                                if (tick < 3724)
                                {
                                    if (tick < 3304)
                                    {
                                        if (tick < 3207)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[23], 1f - factor);
                                            sink.Sample(in payloads[24], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 3273)
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
                                        if (tick < 3495)
                                        {
                                            if (tick < 3453)
                                            {
                                                sink.Sample(in payloads[25], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 3453) / 41f;
                                                sink.Sample(in payloads[25], 1f - factor);
                                                sink.Sample(in payloads[26], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 3593)
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
                                    if (tick < 3847)
                                    {
                                        if (tick < 3725)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[27], 1f - factor);
                                            sink.Sample(in payloads[28], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 3816)
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
                                        if (tick < 4012)
                                        {
                                            if (tick < 3970)
                                            {
                                                sink.Sample(in payloads[29], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 3970) / 41f;
                                                sink.Sample(in payloads[29], 1f - factor);
                                                sink.Sample(in payloads[30], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 4067)
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
                        if (tick < 6223)
                        {
                            if (tick < 5229)
                            {
                                if (tick < 4722)
                                {
                                    if (tick < 4358)
                                    {
                                        if (tick < 4211)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[31], 1f - factor);
                                            sink.Sample(in payloads[32], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 4327)
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
                                        if (tick < 4514)
                                        {
                                            if (tick < 4472)
                                            {
                                                sink.Sample(in payloads[33], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 4472) / 41f;
                                                sink.Sample(in payloads[33], 1f - factor);
                                                sink.Sample(in payloads[34], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 4585)
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
                                    if (tick < 4857)
                                    {
                                        if (tick < 4723)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[35], 1f - factor);
                                            sink.Sample(in payloads[36], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 4826)
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
                                        if (tick < 5034)
                                        {
                                            if (tick < 4992)
                                            {
                                                sink.Sample(in payloads[37], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 4992) / 41f;
                                                sink.Sample(in payloads[37], 1f - factor);
                                                sink.Sample(in payloads[38], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 5110)
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
                                if (tick < 5716)
                                {
                                    if (tick < 5335)
                                    {
                                        if (tick < 5230)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[39], 1f - factor);
                                            sink.Sample(in payloads[40], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 5304)
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
                                        if (tick < 5511)
                                        {
                                            if (tick < 5469)
                                            {
                                                sink.Sample(in payloads[41], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 5469) / 41f;
                                                sink.Sample(in payloads[41], 1f - factor);
                                                sink.Sample(in payloads[42], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 5612)
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
                                    if (tick < 5876)
                                    {
                                        if (tick < 5717)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[43], 1f - factor);
                                            sink.Sample(in payloads[44], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 5845)
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
                                        if (tick < 6050)
                                        {
                                            if (tick < 6008)
                                            {
                                                sink.Sample(in payloads[45], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 6008) / 41f;
                                                sink.Sample(in payloads[45], 1f - factor);
                                                sink.Sample(in payloads[46], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 6099)
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
                            if (tick < 7274)
                            {
                                if (tick < 6740)
                                {
                                    if (tick < 6379)
                                    {
                                        if (tick < 6224)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[47], 1f - factor);
                                            sink.Sample(in payloads[48], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 6348)
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
                                        if (tick < 6555)
                                        {
                                            if (tick < 6513)
                                            {
                                                sink.Sample(in payloads[49], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 6513) / 41f;
                                                sink.Sample(in payloads[49], 1f - factor);
                                                sink.Sample(in payloads[50], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 6604)
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
                                    if (tick < 6860)
                                    {
                                        if (tick < 6741)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[51], 1f - factor);
                                            sink.Sample(in payloads[52], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 6829)
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
                                        if (tick < 7040)
                                        {
                                            if (tick < 6998)
                                            {
                                                sink.Sample(in payloads[53], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 6998) / 41f;
                                                sink.Sample(in payloads[53], 1f - factor);
                                                sink.Sample(in payloads[54], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 7145)
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
                                if (tick < 7787)
                                {
                                    if (tick < 7368)
                                    {
                                        if (tick < 7275)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[55], 1f - factor);
                                            sink.Sample(in payloads[56], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 7337)
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
                                        if (tick < 7567)
                                        {
                                            if (tick < 7525)
                                            {
                                                sink.Sample(in payloads[57], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 7525) / 41f;
                                                sink.Sample(in payloads[57], 1f - factor);
                                                sink.Sample(in payloads[58], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 7660)
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
                                    if (tick < 7915)
                                    {
                                        if (tick < 7788)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[59], 1f - factor);
                                            sink.Sample(in payloads[60], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 7884)
                                            {
                                                sink.Sample(in payloads[60], 1f);
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
                                        if (tick < 8068)
                                        {
                                            if (tick < 8026)
                                            {
                                                sink.Sample(in payloads[61], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 8026) / 41f;
                                                sink.Sample(in payloads[61], 1f - factor);
                                                sink.Sample(in payloads[62], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 8161)
                                            {
                                                sink.Sample(in payloads[62], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[63], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (tick < 12332)
                    {
                        if (tick < 10328)
                        {
                            if (tick < 9304)
                            {
                                if (tick < 8777)
                                {
                                    if (tick < 8387)
                                    {
                                        if (tick < 8295)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[63], 1f - factor);
                                            sink.Sample(in payloads[64], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 8356)
                                            {
                                                sink.Sample(in payloads[64], 1f);
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
                                        if (tick < 8566)
                                        {
                                            if (tick < 8524)
                                            {
                                                sink.Sample(in payloads[65], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 8524) / 41f;
                                                sink.Sample(in payloads[65], 1f - factor);
                                                sink.Sample(in payloads[66], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 8646)
                                            {
                                                sink.Sample(in payloads[66], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[67], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 8900)
                                    {
                                        if (tick < 8778)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[67], 1f - factor);
                                            sink.Sample(in payloads[68], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 8869)
                                            {
                                                sink.Sample(in payloads[68], 1f);
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
                                        if (tick < 9081)
                                        {
                                            if (tick < 9039)
                                            {
                                                sink.Sample(in payloads[69], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 9039) / 41f;
                                                sink.Sample(in payloads[69], 1f - factor);
                                                sink.Sample(in payloads[70], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 9184)
                                            {
                                                sink.Sample(in payloads[70], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[71], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 9781)
                                {
                                    if (tick < 9428)
                                    {
                                        if (tick < 9305)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[71], 1f - factor);
                                            sink.Sample(in payloads[72], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 9397)
                                            {
                                                sink.Sample(in payloads[72], 1f);
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
                                        if (tick < 9579)
                                        {
                                            if (tick < 9537)
                                            {
                                                sink.Sample(in payloads[73], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 9537) / 41f;
                                                sink.Sample(in payloads[73], 1f - factor);
                                                sink.Sample(in payloads[74], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 9678)
                                            {
                                                sink.Sample(in payloads[74], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[75], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 9912)
                                    {
                                        if (tick < 9782)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[75], 1f - factor);
                                            sink.Sample(in payloads[76], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 9881)
                                            {
                                                sink.Sample(in payloads[76], 1f);
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
                                        if (tick < 10103)
                                        {
                                            if (tick < 10061)
                                            {
                                                sink.Sample(in payloads[77], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 10061) / 41f;
                                                sink.Sample(in payloads[77], 1f - factor);
                                                sink.Sample(in payloads[78], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 10169)
                                            {
                                                sink.Sample(in payloads[78], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[79], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (tick < 11316)
                            {
                                if (tick < 10823)
                                {
                                    if (tick < 10423)
                                    {
                                        if (tick < 10329)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[79], 1f - factor);
                                            sink.Sample(in payloads[80], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 10392)
                                            {
                                                sink.Sample(in payloads[80], 1f);
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
                                        if (tick < 10600)
                                        {
                                            if (tick < 10558)
                                            {
                                                sink.Sample(in payloads[81], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 10558) / 41f;
                                                sink.Sample(in payloads[81], 1f - factor);
                                                sink.Sample(in payloads[82], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 10687)
                                            {
                                                sink.Sample(in payloads[82], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[83], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 10930)
                                    {
                                        if (tick < 10824)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[83], 1f - factor);
                                            sink.Sample(in payloads[84], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 10899)
                                            {
                                                sink.Sample(in payloads[84], 1f);
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
                                        if (tick < 11092)
                                        {
                                            if (tick < 11050)
                                            {
                                                sink.Sample(in payloads[85], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 11050) / 41f;
                                                sink.Sample(in payloads[85], 1f - factor);
                                                sink.Sample(in payloads[86], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 11212)
                                            {
                                                sink.Sample(in payloads[86], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[87], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 11845)
                                {
                                    if (tick < 11435)
                                    {
                                        if (tick < 11317)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[87], 1f - factor);
                                            sink.Sample(in payloads[88], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 11404)
                                            {
                                                sink.Sample(in payloads[88], 1f);
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
                                        if (tick < 11614)
                                        {
                                            if (tick < 11572)
                                            {
                                                sink.Sample(in payloads[89], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 11572) / 41f;
                                                sink.Sample(in payloads[89], 1f - factor);
                                                sink.Sample(in payloads[90], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 11707)
                                            {
                                                sink.Sample(in payloads[90], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[91], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 11973)
                                    {
                                        if (tick < 11846)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[91], 1f - factor);
                                            sink.Sample(in payloads[92], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 11942)
                                            {
                                                sink.Sample(in payloads[92], 1f);
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
                                        if (tick < 12113)
                                        {
                                            if (tick < 12071)
                                            {
                                                sink.Sample(in payloads[93], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 12071) / 41f;
                                                sink.Sample(in payloads[93], 1f - factor);
                                                sink.Sample(in payloads[94], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 12232)
                                            {
                                                sink.Sample(in payloads[94], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[95], 1f);
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
                        if (tick < 14388)
                        {
                            if (tick < 13338)
                            {
                                if (tick < 12852)
                                {
                                    if (tick < 12478)
                                    {
                                        if (tick < 12333)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[95], 1f - factor);
                                            sink.Sample(in payloads[96], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 12447)
                                            {
                                                sink.Sample(in payloads[96], 1f);
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
                                        if (tick < 12619)
                                        {
                                            if (tick < 12577)
                                            {
                                                sink.Sample(in payloads[97], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 12577) / 41f;
                                                sink.Sample(in payloads[97], 1f - factor);
                                                sink.Sample(in payloads[98], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 12712)
                                            {
                                                sink.Sample(in payloads[98], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[99], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 12965)
                                    {
                                        if (tick < 12853)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[99], 1f - factor);
                                            sink.Sample(in payloads[100], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 12934)
                                            {
                                                sink.Sample(in payloads[100], 1f);
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
                                        if (tick < 13128)
                                        {
                                            if (tick < 13086)
                                            {
                                                sink.Sample(in payloads[101], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 13086) / 41f;
                                                sink.Sample(in payloads[101], 1f - factor);
                                                sink.Sample(in payloads[102], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 13214)
                                            {
                                                sink.Sample(in payloads[102], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[103], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 13850)
                                {
                                    if (tick < 13496)
                                    {
                                        if (tick < 13339)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[103], 1f - factor);
                                            sink.Sample(in payloads[104], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 13465)
                                            {
                                                sink.Sample(in payloads[104], 1f);
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
                                        if (tick < 13645)
                                        {
                                            if (tick < 13603)
                                            {
                                                sink.Sample(in payloads[105], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 13603) / 41f;
                                                sink.Sample(in payloads[105], 1f - factor);
                                                sink.Sample(in payloads[106], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 13739)
                                            {
                                                sink.Sample(in payloads[106], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[107], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 13972)
                                    {
                                        if (tick < 13851)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[107], 1f - factor);
                                            sink.Sample(in payloads[108], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 13941)
                                            {
                                                sink.Sample(in payloads[108], 1f);
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
                                        if (tick < 14175)
                                        {
                                            if (tick < 14133)
                                            {
                                                sink.Sample(in payloads[109], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 14133) / 41f;
                                                sink.Sample(in payloads[109], 1f - factor);
                                                sink.Sample(in payloads[110], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 14243)
                                            {
                                                sink.Sample(in payloads[110], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[111], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (tick < 15373)
                            {
                                if (tick < 14874)
                                {
                                    if (tick < 14518)
                                    {
                                        if (tick < 14389)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[111], 1f - factor);
                                            sink.Sample(in payloads[112], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 14487)
                                            {
                                                sink.Sample(in payloads[112], 1f);
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
                                        if (tick < 14688)
                                        {
                                            if (tick < 14646)
                                            {
                                                sink.Sample(in payloads[113], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 14646) / 41f;
                                                sink.Sample(in payloads[113], 1f - factor);
                                                sink.Sample(in payloads[114], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 14766)
                                            {
                                                sink.Sample(in payloads[114], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[115], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 15006)
                                    {
                                        if (tick < 14875)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[115], 1f - factor);
                                            sink.Sample(in payloads[116], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 14975)
                                            {
                                                sink.Sample(in payloads[116], 1f);
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
                                        if (tick < 15174)
                                        {
                                            if (tick < 15132)
                                            {
                                                sink.Sample(in payloads[117], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 15132) / 41f;
                                                sink.Sample(in payloads[117], 1f - factor);
                                                sink.Sample(in payloads[118], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 15249)
                                            {
                                                sink.Sample(in payloads[118], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[119], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 15907)
                                {
                                    if (tick < 15500)
                                    {
                                        if (tick < 15374)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[119], 1f - factor);
                                            sink.Sample(in payloads[120], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 15469)
                                            {
                                                sink.Sample(in payloads[120], 1f);
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
                                        if (tick < 15675)
                                        {
                                            if (tick < 15633)
                                            {
                                                sink.Sample(in payloads[121], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 15633) / 41f;
                                                sink.Sample(in payloads[121], 1f - factor);
                                                sink.Sample(in payloads[122], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 15755)
                                            {
                                                sink.Sample(in payloads[122], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[123], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 16009)
                                    {
                                        if (tick < 15908)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[123], 1f - factor);
                                            sink.Sample(in payloads[124], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 15978)
                                            {
                                                sink.Sample(in payloads[124], 1f);
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
                                        if (tick < 16204)
                                        {
                                            if (tick < 16162)
                                            {
                                                sink.Sample(in payloads[125], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 16162) / 41f;
                                                sink.Sample(in payloads[125], 1f - factor);
                                                sink.Sample(in payloads[126], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 16281)
                                            {
                                                sink.Sample(in payloads[126], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[127], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (tick < 24530)
                {
                    if (tick < 20485)
                    {
                        if (tick < 18418)
                        {
                            if (tick < 17422)
                            {
                                if (tick < 16897)
                                {
                                    if (tick < 16543)
                                    {
                                        if (tick < 16421)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[127], 1f - factor);
                                            sink.Sample(in payloads[128], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 16512)
                                            {
                                                sink.Sample(in payloads[128], 1f);
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
                                        if (tick < 16691)
                                        {
                                            if (tick < 16649)
                                            {
                                                sink.Sample(in payloads[129], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 16649) / 41f;
                                                sink.Sample(in payloads[129], 1f - factor);
                                                sink.Sample(in payloads[130], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 16801)
                                            {
                                                sink.Sample(in payloads[130], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[131], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 17045)
                                    {
                                        if (tick < 16898)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[131], 1f - factor);
                                            sink.Sample(in payloads[132], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 17014)
                                            {
                                                sink.Sample(in payloads[132], 1f);
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
                                        if (tick < 17207)
                                        {
                                            if (tick < 17165)
                                            {
                                                sink.Sample(in payloads[133], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 17165) / 41f;
                                                sink.Sample(in payloads[133], 1f - factor);
                                                sink.Sample(in payloads[134], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 17279)
                                            {
                                                sink.Sample(in payloads[134], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[135], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 17927)
                                {
                                    if (tick < 17564)
                                    {
                                        if (tick < 17423)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[135], 1f - factor);
                                            sink.Sample(in payloads[136], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 17533)
                                            {
                                                sink.Sample(in payloads[136], 1f);
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
                                        if (tick < 17700)
                                        {
                                            if (tick < 17658)
                                            {
                                                sink.Sample(in payloads[137], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 17658) / 41f;
                                                sink.Sample(in payloads[137], 1f - factor);
                                                sink.Sample(in payloads[138], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 17788)
                                            {
                                                sink.Sample(in payloads[138], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[139], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 18052)
                                    {
                                        if (tick < 17928)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[139], 1f - factor);
                                            sink.Sample(in payloads[140], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 18021)
                                            {
                                                sink.Sample(in payloads[140], 1f);
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
                                        if (tick < 18238)
                                        {
                                            if (tick < 18196)
                                            {
                                                sink.Sample(in payloads[141], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 18196) / 41f;
                                                sink.Sample(in payloads[141], 1f - factor);
                                                sink.Sample(in payloads[142], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 18312)
                                            {
                                                sink.Sample(in payloads[142], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[143], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (tick < 19437)
                            {
                                if (tick < 18941)
                                {
                                    if (tick < 18549)
                                    {
                                        if (tick < 18419)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[143], 1f - factor);
                                            sink.Sample(in payloads[144], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 18518)
                                            {
                                                sink.Sample(in payloads[144], 1f);
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
                                        if (tick < 18744)
                                        {
                                            if (tick < 18702)
                                            {
                                                sink.Sample(in payloads[145], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 18702) / 41f;
                                                sink.Sample(in payloads[145], 1f - factor);
                                                sink.Sample(in payloads[146], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 18820)
                                            {
                                                sink.Sample(in payloads[146], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[147], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 19068)
                                    {
                                        if (tick < 18942)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[147], 1f - factor);
                                            sink.Sample(in payloads[148], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 19037)
                                            {
                                                sink.Sample(in payloads[148], 1f);
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
                                        if (tick < 19253)
                                        {
                                            if (tick < 19211)
                                            {
                                                sink.Sample(in payloads[149], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 19211) / 41f;
                                                sink.Sample(in payloads[149], 1f - factor);
                                                sink.Sample(in payloads[150], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 19338)
                                            {
                                                sink.Sample(in payloads[150], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[151], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 19952)
                                {
                                    if (tick < 19591)
                                    {
                                        if (tick < 19438)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[151], 1f - factor);
                                            sink.Sample(in payloads[152], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 19560)
                                            {
                                                sink.Sample(in payloads[152], 1f);
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
                                        if (tick < 19743)
                                        {
                                            if (tick < 19701)
                                            {
                                                sink.Sample(in payloads[153], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 19701) / 41f;
                                                sink.Sample(in payloads[153], 1f - factor);
                                                sink.Sample(in payloads[154], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 19815)
                                            {
                                                sink.Sample(in payloads[154], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[155], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 20078)
                                    {
                                        if (tick < 19953)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[155], 1f - factor);
                                            sink.Sample(in payloads[156], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 20047)
                                            {
                                                sink.Sample(in payloads[156], 1f);
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
                                        if (tick < 20272)
                                        {
                                            if (tick < 20230)
                                            {
                                                sink.Sample(in payloads[157], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 20230) / 41f;
                                                sink.Sample(in payloads[157], 1f - factor);
                                                sink.Sample(in payloads[158], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 20325)
                                            {
                                                sink.Sample(in payloads[158], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[159], 1f);
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
                        if (tick < 22492)
                        {
                            if (tick < 21489)
                            {
                                if (tick < 20974)
                                {
                                    if (tick < 20591)
                                    {
                                        if (tick < 20486)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[159], 1f - factor);
                                            sink.Sample(in payloads[160], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 20560)
                                            {
                                                sink.Sample(in payloads[160], 1f);
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
                                        if (tick < 20782)
                                        {
                                            if (tick < 20740)
                                            {
                                                sink.Sample(in payloads[161], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 20740) / 41f;
                                                sink.Sample(in payloads[161], 1f - factor);
                                                sink.Sample(in payloads[162], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 20845)
                                            {
                                                sink.Sample(in payloads[162], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[163], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 21123)
                                    {
                                        if (tick < 20975)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[163], 1f - factor);
                                            sink.Sample(in payloads[164], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 21092)
                                            {
                                                sink.Sample(in payloads[164], 1f);
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
                                        if (tick < 21263)
                                        {
                                            if (tick < 21221)
                                            {
                                                sink.Sample(in payloads[165], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 21221) / 41f;
                                                sink.Sample(in payloads[165], 1f - factor);
                                                sink.Sample(in payloads[166], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 21349)
                                            {
                                                sink.Sample(in payloads[166], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[167], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 21986)
                                {
                                    if (tick < 21620)
                                    {
                                        if (tick < 21490)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[167], 1f - factor);
                                            sink.Sample(in payloads[168], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 21589)
                                            {
                                                sink.Sample(in payloads[168], 1f);
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
                                        if (tick < 21772)
                                        {
                                            if (tick < 21730)
                                            {
                                                sink.Sample(in payloads[169], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 21730) / 41f;
                                                sink.Sample(in payloads[169], 1f - factor);
                                                sink.Sample(in payloads[170], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 21845)
                                            {
                                                sink.Sample(in payloads[170], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[171], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 22132)
                                    {
                                        if (tick < 21987)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[171], 1f - factor);
                                            sink.Sample(in payloads[172], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 22101)
                                            {
                                                sink.Sample(in payloads[172], 1f);
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
                                        if (tick < 22297)
                                        {
                                            if (tick < 22255)
                                            {
                                                sink.Sample(in payloads[173], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 22255) / 41f;
                                                sink.Sample(in payloads[173], 1f - factor);
                                                sink.Sample(in payloads[174], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 22377)
                                            {
                                                sink.Sample(in payloads[174], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[175], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (tick < 23509)
                            {
                                if (tick < 23013)
                                {
                                    if (tick < 22616)
                                    {
                                        if (tick < 22493)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[175], 1f - factor);
                                            sink.Sample(in payloads[176], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 22585)
                                            {
                                                sink.Sample(in payloads[176], 1f);
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
                                        if (tick < 22814)
                                        {
                                            if (tick < 22772)
                                            {
                                                sink.Sample(in payloads[177], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 22772) / 41f;
                                                sink.Sample(in payloads[177], 1f - factor);
                                                sink.Sample(in payloads[178], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 22891)
                                            {
                                                sink.Sample(in payloads[178], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[179], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 23132)
                                    {
                                        if (tick < 23014)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[179], 1f - factor);
                                            sink.Sample(in payloads[180], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 23101)
                                            {
                                                sink.Sample(in payloads[180], 1f);
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
                                        if (tick < 23312)
                                        {
                                            if (tick < 23270)
                                            {
                                                sink.Sample(in payloads[181], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 23270) / 41f;
                                                sink.Sample(in payloads[181], 1f - factor);
                                                sink.Sample(in payloads[182], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 23376)
                                            {
                                                sink.Sample(in payloads[182], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[183], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 24024)
                                {
                                    if (tick < 23639)
                                    {
                                        if (tick < 23510)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[183], 1f - factor);
                                            sink.Sample(in payloads[184], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 23608)
                                            {
                                                sink.Sample(in payloads[184], 1f);
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
                                        if (tick < 23815)
                                        {
                                            if (tick < 23773)
                                            {
                                                sink.Sample(in payloads[185], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 23773) / 41f;
                                                sink.Sample(in payloads[185], 1f - factor);
                                                sink.Sample(in payloads[186], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 23911)
                                            {
                                                sink.Sample(in payloads[186], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[187], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 24152)
                                    {
                                        if (tick < 24025)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[187], 1f - factor);
                                            sink.Sample(in payloads[188], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 24121)
                                            {
                                                sink.Sample(in payloads[188], 1f);
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
                                        if (tick < 24333)
                                        {
                                            if (tick < 24291)
                                            {
                                                sink.Sample(in payloads[189], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 24291) / 41f;
                                                sink.Sample(in payloads[189], 1f - factor);
                                                sink.Sample(in payloads[190], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 24393)
                                            {
                                                sink.Sample(in payloads[190], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[191], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (tick < 28606)
                    {
                        if (tick < 26577)
                        {
                            if (tick < 25553)
                            {
                                if (tick < 25019)
                                {
                                    if (tick < 24656)
                                    {
                                        if (tick < 24531)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[191], 1f - factor);
                                            sink.Sample(in payloads[192], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 24625)
                                            {
                                                sink.Sample(in payloads[192], 1f);
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
                                        if (tick < 24838)
                                        {
                                            if (tick < 24796)
                                            {
                                                sink.Sample(in payloads[193], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 24796) / 41f;
                                                sink.Sample(in payloads[193], 1f - factor);
                                                sink.Sample(in payloads[194], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 24932)
                                            {
                                                sink.Sample(in payloads[194], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[195], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 25167)
                                    {
                                        if (tick < 25020)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[195], 1f - factor);
                                            sink.Sample(in payloads[196], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 25136)
                                            {
                                                sink.Sample(in payloads[196], 1f);
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
                                        if (tick < 25345)
                                        {
                                            if (tick < 25303)
                                            {
                                                sink.Sample(in payloads[197], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 25303) / 41f;
                                                sink.Sample(in payloads[197], 1f - factor);
                                                sink.Sample(in payloads[198], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 25418)
                                            {
                                                sink.Sample(in payloads[198], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[199], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 26058)
                                {
                                    if (tick < 25658)
                                    {
                                        if (tick < 25554)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[199], 1f - factor);
                                            sink.Sample(in payloads[200], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 25627)
                                            {
                                                sink.Sample(in payloads[200], 1f);
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
                                        if (tick < 25860)
                                        {
                                            if (tick < 25818)
                                            {
                                                sink.Sample(in payloads[201], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 25818) / 41f;
                                                sink.Sample(in payloads[201], 1f - factor);
                                                sink.Sample(in payloads[202], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 25941)
                                            {
                                                sink.Sample(in payloads[202], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[203], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 26203)
                                    {
                                        if (tick < 26059)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[203], 1f - factor);
                                            sink.Sample(in payloads[204], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 26172)
                                            {
                                                sink.Sample(in payloads[204], 1f);
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
                                        if (tick < 26351)
                                        {
                                            if (tick < 26309)
                                            {
                                                sink.Sample(in payloads[205], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 26309) / 41f;
                                                sink.Sample(in payloads[205], 1f - factor);
                                                sink.Sample(in payloads[206], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 26451)
                                            {
                                                sink.Sample(in payloads[206], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[207], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (tick < 27597)
                            {
                                if (tick < 27087)
                                {
                                    if (tick < 26696)
                                    {
                                        if (tick < 26578)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[207], 1f - factor);
                                            sink.Sample(in payloads[208], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 26665)
                                            {
                                                sink.Sample(in payloads[208], 1f);
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
                                        if (tick < 26861)
                                        {
                                            if (tick < 26819)
                                            {
                                                sink.Sample(in payloads[209], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 26819) / 41f;
                                                sink.Sample(in payloads[209], 1f - factor);
                                                sink.Sample(in payloads[210], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 26941)
                                            {
                                                sink.Sample(in payloads[210], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[211], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 27214)
                                    {
                                        if (tick < 27088)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[211], 1f - factor);
                                            sink.Sample(in payloads[212], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 27183)
                                            {
                                                sink.Sample(in payloads[212], 1f);
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
                                        if (tick < 27353)
                                        {
                                            if (tick < 27311)
                                            {
                                                sink.Sample(in payloads[213], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 27311) / 41f;
                                                sink.Sample(in payloads[213], 1f - factor);
                                                sink.Sample(in payloads[214], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 27453)
                                            {
                                                sink.Sample(in payloads[214], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[215], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 28100)
                                {
                                    if (tick < 27687)
                                    {
                                        if (tick < 27598)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[215], 1f - factor);
                                            sink.Sample(in payloads[216], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 27656)
                                            {
                                                sink.Sample(in payloads[216], 1f);
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
                                        if (tick < 27874)
                                        {
                                            if (tick < 27832)
                                            {
                                                sink.Sample(in payloads[217], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 27832) / 41f;
                                                sink.Sample(in payloads[217], 1f - factor);
                                                sink.Sample(in payloads[218], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 27981)
                                            {
                                                sink.Sample(in payloads[218], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[219], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 28233)
                                    {
                                        if (tick < 28101)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[219], 1f - factor);
                                            sink.Sample(in payloads[220], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 28202)
                                            {
                                                sink.Sample(in payloads[220], 1f);
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
                                        if (tick < 28381)
                                        {
                                            if (tick < 28339)
                                            {
                                                sink.Sample(in payloads[221], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 28339) / 41f;
                                                sink.Sample(in payloads[221], 1f - factor);
                                                sink.Sample(in payloads[222], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 28468)
                                            {
                                                sink.Sample(in payloads[222], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[223], 1f);
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
                        if (tick < 30618)
                        {
                            if (tick < 29597)
                            {
                                if (tick < 29096)
                                {
                                    if (tick < 28719)
                                    {
                                        if (tick < 28607)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[223], 1f - factor);
                                            sink.Sample(in payloads[224], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 28688)
                                            {
                                                sink.Sample(in payloads[224], 1f);
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
                                        if (tick < 28905)
                                        {
                                            if (tick < 28863)
                                            {
                                                sink.Sample(in payloads[225], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 28863) / 41f;
                                                sink.Sample(in payloads[225], 1f - factor);
                                                sink.Sample(in payloads[226], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 28982)
                                            {
                                                sink.Sample(in payloads[226], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[227], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 29224)
                                    {
                                        if (tick < 29097)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[227], 1f - factor);
                                            sink.Sample(in payloads[228], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 29193)
                                            {
                                                sink.Sample(in payloads[228], 1f);
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
                                        if (tick < 29417)
                                        {
                                            if (tick < 29375)
                                            {
                                                sink.Sample(in payloads[229], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 29375) / 41f;
                                                sink.Sample(in payloads[229], 1f - factor);
                                                sink.Sample(in payloads[230], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 29503)
                                            {
                                                sink.Sample(in payloads[230], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[231], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 30105)
                                {
                                    if (tick < 29758)
                                    {
                                        if (tick < 29598)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[231], 1f - factor);
                                            sink.Sample(in payloads[232], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 29727)
                                            {
                                                sink.Sample(in payloads[232], 1f);
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
                                        if (tick < 29889)
                                        {
                                            if (tick < 29847)
                                            {
                                                sink.Sample(in payloads[233], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 29847) / 41f;
                                                sink.Sample(in payloads[233], 1f - factor);
                                                sink.Sample(in payloads[234], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 29982)
                                            {
                                                sink.Sample(in payloads[234], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[235], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 30226)
                                    {
                                        if (tick < 30106)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[235], 1f - factor);
                                            sink.Sample(in payloads[236], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 30195)
                                            {
                                                sink.Sample(in payloads[236], 1f);
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
                                        if (tick < 30424)
                                        {
                                            if (tick < 30382)
                                            {
                                                sink.Sample(in payloads[237], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 30382) / 41f;
                                                sink.Sample(in payloads[237], 1f - factor);
                                                sink.Sample(in payloads[238], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 30497)
                                            {
                                                sink.Sample(in payloads[238], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[239], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (tick < 31653)
                            {
                                if (tick < 31126)
                                {
                                    if (tick < 30746)
                                    {
                                        if (tick < 30619)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[239], 1f - factor);
                                            sink.Sample(in payloads[240], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 30715)
                                            {
                                                sink.Sample(in payloads[240], 1f);
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
                                        if (tick < 30906)
                                        {
                                            if (tick < 30864)
                                            {
                                                sink.Sample(in payloads[241], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 30864) / 41f;
                                                sink.Sample(in payloads[241], 1f - factor);
                                                sink.Sample(in payloads[242], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 31020)
                                            {
                                                sink.Sample(in payloads[242], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[243], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 31265)
                                    {
                                        if (tick < 31127)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[243], 1f - factor);
                                            sink.Sample(in payloads[244], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 31234)
                                            {
                                                sink.Sample(in payloads[244], 1f);
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
                                        if (tick < 31444)
                                        {
                                            if (tick < 31402)
                                            {
                                                sink.Sample(in payloads[245], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 31402) / 41f;
                                                sink.Sample(in payloads[245], 1f - factor);
                                                sink.Sample(in payloads[246], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 31503)
                                            {
                                                sink.Sample(in payloads[246], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[247], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 32171)
                                {
                                    if (tick < 31789)
                                    {
                                        if (tick < 31654)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[247], 1f - factor);
                                            sink.Sample(in payloads[248], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 31758)
                                            {
                                                sink.Sample(in payloads[248], 1f);
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
                                        if (tick < 31959)
                                        {
                                            if (tick < 31917)
                                            {
                                                sink.Sample(in payloads[249], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 31917) / 41f;
                                                sink.Sample(in payloads[249], 1f - factor);
                                                sink.Sample(in payloads[250], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 32012)
                                            {
                                                sink.Sample(in payloads[250], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[251], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 32273)
                                    {
                                        if (tick < 32172)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[251], 1f - factor);
                                            sink.Sample(in payloads[252], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 32242)
                                            {
                                                sink.Sample(in payloads[252], 1f);
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
                                        if (tick < 32434)
                                        {
                                            if (tick < 32392)
                                            {
                                                sink.Sample(in payloads[253], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 32392) / 41f;
                                                sink.Sample(in payloads[253], 1f - factor);
                                                sink.Sample(in payloads[254], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 32534)
                                            {
                                                sink.Sample(in payloads[254], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[255], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        else
        {
            if (tick < 48899)
            {
                if (tick < 40792)
                {
                    if (tick < 36703)
                    {
                        if (tick < 34708)
                        {
                            if (tick < 33692)
                            {
                                if (tick < 33183)
                                {
                                    if (tick < 32778)
                                    {
                                        if (tick < 32668)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[255], 1f - factor);
                                            sink.Sample(in payloads[256], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 32747)
                                            {
                                                sink.Sample(in payloads[256], 1f);
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
                                        if (tick < 32960)
                                        {
                                            if (tick < 32918)
                                            {
                                                sink.Sample(in payloads[257], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 32918) / 41f;
                                                sink.Sample(in payloads[257], 1f - factor);
                                                sink.Sample(in payloads[258], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 33027)
                                            {
                                                sink.Sample(in payloads[258], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[259], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 33277)
                                    {
                                        if (tick < 33184)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[259], 1f - factor);
                                            sink.Sample(in payloads[260], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 33246)
                                            {
                                                sink.Sample(in payloads[260], 1f);
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
                                        if (tick < 33478)
                                        {
                                            if (tick < 33436)
                                            {
                                                sink.Sample(in payloads[261], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 33436) / 41f;
                                                sink.Sample(in payloads[261], 1f - factor);
                                                sink.Sample(in payloads[262], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 33553)
                                            {
                                                sink.Sample(in payloads[262], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[263], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 34185)
                                {
                                    if (tick < 33813)
                                    {
                                        if (tick < 33693)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[263], 1f - factor);
                                            sink.Sample(in payloads[264], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 33782)
                                            {
                                                sink.Sample(in payloads[264], 1f);
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
                                        if (tick < 33965)
                                        {
                                            if (tick < 33923)
                                            {
                                                sink.Sample(in payloads[265], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 33923) / 41f;
                                                sink.Sample(in payloads[265], 1f - factor);
                                                sink.Sample(in payloads[266], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 34068)
                                            {
                                                sink.Sample(in payloads[266], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[267], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 34313)
                                    {
                                        if (tick < 34186)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[267], 1f - factor);
                                            sink.Sample(in payloads[268], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 34282)
                                            {
                                                sink.Sample(in payloads[268], 1f);
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
                                        if (tick < 34491)
                                        {
                                            if (tick < 34449)
                                            {
                                                sink.Sample(in payloads[269], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 34449) / 41f;
                                                sink.Sample(in payloads[269], 1f - factor);
                                                sink.Sample(in payloads[270], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 34580)
                                            {
                                                sink.Sample(in payloads[270], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[271], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (tick < 35722)
                            {
                                if (tick < 35188)
                                {
                                    if (tick < 34819)
                                    {
                                        if (tick < 34709)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[271], 1f - factor);
                                            sink.Sample(in payloads[272], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 34788)
                                            {
                                                sink.Sample(in payloads[272], 1f);
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
                                        if (tick < 34978)
                                        {
                                            if (tick < 34936)
                                            {
                                                sink.Sample(in payloads[273], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 34936) / 41f;
                                                sink.Sample(in payloads[273], 1f - factor);
                                                sink.Sample(in payloads[274], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 35070)
                                            {
                                                sink.Sample(in payloads[274], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[275], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 35309)
                                    {
                                        if (tick < 35189)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[275], 1f - factor);
                                            sink.Sample(in payloads[276], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 35278)
                                            {
                                                sink.Sample(in payloads[276], 1f);
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
                                        if (tick < 35490)
                                        {
                                            if (tick < 35448)
                                            {
                                                sink.Sample(in payloads[277], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 35448) / 41f;
                                                sink.Sample(in payloads[277], 1f - factor);
                                                sink.Sample(in payloads[278], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 35590)
                                            {
                                                sink.Sample(in payloads[278], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[279], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 36228)
                                {
                                    if (tick < 35833)
                                    {
                                        if (tick < 35723)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[279], 1f - factor);
                                            sink.Sample(in payloads[280], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 35802)
                                            {
                                                sink.Sample(in payloads[280], 1f);
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
                                        if (tick < 35991)
                                        {
                                            if (tick < 35949)
                                            {
                                                sink.Sample(in payloads[281], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 35949) / 41f;
                                                sink.Sample(in payloads[281], 1f - factor);
                                                sink.Sample(in payloads[282], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 36093)
                                            {
                                                sink.Sample(in payloads[282], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[283], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 36330)
                                    {
                                        if (tick < 36229)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[283], 1f - factor);
                                            sink.Sample(in payloads[284], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 36299)
                                            {
                                                sink.Sample(in payloads[284], 1f);
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
                                        if (tick < 36509)
                                        {
                                            if (tick < 36467)
                                            {
                                                sink.Sample(in payloads[285], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 36467) / 41f;
                                                sink.Sample(in payloads[285], 1f - factor);
                                                sink.Sample(in payloads[286], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 36609)
                                            {
                                                sink.Sample(in payloads[286], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[287], 1f);
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
                        if (tick < 38743)
                        {
                            if (tick < 37755)
                            {
                                if (tick < 37233)
                                {
                                    if (tick < 36834)
                                    {
                                        if (tick < 36704)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[287], 1f - factor);
                                            sink.Sample(in payloads[288], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 36803)
                                            {
                                                sink.Sample(in payloads[288], 1f);
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
                                        if (tick < 37014)
                                        {
                                            if (tick < 36972)
                                            {
                                                sink.Sample(in payloads[289], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 36972) / 41f;
                                                sink.Sample(in payloads[289], 1f - factor);
                                                sink.Sample(in payloads[290], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 37088)
                                            {
                                                sink.Sample(in payloads[290], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[291], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 37363)
                                    {
                                        if (tick < 37234)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[291], 1f - factor);
                                            sink.Sample(in payloads[292], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 37332)
                                            {
                                                sink.Sample(in payloads[292], 1f);
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
                                        if (tick < 37537)
                                        {
                                            if (tick < 37495)
                                            {
                                                sink.Sample(in payloads[293], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 37495) / 41f;
                                                sink.Sample(in payloads[293], 1f - factor);
                                                sink.Sample(in payloads[294], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 37613)
                                            {
                                                sink.Sample(in payloads[294], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[295], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 38233)
                                {
                                    if (tick < 37887)
                                    {
                                        if (tick < 37756)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[295], 1f - factor);
                                            sink.Sample(in payloads[296], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 37856)
                                            {
                                                sink.Sample(in payloads[296], 1f);
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
                                        if (tick < 38045)
                                        {
                                            if (tick < 38003)
                                            {
                                                sink.Sample(in payloads[297], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 38003) / 41f;
                                                sink.Sample(in payloads[297], 1f - factor);
                                                sink.Sample(in payloads[298], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 38115)
                                            {
                                                sink.Sample(in payloads[298], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[299], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 38356)
                                    {
                                        if (tick < 38234)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[299], 1f - factor);
                                            sink.Sample(in payloads[300], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 38325)
                                            {
                                                sink.Sample(in payloads[300], 1f);
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
                                        if (tick < 38555)
                                        {
                                            if (tick < 38513)
                                            {
                                                sink.Sample(in payloads[301], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 38513) / 41f;
                                                sink.Sample(in payloads[301], 1f - factor);
                                                sink.Sample(in payloads[302], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 38647)
                                            {
                                                sink.Sample(in payloads[302], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[303], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (tick < 39779)
                            {
                                if (tick < 39282)
                                {
                                    if (tick < 38868)
                                    {
                                        if (tick < 38744)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[303], 1f - factor);
                                            sink.Sample(in payloads[304], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 38837)
                                            {
                                                sink.Sample(in payloads[304], 1f);
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
                                        if (tick < 39054)
                                        {
                                            if (tick < 39012)
                                            {
                                                sink.Sample(in payloads[305], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 39012) / 41f;
                                                sink.Sample(in payloads[305], 1f - factor);
                                                sink.Sample(in payloads[306], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 39149)
                                            {
                                                sink.Sample(in payloads[306], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[307], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 39386)
                                    {
                                        if (tick < 39283)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[307], 1f - factor);
                                            sink.Sample(in payloads[308], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 39355)
                                            {
                                                sink.Sample(in payloads[308], 1f);
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
                                        if (tick < 39554)
                                        {
                                            if (tick < 39512)
                                            {
                                                sink.Sample(in payloads[309], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 39512) / 41f;
                                                sink.Sample(in payloads[309], 1f - factor);
                                                sink.Sample(in payloads[310], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 39625)
                                            {
                                                sink.Sample(in payloads[310], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[311], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 40268)
                                {
                                    if (tick < 39885)
                                    {
                                        if (tick < 39780)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[311], 1f - factor);
                                            sink.Sample(in payloads[312], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 39854)
                                            {
                                                sink.Sample(in payloads[312], 1f);
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
                                        if (tick < 40082)
                                        {
                                            if (tick < 40040)
                                            {
                                                sink.Sample(in payloads[313], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 40040) / 41f;
                                                sink.Sample(in payloads[313], 1f - factor);
                                                sink.Sample(in payloads[314], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 40147)
                                            {
                                                sink.Sample(in payloads[314], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[315], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 40405)
                                    {
                                        if (tick < 40269)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[315], 1f - factor);
                                            sink.Sample(in payloads[316], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 40374)
                                            {
                                                sink.Sample(in payloads[316], 1f);
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
                                        if (tick < 40555)
                                        {
                                            if (tick < 40513)
                                            {
                                                sink.Sample(in payloads[317], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 40513) / 41f;
                                                sink.Sample(in payloads[317], 1f - factor);
                                                sink.Sample(in payloads[318], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 40675)
                                            {
                                                sink.Sample(in payloads[318], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[319], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (tick < 44845)
                    {
                        if (tick < 42826)
                        {
                            if (tick < 41820)
                            {
                                if (tick < 41304)
                                {
                                    if (tick < 40906)
                                    {
                                        if (tick < 40793)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[319], 1f - factor);
                                            sink.Sample(in payloads[320], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 40875)
                                            {
                                                sink.Sample(in payloads[320], 1f);
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
                                        if (tick < 41087)
                                        {
                                            if (tick < 41045)
                                            {
                                                sink.Sample(in payloads[321], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 41045) / 41f;
                                                sink.Sample(in payloads[321], 1f - factor);
                                                sink.Sample(in payloads[322], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 41181)
                                            {
                                                sink.Sample(in payloads[322], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[323], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 41422)
                                    {
                                        if (tick < 41305)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[323], 1f - factor);
                                            sink.Sample(in payloads[324], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 41391)
                                            {
                                                sink.Sample(in payloads[324], 1f);
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
                                        if (tick < 41609)
                                        {
                                            if (tick < 41567)
                                            {
                                                sink.Sample(in payloads[325], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 41567) / 41f;
                                                sink.Sample(in payloads[325], 1f - factor);
                                                sink.Sample(in payloads[326], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 41690)
                                            {
                                                sink.Sample(in payloads[326], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[327], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 42291)
                                {
                                    if (tick < 41914)
                                    {
                                        if (tick < 41821)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[327], 1f - factor);
                                            sink.Sample(in payloads[328], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 41883)
                                            {
                                                sink.Sample(in payloads[328], 1f);
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
                                        if (tick < 42117)
                                        {
                                            if (tick < 42075)
                                            {
                                                sink.Sample(in payloads[329], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 42075) / 41f;
                                                sink.Sample(in payloads[329], 1f - factor);
                                                sink.Sample(in payloads[330], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 42188)
                                            {
                                                sink.Sample(in payloads[330], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[331], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 42432)
                                    {
                                        if (tick < 42292)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[331], 1f - factor);
                                            sink.Sample(in payloads[332], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 42401)
                                            {
                                                sink.Sample(in payloads[332], 1f);
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
                                        if (tick < 42606)
                                        {
                                            if (tick < 42564)
                                            {
                                                sink.Sample(in payloads[333], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 42564) / 41f;
                                                sink.Sample(in payloads[333], 1f - factor);
                                                sink.Sample(in payloads[334], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 42711)
                                            {
                                                sink.Sample(in payloads[334], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[335], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (tick < 43819)
                            {
                                if (tick < 43338)
                                {
                                    if (tick < 42964)
                                    {
                                        if (tick < 42827)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[335], 1f - factor);
                                            sink.Sample(in payloads[336], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 42933)
                                            {
                                                sink.Sample(in payloads[336], 1f);
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
                                        if (tick < 43122)
                                        {
                                            if (tick < 43080)
                                            {
                                                sink.Sample(in payloads[337], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 43080) / 41f;
                                                sink.Sample(in payloads[337], 1f - factor);
                                                sink.Sample(in payloads[338], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 43201)
                                            {
                                                sink.Sample(in payloads[338], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[339], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 43450)
                                    {
                                        if (tick < 43339)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[339], 1f - factor);
                                            sink.Sample(in payloads[340], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 43419)
                                            {
                                                sink.Sample(in payloads[340], 1f);
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
                                        if (tick < 43636)
                                        {
                                            if (tick < 43594)
                                            {
                                                sink.Sample(in payloads[341], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 43594) / 41f;
                                                sink.Sample(in payloads[341], 1f - factor);
                                                sink.Sample(in payloads[342], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 43698)
                                            {
                                                sink.Sample(in payloads[342], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[343], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 44353)
                                {
                                    if (tick < 43948)
                                    {
                                        if (tick < 43820)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[343], 1f - factor);
                                            sink.Sample(in payloads[344], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 43917)
                                            {
                                                sink.Sample(in payloads[344], 1f);
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
                                        if (tick < 44145)
                                        {
                                            if (tick < 44103)
                                            {
                                                sink.Sample(in payloads[345], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 44103) / 41f;
                                                sink.Sample(in payloads[345], 1f - factor);
                                                sink.Sample(in payloads[346], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 44211)
                                            {
                                                sink.Sample(in payloads[346], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[347], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 44452)
                                    {
                                        if (tick < 44354)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[347], 1f - factor);
                                            sink.Sample(in payloads[348], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 44421)
                                            {
                                                sink.Sample(in payloads[348], 1f);
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
                                        if (tick < 44652)
                                        {
                                            if (tick < 44610)
                                            {
                                                sink.Sample(in payloads[349], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 44610) / 41f;
                                                sink.Sample(in payloads[349], 1f - factor);
                                                sink.Sample(in payloads[350], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 44705)
                                            {
                                                sink.Sample(in payloads[350], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[351], 1f);
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
                        if (tick < 46896)
                        {
                            if (tick < 45883)
                            {
                                if (tick < 45367)
                                {
                                    if (tick < 44972)
                                    {
                                        if (tick < 44846)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[351], 1f - factor);
                                            sink.Sample(in payloads[352], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 44941)
                                            {
                                                sink.Sample(in payloads[352], 1f);
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
                                        if (tick < 45138)
                                        {
                                            if (tick < 45096)
                                            {
                                                sink.Sample(in payloads[353], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 45096) / 41f;
                                                sink.Sample(in payloads[353], 1f - factor);
                                                sink.Sample(in payloads[354], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 45216)
                                            {
                                                sink.Sample(in payloads[354], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[355], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 45466)
                                    {
                                        if (tick < 45368)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[355], 1f - factor);
                                            sink.Sample(in payloads[356], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 45435)
                                            {
                                                sink.Sample(in payloads[356], 1f);
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
                                        if (tick < 45664)
                                        {
                                            if (tick < 45622)
                                            {
                                                sink.Sample(in payloads[357], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 45622) / 41f;
                                                sink.Sample(in payloads[357], 1f - factor);
                                                sink.Sample(in payloads[358], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 45741)
                                            {
                                                sink.Sample(in payloads[358], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[359], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 46367)
                                {
                                    if (tick < 45994)
                                    {
                                        if (tick < 45884)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[359], 1f - factor);
                                            sink.Sample(in payloads[360], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 45963)
                                            {
                                                sink.Sample(in payloads[360], 1f);
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
                                        if (tick < 46152)
                                        {
                                            if (tick < 46110)
                                            {
                                                sink.Sample(in payloads[361], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 46110) / 41f;
                                                sink.Sample(in payloads[361], 1f - factor);
                                                sink.Sample(in payloads[362], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 46237)
                                            {
                                                sink.Sample(in payloads[362], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[363], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 46502)
                                    {
                                        if (tick < 46368)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[363], 1f - factor);
                                            sink.Sample(in payloads[364], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 46471)
                                            {
                                                sink.Sample(in payloads[364], 1f);
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
                                        if (tick < 46667)
                                        {
                                            if (tick < 46625)
                                            {
                                                sink.Sample(in payloads[365], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 46625) / 41f;
                                                sink.Sample(in payloads[365], 1f - factor);
                                                sink.Sample(in payloads[366], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 46739)
                                            {
                                                sink.Sample(in payloads[366], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[367], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (tick < 47906)
                            {
                                if (tick < 47383)
                                {
                                    if (tick < 47005)
                                    {
                                        if (tick < 46897)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[367], 1f - factor);
                                            sink.Sample(in payloads[368], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 46974)
                                            {
                                                sink.Sample(in payloads[368], 1f);
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
                                        if (tick < 47185)
                                        {
                                            if (tick < 47143)
                                            {
                                                sink.Sample(in payloads[369], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 47143) / 41f;
                                                sink.Sample(in payloads[369], 1f - factor);
                                                sink.Sample(in payloads[370], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 47264)
                                            {
                                                sink.Sample(in payloads[370], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[371], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 47523)
                                    {
                                        if (tick < 47384)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[371], 1f - factor);
                                            sink.Sample(in payloads[372], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 47492)
                                            {
                                                sink.Sample(in payloads[372], 1f);
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
                                        if (tick < 47704)
                                        {
                                            if (tick < 47662)
                                            {
                                                sink.Sample(in payloads[373], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 47662) / 41f;
                                                sink.Sample(in payloads[373], 1f - factor);
                                                sink.Sample(in payloads[374], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 47773)
                                            {
                                                sink.Sample(in payloads[374], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[375], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 48415)
                                {
                                    if (tick < 48023)
                                    {
                                        if (tick < 47907)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[375], 1f - factor);
                                            sink.Sample(in payloads[376], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 47992)
                                            {
                                                sink.Sample(in payloads[376], 1f);
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
                                        if (tick < 48197)
                                        {
                                            if (tick < 48155)
                                            {
                                                sink.Sample(in payloads[377], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 48155) / 41f;
                                                sink.Sample(in payloads[377], 1f - factor);
                                                sink.Sample(in payloads[378], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 48294)
                                            {
                                                sink.Sample(in payloads[378], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[379], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 48519)
                                    {
                                        if (tick < 48416)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[379], 1f - factor);
                                            sink.Sample(in payloads[380], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 48488)
                                            {
                                                sink.Sample(in payloads[380], 1f);
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
                                        if (tick < 48698)
                                        {
                                            if (tick < 48656)
                                            {
                                                sink.Sample(in payloads[381], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 48656) / 41f;
                                                sink.Sample(in payloads[381], 1f - factor);
                                                sink.Sample(in payloads[382], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 48787)
                                            {
                                                sink.Sample(in payloads[382], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[383], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (tick < 57026)
                {
                    if (tick < 52969)
                    {
                        if (tick < 50945)
                        {
                            if (tick < 49912)
                            {
                                if (tick < 49411)
                                {
                                    if (tick < 49026)
                                    {
                                        if (tick < 48900)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[383], 1f - factor);
                                            sink.Sample(in payloads[384], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 48995)
                                            {
                                                sink.Sample(in payloads[384], 1f);
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
                                        if (tick < 49232)
                                        {
                                            if (tick < 49190)
                                            {
                                                sink.Sample(in payloads[385], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 49190) / 41f;
                                                sink.Sample(in payloads[385], 1f - factor);
                                                sink.Sample(in payloads[386], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 49313)
                                            {
                                                sink.Sample(in payloads[386], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[387], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 49549)
                                    {
                                        if (tick < 49412)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[387], 1f - factor);
                                            sink.Sample(in payloads[388], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 49518)
                                            {
                                                sink.Sample(in payloads[388], 1f);
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
                                        if (tick < 49719)
                                        {
                                            if (tick < 49677)
                                            {
                                                sink.Sample(in payloads[389], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 49677) / 41f;
                                                sink.Sample(in payloads[389], 1f - factor);
                                                sink.Sample(in payloads[390], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 49818)
                                            {
                                                sink.Sample(in payloads[390], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[391], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 50433)
                                {
                                    if (tick < 50041)
                                    {
                                        if (tick < 49913)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[391], 1f - factor);
                                            sink.Sample(in payloads[392], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 50010)
                                            {
                                                sink.Sample(in payloads[392], 1f);
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
                                        if (tick < 50224)
                                        {
                                            if (tick < 50182)
                                            {
                                                sink.Sample(in payloads[393], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 50182) / 41f;
                                                sink.Sample(in payloads[393], 1f - factor);
                                                sink.Sample(in payloads[394], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 50316)
                                            {
                                                sink.Sample(in payloads[394], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[395], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 50582)
                                    {
                                        if (tick < 50434)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[395], 1f - factor);
                                            sink.Sample(in payloads[396], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 50551)
                                            {
                                                sink.Sample(in payloads[396], 1f);
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
                                        if (tick < 50744)
                                        {
                                            if (tick < 50702)
                                            {
                                                sink.Sample(in payloads[397], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 50702) / 41f;
                                                sink.Sample(in payloads[397], 1f - factor);
                                                sink.Sample(in payloads[398], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 50815)
                                            {
                                                sink.Sample(in payloads[398], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[399], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (tick < 51959)
                            {
                                if (tick < 51459)
                                {
                                    if (tick < 51072)
                                    {
                                        if (tick < 50946)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[399], 1f - factor);
                                            sink.Sample(in payloads[400], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 51041)
                                            {
                                                sink.Sample(in payloads[400], 1f);
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
                                        if (tick < 51264)
                                        {
                                            if (tick < 51222)
                                            {
                                                sink.Sample(in payloads[401], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 51222) / 41f;
                                                sink.Sample(in payloads[401], 1f - factor);
                                                sink.Sample(in payloads[402], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 51310)
                                            {
                                                sink.Sample(in payloads[402], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[403], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 51588)
                                    {
                                        if (tick < 51460)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[403], 1f - factor);
                                            sink.Sample(in payloads[404], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 51557)
                                            {
                                                sink.Sample(in payloads[404], 1f);
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
                                        if (tick < 51763)
                                        {
                                            if (tick < 51721)
                                            {
                                                sink.Sample(in payloads[405], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 51721) / 41f;
                                                sink.Sample(in payloads[405], 1f - factor);
                                                sink.Sample(in payloads[406], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 51822)
                                            {
                                                sink.Sample(in payloads[406], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[407], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 52478)
                                {
                                    if (tick < 52078)
                                    {
                                        if (tick < 51960)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[407], 1f - factor);
                                            sink.Sample(in payloads[408], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 52047)
                                            {
                                                sink.Sample(in payloads[408], 1f);
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
                                        if (tick < 52280)
                                        {
                                            if (tick < 52238)
                                            {
                                                sink.Sample(in payloads[409], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 52238) / 41f;
                                                sink.Sample(in payloads[409], 1f - factor);
                                                sink.Sample(in payloads[410], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 52358)
                                            {
                                                sink.Sample(in payloads[410], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[411], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 52599)
                                    {
                                        if (tick < 52479)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[411], 1f - factor);
                                            sink.Sample(in payloads[412], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 52568)
                                            {
                                                sink.Sample(in payloads[412], 1f);
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
                                        if (tick < 52778)
                                        {
                                            if (tick < 52736)
                                            {
                                                sink.Sample(in payloads[413], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 52736) / 41f;
                                                sink.Sample(in payloads[413], 1f - factor);
                                                sink.Sample(in payloads[414], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 52837)
                                            {
                                                sink.Sample(in payloads[414], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[415], 1f);
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
                        if (tick < 55028)
                        {
                            if (tick < 53994)
                            {
                                if (tick < 53497)
                                {
                                    if (tick < 53099)
                                    {
                                        if (tick < 52970)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[415], 1f - factor);
                                            sink.Sample(in payloads[416], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 53068)
                                            {
                                                sink.Sample(in payloads[416], 1f);
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
                                        if (tick < 53293)
                                        {
                                            if (tick < 53251)
                                            {
                                                sink.Sample(in payloads[417], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 53251) / 41f;
                                                sink.Sample(in payloads[417], 1f - factor);
                                                sink.Sample(in payloads[418], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 53351)
                                            {
                                                sink.Sample(in payloads[418], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[419], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 53621)
                                    {
                                        if (tick < 53498)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[419], 1f - factor);
                                            sink.Sample(in payloads[420], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 53590)
                                            {
                                                sink.Sample(in payloads[420], 1f);
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
                                        if (tick < 53769)
                                        {
                                            if (tick < 53727)
                                            {
                                                sink.Sample(in payloads[421], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 53727) / 41f;
                                                sink.Sample(in payloads[421], 1f - factor);
                                                sink.Sample(in payloads[422], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 53854)
                                            {
                                                sink.Sample(in payloads[422], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[423], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 54497)
                                {
                                    if (tick < 54141)
                                    {
                                        if (tick < 53995)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[423], 1f - factor);
                                            sink.Sample(in payloads[424], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 54110)
                                            {
                                                sink.Sample(in payloads[424], 1f);
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
                                        if (tick < 54282)
                                        {
                                            if (tick < 54240)
                                            {
                                                sink.Sample(in payloads[425], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 54240) / 41f;
                                                sink.Sample(in payloads[425], 1f - factor);
                                                sink.Sample(in payloads[426], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 54363)
                                            {
                                                sink.Sample(in payloads[426], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[427], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 54614)
                                    {
                                        if (tick < 54498)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[427], 1f - factor);
                                            sink.Sample(in payloads[428], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 54583)
                                            {
                                                sink.Sample(in payloads[428], 1f);
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
                                        if (tick < 54819)
                                        {
                                            if (tick < 54777)
                                            {
                                                sink.Sample(in payloads[429], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 54777) / 41f;
                                                sink.Sample(in payloads[429], 1f - factor);
                                                sink.Sample(in payloads[430], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 54871)
                                            {
                                                sink.Sample(in payloads[430], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[431], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (tick < 56032)
                            {
                                if (tick < 55535)
                                {
                                    if (tick < 55120)
                                    {
                                        if (tick < 55029)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[431], 1f - factor);
                                            sink.Sample(in payloads[432], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 55089)
                                            {
                                                sink.Sample(in payloads[432], 1f);
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
                                        if (tick < 55291)
                                        {
                                            if (tick < 55249)
                                            {
                                                sink.Sample(in payloads[433], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 55249) / 41f;
                                                sink.Sample(in payloads[433], 1f - factor);
                                                sink.Sample(in payloads[434], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 55396)
                                            {
                                                sink.Sample(in payloads[434], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[435], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 55661)
                                    {
                                        if (tick < 55536)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[435], 1f - factor);
                                            sink.Sample(in payloads[436], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 55630)
                                            {
                                                sink.Sample(in payloads[436], 1f);
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
                                        if (tick < 55804)
                                        {
                                            if (tick < 55762)
                                            {
                                                sink.Sample(in payloads[437], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 55762) / 41f;
                                                sink.Sample(in payloads[437], 1f - factor);
                                                sink.Sample(in payloads[438], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 55900)
                                            {
                                                sink.Sample(in payloads[438], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[439], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 56547)
                                {
                                    if (tick < 56142)
                                    {
                                        if (tick < 56033)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[439], 1f - factor);
                                            sink.Sample(in payloads[440], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 56111)
                                            {
                                                sink.Sample(in payloads[440], 1f);
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
                                        if (tick < 56317)
                                        {
                                            if (tick < 56275)
                                            {
                                                sink.Sample(in payloads[441], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 56275) / 41f;
                                                sink.Sample(in payloads[441], 1f - factor);
                                                sink.Sample(in payloads[442], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 56415)
                                            {
                                                sink.Sample(in payloads[442], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[443], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 56677)
                                    {
                                        if (tick < 56548)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[443], 1f - factor);
                                            sink.Sample(in payloads[444], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 56646)
                                            {
                                                sink.Sample(in payloads[444], 1f);
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
                                        if (tick < 56836)
                                        {
                                            if (tick < 56794)
                                            {
                                                sink.Sample(in payloads[445], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 56794) / 41f;
                                                sink.Sample(in payloads[445], 1f - factor);
                                                sink.Sample(in payloads[446], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 56918)
                                            {
                                                sink.Sample(in payloads[446], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[447], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (tick < 61127)
                    {
                        if (tick < 59064)
                        {
                            if (tick < 58071)
                            {
                                if (tick < 57559)
                                {
                                    if (tick < 57160)
                                    {
                                        if (tick < 57027)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[447], 1f - factor);
                                            sink.Sample(in payloads[448], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 57129)
                                            {
                                                sink.Sample(in payloads[448], 1f);
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
                                        if (tick < 57328)
                                        {
                                            if (tick < 57286)
                                            {
                                                sink.Sample(in payloads[449], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 57286) / 41f;
                                                sink.Sample(in payloads[449], 1f - factor);
                                                sink.Sample(in payloads[450], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 57429)
                                            {
                                                sink.Sample(in payloads[450], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[451], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 57664)
                                    {
                                        if (tick < 57560)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[451], 1f - factor);
                                            sink.Sample(in payloads[452], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 57633)
                                            {
                                                sink.Sample(in payloads[452], 1f);
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
                                        if (tick < 57849)
                                        {
                                            if (tick < 57807)
                                            {
                                                sink.Sample(in payloads[453], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 57807) / 41f;
                                                sink.Sample(in payloads[453], 1f - factor);
                                                sink.Sample(in payloads[454], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 57923)
                                            {
                                                sink.Sample(in payloads[454], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[455], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 58576)
                                {
                                    if (tick < 58174)
                                    {
                                        if (tick < 58072)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[455], 1f - factor);
                                            sink.Sample(in payloads[456], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 58143)
                                            {
                                                sink.Sample(in payloads[456], 1f);
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
                                        if (tick < 58373)
                                        {
                                            if (tick < 58331)
                                            {
                                                sink.Sample(in payloads[457], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 58331) / 41f;
                                                sink.Sample(in payloads[457], 1f - factor);
                                                sink.Sample(in payloads[458], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 58444)
                                            {
                                                sink.Sample(in payloads[458], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[459], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 58681)
                                    {
                                        if (tick < 58577)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[459], 1f - factor);
                                            sink.Sample(in payloads[460], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 58650)
                                            {
                                                sink.Sample(in payloads[460], 1f);
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
                                        if (tick < 58873)
                                        {
                                            if (tick < 58831)
                                            {
                                                sink.Sample(in payloads[461], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 58831) / 41f;
                                                sink.Sample(in payloads[461], 1f - factor);
                                                sink.Sample(in payloads[462], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 58966)
                                            {
                                                sink.Sample(in payloads[462], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[463], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (tick < 60102)
                            {
                                if (tick < 59591)
                                {
                                    if (tick < 59187)
                                    {
                                        if (tick < 59065)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[463], 1f - factor);
                                            sink.Sample(in payloads[464], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 59156)
                                            {
                                                sink.Sample(in payloads[464], 1f);
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
                                        if (tick < 59358)
                                        {
                                            if (tick < 59316)
                                            {
                                                sink.Sample(in payloads[465], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 59316) / 41f;
                                                sink.Sample(in payloads[465], 1f - factor);
                                                sink.Sample(in payloads[466], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 59468)
                                            {
                                                sink.Sample(in payloads[466], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[467], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 59719)
                                    {
                                        if (tick < 59592)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[467], 1f - factor);
                                            sink.Sample(in payloads[468], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 59688)
                                            {
                                                sink.Sample(in payloads[468], 1f);
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
                                        if (tick < 59867)
                                        {
                                            if (tick < 59825)
                                            {
                                                sink.Sample(in payloads[469], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 59825) / 41f;
                                                sink.Sample(in payloads[469], 1f - factor);
                                                sink.Sample(in payloads[470], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 59947)
                                            {
                                                sink.Sample(in payloads[470], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[471], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 60599)
                                {
                                    if (tick < 60222)
                                    {
                                        if (tick < 60103)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[471], 1f - factor);
                                            sink.Sample(in payloads[472], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 60191)
                                            {
                                                sink.Sample(in payloads[472], 1f);
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
                                        if (tick < 60399)
                                        {
                                            if (tick < 60357)
                                            {
                                                sink.Sample(in payloads[473], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 60357) / 41f;
                                                sink.Sample(in payloads[473], 1f - factor);
                                                sink.Sample(in payloads[474], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 60470)
                                            {
                                                sink.Sample(in payloads[474], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[475], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 60734)
                                    {
                                        if (tick < 60600)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[475], 1f - factor);
                                            sink.Sample(in payloads[476], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 60703)
                                            {
                                                sink.Sample(in payloads[476], 1f);
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
                                        if (tick < 60916)
                                        {
                                            if (tick < 60874)
                                            {
                                                sink.Sample(in payloads[477], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 60874) / 41f;
                                                sink.Sample(in payloads[477], 1f - factor);
                                                sink.Sample(in payloads[478], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 60972)
                                            {
                                                sink.Sample(in payloads[478], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[479], 1f);
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
                        if (tick < 63122)
                        {
                            if (tick < 62139)
                            {
                                if (tick < 61630)
                                {
                                    if (tick < 61234)
                                    {
                                        if (tick < 61128)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[479], 1f - factor);
                                            sink.Sample(in payloads[480], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 61203)
                                            {
                                                sink.Sample(in payloads[480], 1f);
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
                                        if (tick < 61416)
                                        {
                                            if (tick < 61374)
                                            {
                                                sink.Sample(in payloads[481], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 61374) / 41f;
                                                sink.Sample(in payloads[481], 1f - factor);
                                                sink.Sample(in payloads[482], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 61473)
                                            {
                                                sink.Sample(in payloads[482], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[483], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 61722)
                                    {
                                        if (tick < 61631)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[483], 1f - factor);
                                            sink.Sample(in payloads[484], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 61691)
                                            {
                                                sink.Sample(in payloads[484], 1f);
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
                                        if (tick < 61905)
                                        {
                                            if (tick < 61863)
                                            {
                                                sink.Sample(in payloads[485], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 61863) / 41f;
                                                sink.Sample(in payloads[485], 1f - factor);
                                                sink.Sample(in payloads[486], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 61981)
                                            {
                                                sink.Sample(in payloads[486], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[487], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 62611)
                                {
                                    if (tick < 62258)
                                    {
                                        if (tick < 62140)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[487], 1f - factor);
                                            sink.Sample(in payloads[488], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 62227)
                                            {
                                                sink.Sample(in payloads[488], 1f);
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
                                        if (tick < 62412)
                                        {
                                            if (tick < 62370)
                                            {
                                                sink.Sample(in payloads[489], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 62370) / 41f;
                                                sink.Sample(in payloads[489], 1f - factor);
                                                sink.Sample(in payloads[490], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 62524)
                                            {
                                                sink.Sample(in payloads[490], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[491], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 62761)
                                    {
                                        if (tick < 62612)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[491], 1f - factor);
                                            sink.Sample(in payloads[492], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 62730)
                                            {
                                                sink.Sample(in payloads[492], 1f);
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
                                        if (tick < 62946)
                                        {
                                            if (tick < 62904)
                                            {
                                                sink.Sample(in payloads[493], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 62904) / 41f;
                                                sink.Sample(in payloads[493], 1f - factor);
                                                sink.Sample(in payloads[494], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 63030)
                                            {
                                                sink.Sample(in payloads[494], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[495], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (tick < 64176)
                            {
                                if (tick < 63658)
                                {
                                    if (tick < 63251)
                                    {
                                        if (tick < 63123)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[495], 1f - factor);
                                            sink.Sample(in payloads[496], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 63220)
                                            {
                                                sink.Sample(in payloads[496], 1f);
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
                                        if (tick < 63436)
                                        {
                                            if (tick < 63394)
                                            {
                                                sink.Sample(in payloads[497], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 63394) / 41f;
                                                sink.Sample(in payloads[497], 1f - factor);
                                                sink.Sample(in payloads[498], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 63509)
                                            {
                                                sink.Sample(in payloads[498], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[499], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 63770)
                                    {
                                        if (tick < 63659)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[499], 1f - factor);
                                            sink.Sample(in payloads[500], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 63739)
                                            {
                                                sink.Sample(in payloads[500], 1f);
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
                                        if (tick < 63930)
                                        {
                                            if (tick < 63888)
                                            {
                                                sink.Sample(in payloads[501], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 63888) / 41f;
                                                sink.Sample(in payloads[501], 1f - factor);
                                                sink.Sample(in payloads[502], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 64024)
                                            {
                                                sink.Sample(in payloads[502], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[503], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (tick < 64683)
                                {
                                    if (tick < 64281)
                                    {
                                        if (tick < 64177)
                                        {
                                            var factor = 0.5f;
                                            sink.Sample(in payloads[503], 1f - factor);
                                            sink.Sample(in payloads[504], factor);
                                            return;
                                        }
                                        else
                                        {
                                            if (tick < 64250)
                                            {
                                                sink.Sample(in payloads[504], 1f);
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
                                        if (tick < 64444)
                                        {
                                            if (tick < 64402)
                                            {
                                                sink.Sample(in payloads[505], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                var factor = (float)(tick - 64402) / 41f;
                                                sink.Sample(in payloads[505], 1f - factor);
                                                sink.Sample(in payloads[506], factor);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 64529)
                                            {
                                                sink.Sample(in payloads[506], 1f);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[507], 1f);
                                                return;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (tick < 64931)
                                    {
                                        if (tick < 64768)
                                        {
                                            if (tick < 64684)
                                            {
                                                var factor = 0.5f;
                                                sink.Sample(in payloads[507], 1f - factor);
                                                sink.Sample(in payloads[508], factor);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[508], 1f);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 64799)
                                            {
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[509], 1f);
                                                return;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (tick < 65051)
                                        {
                                            if (tick < 64973)
                                            {
                                                var factor = (float)(tick - 64931) / 41f;
                                                sink.Sample(in payloads[509], 1f - factor);
                                                sink.Sample(in payloads[510], factor);
                                                return;
                                            }
                                            else
                                            {
                                                sink.Sample(in payloads[510], 1f);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (tick < 65534)
                                            {
                                                sink.Sample(in payloads[511], 1f);
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
                if (tick >= 137)
                {
                    state = 1;
                    goto case 1;
                }
                return;
            }
            case 1:
            {
                if (tick >= 223)
                {
                    state = 2;
                    goto case 2;
                }
                sink.Sample(in payloads[0], 1f);
                return;
            }
            case 2:
            {
                if (tick >= 254)
                {
                    state = 3;
                    goto case 3;
                }
                return;
            }
            case 3:
            {
                if (tick >= 401)
                {
                    state = 4;
                    goto case 4;
                }
                sink.Sample(in payloads[1], 1f);
                return;
            }
            case 4:
            {
                if (tick >= 443)
                {
                    state = 5;
                    goto case 5;
                }
                var factor = (float)(tick - 401) / 41f;
                sink.Sample(in payloads[1], 1f - factor);
                sink.Sample(in payloads[2], factor);
                return;
            }
            case 5:
            {
                if (tick >= 521)
                {
                    state = 6;
                    goto case 6;
                }
                sink.Sample(in payloads[2], 1f);
                return;
            }
            case 6:
            {
                if (tick >= 642)
                {
                    state = 7;
                    goto case 7;
                }
                sink.Sample(in payloads[3], 1f);
                return;
            }
            case 7:
            {
                if (tick >= 643)
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
                if (tick >= 732)
                {
                    state = 9;
                    goto case 9;
                }
                sink.Sample(in payloads[4], 1f);
                return;
            }
            case 9:
            {
                if (tick >= 763)
                {
                    state = 10;
                    goto case 10;
                }
                return;
            }
            case 10:
            {
                if (tick >= 929)
                {
                    state = 11;
                    goto case 11;
                }
                sink.Sample(in payloads[5], 1f);
                return;
            }
            case 11:
            {
                if (tick >= 971)
                {
                    state = 12;
                    goto case 12;
                }
                var factor = (float)(tick - 929) / 41f;
                sink.Sample(in payloads[5], 1f - factor);
                sink.Sample(in payloads[6], factor);
                return;
            }
            case 12:
            {
                if (tick >= 1040)
                {
                    state = 13;
                    goto case 13;
                }
                sink.Sample(in payloads[6], 1f);
                return;
            }
            case 13:
            {
                if (tick >= 1165)
                {
                    state = 14;
                    goto case 14;
                }
                sink.Sample(in payloads[7], 1f);
                return;
            }
            case 14:
            {
                if (tick >= 1166)
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
                if (tick >= 1271)
                {
                    state = 16;
                    goto case 16;
                }
                sink.Sample(in payloads[8], 1f);
                return;
            }
            case 16:
            {
                if (tick >= 1302)
                {
                    state = 17;
                    goto case 17;
                }
                return;
            }
            case 17:
            {
                if (tick >= 1414)
                {
                    state = 18;
                    goto case 18;
                }
                sink.Sample(in payloads[9], 1f);
                return;
            }
            case 18:
            {
                if (tick >= 1456)
                {
                    state = 19;
                    goto case 19;
                }
                var factor = (float)(tick - 1414) / 41f;
                sink.Sample(in payloads[9], 1f - factor);
                sink.Sample(in payloads[10], factor);
                return;
            }
            case 19:
            {
                if (tick >= 1555)
                {
                    state = 20;
                    goto case 20;
                }
                sink.Sample(in payloads[10], 1f);
                return;
            }
            case 20:
            {
                if (tick >= 1672)
                {
                    state = 21;
                    goto case 21;
                }
                sink.Sample(in payloads[11], 1f);
                return;
            }
            case 21:
            {
                if (tick >= 1673)
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
                if (tick >= 1762)
                {
                    state = 23;
                    goto case 23;
                }
                sink.Sample(in payloads[12], 1f);
                return;
            }
            case 23:
            {
                if (tick >= 1793)
                {
                    state = 24;
                    goto case 24;
                }
                return;
            }
            case 24:
            {
                if (tick >= 1912)
                {
                    state = 25;
                    goto case 25;
                }
                sink.Sample(in payloads[13], 1f);
                return;
            }
            case 25:
            {
                if (tick >= 1954)
                {
                    state = 26;
                    goto case 26;
                }
                var factor = (float)(tick - 1912) / 41f;
                sink.Sample(in payloads[13], 1f - factor);
                sink.Sample(in payloads[14], factor);
                return;
            }
            case 26:
            {
                if (tick >= 2068)
                {
                    state = 27;
                    goto case 27;
                }
                sink.Sample(in payloads[14], 1f);
                return;
            }
            case 27:
            {
                if (tick >= 2173)
                {
                    state = 28;
                    goto case 28;
                }
                sink.Sample(in payloads[15], 1f);
                return;
            }
            case 28:
            {
                if (tick >= 2174)
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
                if (tick >= 2296)
                {
                    state = 30;
                    goto case 30;
                }
                sink.Sample(in payloads[16], 1f);
                return;
            }
            case 30:
            {
                if (tick >= 2327)
                {
                    state = 31;
                    goto case 31;
                }
                return;
            }
            case 31:
            {
                if (tick >= 2440)
                {
                    state = 32;
                    goto case 32;
                }
                sink.Sample(in payloads[17], 1f);
                return;
            }
            case 32:
            {
                if (tick >= 2482)
                {
                    state = 33;
                    goto case 33;
                }
                var factor = (float)(tick - 2440) / 41f;
                sink.Sample(in payloads[17], 1f - factor);
                sink.Sample(in payloads[18], factor);
                return;
            }
            case 33:
            {
                if (tick >= 2571)
                {
                    state = 34;
                    goto case 34;
                }
                sink.Sample(in payloads[18], 1f);
                return;
            }
            case 34:
            {
                if (tick >= 2682)
                {
                    state = 35;
                    goto case 35;
                }
                sink.Sample(in payloads[19], 1f);
                return;
            }
            case 35:
            {
                if (tick >= 2683)
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
                if (tick >= 2777)
                {
                    state = 37;
                    goto case 37;
                }
                sink.Sample(in payloads[20], 1f);
                return;
            }
            case 37:
            {
                if (tick >= 2808)
                {
                    state = 38;
                    goto case 38;
                }
                return;
            }
            case 38:
            {
                if (tick >= 2947)
                {
                    state = 39;
                    goto case 39;
                }
                sink.Sample(in payloads[21], 1f);
                return;
            }
            case 39:
            {
                if (tick >= 2989)
                {
                    state = 40;
                    goto case 40;
                }
                var factor = (float)(tick - 2947) / 41f;
                sink.Sample(in payloads[21], 1f - factor);
                sink.Sample(in payloads[22], factor);
                return;
            }
            case 40:
            {
                if (tick >= 3071)
                {
                    state = 41;
                    goto case 41;
                }
                sink.Sample(in payloads[22], 1f);
                return;
            }
            case 41:
            {
                if (tick >= 3206)
                {
                    state = 42;
                    goto case 42;
                }
                sink.Sample(in payloads[23], 1f);
                return;
            }
            case 42:
            {
                if (tick >= 3207)
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
                if (tick >= 3273)
                {
                    state = 44;
                    goto case 44;
                }
                sink.Sample(in payloads[24], 1f);
                return;
            }
            case 44:
            {
                if (tick >= 3304)
                {
                    state = 45;
                    goto case 45;
                }
                return;
            }
            case 45:
            {
                if (tick >= 3453)
                {
                    state = 46;
                    goto case 46;
                }
                sink.Sample(in payloads[25], 1f);
                return;
            }
            case 46:
            {
                if (tick >= 3495)
                {
                    state = 47;
                    goto case 47;
                }
                var factor = (float)(tick - 3453) / 41f;
                sink.Sample(in payloads[25], 1f - factor);
                sink.Sample(in payloads[26], factor);
                return;
            }
            case 47:
            {
                if (tick >= 3593)
                {
                    state = 48;
                    goto case 48;
                }
                sink.Sample(in payloads[26], 1f);
                return;
            }
            case 48:
            {
                if (tick >= 3724)
                {
                    state = 49;
                    goto case 49;
                }
                sink.Sample(in payloads[27], 1f);
                return;
            }
            case 49:
            {
                if (tick >= 3725)
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
                if (tick >= 3816)
                {
                    state = 51;
                    goto case 51;
                }
                sink.Sample(in payloads[28], 1f);
                return;
            }
            case 51:
            {
                if (tick >= 3847)
                {
                    state = 52;
                    goto case 52;
                }
                return;
            }
            case 52:
            {
                if (tick >= 3970)
                {
                    state = 53;
                    goto case 53;
                }
                sink.Sample(in payloads[29], 1f);
                return;
            }
            case 53:
            {
                if (tick >= 4012)
                {
                    state = 54;
                    goto case 54;
                }
                var factor = (float)(tick - 3970) / 41f;
                sink.Sample(in payloads[29], 1f - factor);
                sink.Sample(in payloads[30], factor);
                return;
            }
            case 54:
            {
                if (tick >= 4067)
                {
                    state = 55;
                    goto case 55;
                }
                sink.Sample(in payloads[30], 1f);
                return;
            }
            case 55:
            {
                if (tick >= 4210)
                {
                    state = 56;
                    goto case 56;
                }
                sink.Sample(in payloads[31], 1f);
                return;
            }
            case 56:
            {
                if (tick >= 4211)
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
                if (tick >= 4327)
                {
                    state = 58;
                    goto case 58;
                }
                sink.Sample(in payloads[32], 1f);
                return;
            }
            case 58:
            {
                if (tick >= 4358)
                {
                    state = 59;
                    goto case 59;
                }
                return;
            }
            case 59:
            {
                if (tick >= 4472)
                {
                    state = 60;
                    goto case 60;
                }
                sink.Sample(in payloads[33], 1f);
                return;
            }
            case 60:
            {
                if (tick >= 4514)
                {
                    state = 61;
                    goto case 61;
                }
                var factor = (float)(tick - 4472) / 41f;
                sink.Sample(in payloads[33], 1f - factor);
                sink.Sample(in payloads[34], factor);
                return;
            }
            case 61:
            {
                if (tick >= 4585)
                {
                    state = 62;
                    goto case 62;
                }
                sink.Sample(in payloads[34], 1f);
                return;
            }
            case 62:
            {
                if (tick >= 4722)
                {
                    state = 63;
                    goto case 63;
                }
                sink.Sample(in payloads[35], 1f);
                return;
            }
            case 63:
            {
                if (tick >= 4723)
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
                if (tick >= 4826)
                {
                    state = 65;
                    goto case 65;
                }
                sink.Sample(in payloads[36], 1f);
                return;
            }
            case 65:
            {
                if (tick >= 4857)
                {
                    state = 66;
                    goto case 66;
                }
                return;
            }
            case 66:
            {
                if (tick >= 4992)
                {
                    state = 67;
                    goto case 67;
                }
                sink.Sample(in payloads[37], 1f);
                return;
            }
            case 67:
            {
                if (tick >= 5034)
                {
                    state = 68;
                    goto case 68;
                }
                var factor = (float)(tick - 4992) / 41f;
                sink.Sample(in payloads[37], 1f - factor);
                sink.Sample(in payloads[38], factor);
                return;
            }
            case 68:
            {
                if (tick >= 5110)
                {
                    state = 69;
                    goto case 69;
                }
                sink.Sample(in payloads[38], 1f);
                return;
            }
            case 69:
            {
                if (tick >= 5229)
                {
                    state = 70;
                    goto case 70;
                }
                sink.Sample(in payloads[39], 1f);
                return;
            }
            case 70:
            {
                if (tick >= 5230)
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
                if (tick >= 5304)
                {
                    state = 72;
                    goto case 72;
                }
                sink.Sample(in payloads[40], 1f);
                return;
            }
            case 72:
            {
                if (tick >= 5335)
                {
                    state = 73;
                    goto case 73;
                }
                return;
            }
            case 73:
            {
                if (tick >= 5469)
                {
                    state = 74;
                    goto case 74;
                }
                sink.Sample(in payloads[41], 1f);
                return;
            }
            case 74:
            {
                if (tick >= 5511)
                {
                    state = 75;
                    goto case 75;
                }
                var factor = (float)(tick - 5469) / 41f;
                sink.Sample(in payloads[41], 1f - factor);
                sink.Sample(in payloads[42], factor);
                return;
            }
            case 75:
            {
                if (tick >= 5612)
                {
                    state = 76;
                    goto case 76;
                }
                sink.Sample(in payloads[42], 1f);
                return;
            }
            case 76:
            {
                if (tick >= 5716)
                {
                    state = 77;
                    goto case 77;
                }
                sink.Sample(in payloads[43], 1f);
                return;
            }
            case 77:
            {
                if (tick >= 5717)
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
                if (tick >= 5845)
                {
                    state = 79;
                    goto case 79;
                }
                sink.Sample(in payloads[44], 1f);
                return;
            }
            case 79:
            {
                if (tick >= 5876)
                {
                    state = 80;
                    goto case 80;
                }
                return;
            }
            case 80:
            {
                if (tick >= 6008)
                {
                    state = 81;
                    goto case 81;
                }
                sink.Sample(in payloads[45], 1f);
                return;
            }
            case 81:
            {
                if (tick >= 6050)
                {
                    state = 82;
                    goto case 82;
                }
                var factor = (float)(tick - 6008) / 41f;
                sink.Sample(in payloads[45], 1f - factor);
                sink.Sample(in payloads[46], factor);
                return;
            }
            case 82:
            {
                if (tick >= 6099)
                {
                    state = 83;
                    goto case 83;
                }
                sink.Sample(in payloads[46], 1f);
                return;
            }
            case 83:
            {
                if (tick >= 6223)
                {
                    state = 84;
                    goto case 84;
                }
                sink.Sample(in payloads[47], 1f);
                return;
            }
            case 84:
            {
                if (tick >= 6224)
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
                if (tick >= 6348)
                {
                    state = 86;
                    goto case 86;
                }
                sink.Sample(in payloads[48], 1f);
                return;
            }
            case 86:
            {
                if (tick >= 6379)
                {
                    state = 87;
                    goto case 87;
                }
                return;
            }
            case 87:
            {
                if (tick >= 6513)
                {
                    state = 88;
                    goto case 88;
                }
                sink.Sample(in payloads[49], 1f);
                return;
            }
            case 88:
            {
                if (tick >= 6555)
                {
                    state = 89;
                    goto case 89;
                }
                var factor = (float)(tick - 6513) / 41f;
                sink.Sample(in payloads[49], 1f - factor);
                sink.Sample(in payloads[50], factor);
                return;
            }
            case 89:
            {
                if (tick >= 6604)
                {
                    state = 90;
                    goto case 90;
                }
                sink.Sample(in payloads[50], 1f);
                return;
            }
            case 90:
            {
                if (tick >= 6740)
                {
                    state = 91;
                    goto case 91;
                }
                sink.Sample(in payloads[51], 1f);
                return;
            }
            case 91:
            {
                if (tick >= 6741)
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
                if (tick >= 6829)
                {
                    state = 93;
                    goto case 93;
                }
                sink.Sample(in payloads[52], 1f);
                return;
            }
            case 93:
            {
                if (tick >= 6860)
                {
                    state = 94;
                    goto case 94;
                }
                return;
            }
            case 94:
            {
                if (tick >= 6998)
                {
                    state = 95;
                    goto case 95;
                }
                sink.Sample(in payloads[53], 1f);
                return;
            }
            case 95:
            {
                if (tick >= 7040)
                {
                    state = 96;
                    goto case 96;
                }
                var factor = (float)(tick - 6998) / 41f;
                sink.Sample(in payloads[53], 1f - factor);
                sink.Sample(in payloads[54], factor);
                return;
            }
            case 96:
            {
                if (tick >= 7145)
                {
                    state = 97;
                    goto case 97;
                }
                sink.Sample(in payloads[54], 1f);
                return;
            }
            case 97:
            {
                if (tick >= 7274)
                {
                    state = 98;
                    goto case 98;
                }
                sink.Sample(in payloads[55], 1f);
                return;
            }
            case 98:
            {
                if (tick >= 7275)
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
                if (tick >= 7337)
                {
                    state = 100;
                    goto case 100;
                }
                sink.Sample(in payloads[56], 1f);
                return;
            }
            case 100:
            {
                if (tick >= 7368)
                {
                    state = 101;
                    goto case 101;
                }
                return;
            }
            case 101:
            {
                if (tick >= 7525)
                {
                    state = 102;
                    goto case 102;
                }
                sink.Sample(in payloads[57], 1f);
                return;
            }
            case 102:
            {
                if (tick >= 7567)
                {
                    state = 103;
                    goto case 103;
                }
                var factor = (float)(tick - 7525) / 41f;
                sink.Sample(in payloads[57], 1f - factor);
                sink.Sample(in payloads[58], factor);
                return;
            }
            case 103:
            {
                if (tick >= 7660)
                {
                    state = 104;
                    goto case 104;
                }
                sink.Sample(in payloads[58], 1f);
                return;
            }
            case 104:
            {
                if (tick >= 7787)
                {
                    state = 105;
                    goto case 105;
                }
                sink.Sample(in payloads[59], 1f);
                return;
            }
            case 105:
            {
                if (tick >= 7788)
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
                if (tick >= 7884)
                {
                    state = 107;
                    goto case 107;
                }
                sink.Sample(in payloads[60], 1f);
                return;
            }
            case 107:
            {
                if (tick >= 7915)
                {
                    state = 108;
                    goto case 108;
                }
                return;
            }
            case 108:
            {
                if (tick >= 8026)
                {
                    state = 109;
                    goto case 109;
                }
                sink.Sample(in payloads[61], 1f);
                return;
            }
            case 109:
            {
                if (tick >= 8068)
                {
                    state = 110;
                    goto case 110;
                }
                var factor = (float)(tick - 8026) / 41f;
                sink.Sample(in payloads[61], 1f - factor);
                sink.Sample(in payloads[62], factor);
                return;
            }
            case 110:
            {
                if (tick >= 8161)
                {
                    state = 111;
                    goto case 111;
                }
                sink.Sample(in payloads[62], 1f);
                return;
            }
            case 111:
            {
                if (tick >= 8294)
                {
                    state = 112;
                    goto case 112;
                }
                sink.Sample(in payloads[63], 1f);
                return;
            }
            case 112:
            {
                if (tick >= 8295)
                {
                    state = 113;
                    goto case 113;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[63], 1f - factor);
                sink.Sample(in payloads[64], factor);
                return;
            }
            case 113:
            {
                if (tick >= 8356)
                {
                    state = 114;
                    goto case 114;
                }
                sink.Sample(in payloads[64], 1f);
                return;
            }
            case 114:
            {
                if (tick >= 8387)
                {
                    state = 115;
                    goto case 115;
                }
                return;
            }
            case 115:
            {
                if (tick >= 8524)
                {
                    state = 116;
                    goto case 116;
                }
                sink.Sample(in payloads[65], 1f);
                return;
            }
            case 116:
            {
                if (tick >= 8566)
                {
                    state = 117;
                    goto case 117;
                }
                var factor = (float)(tick - 8524) / 41f;
                sink.Sample(in payloads[65], 1f - factor);
                sink.Sample(in payloads[66], factor);
                return;
            }
            case 117:
            {
                if (tick >= 8646)
                {
                    state = 118;
                    goto case 118;
                }
                sink.Sample(in payloads[66], 1f);
                return;
            }
            case 118:
            {
                if (tick >= 8777)
                {
                    state = 119;
                    goto case 119;
                }
                sink.Sample(in payloads[67], 1f);
                return;
            }
            case 119:
            {
                if (tick >= 8778)
                {
                    state = 120;
                    goto case 120;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[67], 1f - factor);
                sink.Sample(in payloads[68], factor);
                return;
            }
            case 120:
            {
                if (tick >= 8869)
                {
                    state = 121;
                    goto case 121;
                }
                sink.Sample(in payloads[68], 1f);
                return;
            }
            case 121:
            {
                if (tick >= 8900)
                {
                    state = 122;
                    goto case 122;
                }
                return;
            }
            case 122:
            {
                if (tick >= 9039)
                {
                    state = 123;
                    goto case 123;
                }
                sink.Sample(in payloads[69], 1f);
                return;
            }
            case 123:
            {
                if (tick >= 9081)
                {
                    state = 124;
                    goto case 124;
                }
                var factor = (float)(tick - 9039) / 41f;
                sink.Sample(in payloads[69], 1f - factor);
                sink.Sample(in payloads[70], factor);
                return;
            }
            case 124:
            {
                if (tick >= 9184)
                {
                    state = 125;
                    goto case 125;
                }
                sink.Sample(in payloads[70], 1f);
                return;
            }
            case 125:
            {
                if (tick >= 9304)
                {
                    state = 126;
                    goto case 126;
                }
                sink.Sample(in payloads[71], 1f);
                return;
            }
            case 126:
            {
                if (tick >= 9305)
                {
                    state = 127;
                    goto case 127;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[71], 1f - factor);
                sink.Sample(in payloads[72], factor);
                return;
            }
            case 127:
            {
                if (tick >= 9397)
                {
                    state = 128;
                    goto case 128;
                }
                sink.Sample(in payloads[72], 1f);
                return;
            }
            case 128:
            {
                if (tick >= 9428)
                {
                    state = 129;
                    goto case 129;
                }
                return;
            }
            case 129:
            {
                if (tick >= 9537)
                {
                    state = 130;
                    goto case 130;
                }
                sink.Sample(in payloads[73], 1f);
                return;
            }
            case 130:
            {
                if (tick >= 9579)
                {
                    state = 131;
                    goto case 131;
                }
                var factor = (float)(tick - 9537) / 41f;
                sink.Sample(in payloads[73], 1f - factor);
                sink.Sample(in payloads[74], factor);
                return;
            }
            case 131:
            {
                if (tick >= 9678)
                {
                    state = 132;
                    goto case 132;
                }
                sink.Sample(in payloads[74], 1f);
                return;
            }
            case 132:
            {
                if (tick >= 9781)
                {
                    state = 133;
                    goto case 133;
                }
                sink.Sample(in payloads[75], 1f);
                return;
            }
            case 133:
            {
                if (tick >= 9782)
                {
                    state = 134;
                    goto case 134;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[75], 1f - factor);
                sink.Sample(in payloads[76], factor);
                return;
            }
            case 134:
            {
                if (tick >= 9881)
                {
                    state = 135;
                    goto case 135;
                }
                sink.Sample(in payloads[76], 1f);
                return;
            }
            case 135:
            {
                if (tick >= 9912)
                {
                    state = 136;
                    goto case 136;
                }
                return;
            }
            case 136:
            {
                if (tick >= 10061)
                {
                    state = 137;
                    goto case 137;
                }
                sink.Sample(in payloads[77], 1f);
                return;
            }
            case 137:
            {
                if (tick >= 10103)
                {
                    state = 138;
                    goto case 138;
                }
                var factor = (float)(tick - 10061) / 41f;
                sink.Sample(in payloads[77], 1f - factor);
                sink.Sample(in payloads[78], factor);
                return;
            }
            case 138:
            {
                if (tick >= 10169)
                {
                    state = 139;
                    goto case 139;
                }
                sink.Sample(in payloads[78], 1f);
                return;
            }
            case 139:
            {
                if (tick >= 10328)
                {
                    state = 140;
                    goto case 140;
                }
                sink.Sample(in payloads[79], 1f);
                return;
            }
            case 140:
            {
                if (tick >= 10329)
                {
                    state = 141;
                    goto case 141;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[79], 1f - factor);
                sink.Sample(in payloads[80], factor);
                return;
            }
            case 141:
            {
                if (tick >= 10392)
                {
                    state = 142;
                    goto case 142;
                }
                sink.Sample(in payloads[80], 1f);
                return;
            }
            case 142:
            {
                if (tick >= 10423)
                {
                    state = 143;
                    goto case 143;
                }
                return;
            }
            case 143:
            {
                if (tick >= 10558)
                {
                    state = 144;
                    goto case 144;
                }
                sink.Sample(in payloads[81], 1f);
                return;
            }
            case 144:
            {
                if (tick >= 10600)
                {
                    state = 145;
                    goto case 145;
                }
                var factor = (float)(tick - 10558) / 41f;
                sink.Sample(in payloads[81], 1f - factor);
                sink.Sample(in payloads[82], factor);
                return;
            }
            case 145:
            {
                if (tick >= 10687)
                {
                    state = 146;
                    goto case 146;
                }
                sink.Sample(in payloads[82], 1f);
                return;
            }
            case 146:
            {
                if (tick >= 10823)
                {
                    state = 147;
                    goto case 147;
                }
                sink.Sample(in payloads[83], 1f);
                return;
            }
            case 147:
            {
                if (tick >= 10824)
                {
                    state = 148;
                    goto case 148;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[83], 1f - factor);
                sink.Sample(in payloads[84], factor);
                return;
            }
            case 148:
            {
                if (tick >= 10899)
                {
                    state = 149;
                    goto case 149;
                }
                sink.Sample(in payloads[84], 1f);
                return;
            }
            case 149:
            {
                if (tick >= 10930)
                {
                    state = 150;
                    goto case 150;
                }
                return;
            }
            case 150:
            {
                if (tick >= 11050)
                {
                    state = 151;
                    goto case 151;
                }
                sink.Sample(in payloads[85], 1f);
                return;
            }
            case 151:
            {
                if (tick >= 11092)
                {
                    state = 152;
                    goto case 152;
                }
                var factor = (float)(tick - 11050) / 41f;
                sink.Sample(in payloads[85], 1f - factor);
                sink.Sample(in payloads[86], factor);
                return;
            }
            case 152:
            {
                if (tick >= 11212)
                {
                    state = 153;
                    goto case 153;
                }
                sink.Sample(in payloads[86], 1f);
                return;
            }
            case 153:
            {
                if (tick >= 11316)
                {
                    state = 154;
                    goto case 154;
                }
                sink.Sample(in payloads[87], 1f);
                return;
            }
            case 154:
            {
                if (tick >= 11317)
                {
                    state = 155;
                    goto case 155;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[87], 1f - factor);
                sink.Sample(in payloads[88], factor);
                return;
            }
            case 155:
            {
                if (tick >= 11404)
                {
                    state = 156;
                    goto case 156;
                }
                sink.Sample(in payloads[88], 1f);
                return;
            }
            case 156:
            {
                if (tick >= 11435)
                {
                    state = 157;
                    goto case 157;
                }
                return;
            }
            case 157:
            {
                if (tick >= 11572)
                {
                    state = 158;
                    goto case 158;
                }
                sink.Sample(in payloads[89], 1f);
                return;
            }
            case 158:
            {
                if (tick >= 11614)
                {
                    state = 159;
                    goto case 159;
                }
                var factor = (float)(tick - 11572) / 41f;
                sink.Sample(in payloads[89], 1f - factor);
                sink.Sample(in payloads[90], factor);
                return;
            }
            case 159:
            {
                if (tick >= 11707)
                {
                    state = 160;
                    goto case 160;
                }
                sink.Sample(in payloads[90], 1f);
                return;
            }
            case 160:
            {
                if (tick >= 11845)
                {
                    state = 161;
                    goto case 161;
                }
                sink.Sample(in payloads[91], 1f);
                return;
            }
            case 161:
            {
                if (tick >= 11846)
                {
                    state = 162;
                    goto case 162;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[91], 1f - factor);
                sink.Sample(in payloads[92], factor);
                return;
            }
            case 162:
            {
                if (tick >= 11942)
                {
                    state = 163;
                    goto case 163;
                }
                sink.Sample(in payloads[92], 1f);
                return;
            }
            case 163:
            {
                if (tick >= 11973)
                {
                    state = 164;
                    goto case 164;
                }
                return;
            }
            case 164:
            {
                if (tick >= 12071)
                {
                    state = 165;
                    goto case 165;
                }
                sink.Sample(in payloads[93], 1f);
                return;
            }
            case 165:
            {
                if (tick >= 12113)
                {
                    state = 166;
                    goto case 166;
                }
                var factor = (float)(tick - 12071) / 41f;
                sink.Sample(in payloads[93], 1f - factor);
                sink.Sample(in payloads[94], factor);
                return;
            }
            case 166:
            {
                if (tick >= 12232)
                {
                    state = 167;
                    goto case 167;
                }
                sink.Sample(in payloads[94], 1f);
                return;
            }
            case 167:
            {
                if (tick >= 12332)
                {
                    state = 168;
                    goto case 168;
                }
                sink.Sample(in payloads[95], 1f);
                return;
            }
            case 168:
            {
                if (tick >= 12333)
                {
                    state = 169;
                    goto case 169;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[95], 1f - factor);
                sink.Sample(in payloads[96], factor);
                return;
            }
            case 169:
            {
                if (tick >= 12447)
                {
                    state = 170;
                    goto case 170;
                }
                sink.Sample(in payloads[96], 1f);
                return;
            }
            case 170:
            {
                if (tick >= 12478)
                {
                    state = 171;
                    goto case 171;
                }
                return;
            }
            case 171:
            {
                if (tick >= 12577)
                {
                    state = 172;
                    goto case 172;
                }
                sink.Sample(in payloads[97], 1f);
                return;
            }
            case 172:
            {
                if (tick >= 12619)
                {
                    state = 173;
                    goto case 173;
                }
                var factor = (float)(tick - 12577) / 41f;
                sink.Sample(in payloads[97], 1f - factor);
                sink.Sample(in payloads[98], factor);
                return;
            }
            case 173:
            {
                if (tick >= 12712)
                {
                    state = 174;
                    goto case 174;
                }
                sink.Sample(in payloads[98], 1f);
                return;
            }
            case 174:
            {
                if (tick >= 12852)
                {
                    state = 175;
                    goto case 175;
                }
                sink.Sample(in payloads[99], 1f);
                return;
            }
            case 175:
            {
                if (tick >= 12853)
                {
                    state = 176;
                    goto case 176;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[99], 1f - factor);
                sink.Sample(in payloads[100], factor);
                return;
            }
            case 176:
            {
                if (tick >= 12934)
                {
                    state = 177;
                    goto case 177;
                }
                sink.Sample(in payloads[100], 1f);
                return;
            }
            case 177:
            {
                if (tick >= 12965)
                {
                    state = 178;
                    goto case 178;
                }
                return;
            }
            case 178:
            {
                if (tick >= 13086)
                {
                    state = 179;
                    goto case 179;
                }
                sink.Sample(in payloads[101], 1f);
                return;
            }
            case 179:
            {
                if (tick >= 13128)
                {
                    state = 180;
                    goto case 180;
                }
                var factor = (float)(tick - 13086) / 41f;
                sink.Sample(in payloads[101], 1f - factor);
                sink.Sample(in payloads[102], factor);
                return;
            }
            case 180:
            {
                if (tick >= 13214)
                {
                    state = 181;
                    goto case 181;
                }
                sink.Sample(in payloads[102], 1f);
                return;
            }
            case 181:
            {
                if (tick >= 13338)
                {
                    state = 182;
                    goto case 182;
                }
                sink.Sample(in payloads[103], 1f);
                return;
            }
            case 182:
            {
                if (tick >= 13339)
                {
                    state = 183;
                    goto case 183;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[103], 1f - factor);
                sink.Sample(in payloads[104], factor);
                return;
            }
            case 183:
            {
                if (tick >= 13465)
                {
                    state = 184;
                    goto case 184;
                }
                sink.Sample(in payloads[104], 1f);
                return;
            }
            case 184:
            {
                if (tick >= 13496)
                {
                    state = 185;
                    goto case 185;
                }
                return;
            }
            case 185:
            {
                if (tick >= 13603)
                {
                    state = 186;
                    goto case 186;
                }
                sink.Sample(in payloads[105], 1f);
                return;
            }
            case 186:
            {
                if (tick >= 13645)
                {
                    state = 187;
                    goto case 187;
                }
                var factor = (float)(tick - 13603) / 41f;
                sink.Sample(in payloads[105], 1f - factor);
                sink.Sample(in payloads[106], factor);
                return;
            }
            case 187:
            {
                if (tick >= 13739)
                {
                    state = 188;
                    goto case 188;
                }
                sink.Sample(in payloads[106], 1f);
                return;
            }
            case 188:
            {
                if (tick >= 13850)
                {
                    state = 189;
                    goto case 189;
                }
                sink.Sample(in payloads[107], 1f);
                return;
            }
            case 189:
            {
                if (tick >= 13851)
                {
                    state = 190;
                    goto case 190;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[107], 1f - factor);
                sink.Sample(in payloads[108], factor);
                return;
            }
            case 190:
            {
                if (tick >= 13941)
                {
                    state = 191;
                    goto case 191;
                }
                sink.Sample(in payloads[108], 1f);
                return;
            }
            case 191:
            {
                if (tick >= 13972)
                {
                    state = 192;
                    goto case 192;
                }
                return;
            }
            case 192:
            {
                if (tick >= 14133)
                {
                    state = 193;
                    goto case 193;
                }
                sink.Sample(in payloads[109], 1f);
                return;
            }
            case 193:
            {
                if (tick >= 14175)
                {
                    state = 194;
                    goto case 194;
                }
                var factor = (float)(tick - 14133) / 41f;
                sink.Sample(in payloads[109], 1f - factor);
                sink.Sample(in payloads[110], factor);
                return;
            }
            case 194:
            {
                if (tick >= 14243)
                {
                    state = 195;
                    goto case 195;
                }
                sink.Sample(in payloads[110], 1f);
                return;
            }
            case 195:
            {
                if (tick >= 14388)
                {
                    state = 196;
                    goto case 196;
                }
                sink.Sample(in payloads[111], 1f);
                return;
            }
            case 196:
            {
                if (tick >= 14389)
                {
                    state = 197;
                    goto case 197;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[111], 1f - factor);
                sink.Sample(in payloads[112], factor);
                return;
            }
            case 197:
            {
                if (tick >= 14487)
                {
                    state = 198;
                    goto case 198;
                }
                sink.Sample(in payloads[112], 1f);
                return;
            }
            case 198:
            {
                if (tick >= 14518)
                {
                    state = 199;
                    goto case 199;
                }
                return;
            }
            case 199:
            {
                if (tick >= 14646)
                {
                    state = 200;
                    goto case 200;
                }
                sink.Sample(in payloads[113], 1f);
                return;
            }
            case 200:
            {
                if (tick >= 14688)
                {
                    state = 201;
                    goto case 201;
                }
                var factor = (float)(tick - 14646) / 41f;
                sink.Sample(in payloads[113], 1f - factor);
                sink.Sample(in payloads[114], factor);
                return;
            }
            case 201:
            {
                if (tick >= 14766)
                {
                    state = 202;
                    goto case 202;
                }
                sink.Sample(in payloads[114], 1f);
                return;
            }
            case 202:
            {
                if (tick >= 14874)
                {
                    state = 203;
                    goto case 203;
                }
                sink.Sample(in payloads[115], 1f);
                return;
            }
            case 203:
            {
                if (tick >= 14875)
                {
                    state = 204;
                    goto case 204;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[115], 1f - factor);
                sink.Sample(in payloads[116], factor);
                return;
            }
            case 204:
            {
                if (tick >= 14975)
                {
                    state = 205;
                    goto case 205;
                }
                sink.Sample(in payloads[116], 1f);
                return;
            }
            case 205:
            {
                if (tick >= 15006)
                {
                    state = 206;
                    goto case 206;
                }
                return;
            }
            case 206:
            {
                if (tick >= 15132)
                {
                    state = 207;
                    goto case 207;
                }
                sink.Sample(in payloads[117], 1f);
                return;
            }
            case 207:
            {
                if (tick >= 15174)
                {
                    state = 208;
                    goto case 208;
                }
                var factor = (float)(tick - 15132) / 41f;
                sink.Sample(in payloads[117], 1f - factor);
                sink.Sample(in payloads[118], factor);
                return;
            }
            case 208:
            {
                if (tick >= 15249)
                {
                    state = 209;
                    goto case 209;
                }
                sink.Sample(in payloads[118], 1f);
                return;
            }
            case 209:
            {
                if (tick >= 15373)
                {
                    state = 210;
                    goto case 210;
                }
                sink.Sample(in payloads[119], 1f);
                return;
            }
            case 210:
            {
                if (tick >= 15374)
                {
                    state = 211;
                    goto case 211;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[119], 1f - factor);
                sink.Sample(in payloads[120], factor);
                return;
            }
            case 211:
            {
                if (tick >= 15469)
                {
                    state = 212;
                    goto case 212;
                }
                sink.Sample(in payloads[120], 1f);
                return;
            }
            case 212:
            {
                if (tick >= 15500)
                {
                    state = 213;
                    goto case 213;
                }
                return;
            }
            case 213:
            {
                if (tick >= 15633)
                {
                    state = 214;
                    goto case 214;
                }
                sink.Sample(in payloads[121], 1f);
                return;
            }
            case 214:
            {
                if (tick >= 15675)
                {
                    state = 215;
                    goto case 215;
                }
                var factor = (float)(tick - 15633) / 41f;
                sink.Sample(in payloads[121], 1f - factor);
                sink.Sample(in payloads[122], factor);
                return;
            }
            case 215:
            {
                if (tick >= 15755)
                {
                    state = 216;
                    goto case 216;
                }
                sink.Sample(in payloads[122], 1f);
                return;
            }
            case 216:
            {
                if (tick >= 15907)
                {
                    state = 217;
                    goto case 217;
                }
                sink.Sample(in payloads[123], 1f);
                return;
            }
            case 217:
            {
                if (tick >= 15908)
                {
                    state = 218;
                    goto case 218;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[123], 1f - factor);
                sink.Sample(in payloads[124], factor);
                return;
            }
            case 218:
            {
                if (tick >= 15978)
                {
                    state = 219;
                    goto case 219;
                }
                sink.Sample(in payloads[124], 1f);
                return;
            }
            case 219:
            {
                if (tick >= 16009)
                {
                    state = 220;
                    goto case 220;
                }
                return;
            }
            case 220:
            {
                if (tick >= 16162)
                {
                    state = 221;
                    goto case 221;
                }
                sink.Sample(in payloads[125], 1f);
                return;
            }
            case 221:
            {
                if (tick >= 16204)
                {
                    state = 222;
                    goto case 222;
                }
                var factor = (float)(tick - 16162) / 41f;
                sink.Sample(in payloads[125], 1f - factor);
                sink.Sample(in payloads[126], factor);
                return;
            }
            case 222:
            {
                if (tick >= 16281)
                {
                    state = 223;
                    goto case 223;
                }
                sink.Sample(in payloads[126], 1f);
                return;
            }
            case 223:
            {
                if (tick >= 16420)
                {
                    state = 224;
                    goto case 224;
                }
                sink.Sample(in payloads[127], 1f);
                return;
            }
            case 224:
            {
                if (tick >= 16421)
                {
                    state = 225;
                    goto case 225;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[127], 1f - factor);
                sink.Sample(in payloads[128], factor);
                return;
            }
            case 225:
            {
                if (tick >= 16512)
                {
                    state = 226;
                    goto case 226;
                }
                sink.Sample(in payloads[128], 1f);
                return;
            }
            case 226:
            {
                if (tick >= 16543)
                {
                    state = 227;
                    goto case 227;
                }
                return;
            }
            case 227:
            {
                if (tick >= 16649)
                {
                    state = 228;
                    goto case 228;
                }
                sink.Sample(in payloads[129], 1f);
                return;
            }
            case 228:
            {
                if (tick >= 16691)
                {
                    state = 229;
                    goto case 229;
                }
                var factor = (float)(tick - 16649) / 41f;
                sink.Sample(in payloads[129], 1f - factor);
                sink.Sample(in payloads[130], factor);
                return;
            }
            case 229:
            {
                if (tick >= 16801)
                {
                    state = 230;
                    goto case 230;
                }
                sink.Sample(in payloads[130], 1f);
                return;
            }
            case 230:
            {
                if (tick >= 16897)
                {
                    state = 231;
                    goto case 231;
                }
                sink.Sample(in payloads[131], 1f);
                return;
            }
            case 231:
            {
                if (tick >= 16898)
                {
                    state = 232;
                    goto case 232;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[131], 1f - factor);
                sink.Sample(in payloads[132], factor);
                return;
            }
            case 232:
            {
                if (tick >= 17014)
                {
                    state = 233;
                    goto case 233;
                }
                sink.Sample(in payloads[132], 1f);
                return;
            }
            case 233:
            {
                if (tick >= 17045)
                {
                    state = 234;
                    goto case 234;
                }
                return;
            }
            case 234:
            {
                if (tick >= 17165)
                {
                    state = 235;
                    goto case 235;
                }
                sink.Sample(in payloads[133], 1f);
                return;
            }
            case 235:
            {
                if (tick >= 17207)
                {
                    state = 236;
                    goto case 236;
                }
                var factor = (float)(tick - 17165) / 41f;
                sink.Sample(in payloads[133], 1f - factor);
                sink.Sample(in payloads[134], factor);
                return;
            }
            case 236:
            {
                if (tick >= 17279)
                {
                    state = 237;
                    goto case 237;
                }
                sink.Sample(in payloads[134], 1f);
                return;
            }
            case 237:
            {
                if (tick >= 17422)
                {
                    state = 238;
                    goto case 238;
                }
                sink.Sample(in payloads[135], 1f);
                return;
            }
            case 238:
            {
                if (tick >= 17423)
                {
                    state = 239;
                    goto case 239;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[135], 1f - factor);
                sink.Sample(in payloads[136], factor);
                return;
            }
            case 239:
            {
                if (tick >= 17533)
                {
                    state = 240;
                    goto case 240;
                }
                sink.Sample(in payloads[136], 1f);
                return;
            }
            case 240:
            {
                if (tick >= 17564)
                {
                    state = 241;
                    goto case 241;
                }
                return;
            }
            case 241:
            {
                if (tick >= 17658)
                {
                    state = 242;
                    goto case 242;
                }
                sink.Sample(in payloads[137], 1f);
                return;
            }
            case 242:
            {
                if (tick >= 17700)
                {
                    state = 243;
                    goto case 243;
                }
                var factor = (float)(tick - 17658) / 41f;
                sink.Sample(in payloads[137], 1f - factor);
                sink.Sample(in payloads[138], factor);
                return;
            }
            case 243:
            {
                if (tick >= 17788)
                {
                    state = 244;
                    goto case 244;
                }
                sink.Sample(in payloads[138], 1f);
                return;
            }
            case 244:
            {
                if (tick >= 17927)
                {
                    state = 245;
                    goto case 245;
                }
                sink.Sample(in payloads[139], 1f);
                return;
            }
            case 245:
            {
                if (tick >= 17928)
                {
                    state = 246;
                    goto case 246;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[139], 1f - factor);
                sink.Sample(in payloads[140], factor);
                return;
            }
            case 246:
            {
                if (tick >= 18021)
                {
                    state = 247;
                    goto case 247;
                }
                sink.Sample(in payloads[140], 1f);
                return;
            }
            case 247:
            {
                if (tick >= 18052)
                {
                    state = 248;
                    goto case 248;
                }
                return;
            }
            case 248:
            {
                if (tick >= 18196)
                {
                    state = 249;
                    goto case 249;
                }
                sink.Sample(in payloads[141], 1f);
                return;
            }
            case 249:
            {
                if (tick >= 18238)
                {
                    state = 250;
                    goto case 250;
                }
                var factor = (float)(tick - 18196) / 41f;
                sink.Sample(in payloads[141], 1f - factor);
                sink.Sample(in payloads[142], factor);
                return;
            }
            case 250:
            {
                if (tick >= 18312)
                {
                    state = 251;
                    goto case 251;
                }
                sink.Sample(in payloads[142], 1f);
                return;
            }
            case 251:
            {
                if (tick >= 18418)
                {
                    state = 252;
                    goto case 252;
                }
                sink.Sample(in payloads[143], 1f);
                return;
            }
            case 252:
            {
                if (tick >= 18419)
                {
                    state = 253;
                    goto case 253;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[143], 1f - factor);
                sink.Sample(in payloads[144], factor);
                return;
            }
            case 253:
            {
                if (tick >= 18518)
                {
                    state = 254;
                    goto case 254;
                }
                sink.Sample(in payloads[144], 1f);
                return;
            }
            case 254:
            {
                if (tick >= 18549)
                {
                    state = 255;
                    goto case 255;
                }
                return;
            }
            case 255:
            {
                if (tick >= 18702)
                {
                    state = 256;
                    goto case 256;
                }
                sink.Sample(in payloads[145], 1f);
                return;
            }
            case 256:
            {
                if (tick >= 18744)
                {
                    state = 257;
                    goto case 257;
                }
                var factor = (float)(tick - 18702) / 41f;
                sink.Sample(in payloads[145], 1f - factor);
                sink.Sample(in payloads[146], factor);
                return;
            }
            case 257:
            {
                if (tick >= 18820)
                {
                    state = 258;
                    goto case 258;
                }
                sink.Sample(in payloads[146], 1f);
                return;
            }
            case 258:
            {
                if (tick >= 18941)
                {
                    state = 259;
                    goto case 259;
                }
                sink.Sample(in payloads[147], 1f);
                return;
            }
            case 259:
            {
                if (tick >= 18942)
                {
                    state = 260;
                    goto case 260;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[147], 1f - factor);
                sink.Sample(in payloads[148], factor);
                return;
            }
            case 260:
            {
                if (tick >= 19037)
                {
                    state = 261;
                    goto case 261;
                }
                sink.Sample(in payloads[148], 1f);
                return;
            }
            case 261:
            {
                if (tick >= 19068)
                {
                    state = 262;
                    goto case 262;
                }
                return;
            }
            case 262:
            {
                if (tick >= 19211)
                {
                    state = 263;
                    goto case 263;
                }
                sink.Sample(in payloads[149], 1f);
                return;
            }
            case 263:
            {
                if (tick >= 19253)
                {
                    state = 264;
                    goto case 264;
                }
                var factor = (float)(tick - 19211) / 41f;
                sink.Sample(in payloads[149], 1f - factor);
                sink.Sample(in payloads[150], factor);
                return;
            }
            case 264:
            {
                if (tick >= 19338)
                {
                    state = 265;
                    goto case 265;
                }
                sink.Sample(in payloads[150], 1f);
                return;
            }
            case 265:
            {
                if (tick >= 19437)
                {
                    state = 266;
                    goto case 266;
                }
                sink.Sample(in payloads[151], 1f);
                return;
            }
            case 266:
            {
                if (tick >= 19438)
                {
                    state = 267;
                    goto case 267;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[151], 1f - factor);
                sink.Sample(in payloads[152], factor);
                return;
            }
            case 267:
            {
                if (tick >= 19560)
                {
                    state = 268;
                    goto case 268;
                }
                sink.Sample(in payloads[152], 1f);
                return;
            }
            case 268:
            {
                if (tick >= 19591)
                {
                    state = 269;
                    goto case 269;
                }
                return;
            }
            case 269:
            {
                if (tick >= 19701)
                {
                    state = 270;
                    goto case 270;
                }
                sink.Sample(in payloads[153], 1f);
                return;
            }
            case 270:
            {
                if (tick >= 19743)
                {
                    state = 271;
                    goto case 271;
                }
                var factor = (float)(tick - 19701) / 41f;
                sink.Sample(in payloads[153], 1f - factor);
                sink.Sample(in payloads[154], factor);
                return;
            }
            case 271:
            {
                if (tick >= 19815)
                {
                    state = 272;
                    goto case 272;
                }
                sink.Sample(in payloads[154], 1f);
                return;
            }
            case 272:
            {
                if (tick >= 19952)
                {
                    state = 273;
                    goto case 273;
                }
                sink.Sample(in payloads[155], 1f);
                return;
            }
            case 273:
            {
                if (tick >= 19953)
                {
                    state = 274;
                    goto case 274;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[155], 1f - factor);
                sink.Sample(in payloads[156], factor);
                return;
            }
            case 274:
            {
                if (tick >= 20047)
                {
                    state = 275;
                    goto case 275;
                }
                sink.Sample(in payloads[156], 1f);
                return;
            }
            case 275:
            {
                if (tick >= 20078)
                {
                    state = 276;
                    goto case 276;
                }
                return;
            }
            case 276:
            {
                if (tick >= 20230)
                {
                    state = 277;
                    goto case 277;
                }
                sink.Sample(in payloads[157], 1f);
                return;
            }
            case 277:
            {
                if (tick >= 20272)
                {
                    state = 278;
                    goto case 278;
                }
                var factor = (float)(tick - 20230) / 41f;
                sink.Sample(in payloads[157], 1f - factor);
                sink.Sample(in payloads[158], factor);
                return;
            }
            case 278:
            {
                if (tick >= 20325)
                {
                    state = 279;
                    goto case 279;
                }
                sink.Sample(in payloads[158], 1f);
                return;
            }
            case 279:
            {
                if (tick >= 20485)
                {
                    state = 280;
                    goto case 280;
                }
                sink.Sample(in payloads[159], 1f);
                return;
            }
            case 280:
            {
                if (tick >= 20486)
                {
                    state = 281;
                    goto case 281;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[159], 1f - factor);
                sink.Sample(in payloads[160], factor);
                return;
            }
            case 281:
            {
                if (tick >= 20560)
                {
                    state = 282;
                    goto case 282;
                }
                sink.Sample(in payloads[160], 1f);
                return;
            }
            case 282:
            {
                if (tick >= 20591)
                {
                    state = 283;
                    goto case 283;
                }
                return;
            }
            case 283:
            {
                if (tick >= 20740)
                {
                    state = 284;
                    goto case 284;
                }
                sink.Sample(in payloads[161], 1f);
                return;
            }
            case 284:
            {
                if (tick >= 20782)
                {
                    state = 285;
                    goto case 285;
                }
                var factor = (float)(tick - 20740) / 41f;
                sink.Sample(in payloads[161], 1f - factor);
                sink.Sample(in payloads[162], factor);
                return;
            }
            case 285:
            {
                if (tick >= 20845)
                {
                    state = 286;
                    goto case 286;
                }
                sink.Sample(in payloads[162], 1f);
                return;
            }
            case 286:
            {
                if (tick >= 20974)
                {
                    state = 287;
                    goto case 287;
                }
                sink.Sample(in payloads[163], 1f);
                return;
            }
            case 287:
            {
                if (tick >= 20975)
                {
                    state = 288;
                    goto case 288;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[163], 1f - factor);
                sink.Sample(in payloads[164], factor);
                return;
            }
            case 288:
            {
                if (tick >= 21092)
                {
                    state = 289;
                    goto case 289;
                }
                sink.Sample(in payloads[164], 1f);
                return;
            }
            case 289:
            {
                if (tick >= 21123)
                {
                    state = 290;
                    goto case 290;
                }
                return;
            }
            case 290:
            {
                if (tick >= 21221)
                {
                    state = 291;
                    goto case 291;
                }
                sink.Sample(in payloads[165], 1f);
                return;
            }
            case 291:
            {
                if (tick >= 21263)
                {
                    state = 292;
                    goto case 292;
                }
                var factor = (float)(tick - 21221) / 41f;
                sink.Sample(in payloads[165], 1f - factor);
                sink.Sample(in payloads[166], factor);
                return;
            }
            case 292:
            {
                if (tick >= 21349)
                {
                    state = 293;
                    goto case 293;
                }
                sink.Sample(in payloads[166], 1f);
                return;
            }
            case 293:
            {
                if (tick >= 21489)
                {
                    state = 294;
                    goto case 294;
                }
                sink.Sample(in payloads[167], 1f);
                return;
            }
            case 294:
            {
                if (tick >= 21490)
                {
                    state = 295;
                    goto case 295;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[167], 1f - factor);
                sink.Sample(in payloads[168], factor);
                return;
            }
            case 295:
            {
                if (tick >= 21589)
                {
                    state = 296;
                    goto case 296;
                }
                sink.Sample(in payloads[168], 1f);
                return;
            }
            case 296:
            {
                if (tick >= 21620)
                {
                    state = 297;
                    goto case 297;
                }
                return;
            }
            case 297:
            {
                if (tick >= 21730)
                {
                    state = 298;
                    goto case 298;
                }
                sink.Sample(in payloads[169], 1f);
                return;
            }
            case 298:
            {
                if (tick >= 21772)
                {
                    state = 299;
                    goto case 299;
                }
                var factor = (float)(tick - 21730) / 41f;
                sink.Sample(in payloads[169], 1f - factor);
                sink.Sample(in payloads[170], factor);
                return;
            }
            case 299:
            {
                if (tick >= 21845)
                {
                    state = 300;
                    goto case 300;
                }
                sink.Sample(in payloads[170], 1f);
                return;
            }
            case 300:
            {
                if (tick >= 21986)
                {
                    state = 301;
                    goto case 301;
                }
                sink.Sample(in payloads[171], 1f);
                return;
            }
            case 301:
            {
                if (tick >= 21987)
                {
                    state = 302;
                    goto case 302;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[171], 1f - factor);
                sink.Sample(in payloads[172], factor);
                return;
            }
            case 302:
            {
                if (tick >= 22101)
                {
                    state = 303;
                    goto case 303;
                }
                sink.Sample(in payloads[172], 1f);
                return;
            }
            case 303:
            {
                if (tick >= 22132)
                {
                    state = 304;
                    goto case 304;
                }
                return;
            }
            case 304:
            {
                if (tick >= 22255)
                {
                    state = 305;
                    goto case 305;
                }
                sink.Sample(in payloads[173], 1f);
                return;
            }
            case 305:
            {
                if (tick >= 22297)
                {
                    state = 306;
                    goto case 306;
                }
                var factor = (float)(tick - 22255) / 41f;
                sink.Sample(in payloads[173], 1f - factor);
                sink.Sample(in payloads[174], factor);
                return;
            }
            case 306:
            {
                if (tick >= 22377)
                {
                    state = 307;
                    goto case 307;
                }
                sink.Sample(in payloads[174], 1f);
                return;
            }
            case 307:
            {
                if (tick >= 22492)
                {
                    state = 308;
                    goto case 308;
                }
                sink.Sample(in payloads[175], 1f);
                return;
            }
            case 308:
            {
                if (tick >= 22493)
                {
                    state = 309;
                    goto case 309;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[175], 1f - factor);
                sink.Sample(in payloads[176], factor);
                return;
            }
            case 309:
            {
                if (tick >= 22585)
                {
                    state = 310;
                    goto case 310;
                }
                sink.Sample(in payloads[176], 1f);
                return;
            }
            case 310:
            {
                if (tick >= 22616)
                {
                    state = 311;
                    goto case 311;
                }
                return;
            }
            case 311:
            {
                if (tick >= 22772)
                {
                    state = 312;
                    goto case 312;
                }
                sink.Sample(in payloads[177], 1f);
                return;
            }
            case 312:
            {
                if (tick >= 22814)
                {
                    state = 313;
                    goto case 313;
                }
                var factor = (float)(tick - 22772) / 41f;
                sink.Sample(in payloads[177], 1f - factor);
                sink.Sample(in payloads[178], factor);
                return;
            }
            case 313:
            {
                if (tick >= 22891)
                {
                    state = 314;
                    goto case 314;
                }
                sink.Sample(in payloads[178], 1f);
                return;
            }
            case 314:
            {
                if (tick >= 23013)
                {
                    state = 315;
                    goto case 315;
                }
                sink.Sample(in payloads[179], 1f);
                return;
            }
            case 315:
            {
                if (tick >= 23014)
                {
                    state = 316;
                    goto case 316;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[179], 1f - factor);
                sink.Sample(in payloads[180], factor);
                return;
            }
            case 316:
            {
                if (tick >= 23101)
                {
                    state = 317;
                    goto case 317;
                }
                sink.Sample(in payloads[180], 1f);
                return;
            }
            case 317:
            {
                if (tick >= 23132)
                {
                    state = 318;
                    goto case 318;
                }
                return;
            }
            case 318:
            {
                if (tick >= 23270)
                {
                    state = 319;
                    goto case 319;
                }
                sink.Sample(in payloads[181], 1f);
                return;
            }
            case 319:
            {
                if (tick >= 23312)
                {
                    state = 320;
                    goto case 320;
                }
                var factor = (float)(tick - 23270) / 41f;
                sink.Sample(in payloads[181], 1f - factor);
                sink.Sample(in payloads[182], factor);
                return;
            }
            case 320:
            {
                if (tick >= 23376)
                {
                    state = 321;
                    goto case 321;
                }
                sink.Sample(in payloads[182], 1f);
                return;
            }
            case 321:
            {
                if (tick >= 23509)
                {
                    state = 322;
                    goto case 322;
                }
                sink.Sample(in payloads[183], 1f);
                return;
            }
            case 322:
            {
                if (tick >= 23510)
                {
                    state = 323;
                    goto case 323;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[183], 1f - factor);
                sink.Sample(in payloads[184], factor);
                return;
            }
            case 323:
            {
                if (tick >= 23608)
                {
                    state = 324;
                    goto case 324;
                }
                sink.Sample(in payloads[184], 1f);
                return;
            }
            case 324:
            {
                if (tick >= 23639)
                {
                    state = 325;
                    goto case 325;
                }
                return;
            }
            case 325:
            {
                if (tick >= 23773)
                {
                    state = 326;
                    goto case 326;
                }
                sink.Sample(in payloads[185], 1f);
                return;
            }
            case 326:
            {
                if (tick >= 23815)
                {
                    state = 327;
                    goto case 327;
                }
                var factor = (float)(tick - 23773) / 41f;
                sink.Sample(in payloads[185], 1f - factor);
                sink.Sample(in payloads[186], factor);
                return;
            }
            case 327:
            {
                if (tick >= 23911)
                {
                    state = 328;
                    goto case 328;
                }
                sink.Sample(in payloads[186], 1f);
                return;
            }
            case 328:
            {
                if (tick >= 24024)
                {
                    state = 329;
                    goto case 329;
                }
                sink.Sample(in payloads[187], 1f);
                return;
            }
            case 329:
            {
                if (tick >= 24025)
                {
                    state = 330;
                    goto case 330;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[187], 1f - factor);
                sink.Sample(in payloads[188], factor);
                return;
            }
            case 330:
            {
                if (tick >= 24121)
                {
                    state = 331;
                    goto case 331;
                }
                sink.Sample(in payloads[188], 1f);
                return;
            }
            case 331:
            {
                if (tick >= 24152)
                {
                    state = 332;
                    goto case 332;
                }
                return;
            }
            case 332:
            {
                if (tick >= 24291)
                {
                    state = 333;
                    goto case 333;
                }
                sink.Sample(in payloads[189], 1f);
                return;
            }
            case 333:
            {
                if (tick >= 24333)
                {
                    state = 334;
                    goto case 334;
                }
                var factor = (float)(tick - 24291) / 41f;
                sink.Sample(in payloads[189], 1f - factor);
                sink.Sample(in payloads[190], factor);
                return;
            }
            case 334:
            {
                if (tick >= 24393)
                {
                    state = 335;
                    goto case 335;
                }
                sink.Sample(in payloads[190], 1f);
                return;
            }
            case 335:
            {
                if (tick >= 24530)
                {
                    state = 336;
                    goto case 336;
                }
                sink.Sample(in payloads[191], 1f);
                return;
            }
            case 336:
            {
                if (tick >= 24531)
                {
                    state = 337;
                    goto case 337;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[191], 1f - factor);
                sink.Sample(in payloads[192], factor);
                return;
            }
            case 337:
            {
                if (tick >= 24625)
                {
                    state = 338;
                    goto case 338;
                }
                sink.Sample(in payloads[192], 1f);
                return;
            }
            case 338:
            {
                if (tick >= 24656)
                {
                    state = 339;
                    goto case 339;
                }
                return;
            }
            case 339:
            {
                if (tick >= 24796)
                {
                    state = 340;
                    goto case 340;
                }
                sink.Sample(in payloads[193], 1f);
                return;
            }
            case 340:
            {
                if (tick >= 24838)
                {
                    state = 341;
                    goto case 341;
                }
                var factor = (float)(tick - 24796) / 41f;
                sink.Sample(in payloads[193], 1f - factor);
                sink.Sample(in payloads[194], factor);
                return;
            }
            case 341:
            {
                if (tick >= 24932)
                {
                    state = 342;
                    goto case 342;
                }
                sink.Sample(in payloads[194], 1f);
                return;
            }
            case 342:
            {
                if (tick >= 25019)
                {
                    state = 343;
                    goto case 343;
                }
                sink.Sample(in payloads[195], 1f);
                return;
            }
            case 343:
            {
                if (tick >= 25020)
                {
                    state = 344;
                    goto case 344;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[195], 1f - factor);
                sink.Sample(in payloads[196], factor);
                return;
            }
            case 344:
            {
                if (tick >= 25136)
                {
                    state = 345;
                    goto case 345;
                }
                sink.Sample(in payloads[196], 1f);
                return;
            }
            case 345:
            {
                if (tick >= 25167)
                {
                    state = 346;
                    goto case 346;
                }
                return;
            }
            case 346:
            {
                if (tick >= 25303)
                {
                    state = 347;
                    goto case 347;
                }
                sink.Sample(in payloads[197], 1f);
                return;
            }
            case 347:
            {
                if (tick >= 25345)
                {
                    state = 348;
                    goto case 348;
                }
                var factor = (float)(tick - 25303) / 41f;
                sink.Sample(in payloads[197], 1f - factor);
                sink.Sample(in payloads[198], factor);
                return;
            }
            case 348:
            {
                if (tick >= 25418)
                {
                    state = 349;
                    goto case 349;
                }
                sink.Sample(in payloads[198], 1f);
                return;
            }
            case 349:
            {
                if (tick >= 25553)
                {
                    state = 350;
                    goto case 350;
                }
                sink.Sample(in payloads[199], 1f);
                return;
            }
            case 350:
            {
                if (tick >= 25554)
                {
                    state = 351;
                    goto case 351;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[199], 1f - factor);
                sink.Sample(in payloads[200], factor);
                return;
            }
            case 351:
            {
                if (tick >= 25627)
                {
                    state = 352;
                    goto case 352;
                }
                sink.Sample(in payloads[200], 1f);
                return;
            }
            case 352:
            {
                if (tick >= 25658)
                {
                    state = 353;
                    goto case 353;
                }
                return;
            }
            case 353:
            {
                if (tick >= 25818)
                {
                    state = 354;
                    goto case 354;
                }
                sink.Sample(in payloads[201], 1f);
                return;
            }
            case 354:
            {
                if (tick >= 25860)
                {
                    state = 355;
                    goto case 355;
                }
                var factor = (float)(tick - 25818) / 41f;
                sink.Sample(in payloads[201], 1f - factor);
                sink.Sample(in payloads[202], factor);
                return;
            }
            case 355:
            {
                if (tick >= 25941)
                {
                    state = 356;
                    goto case 356;
                }
                sink.Sample(in payloads[202], 1f);
                return;
            }
            case 356:
            {
                if (tick >= 26058)
                {
                    state = 357;
                    goto case 357;
                }
                sink.Sample(in payloads[203], 1f);
                return;
            }
            case 357:
            {
                if (tick >= 26059)
                {
                    state = 358;
                    goto case 358;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[203], 1f - factor);
                sink.Sample(in payloads[204], factor);
                return;
            }
            case 358:
            {
                if (tick >= 26172)
                {
                    state = 359;
                    goto case 359;
                }
                sink.Sample(in payloads[204], 1f);
                return;
            }
            case 359:
            {
                if (tick >= 26203)
                {
                    state = 360;
                    goto case 360;
                }
                return;
            }
            case 360:
            {
                if (tick >= 26309)
                {
                    state = 361;
                    goto case 361;
                }
                sink.Sample(in payloads[205], 1f);
                return;
            }
            case 361:
            {
                if (tick >= 26351)
                {
                    state = 362;
                    goto case 362;
                }
                var factor = (float)(tick - 26309) / 41f;
                sink.Sample(in payloads[205], 1f - factor);
                sink.Sample(in payloads[206], factor);
                return;
            }
            case 362:
            {
                if (tick >= 26451)
                {
                    state = 363;
                    goto case 363;
                }
                sink.Sample(in payloads[206], 1f);
                return;
            }
            case 363:
            {
                if (tick >= 26577)
                {
                    state = 364;
                    goto case 364;
                }
                sink.Sample(in payloads[207], 1f);
                return;
            }
            case 364:
            {
                if (tick >= 26578)
                {
                    state = 365;
                    goto case 365;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[207], 1f - factor);
                sink.Sample(in payloads[208], factor);
                return;
            }
            case 365:
            {
                if (tick >= 26665)
                {
                    state = 366;
                    goto case 366;
                }
                sink.Sample(in payloads[208], 1f);
                return;
            }
            case 366:
            {
                if (tick >= 26696)
                {
                    state = 367;
                    goto case 367;
                }
                return;
            }
            case 367:
            {
                if (tick >= 26819)
                {
                    state = 368;
                    goto case 368;
                }
                sink.Sample(in payloads[209], 1f);
                return;
            }
            case 368:
            {
                if (tick >= 26861)
                {
                    state = 369;
                    goto case 369;
                }
                var factor = (float)(tick - 26819) / 41f;
                sink.Sample(in payloads[209], 1f - factor);
                sink.Sample(in payloads[210], factor);
                return;
            }
            case 369:
            {
                if (tick >= 26941)
                {
                    state = 370;
                    goto case 370;
                }
                sink.Sample(in payloads[210], 1f);
                return;
            }
            case 370:
            {
                if (tick >= 27087)
                {
                    state = 371;
                    goto case 371;
                }
                sink.Sample(in payloads[211], 1f);
                return;
            }
            case 371:
            {
                if (tick >= 27088)
                {
                    state = 372;
                    goto case 372;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[211], 1f - factor);
                sink.Sample(in payloads[212], factor);
                return;
            }
            case 372:
            {
                if (tick >= 27183)
                {
                    state = 373;
                    goto case 373;
                }
                sink.Sample(in payloads[212], 1f);
                return;
            }
            case 373:
            {
                if (tick >= 27214)
                {
                    state = 374;
                    goto case 374;
                }
                return;
            }
            case 374:
            {
                if (tick >= 27311)
                {
                    state = 375;
                    goto case 375;
                }
                sink.Sample(in payloads[213], 1f);
                return;
            }
            case 375:
            {
                if (tick >= 27353)
                {
                    state = 376;
                    goto case 376;
                }
                var factor = (float)(tick - 27311) / 41f;
                sink.Sample(in payloads[213], 1f - factor);
                sink.Sample(in payloads[214], factor);
                return;
            }
            case 376:
            {
                if (tick >= 27453)
                {
                    state = 377;
                    goto case 377;
                }
                sink.Sample(in payloads[214], 1f);
                return;
            }
            case 377:
            {
                if (tick >= 27597)
                {
                    state = 378;
                    goto case 378;
                }
                sink.Sample(in payloads[215], 1f);
                return;
            }
            case 378:
            {
                if (tick >= 27598)
                {
                    state = 379;
                    goto case 379;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[215], 1f - factor);
                sink.Sample(in payloads[216], factor);
                return;
            }
            case 379:
            {
                if (tick >= 27656)
                {
                    state = 380;
                    goto case 380;
                }
                sink.Sample(in payloads[216], 1f);
                return;
            }
            case 380:
            {
                if (tick >= 27687)
                {
                    state = 381;
                    goto case 381;
                }
                return;
            }
            case 381:
            {
                if (tick >= 27832)
                {
                    state = 382;
                    goto case 382;
                }
                sink.Sample(in payloads[217], 1f);
                return;
            }
            case 382:
            {
                if (tick >= 27874)
                {
                    state = 383;
                    goto case 383;
                }
                var factor = (float)(tick - 27832) / 41f;
                sink.Sample(in payloads[217], 1f - factor);
                sink.Sample(in payloads[218], factor);
                return;
            }
            case 383:
            {
                if (tick >= 27981)
                {
                    state = 384;
                    goto case 384;
                }
                sink.Sample(in payloads[218], 1f);
                return;
            }
            case 384:
            {
                if (tick >= 28100)
                {
                    state = 385;
                    goto case 385;
                }
                sink.Sample(in payloads[219], 1f);
                return;
            }
            case 385:
            {
                if (tick >= 28101)
                {
                    state = 386;
                    goto case 386;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[219], 1f - factor);
                sink.Sample(in payloads[220], factor);
                return;
            }
            case 386:
            {
                if (tick >= 28202)
                {
                    state = 387;
                    goto case 387;
                }
                sink.Sample(in payloads[220], 1f);
                return;
            }
            case 387:
            {
                if (tick >= 28233)
                {
                    state = 388;
                    goto case 388;
                }
                return;
            }
            case 388:
            {
                if (tick >= 28339)
                {
                    state = 389;
                    goto case 389;
                }
                sink.Sample(in payloads[221], 1f);
                return;
            }
            case 389:
            {
                if (tick >= 28381)
                {
                    state = 390;
                    goto case 390;
                }
                var factor = (float)(tick - 28339) / 41f;
                sink.Sample(in payloads[221], 1f - factor);
                sink.Sample(in payloads[222], factor);
                return;
            }
            case 390:
            {
                if (tick >= 28468)
                {
                    state = 391;
                    goto case 391;
                }
                sink.Sample(in payloads[222], 1f);
                return;
            }
            case 391:
            {
                if (tick >= 28606)
                {
                    state = 392;
                    goto case 392;
                }
                sink.Sample(in payloads[223], 1f);
                return;
            }
            case 392:
            {
                if (tick >= 28607)
                {
                    state = 393;
                    goto case 393;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[223], 1f - factor);
                sink.Sample(in payloads[224], factor);
                return;
            }
            case 393:
            {
                if (tick >= 28688)
                {
                    state = 394;
                    goto case 394;
                }
                sink.Sample(in payloads[224], 1f);
                return;
            }
            case 394:
            {
                if (tick >= 28719)
                {
                    state = 395;
                    goto case 395;
                }
                return;
            }
            case 395:
            {
                if (tick >= 28863)
                {
                    state = 396;
                    goto case 396;
                }
                sink.Sample(in payloads[225], 1f);
                return;
            }
            case 396:
            {
                if (tick >= 28905)
                {
                    state = 397;
                    goto case 397;
                }
                var factor = (float)(tick - 28863) / 41f;
                sink.Sample(in payloads[225], 1f - factor);
                sink.Sample(in payloads[226], factor);
                return;
            }
            case 397:
            {
                if (tick >= 28982)
                {
                    state = 398;
                    goto case 398;
                }
                sink.Sample(in payloads[226], 1f);
                return;
            }
            case 398:
            {
                if (tick >= 29096)
                {
                    state = 399;
                    goto case 399;
                }
                sink.Sample(in payloads[227], 1f);
                return;
            }
            case 399:
            {
                if (tick >= 29097)
                {
                    state = 400;
                    goto case 400;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[227], 1f - factor);
                sink.Sample(in payloads[228], factor);
                return;
            }
            case 400:
            {
                if (tick >= 29193)
                {
                    state = 401;
                    goto case 401;
                }
                sink.Sample(in payloads[228], 1f);
                return;
            }
            case 401:
            {
                if (tick >= 29224)
                {
                    state = 402;
                    goto case 402;
                }
                return;
            }
            case 402:
            {
                if (tick >= 29375)
                {
                    state = 403;
                    goto case 403;
                }
                sink.Sample(in payloads[229], 1f);
                return;
            }
            case 403:
            {
                if (tick >= 29417)
                {
                    state = 404;
                    goto case 404;
                }
                var factor = (float)(tick - 29375) / 41f;
                sink.Sample(in payloads[229], 1f - factor);
                sink.Sample(in payloads[230], factor);
                return;
            }
            case 404:
            {
                if (tick >= 29503)
                {
                    state = 405;
                    goto case 405;
                }
                sink.Sample(in payloads[230], 1f);
                return;
            }
            case 405:
            {
                if (tick >= 29597)
                {
                    state = 406;
                    goto case 406;
                }
                sink.Sample(in payloads[231], 1f);
                return;
            }
            case 406:
            {
                if (tick >= 29598)
                {
                    state = 407;
                    goto case 407;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[231], 1f - factor);
                sink.Sample(in payloads[232], factor);
                return;
            }
            case 407:
            {
                if (tick >= 29727)
                {
                    state = 408;
                    goto case 408;
                }
                sink.Sample(in payloads[232], 1f);
                return;
            }
            case 408:
            {
                if (tick >= 29758)
                {
                    state = 409;
                    goto case 409;
                }
                return;
            }
            case 409:
            {
                if (tick >= 29847)
                {
                    state = 410;
                    goto case 410;
                }
                sink.Sample(in payloads[233], 1f);
                return;
            }
            case 410:
            {
                if (tick >= 29889)
                {
                    state = 411;
                    goto case 411;
                }
                var factor = (float)(tick - 29847) / 41f;
                sink.Sample(in payloads[233], 1f - factor);
                sink.Sample(in payloads[234], factor);
                return;
            }
            case 411:
            {
                if (tick >= 29982)
                {
                    state = 412;
                    goto case 412;
                }
                sink.Sample(in payloads[234], 1f);
                return;
            }
            case 412:
            {
                if (tick >= 30105)
                {
                    state = 413;
                    goto case 413;
                }
                sink.Sample(in payloads[235], 1f);
                return;
            }
            case 413:
            {
                if (tick >= 30106)
                {
                    state = 414;
                    goto case 414;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[235], 1f - factor);
                sink.Sample(in payloads[236], factor);
                return;
            }
            case 414:
            {
                if (tick >= 30195)
                {
                    state = 415;
                    goto case 415;
                }
                sink.Sample(in payloads[236], 1f);
                return;
            }
            case 415:
            {
                if (tick >= 30226)
                {
                    state = 416;
                    goto case 416;
                }
                return;
            }
            case 416:
            {
                if (tick >= 30382)
                {
                    state = 417;
                    goto case 417;
                }
                sink.Sample(in payloads[237], 1f);
                return;
            }
            case 417:
            {
                if (tick >= 30424)
                {
                    state = 418;
                    goto case 418;
                }
                var factor = (float)(tick - 30382) / 41f;
                sink.Sample(in payloads[237], 1f - factor);
                sink.Sample(in payloads[238], factor);
                return;
            }
            case 418:
            {
                if (tick >= 30497)
                {
                    state = 419;
                    goto case 419;
                }
                sink.Sample(in payloads[238], 1f);
                return;
            }
            case 419:
            {
                if (tick >= 30618)
                {
                    state = 420;
                    goto case 420;
                }
                sink.Sample(in payloads[239], 1f);
                return;
            }
            case 420:
            {
                if (tick >= 30619)
                {
                    state = 421;
                    goto case 421;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[239], 1f - factor);
                sink.Sample(in payloads[240], factor);
                return;
            }
            case 421:
            {
                if (tick >= 30715)
                {
                    state = 422;
                    goto case 422;
                }
                sink.Sample(in payloads[240], 1f);
                return;
            }
            case 422:
            {
                if (tick >= 30746)
                {
                    state = 423;
                    goto case 423;
                }
                return;
            }
            case 423:
            {
                if (tick >= 30864)
                {
                    state = 424;
                    goto case 424;
                }
                sink.Sample(in payloads[241], 1f);
                return;
            }
            case 424:
            {
                if (tick >= 30906)
                {
                    state = 425;
                    goto case 425;
                }
                var factor = (float)(tick - 30864) / 41f;
                sink.Sample(in payloads[241], 1f - factor);
                sink.Sample(in payloads[242], factor);
                return;
            }
            case 425:
            {
                if (tick >= 31020)
                {
                    state = 426;
                    goto case 426;
                }
                sink.Sample(in payloads[242], 1f);
                return;
            }
            case 426:
            {
                if (tick >= 31126)
                {
                    state = 427;
                    goto case 427;
                }
                sink.Sample(in payloads[243], 1f);
                return;
            }
            case 427:
            {
                if (tick >= 31127)
                {
                    state = 428;
                    goto case 428;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[243], 1f - factor);
                sink.Sample(in payloads[244], factor);
                return;
            }
            case 428:
            {
                if (tick >= 31234)
                {
                    state = 429;
                    goto case 429;
                }
                sink.Sample(in payloads[244], 1f);
                return;
            }
            case 429:
            {
                if (tick >= 31265)
                {
                    state = 430;
                    goto case 430;
                }
                return;
            }
            case 430:
            {
                if (tick >= 31402)
                {
                    state = 431;
                    goto case 431;
                }
                sink.Sample(in payloads[245], 1f);
                return;
            }
            case 431:
            {
                if (tick >= 31444)
                {
                    state = 432;
                    goto case 432;
                }
                var factor = (float)(tick - 31402) / 41f;
                sink.Sample(in payloads[245], 1f - factor);
                sink.Sample(in payloads[246], factor);
                return;
            }
            case 432:
            {
                if (tick >= 31503)
                {
                    state = 433;
                    goto case 433;
                }
                sink.Sample(in payloads[246], 1f);
                return;
            }
            case 433:
            {
                if (tick >= 31653)
                {
                    state = 434;
                    goto case 434;
                }
                sink.Sample(in payloads[247], 1f);
                return;
            }
            case 434:
            {
                if (tick >= 31654)
                {
                    state = 435;
                    goto case 435;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[247], 1f - factor);
                sink.Sample(in payloads[248], factor);
                return;
            }
            case 435:
            {
                if (tick >= 31758)
                {
                    state = 436;
                    goto case 436;
                }
                sink.Sample(in payloads[248], 1f);
                return;
            }
            case 436:
            {
                if (tick >= 31789)
                {
                    state = 437;
                    goto case 437;
                }
                return;
            }
            case 437:
            {
                if (tick >= 31917)
                {
                    state = 438;
                    goto case 438;
                }
                sink.Sample(in payloads[249], 1f);
                return;
            }
            case 438:
            {
                if (tick >= 31959)
                {
                    state = 439;
                    goto case 439;
                }
                var factor = (float)(tick - 31917) / 41f;
                sink.Sample(in payloads[249], 1f - factor);
                sink.Sample(in payloads[250], factor);
                return;
            }
            case 439:
            {
                if (tick >= 32012)
                {
                    state = 440;
                    goto case 440;
                }
                sink.Sample(in payloads[250], 1f);
                return;
            }
            case 440:
            {
                if (tick >= 32171)
                {
                    state = 441;
                    goto case 441;
                }
                sink.Sample(in payloads[251], 1f);
                return;
            }
            case 441:
            {
                if (tick >= 32172)
                {
                    state = 442;
                    goto case 442;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[251], 1f - factor);
                sink.Sample(in payloads[252], factor);
                return;
            }
            case 442:
            {
                if (tick >= 32242)
                {
                    state = 443;
                    goto case 443;
                }
                sink.Sample(in payloads[252], 1f);
                return;
            }
            case 443:
            {
                if (tick >= 32273)
                {
                    state = 444;
                    goto case 444;
                }
                return;
            }
            case 444:
            {
                if (tick >= 32392)
                {
                    state = 445;
                    goto case 445;
                }
                sink.Sample(in payloads[253], 1f);
                return;
            }
            case 445:
            {
                if (tick >= 32434)
                {
                    state = 446;
                    goto case 446;
                }
                var factor = (float)(tick - 32392) / 41f;
                sink.Sample(in payloads[253], 1f - factor);
                sink.Sample(in payloads[254], factor);
                return;
            }
            case 446:
            {
                if (tick >= 32534)
                {
                    state = 447;
                    goto case 447;
                }
                sink.Sample(in payloads[254], 1f);
                return;
            }
            case 447:
            {
                if (tick >= 32667)
                {
                    state = 448;
                    goto case 448;
                }
                sink.Sample(in payloads[255], 1f);
                return;
            }
            case 448:
            {
                if (tick >= 32668)
                {
                    state = 449;
                    goto case 449;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[255], 1f - factor);
                sink.Sample(in payloads[256], factor);
                return;
            }
            case 449:
            {
                if (tick >= 32747)
                {
                    state = 450;
                    goto case 450;
                }
                sink.Sample(in payloads[256], 1f);
                return;
            }
            case 450:
            {
                if (tick >= 32778)
                {
                    state = 451;
                    goto case 451;
                }
                return;
            }
            case 451:
            {
                if (tick >= 32918)
                {
                    state = 452;
                    goto case 452;
                }
                sink.Sample(in payloads[257], 1f);
                return;
            }
            case 452:
            {
                if (tick >= 32960)
                {
                    state = 453;
                    goto case 453;
                }
                var factor = (float)(tick - 32918) / 41f;
                sink.Sample(in payloads[257], 1f - factor);
                sink.Sample(in payloads[258], factor);
                return;
            }
            case 453:
            {
                if (tick >= 33027)
                {
                    state = 454;
                    goto case 454;
                }
                sink.Sample(in payloads[258], 1f);
                return;
            }
            case 454:
            {
                if (tick >= 33183)
                {
                    state = 455;
                    goto case 455;
                }
                sink.Sample(in payloads[259], 1f);
                return;
            }
            case 455:
            {
                if (tick >= 33184)
                {
                    state = 456;
                    goto case 456;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[259], 1f - factor);
                sink.Sample(in payloads[260], factor);
                return;
            }
            case 456:
            {
                if (tick >= 33246)
                {
                    state = 457;
                    goto case 457;
                }
                sink.Sample(in payloads[260], 1f);
                return;
            }
            case 457:
            {
                if (tick >= 33277)
                {
                    state = 458;
                    goto case 458;
                }
                return;
            }
            case 458:
            {
                if (tick >= 33436)
                {
                    state = 459;
                    goto case 459;
                }
                sink.Sample(in payloads[261], 1f);
                return;
            }
            case 459:
            {
                if (tick >= 33478)
                {
                    state = 460;
                    goto case 460;
                }
                var factor = (float)(tick - 33436) / 41f;
                sink.Sample(in payloads[261], 1f - factor);
                sink.Sample(in payloads[262], factor);
                return;
            }
            case 460:
            {
                if (tick >= 33553)
                {
                    state = 461;
                    goto case 461;
                }
                sink.Sample(in payloads[262], 1f);
                return;
            }
            case 461:
            {
                if (tick >= 33692)
                {
                    state = 462;
                    goto case 462;
                }
                sink.Sample(in payloads[263], 1f);
                return;
            }
            case 462:
            {
                if (tick >= 33693)
                {
                    state = 463;
                    goto case 463;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[263], 1f - factor);
                sink.Sample(in payloads[264], factor);
                return;
            }
            case 463:
            {
                if (tick >= 33782)
                {
                    state = 464;
                    goto case 464;
                }
                sink.Sample(in payloads[264], 1f);
                return;
            }
            case 464:
            {
                if (tick >= 33813)
                {
                    state = 465;
                    goto case 465;
                }
                return;
            }
            case 465:
            {
                if (tick >= 33923)
                {
                    state = 466;
                    goto case 466;
                }
                sink.Sample(in payloads[265], 1f);
                return;
            }
            case 466:
            {
                if (tick >= 33965)
                {
                    state = 467;
                    goto case 467;
                }
                var factor = (float)(tick - 33923) / 41f;
                sink.Sample(in payloads[265], 1f - factor);
                sink.Sample(in payloads[266], factor);
                return;
            }
            case 467:
            {
                if (tick >= 34068)
                {
                    state = 468;
                    goto case 468;
                }
                sink.Sample(in payloads[266], 1f);
                return;
            }
            case 468:
            {
                if (tick >= 34185)
                {
                    state = 469;
                    goto case 469;
                }
                sink.Sample(in payloads[267], 1f);
                return;
            }
            case 469:
            {
                if (tick >= 34186)
                {
                    state = 470;
                    goto case 470;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[267], 1f - factor);
                sink.Sample(in payloads[268], factor);
                return;
            }
            case 470:
            {
                if (tick >= 34282)
                {
                    state = 471;
                    goto case 471;
                }
                sink.Sample(in payloads[268], 1f);
                return;
            }
            case 471:
            {
                if (tick >= 34313)
                {
                    state = 472;
                    goto case 472;
                }
                return;
            }
            case 472:
            {
                if (tick >= 34449)
                {
                    state = 473;
                    goto case 473;
                }
                sink.Sample(in payloads[269], 1f);
                return;
            }
            case 473:
            {
                if (tick >= 34491)
                {
                    state = 474;
                    goto case 474;
                }
                var factor = (float)(tick - 34449) / 41f;
                sink.Sample(in payloads[269], 1f - factor);
                sink.Sample(in payloads[270], factor);
                return;
            }
            case 474:
            {
                if (tick >= 34580)
                {
                    state = 475;
                    goto case 475;
                }
                sink.Sample(in payloads[270], 1f);
                return;
            }
            case 475:
            {
                if (tick >= 34708)
                {
                    state = 476;
                    goto case 476;
                }
                sink.Sample(in payloads[271], 1f);
                return;
            }
            case 476:
            {
                if (tick >= 34709)
                {
                    state = 477;
                    goto case 477;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[271], 1f - factor);
                sink.Sample(in payloads[272], factor);
                return;
            }
            case 477:
            {
                if (tick >= 34788)
                {
                    state = 478;
                    goto case 478;
                }
                sink.Sample(in payloads[272], 1f);
                return;
            }
            case 478:
            {
                if (tick >= 34819)
                {
                    state = 479;
                    goto case 479;
                }
                return;
            }
            case 479:
            {
                if (tick >= 34936)
                {
                    state = 480;
                    goto case 480;
                }
                sink.Sample(in payloads[273], 1f);
                return;
            }
            case 480:
            {
                if (tick >= 34978)
                {
                    state = 481;
                    goto case 481;
                }
                var factor = (float)(tick - 34936) / 41f;
                sink.Sample(in payloads[273], 1f - factor);
                sink.Sample(in payloads[274], factor);
                return;
            }
            case 481:
            {
                if (tick >= 35070)
                {
                    state = 482;
                    goto case 482;
                }
                sink.Sample(in payloads[274], 1f);
                return;
            }
            case 482:
            {
                if (tick >= 35188)
                {
                    state = 483;
                    goto case 483;
                }
                sink.Sample(in payloads[275], 1f);
                return;
            }
            case 483:
            {
                if (tick >= 35189)
                {
                    state = 484;
                    goto case 484;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[275], 1f - factor);
                sink.Sample(in payloads[276], factor);
                return;
            }
            case 484:
            {
                if (tick >= 35278)
                {
                    state = 485;
                    goto case 485;
                }
                sink.Sample(in payloads[276], 1f);
                return;
            }
            case 485:
            {
                if (tick >= 35309)
                {
                    state = 486;
                    goto case 486;
                }
                return;
            }
            case 486:
            {
                if (tick >= 35448)
                {
                    state = 487;
                    goto case 487;
                }
                sink.Sample(in payloads[277], 1f);
                return;
            }
            case 487:
            {
                if (tick >= 35490)
                {
                    state = 488;
                    goto case 488;
                }
                var factor = (float)(tick - 35448) / 41f;
                sink.Sample(in payloads[277], 1f - factor);
                sink.Sample(in payloads[278], factor);
                return;
            }
            case 488:
            {
                if (tick >= 35590)
                {
                    state = 489;
                    goto case 489;
                }
                sink.Sample(in payloads[278], 1f);
                return;
            }
            case 489:
            {
                if (tick >= 35722)
                {
                    state = 490;
                    goto case 490;
                }
                sink.Sample(in payloads[279], 1f);
                return;
            }
            case 490:
            {
                if (tick >= 35723)
                {
                    state = 491;
                    goto case 491;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[279], 1f - factor);
                sink.Sample(in payloads[280], factor);
                return;
            }
            case 491:
            {
                if (tick >= 35802)
                {
                    state = 492;
                    goto case 492;
                }
                sink.Sample(in payloads[280], 1f);
                return;
            }
            case 492:
            {
                if (tick >= 35833)
                {
                    state = 493;
                    goto case 493;
                }
                return;
            }
            case 493:
            {
                if (tick >= 35949)
                {
                    state = 494;
                    goto case 494;
                }
                sink.Sample(in payloads[281], 1f);
                return;
            }
            case 494:
            {
                if (tick >= 35991)
                {
                    state = 495;
                    goto case 495;
                }
                var factor = (float)(tick - 35949) / 41f;
                sink.Sample(in payloads[281], 1f - factor);
                sink.Sample(in payloads[282], factor);
                return;
            }
            case 495:
            {
                if (tick >= 36093)
                {
                    state = 496;
                    goto case 496;
                }
                sink.Sample(in payloads[282], 1f);
                return;
            }
            case 496:
            {
                if (tick >= 36228)
                {
                    state = 497;
                    goto case 497;
                }
                sink.Sample(in payloads[283], 1f);
                return;
            }
            case 497:
            {
                if (tick >= 36229)
                {
                    state = 498;
                    goto case 498;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[283], 1f - factor);
                sink.Sample(in payloads[284], factor);
                return;
            }
            case 498:
            {
                if (tick >= 36299)
                {
                    state = 499;
                    goto case 499;
                }
                sink.Sample(in payloads[284], 1f);
                return;
            }
            case 499:
            {
                if (tick >= 36330)
                {
                    state = 500;
                    goto case 500;
                }
                return;
            }
            case 500:
            {
                if (tick >= 36467)
                {
                    state = 501;
                    goto case 501;
                }
                sink.Sample(in payloads[285], 1f);
                return;
            }
            case 501:
            {
                if (tick >= 36509)
                {
                    state = 502;
                    goto case 502;
                }
                var factor = (float)(tick - 36467) / 41f;
                sink.Sample(in payloads[285], 1f - factor);
                sink.Sample(in payloads[286], factor);
                return;
            }
            case 502:
            {
                if (tick >= 36609)
                {
                    state = 503;
                    goto case 503;
                }
                sink.Sample(in payloads[286], 1f);
                return;
            }
            case 503:
            {
                if (tick >= 36703)
                {
                    state = 504;
                    goto case 504;
                }
                sink.Sample(in payloads[287], 1f);
                return;
            }
            case 504:
            {
                if (tick >= 36704)
                {
                    state = 505;
                    goto case 505;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[287], 1f - factor);
                sink.Sample(in payloads[288], factor);
                return;
            }
            case 505:
            {
                if (tick >= 36803)
                {
                    state = 506;
                    goto case 506;
                }
                sink.Sample(in payloads[288], 1f);
                return;
            }
            case 506:
            {
                if (tick >= 36834)
                {
                    state = 507;
                    goto case 507;
                }
                return;
            }
            case 507:
            {
                if (tick >= 36972)
                {
                    state = 508;
                    goto case 508;
                }
                sink.Sample(in payloads[289], 1f);
                return;
            }
            case 508:
            {
                if (tick >= 37014)
                {
                    state = 509;
                    goto case 509;
                }
                var factor = (float)(tick - 36972) / 41f;
                sink.Sample(in payloads[289], 1f - factor);
                sink.Sample(in payloads[290], factor);
                return;
            }
            case 509:
            {
                if (tick >= 37088)
                {
                    state = 510;
                    goto case 510;
                }
                sink.Sample(in payloads[290], 1f);
                return;
            }
            case 510:
            {
                if (tick >= 37233)
                {
                    state = 511;
                    goto case 511;
                }
                sink.Sample(in payloads[291], 1f);
                return;
            }
            case 511:
            {
                if (tick >= 37234)
                {
                    state = 512;
                    goto case 512;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[291], 1f - factor);
                sink.Sample(in payloads[292], factor);
                return;
            }
            case 512:
            {
                if (tick >= 37332)
                {
                    state = 513;
                    goto case 513;
                }
                sink.Sample(in payloads[292], 1f);
                return;
            }
            case 513:
            {
                if (tick >= 37363)
                {
                    state = 514;
                    goto case 514;
                }
                return;
            }
            case 514:
            {
                if (tick >= 37495)
                {
                    state = 515;
                    goto case 515;
                }
                sink.Sample(in payloads[293], 1f);
                return;
            }
            case 515:
            {
                if (tick >= 37537)
                {
                    state = 516;
                    goto case 516;
                }
                var factor = (float)(tick - 37495) / 41f;
                sink.Sample(in payloads[293], 1f - factor);
                sink.Sample(in payloads[294], factor);
                return;
            }
            case 516:
            {
                if (tick >= 37613)
                {
                    state = 517;
                    goto case 517;
                }
                sink.Sample(in payloads[294], 1f);
                return;
            }
            case 517:
            {
                if (tick >= 37755)
                {
                    state = 518;
                    goto case 518;
                }
                sink.Sample(in payloads[295], 1f);
                return;
            }
            case 518:
            {
                if (tick >= 37756)
                {
                    state = 519;
                    goto case 519;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[295], 1f - factor);
                sink.Sample(in payloads[296], factor);
                return;
            }
            case 519:
            {
                if (tick >= 37856)
                {
                    state = 520;
                    goto case 520;
                }
                sink.Sample(in payloads[296], 1f);
                return;
            }
            case 520:
            {
                if (tick >= 37887)
                {
                    state = 521;
                    goto case 521;
                }
                return;
            }
            case 521:
            {
                if (tick >= 38003)
                {
                    state = 522;
                    goto case 522;
                }
                sink.Sample(in payloads[297], 1f);
                return;
            }
            case 522:
            {
                if (tick >= 38045)
                {
                    state = 523;
                    goto case 523;
                }
                var factor = (float)(tick - 38003) / 41f;
                sink.Sample(in payloads[297], 1f - factor);
                sink.Sample(in payloads[298], factor);
                return;
            }
            case 523:
            {
                if (tick >= 38115)
                {
                    state = 524;
                    goto case 524;
                }
                sink.Sample(in payloads[298], 1f);
                return;
            }
            case 524:
            {
                if (tick >= 38233)
                {
                    state = 525;
                    goto case 525;
                }
                sink.Sample(in payloads[299], 1f);
                return;
            }
            case 525:
            {
                if (tick >= 38234)
                {
                    state = 526;
                    goto case 526;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[299], 1f - factor);
                sink.Sample(in payloads[300], factor);
                return;
            }
            case 526:
            {
                if (tick >= 38325)
                {
                    state = 527;
                    goto case 527;
                }
                sink.Sample(in payloads[300], 1f);
                return;
            }
            case 527:
            {
                if (tick >= 38356)
                {
                    state = 528;
                    goto case 528;
                }
                return;
            }
            case 528:
            {
                if (tick >= 38513)
                {
                    state = 529;
                    goto case 529;
                }
                sink.Sample(in payloads[301], 1f);
                return;
            }
            case 529:
            {
                if (tick >= 38555)
                {
                    state = 530;
                    goto case 530;
                }
                var factor = (float)(tick - 38513) / 41f;
                sink.Sample(in payloads[301], 1f - factor);
                sink.Sample(in payloads[302], factor);
                return;
            }
            case 530:
            {
                if (tick >= 38647)
                {
                    state = 531;
                    goto case 531;
                }
                sink.Sample(in payloads[302], 1f);
                return;
            }
            case 531:
            {
                if (tick >= 38743)
                {
                    state = 532;
                    goto case 532;
                }
                sink.Sample(in payloads[303], 1f);
                return;
            }
            case 532:
            {
                if (tick >= 38744)
                {
                    state = 533;
                    goto case 533;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[303], 1f - factor);
                sink.Sample(in payloads[304], factor);
                return;
            }
            case 533:
            {
                if (tick >= 38837)
                {
                    state = 534;
                    goto case 534;
                }
                sink.Sample(in payloads[304], 1f);
                return;
            }
            case 534:
            {
                if (tick >= 38868)
                {
                    state = 535;
                    goto case 535;
                }
                return;
            }
            case 535:
            {
                if (tick >= 39012)
                {
                    state = 536;
                    goto case 536;
                }
                sink.Sample(in payloads[305], 1f);
                return;
            }
            case 536:
            {
                if (tick >= 39054)
                {
                    state = 537;
                    goto case 537;
                }
                var factor = (float)(tick - 39012) / 41f;
                sink.Sample(in payloads[305], 1f - factor);
                sink.Sample(in payloads[306], factor);
                return;
            }
            case 537:
            {
                if (tick >= 39149)
                {
                    state = 538;
                    goto case 538;
                }
                sink.Sample(in payloads[306], 1f);
                return;
            }
            case 538:
            {
                if (tick >= 39282)
                {
                    state = 539;
                    goto case 539;
                }
                sink.Sample(in payloads[307], 1f);
                return;
            }
            case 539:
            {
                if (tick >= 39283)
                {
                    state = 540;
                    goto case 540;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[307], 1f - factor);
                sink.Sample(in payloads[308], factor);
                return;
            }
            case 540:
            {
                if (tick >= 39355)
                {
                    state = 541;
                    goto case 541;
                }
                sink.Sample(in payloads[308], 1f);
                return;
            }
            case 541:
            {
                if (tick >= 39386)
                {
                    state = 542;
                    goto case 542;
                }
                return;
            }
            case 542:
            {
                if (tick >= 39512)
                {
                    state = 543;
                    goto case 543;
                }
                sink.Sample(in payloads[309], 1f);
                return;
            }
            case 543:
            {
                if (tick >= 39554)
                {
                    state = 544;
                    goto case 544;
                }
                var factor = (float)(tick - 39512) / 41f;
                sink.Sample(in payloads[309], 1f - factor);
                sink.Sample(in payloads[310], factor);
                return;
            }
            case 544:
            {
                if (tick >= 39625)
                {
                    state = 545;
                    goto case 545;
                }
                sink.Sample(in payloads[310], 1f);
                return;
            }
            case 545:
            {
                if (tick >= 39779)
                {
                    state = 546;
                    goto case 546;
                }
                sink.Sample(in payloads[311], 1f);
                return;
            }
            case 546:
            {
                if (tick >= 39780)
                {
                    state = 547;
                    goto case 547;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[311], 1f - factor);
                sink.Sample(in payloads[312], factor);
                return;
            }
            case 547:
            {
                if (tick >= 39854)
                {
                    state = 548;
                    goto case 548;
                }
                sink.Sample(in payloads[312], 1f);
                return;
            }
            case 548:
            {
                if (tick >= 39885)
                {
                    state = 549;
                    goto case 549;
                }
                return;
            }
            case 549:
            {
                if (tick >= 40040)
                {
                    state = 550;
                    goto case 550;
                }
                sink.Sample(in payloads[313], 1f);
                return;
            }
            case 550:
            {
                if (tick >= 40082)
                {
                    state = 551;
                    goto case 551;
                }
                var factor = (float)(tick - 40040) / 41f;
                sink.Sample(in payloads[313], 1f - factor);
                sink.Sample(in payloads[314], factor);
                return;
            }
            case 551:
            {
                if (tick >= 40147)
                {
                    state = 552;
                    goto case 552;
                }
                sink.Sample(in payloads[314], 1f);
                return;
            }
            case 552:
            {
                if (tick >= 40268)
                {
                    state = 553;
                    goto case 553;
                }
                sink.Sample(in payloads[315], 1f);
                return;
            }
            case 553:
            {
                if (tick >= 40269)
                {
                    state = 554;
                    goto case 554;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[315], 1f - factor);
                sink.Sample(in payloads[316], factor);
                return;
            }
            case 554:
            {
                if (tick >= 40374)
                {
                    state = 555;
                    goto case 555;
                }
                sink.Sample(in payloads[316], 1f);
                return;
            }
            case 555:
            {
                if (tick >= 40405)
                {
                    state = 556;
                    goto case 556;
                }
                return;
            }
            case 556:
            {
                if (tick >= 40513)
                {
                    state = 557;
                    goto case 557;
                }
                sink.Sample(in payloads[317], 1f);
                return;
            }
            case 557:
            {
                if (tick >= 40555)
                {
                    state = 558;
                    goto case 558;
                }
                var factor = (float)(tick - 40513) / 41f;
                sink.Sample(in payloads[317], 1f - factor);
                sink.Sample(in payloads[318], factor);
                return;
            }
            case 558:
            {
                if (tick >= 40675)
                {
                    state = 559;
                    goto case 559;
                }
                sink.Sample(in payloads[318], 1f);
                return;
            }
            case 559:
            {
                if (tick >= 40792)
                {
                    state = 560;
                    goto case 560;
                }
                sink.Sample(in payloads[319], 1f);
                return;
            }
            case 560:
            {
                if (tick >= 40793)
                {
                    state = 561;
                    goto case 561;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[319], 1f - factor);
                sink.Sample(in payloads[320], factor);
                return;
            }
            case 561:
            {
                if (tick >= 40875)
                {
                    state = 562;
                    goto case 562;
                }
                sink.Sample(in payloads[320], 1f);
                return;
            }
            case 562:
            {
                if (tick >= 40906)
                {
                    state = 563;
                    goto case 563;
                }
                return;
            }
            case 563:
            {
                if (tick >= 41045)
                {
                    state = 564;
                    goto case 564;
                }
                sink.Sample(in payloads[321], 1f);
                return;
            }
            case 564:
            {
                if (tick >= 41087)
                {
                    state = 565;
                    goto case 565;
                }
                var factor = (float)(tick - 41045) / 41f;
                sink.Sample(in payloads[321], 1f - factor);
                sink.Sample(in payloads[322], factor);
                return;
            }
            case 565:
            {
                if (tick >= 41181)
                {
                    state = 566;
                    goto case 566;
                }
                sink.Sample(in payloads[322], 1f);
                return;
            }
            case 566:
            {
                if (tick >= 41304)
                {
                    state = 567;
                    goto case 567;
                }
                sink.Sample(in payloads[323], 1f);
                return;
            }
            case 567:
            {
                if (tick >= 41305)
                {
                    state = 568;
                    goto case 568;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[323], 1f - factor);
                sink.Sample(in payloads[324], factor);
                return;
            }
            case 568:
            {
                if (tick >= 41391)
                {
                    state = 569;
                    goto case 569;
                }
                sink.Sample(in payloads[324], 1f);
                return;
            }
            case 569:
            {
                if (tick >= 41422)
                {
                    state = 570;
                    goto case 570;
                }
                return;
            }
            case 570:
            {
                if (tick >= 41567)
                {
                    state = 571;
                    goto case 571;
                }
                sink.Sample(in payloads[325], 1f);
                return;
            }
            case 571:
            {
                if (tick >= 41609)
                {
                    state = 572;
                    goto case 572;
                }
                var factor = (float)(tick - 41567) / 41f;
                sink.Sample(in payloads[325], 1f - factor);
                sink.Sample(in payloads[326], factor);
                return;
            }
            case 572:
            {
                if (tick >= 41690)
                {
                    state = 573;
                    goto case 573;
                }
                sink.Sample(in payloads[326], 1f);
                return;
            }
            case 573:
            {
                if (tick >= 41820)
                {
                    state = 574;
                    goto case 574;
                }
                sink.Sample(in payloads[327], 1f);
                return;
            }
            case 574:
            {
                if (tick >= 41821)
                {
                    state = 575;
                    goto case 575;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[327], 1f - factor);
                sink.Sample(in payloads[328], factor);
                return;
            }
            case 575:
            {
                if (tick >= 41883)
                {
                    state = 576;
                    goto case 576;
                }
                sink.Sample(in payloads[328], 1f);
                return;
            }
            case 576:
            {
                if (tick >= 41914)
                {
                    state = 577;
                    goto case 577;
                }
                return;
            }
            case 577:
            {
                if (tick >= 42075)
                {
                    state = 578;
                    goto case 578;
                }
                sink.Sample(in payloads[329], 1f);
                return;
            }
            case 578:
            {
                if (tick >= 42117)
                {
                    state = 579;
                    goto case 579;
                }
                var factor = (float)(tick - 42075) / 41f;
                sink.Sample(in payloads[329], 1f - factor);
                sink.Sample(in payloads[330], factor);
                return;
            }
            case 579:
            {
                if (tick >= 42188)
                {
                    state = 580;
                    goto case 580;
                }
                sink.Sample(in payloads[330], 1f);
                return;
            }
            case 580:
            {
                if (tick >= 42291)
                {
                    state = 581;
                    goto case 581;
                }
                sink.Sample(in payloads[331], 1f);
                return;
            }
            case 581:
            {
                if (tick >= 42292)
                {
                    state = 582;
                    goto case 582;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[331], 1f - factor);
                sink.Sample(in payloads[332], factor);
                return;
            }
            case 582:
            {
                if (tick >= 42401)
                {
                    state = 583;
                    goto case 583;
                }
                sink.Sample(in payloads[332], 1f);
                return;
            }
            case 583:
            {
                if (tick >= 42432)
                {
                    state = 584;
                    goto case 584;
                }
                return;
            }
            case 584:
            {
                if (tick >= 42564)
                {
                    state = 585;
                    goto case 585;
                }
                sink.Sample(in payloads[333], 1f);
                return;
            }
            case 585:
            {
                if (tick >= 42606)
                {
                    state = 586;
                    goto case 586;
                }
                var factor = (float)(tick - 42564) / 41f;
                sink.Sample(in payloads[333], 1f - factor);
                sink.Sample(in payloads[334], factor);
                return;
            }
            case 586:
            {
                if (tick >= 42711)
                {
                    state = 587;
                    goto case 587;
                }
                sink.Sample(in payloads[334], 1f);
                return;
            }
            case 587:
            {
                if (tick >= 42826)
                {
                    state = 588;
                    goto case 588;
                }
                sink.Sample(in payloads[335], 1f);
                return;
            }
            case 588:
            {
                if (tick >= 42827)
                {
                    state = 589;
                    goto case 589;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[335], 1f - factor);
                sink.Sample(in payloads[336], factor);
                return;
            }
            case 589:
            {
                if (tick >= 42933)
                {
                    state = 590;
                    goto case 590;
                }
                sink.Sample(in payloads[336], 1f);
                return;
            }
            case 590:
            {
                if (tick >= 42964)
                {
                    state = 591;
                    goto case 591;
                }
                return;
            }
            case 591:
            {
                if (tick >= 43080)
                {
                    state = 592;
                    goto case 592;
                }
                sink.Sample(in payloads[337], 1f);
                return;
            }
            case 592:
            {
                if (tick >= 43122)
                {
                    state = 593;
                    goto case 593;
                }
                var factor = (float)(tick - 43080) / 41f;
                sink.Sample(in payloads[337], 1f - factor);
                sink.Sample(in payloads[338], factor);
                return;
            }
            case 593:
            {
                if (tick >= 43201)
                {
                    state = 594;
                    goto case 594;
                }
                sink.Sample(in payloads[338], 1f);
                return;
            }
            case 594:
            {
                if (tick >= 43338)
                {
                    state = 595;
                    goto case 595;
                }
                sink.Sample(in payloads[339], 1f);
                return;
            }
            case 595:
            {
                if (tick >= 43339)
                {
                    state = 596;
                    goto case 596;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[339], 1f - factor);
                sink.Sample(in payloads[340], factor);
                return;
            }
            case 596:
            {
                if (tick >= 43419)
                {
                    state = 597;
                    goto case 597;
                }
                sink.Sample(in payloads[340], 1f);
                return;
            }
            case 597:
            {
                if (tick >= 43450)
                {
                    state = 598;
                    goto case 598;
                }
                return;
            }
            case 598:
            {
                if (tick >= 43594)
                {
                    state = 599;
                    goto case 599;
                }
                sink.Sample(in payloads[341], 1f);
                return;
            }
            case 599:
            {
                if (tick >= 43636)
                {
                    state = 600;
                    goto case 600;
                }
                var factor = (float)(tick - 43594) / 41f;
                sink.Sample(in payloads[341], 1f - factor);
                sink.Sample(in payloads[342], factor);
                return;
            }
            case 600:
            {
                if (tick >= 43698)
                {
                    state = 601;
                    goto case 601;
                }
                sink.Sample(in payloads[342], 1f);
                return;
            }
            case 601:
            {
                if (tick >= 43819)
                {
                    state = 602;
                    goto case 602;
                }
                sink.Sample(in payloads[343], 1f);
                return;
            }
            case 602:
            {
                if (tick >= 43820)
                {
                    state = 603;
                    goto case 603;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[343], 1f - factor);
                sink.Sample(in payloads[344], factor);
                return;
            }
            case 603:
            {
                if (tick >= 43917)
                {
                    state = 604;
                    goto case 604;
                }
                sink.Sample(in payloads[344], 1f);
                return;
            }
            case 604:
            {
                if (tick >= 43948)
                {
                    state = 605;
                    goto case 605;
                }
                return;
            }
            case 605:
            {
                if (tick >= 44103)
                {
                    state = 606;
                    goto case 606;
                }
                sink.Sample(in payloads[345], 1f);
                return;
            }
            case 606:
            {
                if (tick >= 44145)
                {
                    state = 607;
                    goto case 607;
                }
                var factor = (float)(tick - 44103) / 41f;
                sink.Sample(in payloads[345], 1f - factor);
                sink.Sample(in payloads[346], factor);
                return;
            }
            case 607:
            {
                if (tick >= 44211)
                {
                    state = 608;
                    goto case 608;
                }
                sink.Sample(in payloads[346], 1f);
                return;
            }
            case 608:
            {
                if (tick >= 44353)
                {
                    state = 609;
                    goto case 609;
                }
                sink.Sample(in payloads[347], 1f);
                return;
            }
            case 609:
            {
                if (tick >= 44354)
                {
                    state = 610;
                    goto case 610;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[347], 1f - factor);
                sink.Sample(in payloads[348], factor);
                return;
            }
            case 610:
            {
                if (tick >= 44421)
                {
                    state = 611;
                    goto case 611;
                }
                sink.Sample(in payloads[348], 1f);
                return;
            }
            case 611:
            {
                if (tick >= 44452)
                {
                    state = 612;
                    goto case 612;
                }
                return;
            }
            case 612:
            {
                if (tick >= 44610)
                {
                    state = 613;
                    goto case 613;
                }
                sink.Sample(in payloads[349], 1f);
                return;
            }
            case 613:
            {
                if (tick >= 44652)
                {
                    state = 614;
                    goto case 614;
                }
                var factor = (float)(tick - 44610) / 41f;
                sink.Sample(in payloads[349], 1f - factor);
                sink.Sample(in payloads[350], factor);
                return;
            }
            case 614:
            {
                if (tick >= 44705)
                {
                    state = 615;
                    goto case 615;
                }
                sink.Sample(in payloads[350], 1f);
                return;
            }
            case 615:
            {
                if (tick >= 44845)
                {
                    state = 616;
                    goto case 616;
                }
                sink.Sample(in payloads[351], 1f);
                return;
            }
            case 616:
            {
                if (tick >= 44846)
                {
                    state = 617;
                    goto case 617;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[351], 1f - factor);
                sink.Sample(in payloads[352], factor);
                return;
            }
            case 617:
            {
                if (tick >= 44941)
                {
                    state = 618;
                    goto case 618;
                }
                sink.Sample(in payloads[352], 1f);
                return;
            }
            case 618:
            {
                if (tick >= 44972)
                {
                    state = 619;
                    goto case 619;
                }
                return;
            }
            case 619:
            {
                if (tick >= 45096)
                {
                    state = 620;
                    goto case 620;
                }
                sink.Sample(in payloads[353], 1f);
                return;
            }
            case 620:
            {
                if (tick >= 45138)
                {
                    state = 621;
                    goto case 621;
                }
                var factor = (float)(tick - 45096) / 41f;
                sink.Sample(in payloads[353], 1f - factor);
                sink.Sample(in payloads[354], factor);
                return;
            }
            case 621:
            {
                if (tick >= 45216)
                {
                    state = 622;
                    goto case 622;
                }
                sink.Sample(in payloads[354], 1f);
                return;
            }
            case 622:
            {
                if (tick >= 45367)
                {
                    state = 623;
                    goto case 623;
                }
                sink.Sample(in payloads[355], 1f);
                return;
            }
            case 623:
            {
                if (tick >= 45368)
                {
                    state = 624;
                    goto case 624;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[355], 1f - factor);
                sink.Sample(in payloads[356], factor);
                return;
            }
            case 624:
            {
                if (tick >= 45435)
                {
                    state = 625;
                    goto case 625;
                }
                sink.Sample(in payloads[356], 1f);
                return;
            }
            case 625:
            {
                if (tick >= 45466)
                {
                    state = 626;
                    goto case 626;
                }
                return;
            }
            case 626:
            {
                if (tick >= 45622)
                {
                    state = 627;
                    goto case 627;
                }
                sink.Sample(in payloads[357], 1f);
                return;
            }
            case 627:
            {
                if (tick >= 45664)
                {
                    state = 628;
                    goto case 628;
                }
                var factor = (float)(tick - 45622) / 41f;
                sink.Sample(in payloads[357], 1f - factor);
                sink.Sample(in payloads[358], factor);
                return;
            }
            case 628:
            {
                if (tick >= 45741)
                {
                    state = 629;
                    goto case 629;
                }
                sink.Sample(in payloads[358], 1f);
                return;
            }
            case 629:
            {
                if (tick >= 45883)
                {
                    state = 630;
                    goto case 630;
                }
                sink.Sample(in payloads[359], 1f);
                return;
            }
            case 630:
            {
                if (tick >= 45884)
                {
                    state = 631;
                    goto case 631;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[359], 1f - factor);
                sink.Sample(in payloads[360], factor);
                return;
            }
            case 631:
            {
                if (tick >= 45963)
                {
                    state = 632;
                    goto case 632;
                }
                sink.Sample(in payloads[360], 1f);
                return;
            }
            case 632:
            {
                if (tick >= 45994)
                {
                    state = 633;
                    goto case 633;
                }
                return;
            }
            case 633:
            {
                if (tick >= 46110)
                {
                    state = 634;
                    goto case 634;
                }
                sink.Sample(in payloads[361], 1f);
                return;
            }
            case 634:
            {
                if (tick >= 46152)
                {
                    state = 635;
                    goto case 635;
                }
                var factor = (float)(tick - 46110) / 41f;
                sink.Sample(in payloads[361], 1f - factor);
                sink.Sample(in payloads[362], factor);
                return;
            }
            case 635:
            {
                if (tick >= 46237)
                {
                    state = 636;
                    goto case 636;
                }
                sink.Sample(in payloads[362], 1f);
                return;
            }
            case 636:
            {
                if (tick >= 46367)
                {
                    state = 637;
                    goto case 637;
                }
                sink.Sample(in payloads[363], 1f);
                return;
            }
            case 637:
            {
                if (tick >= 46368)
                {
                    state = 638;
                    goto case 638;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[363], 1f - factor);
                sink.Sample(in payloads[364], factor);
                return;
            }
            case 638:
            {
                if (tick >= 46471)
                {
                    state = 639;
                    goto case 639;
                }
                sink.Sample(in payloads[364], 1f);
                return;
            }
            case 639:
            {
                if (tick >= 46502)
                {
                    state = 640;
                    goto case 640;
                }
                return;
            }
            case 640:
            {
                if (tick >= 46625)
                {
                    state = 641;
                    goto case 641;
                }
                sink.Sample(in payloads[365], 1f);
                return;
            }
            case 641:
            {
                if (tick >= 46667)
                {
                    state = 642;
                    goto case 642;
                }
                var factor = (float)(tick - 46625) / 41f;
                sink.Sample(in payloads[365], 1f - factor);
                sink.Sample(in payloads[366], factor);
                return;
            }
            case 642:
            {
                if (tick >= 46739)
                {
                    state = 643;
                    goto case 643;
                }
                sink.Sample(in payloads[366], 1f);
                return;
            }
            case 643:
            {
                if (tick >= 46896)
                {
                    state = 644;
                    goto case 644;
                }
                sink.Sample(in payloads[367], 1f);
                return;
            }
            case 644:
            {
                if (tick >= 46897)
                {
                    state = 645;
                    goto case 645;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[367], 1f - factor);
                sink.Sample(in payloads[368], factor);
                return;
            }
            case 645:
            {
                if (tick >= 46974)
                {
                    state = 646;
                    goto case 646;
                }
                sink.Sample(in payloads[368], 1f);
                return;
            }
            case 646:
            {
                if (tick >= 47005)
                {
                    state = 647;
                    goto case 647;
                }
                return;
            }
            case 647:
            {
                if (tick >= 47143)
                {
                    state = 648;
                    goto case 648;
                }
                sink.Sample(in payloads[369], 1f);
                return;
            }
            case 648:
            {
                if (tick >= 47185)
                {
                    state = 649;
                    goto case 649;
                }
                var factor = (float)(tick - 47143) / 41f;
                sink.Sample(in payloads[369], 1f - factor);
                sink.Sample(in payloads[370], factor);
                return;
            }
            case 649:
            {
                if (tick >= 47264)
                {
                    state = 650;
                    goto case 650;
                }
                sink.Sample(in payloads[370], 1f);
                return;
            }
            case 650:
            {
                if (tick >= 47383)
                {
                    state = 651;
                    goto case 651;
                }
                sink.Sample(in payloads[371], 1f);
                return;
            }
            case 651:
            {
                if (tick >= 47384)
                {
                    state = 652;
                    goto case 652;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[371], 1f - factor);
                sink.Sample(in payloads[372], factor);
                return;
            }
            case 652:
            {
                if (tick >= 47492)
                {
                    state = 653;
                    goto case 653;
                }
                sink.Sample(in payloads[372], 1f);
                return;
            }
            case 653:
            {
                if (tick >= 47523)
                {
                    state = 654;
                    goto case 654;
                }
                return;
            }
            case 654:
            {
                if (tick >= 47662)
                {
                    state = 655;
                    goto case 655;
                }
                sink.Sample(in payloads[373], 1f);
                return;
            }
            case 655:
            {
                if (tick >= 47704)
                {
                    state = 656;
                    goto case 656;
                }
                var factor = (float)(tick - 47662) / 41f;
                sink.Sample(in payloads[373], 1f - factor);
                sink.Sample(in payloads[374], factor);
                return;
            }
            case 656:
            {
                if (tick >= 47773)
                {
                    state = 657;
                    goto case 657;
                }
                sink.Sample(in payloads[374], 1f);
                return;
            }
            case 657:
            {
                if (tick >= 47906)
                {
                    state = 658;
                    goto case 658;
                }
                sink.Sample(in payloads[375], 1f);
                return;
            }
            case 658:
            {
                if (tick >= 47907)
                {
                    state = 659;
                    goto case 659;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[375], 1f - factor);
                sink.Sample(in payloads[376], factor);
                return;
            }
            case 659:
            {
                if (tick >= 47992)
                {
                    state = 660;
                    goto case 660;
                }
                sink.Sample(in payloads[376], 1f);
                return;
            }
            case 660:
            {
                if (tick >= 48023)
                {
                    state = 661;
                    goto case 661;
                }
                return;
            }
            case 661:
            {
                if (tick >= 48155)
                {
                    state = 662;
                    goto case 662;
                }
                sink.Sample(in payloads[377], 1f);
                return;
            }
            case 662:
            {
                if (tick >= 48197)
                {
                    state = 663;
                    goto case 663;
                }
                var factor = (float)(tick - 48155) / 41f;
                sink.Sample(in payloads[377], 1f - factor);
                sink.Sample(in payloads[378], factor);
                return;
            }
            case 663:
            {
                if (tick >= 48294)
                {
                    state = 664;
                    goto case 664;
                }
                sink.Sample(in payloads[378], 1f);
                return;
            }
            case 664:
            {
                if (tick >= 48415)
                {
                    state = 665;
                    goto case 665;
                }
                sink.Sample(in payloads[379], 1f);
                return;
            }
            case 665:
            {
                if (tick >= 48416)
                {
                    state = 666;
                    goto case 666;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[379], 1f - factor);
                sink.Sample(in payloads[380], factor);
                return;
            }
            case 666:
            {
                if (tick >= 48488)
                {
                    state = 667;
                    goto case 667;
                }
                sink.Sample(in payloads[380], 1f);
                return;
            }
            case 667:
            {
                if (tick >= 48519)
                {
                    state = 668;
                    goto case 668;
                }
                return;
            }
            case 668:
            {
                if (tick >= 48656)
                {
                    state = 669;
                    goto case 669;
                }
                sink.Sample(in payloads[381], 1f);
                return;
            }
            case 669:
            {
                if (tick >= 48698)
                {
                    state = 670;
                    goto case 670;
                }
                var factor = (float)(tick - 48656) / 41f;
                sink.Sample(in payloads[381], 1f - factor);
                sink.Sample(in payloads[382], factor);
                return;
            }
            case 670:
            {
                if (tick >= 48787)
                {
                    state = 671;
                    goto case 671;
                }
                sink.Sample(in payloads[382], 1f);
                return;
            }
            case 671:
            {
                if (tick >= 48899)
                {
                    state = 672;
                    goto case 672;
                }
                sink.Sample(in payloads[383], 1f);
                return;
            }
            case 672:
            {
                if (tick >= 48900)
                {
                    state = 673;
                    goto case 673;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[383], 1f - factor);
                sink.Sample(in payloads[384], factor);
                return;
            }
            case 673:
            {
                if (tick >= 48995)
                {
                    state = 674;
                    goto case 674;
                }
                sink.Sample(in payloads[384], 1f);
                return;
            }
            case 674:
            {
                if (tick >= 49026)
                {
                    state = 675;
                    goto case 675;
                }
                return;
            }
            case 675:
            {
                if (tick >= 49190)
                {
                    state = 676;
                    goto case 676;
                }
                sink.Sample(in payloads[385], 1f);
                return;
            }
            case 676:
            {
                if (tick >= 49232)
                {
                    state = 677;
                    goto case 677;
                }
                var factor = (float)(tick - 49190) / 41f;
                sink.Sample(in payloads[385], 1f - factor);
                sink.Sample(in payloads[386], factor);
                return;
            }
            case 677:
            {
                if (tick >= 49313)
                {
                    state = 678;
                    goto case 678;
                }
                sink.Sample(in payloads[386], 1f);
                return;
            }
            case 678:
            {
                if (tick >= 49411)
                {
                    state = 679;
                    goto case 679;
                }
                sink.Sample(in payloads[387], 1f);
                return;
            }
            case 679:
            {
                if (tick >= 49412)
                {
                    state = 680;
                    goto case 680;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[387], 1f - factor);
                sink.Sample(in payloads[388], factor);
                return;
            }
            case 680:
            {
                if (tick >= 49518)
                {
                    state = 681;
                    goto case 681;
                }
                sink.Sample(in payloads[388], 1f);
                return;
            }
            case 681:
            {
                if (tick >= 49549)
                {
                    state = 682;
                    goto case 682;
                }
                return;
            }
            case 682:
            {
                if (tick >= 49677)
                {
                    state = 683;
                    goto case 683;
                }
                sink.Sample(in payloads[389], 1f);
                return;
            }
            case 683:
            {
                if (tick >= 49719)
                {
                    state = 684;
                    goto case 684;
                }
                var factor = (float)(tick - 49677) / 41f;
                sink.Sample(in payloads[389], 1f - factor);
                sink.Sample(in payloads[390], factor);
                return;
            }
            case 684:
            {
                if (tick >= 49818)
                {
                    state = 685;
                    goto case 685;
                }
                sink.Sample(in payloads[390], 1f);
                return;
            }
            case 685:
            {
                if (tick >= 49912)
                {
                    state = 686;
                    goto case 686;
                }
                sink.Sample(in payloads[391], 1f);
                return;
            }
            case 686:
            {
                if (tick >= 49913)
                {
                    state = 687;
                    goto case 687;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[391], 1f - factor);
                sink.Sample(in payloads[392], factor);
                return;
            }
            case 687:
            {
                if (tick >= 50010)
                {
                    state = 688;
                    goto case 688;
                }
                sink.Sample(in payloads[392], 1f);
                return;
            }
            case 688:
            {
                if (tick >= 50041)
                {
                    state = 689;
                    goto case 689;
                }
                return;
            }
            case 689:
            {
                if (tick >= 50182)
                {
                    state = 690;
                    goto case 690;
                }
                sink.Sample(in payloads[393], 1f);
                return;
            }
            case 690:
            {
                if (tick >= 50224)
                {
                    state = 691;
                    goto case 691;
                }
                var factor = (float)(tick - 50182) / 41f;
                sink.Sample(in payloads[393], 1f - factor);
                sink.Sample(in payloads[394], factor);
                return;
            }
            case 691:
            {
                if (tick >= 50316)
                {
                    state = 692;
                    goto case 692;
                }
                sink.Sample(in payloads[394], 1f);
                return;
            }
            case 692:
            {
                if (tick >= 50433)
                {
                    state = 693;
                    goto case 693;
                }
                sink.Sample(in payloads[395], 1f);
                return;
            }
            case 693:
            {
                if (tick >= 50434)
                {
                    state = 694;
                    goto case 694;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[395], 1f - factor);
                sink.Sample(in payloads[396], factor);
                return;
            }
            case 694:
            {
                if (tick >= 50551)
                {
                    state = 695;
                    goto case 695;
                }
                sink.Sample(in payloads[396], 1f);
                return;
            }
            case 695:
            {
                if (tick >= 50582)
                {
                    state = 696;
                    goto case 696;
                }
                return;
            }
            case 696:
            {
                if (tick >= 50702)
                {
                    state = 697;
                    goto case 697;
                }
                sink.Sample(in payloads[397], 1f);
                return;
            }
            case 697:
            {
                if (tick >= 50744)
                {
                    state = 698;
                    goto case 698;
                }
                var factor = (float)(tick - 50702) / 41f;
                sink.Sample(in payloads[397], 1f - factor);
                sink.Sample(in payloads[398], factor);
                return;
            }
            case 698:
            {
                if (tick >= 50815)
                {
                    state = 699;
                    goto case 699;
                }
                sink.Sample(in payloads[398], 1f);
                return;
            }
            case 699:
            {
                if (tick >= 50945)
                {
                    state = 700;
                    goto case 700;
                }
                sink.Sample(in payloads[399], 1f);
                return;
            }
            case 700:
            {
                if (tick >= 50946)
                {
                    state = 701;
                    goto case 701;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[399], 1f - factor);
                sink.Sample(in payloads[400], factor);
                return;
            }
            case 701:
            {
                if (tick >= 51041)
                {
                    state = 702;
                    goto case 702;
                }
                sink.Sample(in payloads[400], 1f);
                return;
            }
            case 702:
            {
                if (tick >= 51072)
                {
                    state = 703;
                    goto case 703;
                }
                return;
            }
            case 703:
            {
                if (tick >= 51222)
                {
                    state = 704;
                    goto case 704;
                }
                sink.Sample(in payloads[401], 1f);
                return;
            }
            case 704:
            {
                if (tick >= 51264)
                {
                    state = 705;
                    goto case 705;
                }
                var factor = (float)(tick - 51222) / 41f;
                sink.Sample(in payloads[401], 1f - factor);
                sink.Sample(in payloads[402], factor);
                return;
            }
            case 705:
            {
                if (tick >= 51310)
                {
                    state = 706;
                    goto case 706;
                }
                sink.Sample(in payloads[402], 1f);
                return;
            }
            case 706:
            {
                if (tick >= 51459)
                {
                    state = 707;
                    goto case 707;
                }
                sink.Sample(in payloads[403], 1f);
                return;
            }
            case 707:
            {
                if (tick >= 51460)
                {
                    state = 708;
                    goto case 708;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[403], 1f - factor);
                sink.Sample(in payloads[404], factor);
                return;
            }
            case 708:
            {
                if (tick >= 51557)
                {
                    state = 709;
                    goto case 709;
                }
                sink.Sample(in payloads[404], 1f);
                return;
            }
            case 709:
            {
                if (tick >= 51588)
                {
                    state = 710;
                    goto case 710;
                }
                return;
            }
            case 710:
            {
                if (tick >= 51721)
                {
                    state = 711;
                    goto case 711;
                }
                sink.Sample(in payloads[405], 1f);
                return;
            }
            case 711:
            {
                if (tick >= 51763)
                {
                    state = 712;
                    goto case 712;
                }
                var factor = (float)(tick - 51721) / 41f;
                sink.Sample(in payloads[405], 1f - factor);
                sink.Sample(in payloads[406], factor);
                return;
            }
            case 712:
            {
                if (tick >= 51822)
                {
                    state = 713;
                    goto case 713;
                }
                sink.Sample(in payloads[406], 1f);
                return;
            }
            case 713:
            {
                if (tick >= 51959)
                {
                    state = 714;
                    goto case 714;
                }
                sink.Sample(in payloads[407], 1f);
                return;
            }
            case 714:
            {
                if (tick >= 51960)
                {
                    state = 715;
                    goto case 715;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[407], 1f - factor);
                sink.Sample(in payloads[408], factor);
                return;
            }
            case 715:
            {
                if (tick >= 52047)
                {
                    state = 716;
                    goto case 716;
                }
                sink.Sample(in payloads[408], 1f);
                return;
            }
            case 716:
            {
                if (tick >= 52078)
                {
                    state = 717;
                    goto case 717;
                }
                return;
            }
            case 717:
            {
                if (tick >= 52238)
                {
                    state = 718;
                    goto case 718;
                }
                sink.Sample(in payloads[409], 1f);
                return;
            }
            case 718:
            {
                if (tick >= 52280)
                {
                    state = 719;
                    goto case 719;
                }
                var factor = (float)(tick - 52238) / 41f;
                sink.Sample(in payloads[409], 1f - factor);
                sink.Sample(in payloads[410], factor);
                return;
            }
            case 719:
            {
                if (tick >= 52358)
                {
                    state = 720;
                    goto case 720;
                }
                sink.Sample(in payloads[410], 1f);
                return;
            }
            case 720:
            {
                if (tick >= 52478)
                {
                    state = 721;
                    goto case 721;
                }
                sink.Sample(in payloads[411], 1f);
                return;
            }
            case 721:
            {
                if (tick >= 52479)
                {
                    state = 722;
                    goto case 722;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[411], 1f - factor);
                sink.Sample(in payloads[412], factor);
                return;
            }
            case 722:
            {
                if (tick >= 52568)
                {
                    state = 723;
                    goto case 723;
                }
                sink.Sample(in payloads[412], 1f);
                return;
            }
            case 723:
            {
                if (tick >= 52599)
                {
                    state = 724;
                    goto case 724;
                }
                return;
            }
            case 724:
            {
                if (tick >= 52736)
                {
                    state = 725;
                    goto case 725;
                }
                sink.Sample(in payloads[413], 1f);
                return;
            }
            case 725:
            {
                if (tick >= 52778)
                {
                    state = 726;
                    goto case 726;
                }
                var factor = (float)(tick - 52736) / 41f;
                sink.Sample(in payloads[413], 1f - factor);
                sink.Sample(in payloads[414], factor);
                return;
            }
            case 726:
            {
                if (tick >= 52837)
                {
                    state = 727;
                    goto case 727;
                }
                sink.Sample(in payloads[414], 1f);
                return;
            }
            case 727:
            {
                if (tick >= 52969)
                {
                    state = 728;
                    goto case 728;
                }
                sink.Sample(in payloads[415], 1f);
                return;
            }
            case 728:
            {
                if (tick >= 52970)
                {
                    state = 729;
                    goto case 729;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[415], 1f - factor);
                sink.Sample(in payloads[416], factor);
                return;
            }
            case 729:
            {
                if (tick >= 53068)
                {
                    state = 730;
                    goto case 730;
                }
                sink.Sample(in payloads[416], 1f);
                return;
            }
            case 730:
            {
                if (tick >= 53099)
                {
                    state = 731;
                    goto case 731;
                }
                return;
            }
            case 731:
            {
                if (tick >= 53251)
                {
                    state = 732;
                    goto case 732;
                }
                sink.Sample(in payloads[417], 1f);
                return;
            }
            case 732:
            {
                if (tick >= 53293)
                {
                    state = 733;
                    goto case 733;
                }
                var factor = (float)(tick - 53251) / 41f;
                sink.Sample(in payloads[417], 1f - factor);
                sink.Sample(in payloads[418], factor);
                return;
            }
            case 733:
            {
                if (tick >= 53351)
                {
                    state = 734;
                    goto case 734;
                }
                sink.Sample(in payloads[418], 1f);
                return;
            }
            case 734:
            {
                if (tick >= 53497)
                {
                    state = 735;
                    goto case 735;
                }
                sink.Sample(in payloads[419], 1f);
                return;
            }
            case 735:
            {
                if (tick >= 53498)
                {
                    state = 736;
                    goto case 736;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[419], 1f - factor);
                sink.Sample(in payloads[420], factor);
                return;
            }
            case 736:
            {
                if (tick >= 53590)
                {
                    state = 737;
                    goto case 737;
                }
                sink.Sample(in payloads[420], 1f);
                return;
            }
            case 737:
            {
                if (tick >= 53621)
                {
                    state = 738;
                    goto case 738;
                }
                return;
            }
            case 738:
            {
                if (tick >= 53727)
                {
                    state = 739;
                    goto case 739;
                }
                sink.Sample(in payloads[421], 1f);
                return;
            }
            case 739:
            {
                if (tick >= 53769)
                {
                    state = 740;
                    goto case 740;
                }
                var factor = (float)(tick - 53727) / 41f;
                sink.Sample(in payloads[421], 1f - factor);
                sink.Sample(in payloads[422], factor);
                return;
            }
            case 740:
            {
                if (tick >= 53854)
                {
                    state = 741;
                    goto case 741;
                }
                sink.Sample(in payloads[422], 1f);
                return;
            }
            case 741:
            {
                if (tick >= 53994)
                {
                    state = 742;
                    goto case 742;
                }
                sink.Sample(in payloads[423], 1f);
                return;
            }
            case 742:
            {
                if (tick >= 53995)
                {
                    state = 743;
                    goto case 743;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[423], 1f - factor);
                sink.Sample(in payloads[424], factor);
                return;
            }
            case 743:
            {
                if (tick >= 54110)
                {
                    state = 744;
                    goto case 744;
                }
                sink.Sample(in payloads[424], 1f);
                return;
            }
            case 744:
            {
                if (tick >= 54141)
                {
                    state = 745;
                    goto case 745;
                }
                return;
            }
            case 745:
            {
                if (tick >= 54240)
                {
                    state = 746;
                    goto case 746;
                }
                sink.Sample(in payloads[425], 1f);
                return;
            }
            case 746:
            {
                if (tick >= 54282)
                {
                    state = 747;
                    goto case 747;
                }
                var factor = (float)(tick - 54240) / 41f;
                sink.Sample(in payloads[425], 1f - factor);
                sink.Sample(in payloads[426], factor);
                return;
            }
            case 747:
            {
                if (tick >= 54363)
                {
                    state = 748;
                    goto case 748;
                }
                sink.Sample(in payloads[426], 1f);
                return;
            }
            case 748:
            {
                if (tick >= 54497)
                {
                    state = 749;
                    goto case 749;
                }
                sink.Sample(in payloads[427], 1f);
                return;
            }
            case 749:
            {
                if (tick >= 54498)
                {
                    state = 750;
                    goto case 750;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[427], 1f - factor);
                sink.Sample(in payloads[428], factor);
                return;
            }
            case 750:
            {
                if (tick >= 54583)
                {
                    state = 751;
                    goto case 751;
                }
                sink.Sample(in payloads[428], 1f);
                return;
            }
            case 751:
            {
                if (tick >= 54614)
                {
                    state = 752;
                    goto case 752;
                }
                return;
            }
            case 752:
            {
                if (tick >= 54777)
                {
                    state = 753;
                    goto case 753;
                }
                sink.Sample(in payloads[429], 1f);
                return;
            }
            case 753:
            {
                if (tick >= 54819)
                {
                    state = 754;
                    goto case 754;
                }
                var factor = (float)(tick - 54777) / 41f;
                sink.Sample(in payloads[429], 1f - factor);
                sink.Sample(in payloads[430], factor);
                return;
            }
            case 754:
            {
                if (tick >= 54871)
                {
                    state = 755;
                    goto case 755;
                }
                sink.Sample(in payloads[430], 1f);
                return;
            }
            case 755:
            {
                if (tick >= 55028)
                {
                    state = 756;
                    goto case 756;
                }
                sink.Sample(in payloads[431], 1f);
                return;
            }
            case 756:
            {
                if (tick >= 55029)
                {
                    state = 757;
                    goto case 757;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[431], 1f - factor);
                sink.Sample(in payloads[432], factor);
                return;
            }
            case 757:
            {
                if (tick >= 55089)
                {
                    state = 758;
                    goto case 758;
                }
                sink.Sample(in payloads[432], 1f);
                return;
            }
            case 758:
            {
                if (tick >= 55120)
                {
                    state = 759;
                    goto case 759;
                }
                return;
            }
            case 759:
            {
                if (tick >= 55249)
                {
                    state = 760;
                    goto case 760;
                }
                sink.Sample(in payloads[433], 1f);
                return;
            }
            case 760:
            {
                if (tick >= 55291)
                {
                    state = 761;
                    goto case 761;
                }
                var factor = (float)(tick - 55249) / 41f;
                sink.Sample(in payloads[433], 1f - factor);
                sink.Sample(in payloads[434], factor);
                return;
            }
            case 761:
            {
                if (tick >= 55396)
                {
                    state = 762;
                    goto case 762;
                }
                sink.Sample(in payloads[434], 1f);
                return;
            }
            case 762:
            {
                if (tick >= 55535)
                {
                    state = 763;
                    goto case 763;
                }
                sink.Sample(in payloads[435], 1f);
                return;
            }
            case 763:
            {
                if (tick >= 55536)
                {
                    state = 764;
                    goto case 764;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[435], 1f - factor);
                sink.Sample(in payloads[436], factor);
                return;
            }
            case 764:
            {
                if (tick >= 55630)
                {
                    state = 765;
                    goto case 765;
                }
                sink.Sample(in payloads[436], 1f);
                return;
            }
            case 765:
            {
                if (tick >= 55661)
                {
                    state = 766;
                    goto case 766;
                }
                return;
            }
            case 766:
            {
                if (tick >= 55762)
                {
                    state = 767;
                    goto case 767;
                }
                sink.Sample(in payloads[437], 1f);
                return;
            }
            case 767:
            {
                if (tick >= 55804)
                {
                    state = 768;
                    goto case 768;
                }
                var factor = (float)(tick - 55762) / 41f;
                sink.Sample(in payloads[437], 1f - factor);
                sink.Sample(in payloads[438], factor);
                return;
            }
            case 768:
            {
                if (tick >= 55900)
                {
                    state = 769;
                    goto case 769;
                }
                sink.Sample(in payloads[438], 1f);
                return;
            }
            case 769:
            {
                if (tick >= 56032)
                {
                    state = 770;
                    goto case 770;
                }
                sink.Sample(in payloads[439], 1f);
                return;
            }
            case 770:
            {
                if (tick >= 56033)
                {
                    state = 771;
                    goto case 771;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[439], 1f - factor);
                sink.Sample(in payloads[440], factor);
                return;
            }
            case 771:
            {
                if (tick >= 56111)
                {
                    state = 772;
                    goto case 772;
                }
                sink.Sample(in payloads[440], 1f);
                return;
            }
            case 772:
            {
                if (tick >= 56142)
                {
                    state = 773;
                    goto case 773;
                }
                return;
            }
            case 773:
            {
                if (tick >= 56275)
                {
                    state = 774;
                    goto case 774;
                }
                sink.Sample(in payloads[441], 1f);
                return;
            }
            case 774:
            {
                if (tick >= 56317)
                {
                    state = 775;
                    goto case 775;
                }
                var factor = (float)(tick - 56275) / 41f;
                sink.Sample(in payloads[441], 1f - factor);
                sink.Sample(in payloads[442], factor);
                return;
            }
            case 775:
            {
                if (tick >= 56415)
                {
                    state = 776;
                    goto case 776;
                }
                sink.Sample(in payloads[442], 1f);
                return;
            }
            case 776:
            {
                if (tick >= 56547)
                {
                    state = 777;
                    goto case 777;
                }
                sink.Sample(in payloads[443], 1f);
                return;
            }
            case 777:
            {
                if (tick >= 56548)
                {
                    state = 778;
                    goto case 778;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[443], 1f - factor);
                sink.Sample(in payloads[444], factor);
                return;
            }
            case 778:
            {
                if (tick >= 56646)
                {
                    state = 779;
                    goto case 779;
                }
                sink.Sample(in payloads[444], 1f);
                return;
            }
            case 779:
            {
                if (tick >= 56677)
                {
                    state = 780;
                    goto case 780;
                }
                return;
            }
            case 780:
            {
                if (tick >= 56794)
                {
                    state = 781;
                    goto case 781;
                }
                sink.Sample(in payloads[445], 1f);
                return;
            }
            case 781:
            {
                if (tick >= 56836)
                {
                    state = 782;
                    goto case 782;
                }
                var factor = (float)(tick - 56794) / 41f;
                sink.Sample(in payloads[445], 1f - factor);
                sink.Sample(in payloads[446], factor);
                return;
            }
            case 782:
            {
                if (tick >= 56918)
                {
                    state = 783;
                    goto case 783;
                }
                sink.Sample(in payloads[446], 1f);
                return;
            }
            case 783:
            {
                if (tick >= 57026)
                {
                    state = 784;
                    goto case 784;
                }
                sink.Sample(in payloads[447], 1f);
                return;
            }
            case 784:
            {
                if (tick >= 57027)
                {
                    state = 785;
                    goto case 785;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[447], 1f - factor);
                sink.Sample(in payloads[448], factor);
                return;
            }
            case 785:
            {
                if (tick >= 57129)
                {
                    state = 786;
                    goto case 786;
                }
                sink.Sample(in payloads[448], 1f);
                return;
            }
            case 786:
            {
                if (tick >= 57160)
                {
                    state = 787;
                    goto case 787;
                }
                return;
            }
            case 787:
            {
                if (tick >= 57286)
                {
                    state = 788;
                    goto case 788;
                }
                sink.Sample(in payloads[449], 1f);
                return;
            }
            case 788:
            {
                if (tick >= 57328)
                {
                    state = 789;
                    goto case 789;
                }
                var factor = (float)(tick - 57286) / 41f;
                sink.Sample(in payloads[449], 1f - factor);
                sink.Sample(in payloads[450], factor);
                return;
            }
            case 789:
            {
                if (tick >= 57429)
                {
                    state = 790;
                    goto case 790;
                }
                sink.Sample(in payloads[450], 1f);
                return;
            }
            case 790:
            {
                if (tick >= 57559)
                {
                    state = 791;
                    goto case 791;
                }
                sink.Sample(in payloads[451], 1f);
                return;
            }
            case 791:
            {
                if (tick >= 57560)
                {
                    state = 792;
                    goto case 792;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[451], 1f - factor);
                sink.Sample(in payloads[452], factor);
                return;
            }
            case 792:
            {
                if (tick >= 57633)
                {
                    state = 793;
                    goto case 793;
                }
                sink.Sample(in payloads[452], 1f);
                return;
            }
            case 793:
            {
                if (tick >= 57664)
                {
                    state = 794;
                    goto case 794;
                }
                return;
            }
            case 794:
            {
                if (tick >= 57807)
                {
                    state = 795;
                    goto case 795;
                }
                sink.Sample(in payloads[453], 1f);
                return;
            }
            case 795:
            {
                if (tick >= 57849)
                {
                    state = 796;
                    goto case 796;
                }
                var factor = (float)(tick - 57807) / 41f;
                sink.Sample(in payloads[453], 1f - factor);
                sink.Sample(in payloads[454], factor);
                return;
            }
            case 796:
            {
                if (tick >= 57923)
                {
                    state = 797;
                    goto case 797;
                }
                sink.Sample(in payloads[454], 1f);
                return;
            }
            case 797:
            {
                if (tick >= 58071)
                {
                    state = 798;
                    goto case 798;
                }
                sink.Sample(in payloads[455], 1f);
                return;
            }
            case 798:
            {
                if (tick >= 58072)
                {
                    state = 799;
                    goto case 799;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[455], 1f - factor);
                sink.Sample(in payloads[456], factor);
                return;
            }
            case 799:
            {
                if (tick >= 58143)
                {
                    state = 800;
                    goto case 800;
                }
                sink.Sample(in payloads[456], 1f);
                return;
            }
            case 800:
            {
                if (tick >= 58174)
                {
                    state = 801;
                    goto case 801;
                }
                return;
            }
            case 801:
            {
                if (tick >= 58331)
                {
                    state = 802;
                    goto case 802;
                }
                sink.Sample(in payloads[457], 1f);
                return;
            }
            case 802:
            {
                if (tick >= 58373)
                {
                    state = 803;
                    goto case 803;
                }
                var factor = (float)(tick - 58331) / 41f;
                sink.Sample(in payloads[457], 1f - factor);
                sink.Sample(in payloads[458], factor);
                return;
            }
            case 803:
            {
                if (tick >= 58444)
                {
                    state = 804;
                    goto case 804;
                }
                sink.Sample(in payloads[458], 1f);
                return;
            }
            case 804:
            {
                if (tick >= 58576)
                {
                    state = 805;
                    goto case 805;
                }
                sink.Sample(in payloads[459], 1f);
                return;
            }
            case 805:
            {
                if (tick >= 58577)
                {
                    state = 806;
                    goto case 806;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[459], 1f - factor);
                sink.Sample(in payloads[460], factor);
                return;
            }
            case 806:
            {
                if (tick >= 58650)
                {
                    state = 807;
                    goto case 807;
                }
                sink.Sample(in payloads[460], 1f);
                return;
            }
            case 807:
            {
                if (tick >= 58681)
                {
                    state = 808;
                    goto case 808;
                }
                return;
            }
            case 808:
            {
                if (tick >= 58831)
                {
                    state = 809;
                    goto case 809;
                }
                sink.Sample(in payloads[461], 1f);
                return;
            }
            case 809:
            {
                if (tick >= 58873)
                {
                    state = 810;
                    goto case 810;
                }
                var factor = (float)(tick - 58831) / 41f;
                sink.Sample(in payloads[461], 1f - factor);
                sink.Sample(in payloads[462], factor);
                return;
            }
            case 810:
            {
                if (tick >= 58966)
                {
                    state = 811;
                    goto case 811;
                }
                sink.Sample(in payloads[462], 1f);
                return;
            }
            case 811:
            {
                if (tick >= 59064)
                {
                    state = 812;
                    goto case 812;
                }
                sink.Sample(in payloads[463], 1f);
                return;
            }
            case 812:
            {
                if (tick >= 59065)
                {
                    state = 813;
                    goto case 813;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[463], 1f - factor);
                sink.Sample(in payloads[464], factor);
                return;
            }
            case 813:
            {
                if (tick >= 59156)
                {
                    state = 814;
                    goto case 814;
                }
                sink.Sample(in payloads[464], 1f);
                return;
            }
            case 814:
            {
                if (tick >= 59187)
                {
                    state = 815;
                    goto case 815;
                }
                return;
            }
            case 815:
            {
                if (tick >= 59316)
                {
                    state = 816;
                    goto case 816;
                }
                sink.Sample(in payloads[465], 1f);
                return;
            }
            case 816:
            {
                if (tick >= 59358)
                {
                    state = 817;
                    goto case 817;
                }
                var factor = (float)(tick - 59316) / 41f;
                sink.Sample(in payloads[465], 1f - factor);
                sink.Sample(in payloads[466], factor);
                return;
            }
            case 817:
            {
                if (tick >= 59468)
                {
                    state = 818;
                    goto case 818;
                }
                sink.Sample(in payloads[466], 1f);
                return;
            }
            case 818:
            {
                if (tick >= 59591)
                {
                    state = 819;
                    goto case 819;
                }
                sink.Sample(in payloads[467], 1f);
                return;
            }
            case 819:
            {
                if (tick >= 59592)
                {
                    state = 820;
                    goto case 820;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[467], 1f - factor);
                sink.Sample(in payloads[468], factor);
                return;
            }
            case 820:
            {
                if (tick >= 59688)
                {
                    state = 821;
                    goto case 821;
                }
                sink.Sample(in payloads[468], 1f);
                return;
            }
            case 821:
            {
                if (tick >= 59719)
                {
                    state = 822;
                    goto case 822;
                }
                return;
            }
            case 822:
            {
                if (tick >= 59825)
                {
                    state = 823;
                    goto case 823;
                }
                sink.Sample(in payloads[469], 1f);
                return;
            }
            case 823:
            {
                if (tick >= 59867)
                {
                    state = 824;
                    goto case 824;
                }
                var factor = (float)(tick - 59825) / 41f;
                sink.Sample(in payloads[469], 1f - factor);
                sink.Sample(in payloads[470], factor);
                return;
            }
            case 824:
            {
                if (tick >= 59947)
                {
                    state = 825;
                    goto case 825;
                }
                sink.Sample(in payloads[470], 1f);
                return;
            }
            case 825:
            {
                if (tick >= 60102)
                {
                    state = 826;
                    goto case 826;
                }
                sink.Sample(in payloads[471], 1f);
                return;
            }
            case 826:
            {
                if (tick >= 60103)
                {
                    state = 827;
                    goto case 827;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[471], 1f - factor);
                sink.Sample(in payloads[472], factor);
                return;
            }
            case 827:
            {
                if (tick >= 60191)
                {
                    state = 828;
                    goto case 828;
                }
                sink.Sample(in payloads[472], 1f);
                return;
            }
            case 828:
            {
                if (tick >= 60222)
                {
                    state = 829;
                    goto case 829;
                }
                return;
            }
            case 829:
            {
                if (tick >= 60357)
                {
                    state = 830;
                    goto case 830;
                }
                sink.Sample(in payloads[473], 1f);
                return;
            }
            case 830:
            {
                if (tick >= 60399)
                {
                    state = 831;
                    goto case 831;
                }
                var factor = (float)(tick - 60357) / 41f;
                sink.Sample(in payloads[473], 1f - factor);
                sink.Sample(in payloads[474], factor);
                return;
            }
            case 831:
            {
                if (tick >= 60470)
                {
                    state = 832;
                    goto case 832;
                }
                sink.Sample(in payloads[474], 1f);
                return;
            }
            case 832:
            {
                if (tick >= 60599)
                {
                    state = 833;
                    goto case 833;
                }
                sink.Sample(in payloads[475], 1f);
                return;
            }
            case 833:
            {
                if (tick >= 60600)
                {
                    state = 834;
                    goto case 834;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[475], 1f - factor);
                sink.Sample(in payloads[476], factor);
                return;
            }
            case 834:
            {
                if (tick >= 60703)
                {
                    state = 835;
                    goto case 835;
                }
                sink.Sample(in payloads[476], 1f);
                return;
            }
            case 835:
            {
                if (tick >= 60734)
                {
                    state = 836;
                    goto case 836;
                }
                return;
            }
            case 836:
            {
                if (tick >= 60874)
                {
                    state = 837;
                    goto case 837;
                }
                sink.Sample(in payloads[477], 1f);
                return;
            }
            case 837:
            {
                if (tick >= 60916)
                {
                    state = 838;
                    goto case 838;
                }
                var factor = (float)(tick - 60874) / 41f;
                sink.Sample(in payloads[477], 1f - factor);
                sink.Sample(in payloads[478], factor);
                return;
            }
            case 838:
            {
                if (tick >= 60972)
                {
                    state = 839;
                    goto case 839;
                }
                sink.Sample(in payloads[478], 1f);
                return;
            }
            case 839:
            {
                if (tick >= 61127)
                {
                    state = 840;
                    goto case 840;
                }
                sink.Sample(in payloads[479], 1f);
                return;
            }
            case 840:
            {
                if (tick >= 61128)
                {
                    state = 841;
                    goto case 841;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[479], 1f - factor);
                sink.Sample(in payloads[480], factor);
                return;
            }
            case 841:
            {
                if (tick >= 61203)
                {
                    state = 842;
                    goto case 842;
                }
                sink.Sample(in payloads[480], 1f);
                return;
            }
            case 842:
            {
                if (tick >= 61234)
                {
                    state = 843;
                    goto case 843;
                }
                return;
            }
            case 843:
            {
                if (tick >= 61374)
                {
                    state = 844;
                    goto case 844;
                }
                sink.Sample(in payloads[481], 1f);
                return;
            }
            case 844:
            {
                if (tick >= 61416)
                {
                    state = 845;
                    goto case 845;
                }
                var factor = (float)(tick - 61374) / 41f;
                sink.Sample(in payloads[481], 1f - factor);
                sink.Sample(in payloads[482], factor);
                return;
            }
            case 845:
            {
                if (tick >= 61473)
                {
                    state = 846;
                    goto case 846;
                }
                sink.Sample(in payloads[482], 1f);
                return;
            }
            case 846:
            {
                if (tick >= 61630)
                {
                    state = 847;
                    goto case 847;
                }
                sink.Sample(in payloads[483], 1f);
                return;
            }
            case 847:
            {
                if (tick >= 61631)
                {
                    state = 848;
                    goto case 848;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[483], 1f - factor);
                sink.Sample(in payloads[484], factor);
                return;
            }
            case 848:
            {
                if (tick >= 61691)
                {
                    state = 849;
                    goto case 849;
                }
                sink.Sample(in payloads[484], 1f);
                return;
            }
            case 849:
            {
                if (tick >= 61722)
                {
                    state = 850;
                    goto case 850;
                }
                return;
            }
            case 850:
            {
                if (tick >= 61863)
                {
                    state = 851;
                    goto case 851;
                }
                sink.Sample(in payloads[485], 1f);
                return;
            }
            case 851:
            {
                if (tick >= 61905)
                {
                    state = 852;
                    goto case 852;
                }
                var factor = (float)(tick - 61863) / 41f;
                sink.Sample(in payloads[485], 1f - factor);
                sink.Sample(in payloads[486], factor);
                return;
            }
            case 852:
            {
                if (tick >= 61981)
                {
                    state = 853;
                    goto case 853;
                }
                sink.Sample(in payloads[486], 1f);
                return;
            }
            case 853:
            {
                if (tick >= 62139)
                {
                    state = 854;
                    goto case 854;
                }
                sink.Sample(in payloads[487], 1f);
                return;
            }
            case 854:
            {
                if (tick >= 62140)
                {
                    state = 855;
                    goto case 855;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[487], 1f - factor);
                sink.Sample(in payloads[488], factor);
                return;
            }
            case 855:
            {
                if (tick >= 62227)
                {
                    state = 856;
                    goto case 856;
                }
                sink.Sample(in payloads[488], 1f);
                return;
            }
            case 856:
            {
                if (tick >= 62258)
                {
                    state = 857;
                    goto case 857;
                }
                return;
            }
            case 857:
            {
                if (tick >= 62370)
                {
                    state = 858;
                    goto case 858;
                }
                sink.Sample(in payloads[489], 1f);
                return;
            }
            case 858:
            {
                if (tick >= 62412)
                {
                    state = 859;
                    goto case 859;
                }
                var factor = (float)(tick - 62370) / 41f;
                sink.Sample(in payloads[489], 1f - factor);
                sink.Sample(in payloads[490], factor);
                return;
            }
            case 859:
            {
                if (tick >= 62524)
                {
                    state = 860;
                    goto case 860;
                }
                sink.Sample(in payloads[490], 1f);
                return;
            }
            case 860:
            {
                if (tick >= 62611)
                {
                    state = 861;
                    goto case 861;
                }
                sink.Sample(in payloads[491], 1f);
                return;
            }
            case 861:
            {
                if (tick >= 62612)
                {
                    state = 862;
                    goto case 862;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[491], 1f - factor);
                sink.Sample(in payloads[492], factor);
                return;
            }
            case 862:
            {
                if (tick >= 62730)
                {
                    state = 863;
                    goto case 863;
                }
                sink.Sample(in payloads[492], 1f);
                return;
            }
            case 863:
            {
                if (tick >= 62761)
                {
                    state = 864;
                    goto case 864;
                }
                return;
            }
            case 864:
            {
                if (tick >= 62904)
                {
                    state = 865;
                    goto case 865;
                }
                sink.Sample(in payloads[493], 1f);
                return;
            }
            case 865:
            {
                if (tick >= 62946)
                {
                    state = 866;
                    goto case 866;
                }
                var factor = (float)(tick - 62904) / 41f;
                sink.Sample(in payloads[493], 1f - factor);
                sink.Sample(in payloads[494], factor);
                return;
            }
            case 866:
            {
                if (tick >= 63030)
                {
                    state = 867;
                    goto case 867;
                }
                sink.Sample(in payloads[494], 1f);
                return;
            }
            case 867:
            {
                if (tick >= 63122)
                {
                    state = 868;
                    goto case 868;
                }
                sink.Sample(in payloads[495], 1f);
                return;
            }
            case 868:
            {
                if (tick >= 63123)
                {
                    state = 869;
                    goto case 869;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[495], 1f - factor);
                sink.Sample(in payloads[496], factor);
                return;
            }
            case 869:
            {
                if (tick >= 63220)
                {
                    state = 870;
                    goto case 870;
                }
                sink.Sample(in payloads[496], 1f);
                return;
            }
            case 870:
            {
                if (tick >= 63251)
                {
                    state = 871;
                    goto case 871;
                }
                return;
            }
            case 871:
            {
                if (tick >= 63394)
                {
                    state = 872;
                    goto case 872;
                }
                sink.Sample(in payloads[497], 1f);
                return;
            }
            case 872:
            {
                if (tick >= 63436)
                {
                    state = 873;
                    goto case 873;
                }
                var factor = (float)(tick - 63394) / 41f;
                sink.Sample(in payloads[497], 1f - factor);
                sink.Sample(in payloads[498], factor);
                return;
            }
            case 873:
            {
                if (tick >= 63509)
                {
                    state = 874;
                    goto case 874;
                }
                sink.Sample(in payloads[498], 1f);
                return;
            }
            case 874:
            {
                if (tick >= 63658)
                {
                    state = 875;
                    goto case 875;
                }
                sink.Sample(in payloads[499], 1f);
                return;
            }
            case 875:
            {
                if (tick >= 63659)
                {
                    state = 876;
                    goto case 876;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[499], 1f - factor);
                sink.Sample(in payloads[500], factor);
                return;
            }
            case 876:
            {
                if (tick >= 63739)
                {
                    state = 877;
                    goto case 877;
                }
                sink.Sample(in payloads[500], 1f);
                return;
            }
            case 877:
            {
                if (tick >= 63770)
                {
                    state = 878;
                    goto case 878;
                }
                return;
            }
            case 878:
            {
                if (tick >= 63888)
                {
                    state = 879;
                    goto case 879;
                }
                sink.Sample(in payloads[501], 1f);
                return;
            }
            case 879:
            {
                if (tick >= 63930)
                {
                    state = 880;
                    goto case 880;
                }
                var factor = (float)(tick - 63888) / 41f;
                sink.Sample(in payloads[501], 1f - factor);
                sink.Sample(in payloads[502], factor);
                return;
            }
            case 880:
            {
                if (tick >= 64024)
                {
                    state = 881;
                    goto case 881;
                }
                sink.Sample(in payloads[502], 1f);
                return;
            }
            case 881:
            {
                if (tick >= 64176)
                {
                    state = 882;
                    goto case 882;
                }
                sink.Sample(in payloads[503], 1f);
                return;
            }
            case 882:
            {
                if (tick >= 64177)
                {
                    state = 883;
                    goto case 883;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[503], 1f - factor);
                sink.Sample(in payloads[504], factor);
                return;
            }
            case 883:
            {
                if (tick >= 64250)
                {
                    state = 884;
                    goto case 884;
                }
                sink.Sample(in payloads[504], 1f);
                return;
            }
            case 884:
            {
                if (tick >= 64281)
                {
                    state = 885;
                    goto case 885;
                }
                return;
            }
            case 885:
            {
                if (tick >= 64402)
                {
                    state = 886;
                    goto case 886;
                }
                sink.Sample(in payloads[505], 1f);
                return;
            }
            case 886:
            {
                if (tick >= 64444)
                {
                    state = 887;
                    goto case 887;
                }
                var factor = (float)(tick - 64402) / 41f;
                sink.Sample(in payloads[505], 1f - factor);
                sink.Sample(in payloads[506], factor);
                return;
            }
            case 887:
            {
                if (tick >= 64529)
                {
                    state = 888;
                    goto case 888;
                }
                sink.Sample(in payloads[506], 1f);
                return;
            }
            case 888:
            {
                if (tick >= 64683)
                {
                    state = 889;
                    goto case 889;
                }
                sink.Sample(in payloads[507], 1f);
                return;
            }
            case 889:
            {
                if (tick >= 64684)
                {
                    state = 890;
                    goto case 890;
                }
                var factor = 0.5f;
                sink.Sample(in payloads[507], 1f - factor);
                sink.Sample(in payloads[508], factor);
                return;
            }
            case 890:
            {
                if (tick >= 64768)
                {
                    state = 891;
                    goto case 891;
                }
                sink.Sample(in payloads[508], 1f);
                return;
            }
            case 891:
            {
                if (tick >= 64799)
                {
                    state = 892;
                    goto case 892;
                }
                return;
            }
            case 892:
            {
                if (tick >= 64931)
                {
                    state = 893;
                    goto case 893;
                }
                sink.Sample(in payloads[509], 1f);
                return;
            }
            case 893:
            {
                if (tick >= 64973)
                {
                    state = 894;
                    goto case 894;
                }
                var factor = (float)(tick - 64931) / 41f;
                sink.Sample(in payloads[509], 1f - factor);
                sink.Sample(in payloads[510], factor);
                return;
            }
            case 894:
            {
                if (tick >= 65051)
                {
                    state = 895;
                    goto case 895;
                }
                sink.Sample(in payloads[510], 1f);
                return;
            }
            case 895:
            {
                if (tick >= 65534)
                {
                    state = 896;
                    goto case 896;
                }
                sink.Sample(in payloads[511], 1f);
                return;
            }
            case 896:
            {
                
                return;
            }
        }
    }
}