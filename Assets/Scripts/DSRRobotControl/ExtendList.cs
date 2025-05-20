using System.Collections.Generic;


namespace DSRRobotControl
{
    public class ExtendList
    {
        public static void extendList(List<double> list, int length)
        {
            if (list.Count < length)
            {
                double lastElement = list[list.Count - 1];
                while (list.Count < length)
                {
                    list.Add(lastElement);
                }
            }
        }
    }
}
