using System.Runtime.CompilerServices;

namespace Tl.Hooks;

public static class ReceiverLink
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Forward(ref Receiver receiver, in Frame frame)
        => receiver.OnForward(in frame);
}