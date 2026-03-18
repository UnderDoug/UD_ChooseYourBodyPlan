using System;
using System.Collections.Generic;
using System.Text;

using XRL;

namespace UD_ChooseYourBodyPlan.Mod.Logging
{
    public class Indent : IDisposable
    {
        public static int MaxIndent = 16;

        protected int BaseValue;

        protected int LastValue;

        protected int Factor;

        protected char Char;

        public Indent()
        {
            BaseValue = 0;
            LastValue = 0;
            Factor = 4;
            Char = ' ';
            if (Debug.HaveIndents())
            {
                BaseValue = CapIndent(Debug.LastIndent.LastValue);
                LastValue = CapIndent(Debug.LastIndent.LastValue);
                Factor = Debug.LastIndent.Factor;
                Char = Debug.LastIndent.Char;
            }
            Debug.PushToIndents(this);
        }
        public Indent(Indent Source) 
            : this() 
        {
            BaseValue = Source.LastValue;
            LastValue = Source.LastValue;
            Factor = Source.Factor;
            Char = Source.Char;
        }
        public Indent(int Offset) 
            : this() 
        {
            BaseValue = CapIndent(BaseValue + Offset);
            LastValue = BaseValue;
        }
        public Indent(Indent Source, bool Store)
        {
            BaseValue = Source.LastValue;
            LastValue = Source.LastValue;
            Factor = Source.Factor;
            Char = Source.Char;

            if (Store)
                Debug.PushToIndents(this);
        }

        public Indent this[int Indent]
        {
            get
            {
                if (DebugMethodRegistry.GetDoDebug(Debug.CallingMethodName()))
                    LastValue = CapIndent(BaseValue + Indent);

                return this;
            }
            protected set => SetIndent(value + Indent);
        }

        protected int CapIndent(int Indent)
            => Math.Clamp(Indent, BaseValue, MaxIndent);

        protected int CapIndent()
            => CapIndent(LastValue);

        public Indent ResetIndent()
            => ResetIndent(out _);

        public Indent ResetIndent(out Indent Indent)
        {
            LastValue = BaseValue;
            return Indent = this;
        }
        public Indent SetIndent(int Offset)
        {
            BaseValue = CapIndent(Offset);
            LastValue = BaseValue;
            return this;
        }

        public int GetBaseValue()
            => BaseValue;

        public override string ToString()
            => Char.ThisManyTimes(CapIndent() * Factor);

        public static implicit operator int(Indent Operand)
            => Operand.LastValue;

        public Indent DiscardIndent()
            => Debug.DiscardIndent();

        public void Dispose()
        {
            if (Debug.LastIndent == this
                || Debug.HasIndent(this))
                DiscardIndent();
        }
    }
}
