namespace DSRRobotControl
{
    public class DLerp
    {
        public static double dLerp(double a, double b, float t)
        {
            return a + (b - a) * t;
        }
    }
}
