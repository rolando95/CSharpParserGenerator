using System;

namespace Syntax
{
    public class Op<T>
    {
        Func<T, T> @Func;
        Action<T> @Action;

        public Op(Func<T, T> func)
        {
            @Func = func;
        }

        public Op(Action<T> action)
        {
            @Action = action;
        }

        public void Execute(T args) {
            if(Func != null ) { @Func(args); return; }
            if(Action != null) { @Action(args); return; }
        }
    }

    public class Op : Op<object> { 
        public Op(Func<object, object> func) : base(func) { } 
        public Op(Action<object> action) : base(action) { } 
    }
}