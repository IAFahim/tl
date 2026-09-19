using Tl;

namespace TwinA
{
    public readonly struct TwinClip(int value)
    {
        public readonly int Value = value;
    }

    public readonly struct TwinTrack(int code) : IBlend<TwinClip>
    {
        public readonly int Code = code;

        public void Blend(in TwinClip first, in TwinClip second, float factor, out TwinClip result) => result = first;
    }
}

namespace TwinB
{
    public readonly struct TwinClip(int value)
    {
        public readonly int Value = value;
    }

    public readonly struct TwinTrack(int code) : IBlend<TwinClip>
    {
        public readonly int Code = code;

        public void Blend(in TwinClip first, in TwinClip second, float factor, out TwinClip result) => result = first;
    }
}
