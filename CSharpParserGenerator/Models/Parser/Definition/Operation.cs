using System;
using System.Collections.Generic;

namespace CSharpParserGenerator
{
    public class Op
    {
        Func<dynamic[], dynamic> @Func;
        Action<dynamic[]> @Action;

        public Op(Func<dynamic[], dynamic> func)
        {
            @Func = func;
        }

        public Op(Action<dynamic[]> action)
        {
            @Action = action;
        }

        public void Callback(dynamic[] args)
        {
            if (Func != null) { @Func(args); return; }
            if (Action != null) { @Action(args); return; }
        }
    }
}