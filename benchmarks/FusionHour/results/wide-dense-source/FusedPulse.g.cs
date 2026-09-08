#nullable enable
using System;
using System.Runtime.CompilerServices;
using Tl;

namespace Tl.FusionExperiment;

public static class FusedPulse
{
    public const uint Duration = 600u;
    public const bool Loops = true;

    private static ReadOnlySpan<byte> Counts => [1, 1, 1, 3, 3, 3, 3, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1];
    private static ReadOnlySpan<float> Amounts => [1f, 0f, 0f, 1f, 0f, 0f, 1f, 0f, 0f, 1f, 2f, 5f, 1f, 2.3333333f, 5f, 1f, 2.6666667f, 5f, 1f, 3f, 5f, 3f, 5f, 0f, 3f, 5f, 0f, 3f, 5f, 0f, 3f, 5f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 8f, 5f, 0f, 8f, 5f, 0f, 8f, 5f, 0f, 8f, 5f, 0f, 8f, 5f, 0f, 8f, 5f, 0f, 8f, 5f, 0f, 8f, 5f, 0f, 8f, 5f, 0f, 8f, 5f, 0f, 8f, 5f, 0f, 13f, 0f, 0f, 13.470589f, 0f, 0f, 13.941176f, 0f, 0f, 14.411765f, 0f, 0f, 14.882353f, 0f, 0f, 15.3529415f, 0f, 0f, 15.823529f, 0f, 0f, 16.294117f, 0f, 0f, 16.764706f, 0f, 0f, 17.235294f, 0f, 0f, 17.705883f, 0f, 0f, 18.176472f, 0f, 0f, 18.647058f, 0f, 0f, 19.117647f, 0f, 0f, 19.588236f, 0f, 0f, 20.058823f, 0f, 0f, 20.529411f, 0f, 0f, 21f, 0f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 13f, 2f, 0f, 8f, 34f, 0f, 8f, 34.45652f, 0f, 8f, 34.913044f, 0f, 8f, 35.369564f, 0f, 8f, 35.826088f, 0f, 8f, 36.282608f, 0f, 8f, 36.739132f, 0f, 8f, 37.195652f, 0f, 8f, 37.652176f, 0f, 8f, 38.108696f, 0f, 8f, 38.565216f, 0f, 8f, 39.02174f, 0f, 8f, 39.47826f, 0f, 8f, 39.934784f, 0f, 8f, 40.391304f, 0f, 8f, 40.847828f, 0f, 8f, 41.304348f, 0f, 8f, 41.760868f, 0f, 8f, 42.217392f, 0f, 8f, 42.673912f, 0f, 8f, 43.130432f, 0f, 8f, 43.586956f, 0f, 8f, 44.04348f, 0f, 8f, 44.5f, 0f, 8f, 44.95652f, 0f, 8f, 45.413044f, 0f, 8f, 45.869564f, 0f, 8f, 46.326088f, 0f, 8f, 46.782608f, 0f, 8f, 47.239132f, 0f, 8f, 47.695652f, 0f, 8f, 48.152176f, 0f, 8f, 48.608696f, 0f, 8f, 49.065216f, 0f, 8f, 49.52174f, 0f, 8f, 49.97826f, 0f, 8f, 50.434784f, 0f, 8f, 50.891304f, 0f, 8f, 51.347824f, 0f, 8f, 51.804348f, 0f, 8f, 52.260868f, 0f, 8f, 52.717392f, 0f, 8f, 53.173912f, 0f, 8f, 53.630436f, 0f, 8f, 54.086956f, 0f, 8f, 54.54348f, 0f, 8f, 55f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 0f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 2f, 5f, 0f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 1f, 8f, 5f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f, 3f, 0f, 0f];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Playback Start(uint tick = 0)
        => Mint(tick, 0, PlaybackFlags.Started);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Playback Stop(in Playback playback)
    {
        if (!playback.Has(PlaybackFlags.Started))
            throw new InvalidOperationException("Cannot stop a playback that was never started.");
        return Mint(playback.Tick, playback.Cycles, playback.Flags | PlaybackFlags.Stopped);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Playback Forward(in Playback from, ref float sum, uint tick)
    {
        if (!from.Has(PlaybackFlags.Started))
            throw new InvalidOperationException("Playback was never started; mint one with FusedPulse.Start.");
        if (from.Has(PlaybackFlags.Stopped))
            throw new InvalidOperationException("Playback is stopped.");
        return ForwardOne(in from, ref sum, tick);
    }

    public static Playback Forward(in Playback from, ref float sum, ReadOnlySpan<uint> ticks)
    {
        if (!from.Has(PlaybackFlags.Started))
            throw new InvalidOperationException("Playback was never started; mint one with FusedPulse.Start.");
        if (from.Has(PlaybackFlags.Stopped))
            throw new InvalidOperationException("Playback is stopped.");
        if (ticks.IsEmpty)
            return from;
        var stateTick = from.Tick;
        var stateCycles = from.Cycles;
        var previousQuotient = stateTick / Duration;
        var previousEffective = stateTick - previousQuotient * Duration;
        foreach (var tick in ticks)
        {
            var quotient = tick / Duration;
            var effective = tick - quotient * Duration;
            uint cycles;
            if (tick >= stateTick)
                cycles = quotient - previousQuotient;
            else
                cycles = effective < previousEffective ? 1u : 0u;
            if (cycles > ushort.MaxValue - stateCycles)
                throw new ArgumentOutOfRangeException("ticks", "Playback cycle capacity exceeded.");
            stateCycles = (ushort)(stateCycles + cycles);
            var denseIndex = (int)effective;
            var denseOffset = denseIndex * 3;
            var denseCount = Counts[denseIndex];
            var denseAmounts = Amounts;
            if (denseCount != 0)
            {
                sum += denseAmounts[denseOffset];
                if (denseCount > 1)
                    sum += denseAmounts[denseOffset + 1];
                if (denseCount > 2)
                    sum += denseAmounts[denseOffset + 2];
            }
            stateTick = tick;
            previousEffective = effective;
            previousQuotient = quotient;
        }
        var flags = PlaybackFlags.Started;
        if (previousEffective == Duration - 1u)
            flags |= PlaybackFlags.LastLoopFrame;
        return Mint(stateTick, stateCycles, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Playback Backward(in Playback from, ref float sum, uint tick)
    {
        if (!from.Has(PlaybackFlags.Started))
            throw new InvalidOperationException("Playback was never started; mint one with FusedPulse.Start.");
        if (from.Has(PlaybackFlags.Stopped))
            throw new InvalidOperationException("Playback is stopped.");
        return BackwardOne(in from, ref sum, tick);
    }

    public static Playback Backward(in Playback from, ref float sum, ReadOnlySpan<uint> ticks)
    {
        if (!from.Has(PlaybackFlags.Started))
            throw new InvalidOperationException("Playback was never started; mint one with FusedPulse.Start.");
        if (from.Has(PlaybackFlags.Stopped))
            throw new InvalidOperationException("Playback is stopped.");
        if (ticks.IsEmpty)
            return from;
        var stateTick = from.Tick;
        var stateCycles = from.Cycles;
        var previousQuotient = stateTick / Duration;
        var previousEffective = stateTick - previousQuotient * Duration;
        foreach (var tick in ticks)
        {
            var quotient = tick / Duration;
            var effective = tick - quotient * Duration;
            uint cycles;
            if (tick <= stateTick)
                cycles = previousQuotient - quotient;
            else
                cycles = effective > previousEffective ? 1u : 0u;
            stateCycles = (ushort)(stateCycles - Math.Min(stateCycles, cycles));
            var denseIndex = (int)effective;
            var denseOffset = denseIndex * 3;
            var denseCount = Counts[denseIndex];
            var denseAmounts = Amounts;
            if (denseCount != 0)
            {
                sum -= denseAmounts[denseOffset];
                if (denseCount > 1)
                    sum -= denseAmounts[denseOffset + 1];
                if (denseCount > 2)
                    sum -= denseAmounts[denseOffset + 2];
            }
            stateTick = tick;
            previousEffective = effective;
            previousQuotient = quotient;
        }
        var flags = PlaybackFlags.Started;
        if (previousEffective == Duration - 1u)
            flags |= PlaybackFlags.LastLoopFrame;
        return Mint(stateTick, stateCycles, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Playback ForwardOne(in Playback from, ref float sum, uint tick)
    {
        var previousQuotient = from.Tick / Duration;
        var quotient = tick / Duration;
        var effective = tick - quotient * Duration;
        uint cycles;
        if (tick >= from.Tick)
            cycles = quotient - previousQuotient;
        else
        {
            var previousEffective = from.Tick - previousQuotient * Duration;
            cycles = effective < previousEffective ? 1u : 0u;
        }
        if (cycles > ushort.MaxValue - from.Cycles)
            throw new ArgumentOutOfRangeException("ticks", "Playback cycle capacity exceeded.");
        var newCycles = (ushort)(from.Cycles + cycles);
        var flags = PlaybackFlags.Started;
        if (effective == Duration - 1u)
            flags |= PlaybackFlags.LastLoopFrame;
        var denseIndex = (int)effective;
        var denseOffset = denseIndex * 3;
        var denseCount = Counts[denseIndex];
        var denseAmounts = Amounts;
        if (denseCount != 0)
        {
            sum += denseAmounts[denseOffset];
            if (denseCount > 1)
                sum += denseAmounts[denseOffset + 1];
            if (denseCount > 2)
                sum += denseAmounts[denseOffset + 2];
        }
        return Mint(tick, newCycles, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Playback BackwardOne(in Playback from, ref float sum, uint tick)
    {
        var previousQuotient = from.Tick / Duration;
        var quotient = tick / Duration;
        var effective = tick - quotient * Duration;
        uint cycles;
        if (tick <= from.Tick)
            cycles = previousQuotient - quotient;
        else
        {
            var previousEffective = from.Tick - previousQuotient * Duration;
            cycles = effective > previousEffective ? 1u : 0u;
        }
        var newCycles = (ushort)(from.Cycles - Math.Min(from.Cycles, cycles));
        var flags = PlaybackFlags.Started;
        if (effective == Duration - 1u)
            flags |= PlaybackFlags.LastLoopFrame;
        var denseIndex = (int)effective;
        var denseOffset = denseIndex * 3;
        var denseCount = Counts[denseIndex];
        var denseAmounts = Amounts;
        if (denseCount != 0)
        {
            sum -= denseAmounts[denseOffset];
            if (denseCount > 1)
                sum -= denseAmounts[denseOffset + 1];
            if (denseCount > 2)
                sum -= denseAmounts[denseOffset + 2];
        }
        return Mint(tick, newCycles, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Playback Mint(uint tick, ushort cycles, PlaybackFlags flags)
        => Unsafe.BitCast<ulong, Playback>(tick | (ulong)cycles << 32 | (ulong)(ushort)flags << 48);
}
