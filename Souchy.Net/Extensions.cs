namespace Souchy.Net;

public static class Extensions
{

    public static bool NextBool(this Random rnd)
    {
        return rnd.Next(0, 2) == 0;
    }

}
