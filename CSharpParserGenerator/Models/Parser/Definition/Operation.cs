using System;
namespace CSharpParserGenerator
{
    public class Op
    {
        Func<ParserStack, dynamic> @Func;
        Action<ParserStack> @Action;

        public Op(Func<ParserStack, dynamic> func)
        {
            @Func = func;
        }

        public Op(Action<ParserStack> action)
        {
            @Action = action;
        }

        public void Callback(ParserStack args)
        {
            if (Func != null) { @Func(args); return; }
            if (Action != null) { @Action(args); return; }
        }
    }
}