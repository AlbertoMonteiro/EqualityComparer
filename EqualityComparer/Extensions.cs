namespace EqualityComparer
{
    public static class Extensions
    {
        public static bool IsEquals(this object a, object b)
        {
            return DepthObjectEqualityComparer.EqualityComparer.AreEquals(a, b);
        }
    }
}