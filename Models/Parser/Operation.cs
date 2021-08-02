using System;
using System.Collections.Generic;

namespace CSharpParserGenerator
{
    public class Op
    {
        Func<List<dynamic>, dynamic> @Func;
        Action<List<dynamic>> @Action;

        public Op(Func<List<dynamic>, dynamic> func)
        {
            @Func = func;
        }

        public Op(Action<List<dynamic>> action)
        {
            @Action = action;
        }

        public void Callback(List<dynamic> args)
        {
            if (Func != null) { @Func(args); return; }
            if (Action != null) { @Action(args); return; }
        }
    }
}