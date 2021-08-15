namespace DynamicQuery.Models
{

    public enum ELang
    {
        // NonTerminal Symbols
        Expression, Relational, LogicalAnd, LogicalOr, Term,


        // Terminal Symbols
        Ignore,

        LParenthesis,
        RParenthesis,

        Eq, Neq, Gt, Lt, Gte, Lte,

        And, Or,

        Boolean, String, Number,

        Property,
    }
}