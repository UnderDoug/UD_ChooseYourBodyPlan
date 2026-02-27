using System;
using System.Collections.Generic;
using System.Text;

namespace UD_BodyPlan_Selection.Mod
{
    public static class Utils
    {
        public static bool LogReturnBool(bool Return, string Message)
        {
            UnityEngine.Debug.Log(Message);
            return Return;
        }
        public static bool LogTrue(string Message)
            => LogReturnBool(true, Message)
            ;
        public static bool LogFalse(string Message)
            => LogReturnBool(false, Message)
            ;
    }
}
