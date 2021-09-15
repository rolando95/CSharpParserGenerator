# Dynamic Query (CSharpParserGenerator example)
This sample seeks to create your own query language to get a list of results by using linq<br/>

## About syntaxis 

* In this syntax, the primitive types are String, Number, Boolean, and Property. String and property types are case insensitive
    - Property: **LastName**
    - String: **"Tom"**
    - Number: **30**
    - Boolean: **true**

* You can use any relational operation in this format:  _Property RelationalOperator value_ 
    - For example: **Firstname eq "Jose"**
    - Allowed relational operators: 
        * Equal: **eq**
        * Not equal: **neq**
        * Greater than or equal: **gte**
        * Less than or equal: **lte**
        * Greater than: **gt**
        * Less than: **lt**

* You can perform logical operations using the **AND**, **OR** operators.
    - Operator precedence is taken into account, so AND operations are performed before OR
    - For example: **haslicense eq true and DateOfBirth lte "1990-01-01"**

* It is possible to group operations with parentheses, these are valid expressions: 
    - **haslicense eq true and ( lastname eq "Rosales" or dateOfBirth lte "1990-01-01" )**

## How to run this sample?
By default, the **/People** endpoint returns a list of people. 
According to the filters you send in the request only those people who meet the criteria will be returned. You can filter by any field within the Person object.

