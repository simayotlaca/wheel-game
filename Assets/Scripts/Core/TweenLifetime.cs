using PrimeTween;

namespace VertigoWheel
{
internal static class TweenLifetime
{
    internal static void StopIfAlive(Sequence s)
    {
        if (s.isAlive)
        {
            s.Stop();
        }
    }

    internal static void StopIfAlive(Tween t)
    {
        if (t.isAlive)
        {
            t.Stop();
        }
    }
}
}
