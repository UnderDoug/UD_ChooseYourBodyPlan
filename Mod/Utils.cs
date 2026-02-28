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

        public static void Log(string Message, int Indent = 0)
        {
            if (Indent > 0)
                Message = " ".ThisManyTimes(Indent * 4);
            UnityEngine.Debug.Log(Message);
        }

        public static void Log(object Context, int Indent = 0)
            => Log(Context.ToString(), Indent);
    }
}
