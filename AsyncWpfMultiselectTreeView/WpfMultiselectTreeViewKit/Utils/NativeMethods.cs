using System.Runtime.InteropServices;

namespace WpfMultiselectTreeViewKit.Utils
{
    public class NativeMethods
    {
        [DllImport("user32.dll")]
        private static extern uint GetDoubleClickTime();

        private static uint? msDoubleClickTime;

        public static uint DoubleClickTime
        {
            get
            {
                if (!msDoubleClickTime.HasValue)
                {
                    msDoubleClickTime = GetDoubleClickTime();
                }
                return msDoubleClickTime.Value;
            }
        }
    }
}