* Download and install the latest .Net Core version available [Here](https://dotnet.microsoft.com/).
* Clone [CSharpParserGenerator](https://github.com/rolando95/CSharpParserGenerator) repository. The repository includes this example
* From the workspace folder, locate the terminal in the path of this example
```properties
    cd Examples/DynamicQuery/
```

* Run the following commands:
```properties
    dotnet restore
    dotnet run
```

* You can try some requests:
    - https://localhost:5001/People
    - [https://localhost:5001/People?filter= dateOfBirth lte "1990-01-01"](https://localhost:5001/People?filter=dateOfBirth%20lte%20%221990-01-01%22)
    - [https://localhost:5001/People?filter= haslicense eq true and ( lastname eq "Rosales" or dateOfBirth lte "1990-01-01" )](https://localhost:5001/People?filter=haslicense%20eq%20true%20and%20%28%20lastname%20eq%20%22Rosales%22%20or%20dateOfBirth%20lte%20%221990-01-01%22%20%29)

## Example project structure
* Parser Class
    - It is recommended to create a 'own' class that contains all the lexical, syntactic and semantic definitions of the language and compiled parser. If you choose to create your own parser class and work with dependency injection, you can guide yourself with [./Services/MyQueryParser.cs](./Services/MyQueryParser.cs) (Notice that in the [Startup.cs](./Startup.cs), MyQueryParser class was injected as a Singleton).
    - To generate the parser, lexer and syntax rules are required.
```C#
    var tokens = new LexerDefinition<MyLangEnum>(new Dictionary<MyLangEnum, TokenRegex>
    {
        [MyLangEnum.TokenName] = "regex pattern"
        // ...
    });

    var rules = new GrammarRules<MyLangEnum>(new Dictionary<MyLangEnum, Token[][]>()
    {
        [MyLangEnum.NonTerminalToken] = new Token[][]
        {
            new Token[] { /* Production rules */, new Op(o => { /* Semantic actions */ }) }
            // ...
        },

        // ...
    });

    var lexer = new Lexer<MyLangEnum>(tokens, MyLangEnum.IgnoreToken);
    var MyParser = new ParserGenerator<MyLangEnum>(lexer, rules).CompileParser();
```
* Parse Function
    - Having the parser, simply call the Parse method and send an input string as a parameter.
```C#
    var parseResult = MyParser.Parse<MyResultType>(inputString);
```
And that's it! After defining the rules of the parser and having an instance it is possible to call the Parse method as many times as you want. In this example you can see it in action in [./Controllers/DynamicQueryController.cs](./Controllers/DynamicQueryController.cs).

## How this sample has been implemented using CSharpParserGenerator?
### Lexer
* The first thing is to define the tokens and required for the lexical parser.
* The language in this example consists of the **String, Number**, and **Boolean** tokens, a token named **property** that refers to the property you want to query; the relational operators **Eq (equal), Neq (not equal), Gt (greater than), Lt (Less than), Gte (greater than equal)** and **lte (less than equal)**; the logical operators **And** and **Or** and the **parentheses**. Additional a token called **Ignore** will be created that will contain the spaces and line breaks that will be ignored when reading the input string.
* For each token, you need to define a regular expression that represents it. It is important to put those tokens that are reserved words first, because otherwise (for example), some tokens such as And, Or, Gte, etc. can be taken as property, because they really belong to the regular expression property. Be careful with order.
* For CSharpParserGenerator, tokens can be defined as follows:
```C#
    var tokens = new LexerDefinition<ELang>(new Dictionary<ELang, TokenRegex>
    {
        [ELang.Ignore] = "[ \\n]+", // Ignore spaces and line breaks
        [ELang.LParenthesis] = "\\(", // Left parenthesis
        [ELang.RParenthesis] = "\\)", // Right parenthesis

        [ELang.Eq] = "(?i)eq", // Equal
        [ELang.Neq] = "(?i)neq", // Not Equal

        [ELang.Gte] = "(?i)gte", // Greater than or equal
        [ELang.Lte] = "(?i)lte", // Less than or equal

        [ELang.Gt] = "(?i)gt", // Greater than
        [ELang.Lt] = "(?i)lt", // Less than

        [ELang.And] = "(?i)and", // And
        [ELang.Or] = "(?i)or", // Or

        [ELang.Boolean] = "(?i)(true|false)", // Boolean
        [ELang.String] = "(\"[^\"]*\")", // Quoted String
        [ELang.Number] = "[-+]?\\d*(\\.\\d+)?", // Number

        [ELang.Property] = "[_a-zA-Z]+\\w*" // Property
    });

    //PD: (?i) means that the pattern is case insensitive
```
* It can be noted that there is an **enum** called [ELang](./Models/Parser/ELang.cs). This must carry all terminal and non-terminal tokens of the language
```C#
    public enum ELang
    {
        // NonTerminal Symbols
        Expression, Relational, LogicalAnd, LogicalOr, Term,


        // Terminal Symbols
        LParenthesis,
        RParenthesis,

        Eq, Neq, Gt, Lt, Gte, Lte,

        And, Or,

        Boolean, String, Number,

        Property,

        // Ignore
        Ignore
    }
```
* When you create the token list, you must instantiate an object of type ```Lexer<T>(LexerDefinition<T> tokens, T ignoreToken)```, as shown below:
```C#
    var lexer = new Lexer<ELang>(tokens, ELang.Ignore);
```
### Syntax
* The next thing is to define the production rules corresponding to the syntax of our language. So far CSharpParserGenerator only supports **LALR(1) grammars**, so you should take that into account. You can read about it through this [link](https://en.wikipedia.org/wiki/LALR_parser).
* For this example, we have defined the following grammar:
```prolog
    Expression -> LogicalOr
    
    LogicalOr -> LogicalOr or LogicalAnd
    LogicalOr -> LogicalAnd
    
    LogicalAnd -> LogicalAnd and Relational
    LogicalAnd -> Relational
    
    Relational -> property eq Term
    Relational -> property neq Term
    Relational -> property gt Term
    Relational -> property lt Term
    Relational -> property gte Term
    Relational -> property lte Term
    Relational -> ( Expression )
    
    Term -> number
    Term -> boolean
    Term -> string
```
* In CSharpParserGenerator, the definition of the rules would look as follows:
```C#
    var rules = new GrammarRules<ELang>(new Dictionary<ELang, Token[][]>()
    {
        [ELang.Expression] = new Token[][]
        {
            // Expression -> LogicalOr
            new Token[] { ELang.LogicalOr }
        },
        [ELang.LogicalOr] = new Token[][]
        {
            // LogicalOr -> LogicalOr or LogicalAnd
            new Token[] { ELang.LogicalOr, ELang.Or, ELang.LogicalAnd },
            
            // LogicalOr -> LogicalAnd
            new Token[] { ELang.LogicalAnd }
        },
        [ELang.LogicalAnd] = new Token[][]
        {
            // LogicalAnd -> LogicalAnd and Relational
            new Token[] { ELang.LogicalAnd, ELang.And, ELang.Relational },
            
            // LogicalAnd -> Relational
            new Token[] { ELang.Relational }
        },
        [ELang.Relational] = new Token[][]
        {
            // Relational -> property eq Term
            new Token[] { ELang.Property, ELang.Eq, ELang.Term },

            // Relational -> property neq Term
            new Token[] { ELang.Property, ELang.Neq, ELang.Term },

            // Relational -> property gt Term
            new Token[] { ELang.Property, ELang.Gt, ELang.Term },
            
            // Relational -> property lt Term
            new Token[] { ELang.Property, ELang.Lt, ELang.Term },

            // Relational -> property gte Term
            new Token[] { ELang.Property, ELang.Gte, ELang.Term },
            
            // Relational -> property lte Term
            new Token[] { ELang.Property, ELang.Lte, ELang.Term },

            // Relational -> ( Expression )
            new Token[] { ELang.LParenthesis, ELang.Expression, ELang.RParenthesis },
        },
        [ELang.Term] = new Token[][]
        {
            // Term -> number
            new Token[] { ELang.Number },

            // Term -> boolean
            new Token[] { ELang.Boolean },

            // Term -> string
            new Token[] { ELang.String },
        }
    });
```
### Semantic Actions
* The previous syntax definition is incomplete. As you can see, the rules of production are clear, but there is no 'logic' that shows what to do.
    - We take this production rule from another example:
    ```C#
        [ELang.Addition] = new Token[][]
        {
            new Token[] { ELang.Number, ELang.Add, ELang.Number },
        }
    ```
    - It can be read as _"The addition produces number add number"_. For **CSharpParserGenerator**, this translates into a single **List** where positions are _Number_ ```[0]```, _Add_ ```[1]``` and _Number_ ```[2]```. In each production rule the resulting value of position 0 is raised to the tree, therefore the semantic action should operate on the numbers and assign the result to position 0 before rising.
    - In CSharpParserGenerator, you can create a semantic operation with an instance with the ```Op``` class. For the sum production rule mentioned, it can be written as follows:
    ```C#
        new Op(o => { o[0] = o[0] + o[2]; })
    ```
    - The output of the production rule with the semantic operation in CSharpParserGenerator would be as follows:
    ```C#
        [ELang.Addition] = new Token[][]
        {
            new Token[] { ELang.Number, ELang.Add, ELang.Number, new Op(o => { o[0] = o[0] + o[2]; }) },
        }
    ```
    - As you can see, just write the operation at the end of the production rule. Now let's continue with the example of the Dynamic Parser...
* In definition rules you can upload any object you want in each production rule. For this example we have created a class called MyExpressionTree:
```C#
    public class MyExpressionTree
    {
        public ELang Operation { get; set; }

        // Relacional Operation Props
        public string Property { get; set; }
        public object Value { get; set; }

        // Logical Operation Props
        public MyExpressionTree Left { get; set; }
        public MyExpressionTree Right { get; set; }

    }
```
* The idea with this example is to create a binary syntax tree. Each node indicates whether the operation is relational (Storing the property, operation, and value to compare) or whether it is logical (Storing the operation and the nodes on the left and right side of the tree).
* So if we have the expression "Relational produces Property Eq Term" written as: 
```C#
    [ELang.Relational] = new Token[][]
    {
        new Token[] { ELang.Property, ELang.Eq, ELang.Term }
    }
```
* The semantic operation would be:
```C#
    new Op(
        o => o[0] = new MyExpressionTree() 
        { 
            Property = o[0], 
            Operation = ELang.Eq, 
            Value = o[2] 
        }
    )
```
* The production rule with the semantic action would be as follows:
```C#
    [ELang.Relational] = new Token[][]
    {
        new Token[] { 
            ELang.Property, ELang.Eq, ELang.Term, 
            new Op(o => o[0] = new MyExpressionTree() 
            { 
                Property = o[0], 
                Operation = ELang.Eq, 
                Value = o[2] 
            }) 
        }
    }
```
* By having the production rules defined it is possible to create an instance of the parser
```C#
    var parser = new ParserGenerator<ELang>(lexer, rules).CompileParser();
```
* You can see the implementation of all rules in [./Services/MyQueryParser.cs](./Services/MyQueryParser.cs)