using System;
namespace CSharpParserGenerator
{
    public class Op
    {
        Func<ParseStack, dynamic> @Func;
        Action<ParseStack> @Action;

        public Op(Func<ParseStack, dynamic> func)
        {
            @Func = func;
        }

        public Op(Action<ParseStack> action)
        {
            @Action = action;
        }

        public void Callback(ParseStack args)
        {
            if (Func != null) { @Func(args); return; }
            if (Action != null) { @Action(args); return; }
        }
    }
}