# Dynamic Query (CSharpParserGenerator example)
This sample seeks to create your own query language to get a list of results by using linq<br/>

## About syntaxis: 

* In this syntax, properties are case insensitive. numeric values group integers and floats and strings are enclosed in quotation marks
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
By default, the /People endpoint returns a list of people. 
According to the filters you send in the request only those people who meet the criteria will be returned. You can filter by any field within the Person object.

* Download and install the latest .Net Core version available [Here](https://dotnet.microsoft.com/).
* Clone [CSharpParserGenerator](https://github.com/rolando95/CSharpParserGenerator) repository. The repository includes this example
* From the workspace folder, locate the terminal in the path of this example
```
    cd Examples/DynamicQuery/
```

* Run the following commands:
```
    dotnet restore
    dotnet run
```

* You can try some requests:
    - https://localhost:5001/People
    - [https://localhost:5001/People?filter= dateOfBirth lte "1990-01-01"](https://localhost:5001/People?filter=dateOfBirth%20lte%20%221990-01-01%22)
    - [https://localhost:5001/People?filter= haslicense eq true and ( lastname eq "Rosales" or dateOfBirth lte "1990-01-01" )](https://localhost:5001/People?filter=haslicense%20eq%20true%20and%20%28%20lastname%20eq%20%22Rosales%22%20or%20dateOfBirth%20lte%20%221990-01-01%22%20%29)

## Example project structure
* Parser Class
    - It is recommended to create a 'own' class that contains all the lexical, syntactic and semantic definitions of the language. It is important to instantiate the parser only once during the entire execution of the program, because it is an expensive operation. You can guide yourself in [./Services/MyQueryParser.cs](./Services/MyQueryParser.cs).
* Startup
    - If you use dependency injection, it is recommended to use the Singleton pattern for your 'own' parser class. Or, if you prefer, have an instance of the static Parser and make sure it is only instantiated once.

```C#
    services.AddSingleton<MyParserClass>();
```
And that's it! After defining the rules of the parser and having an instance it is possible to call the Parse method as many times as you want. In this example you can see it in action in [./Controllers/DynamicQueryController.cs](./Controllers/DynamicQueryController.cs).

## How this sample has been implemented using CSharpParserGenerator?
### Lexer
* The first thing is to define the tokens and required for the lexical parser.
* The language in this example consists of the **String, Number**, and **Boolean** tokens, a token named **property** that refers to the property you want to query, the relational operators **Eq (equal), Neq (not equal), Gt (greater than), Lt (Less than), Gte (greater than equal)and lte (less than equal)**, the logical operators **And** and **Or**, and the **parentheses**. Additional a token called **Ignore** will be created that will contain the spaces and line breaks that will be ignored when reading the input string.
* For each token, you need to define a regular expression that represents it. It is important to put those tokens that are reserved words first, because otherwise (for example), some tokens such as And, Or, Gte, etc. can be taken as property, because they really belong to the regular expression property. Be careful with order.
* For CSharpParserGenerator, tokens can be defined as follows (PD: **(?i)** means that the pattern is case insensitive):
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
```
* It can be noted that there is an **enum** called **ELang**. This must carry all terminal and non-terminal tokens of the language
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
* The next thing is to define the production rules corresponding to the syntax of our language. So far CSharpParserGenerator only supports **SLR(1) grammar**, so you should take that into account. You can read about it through this [link](https://en.wikipedia.org/wiki/SLR_grammar).
* For this example, we have defined the following grammar:
```prolog
    Expression -> LogicalOr
    
    LogicalOr -> LogicalOr or LogicalAnd
    LogicalOr -> LogicalAnd
    
    LogicalAnd -> LogicalAnd and Relational
    LogicalAnd -> Relational
    
    Relational -> Property eq Term
    Relational -> Property neq Term
    Relational -> Property gt Term
    Relational -> Property lt Term
    Relational -> Property gte Term
    Relational -> Property lte Term
    Relational -> ( Expression )
    
    Term -> Number
    Term -> Boolean
    Term -> String
```
* In CSharpParserGenerator, the definition of the rules would look as follows:
```C#
    var rules = new SyntaxDefinition<ELang>(new Dictionary<ELang, DefinitionRules>()
    {
        [ELang.Expression] = new DefinitionRules
        {
            // Expression -> LogicalOr
            new List<Token> { ELang.LogicalOr }
        },
        [ELang.LogicalOr] = new DefinitionRules
        {
            // LogicalOr -> LogicalOr or LogicalAnd
            new List<Token> { ELang.LogicalOr, ELang.Or, ELang.LogicalAnd },
            
            // LogicalOr -> LogicalAnd
            new List<Token> { ELang.LogicalAnd }
        },
        [ELang.LogicalAnd] = new DefinitionRules
        {
            // LogicalAnd -> LogicalAnd and Relational
            new List<Token> { ELang.LogicalAnd, ELang.And, ELang.Relational },
            
            // LogicalAnd -> Relational
            new List<Token> { ELang.Relational }
        },
        [ELang.Relational] = new DefinitionRules
        {
            // Relational -> Property eq Term
            new List<Token> { ELang.Property, ELang.Eq, ELang.Term },

            // Relational -> Property neq Term
            new List<Token> { ELang.Property, ELang.Neq, ELang.Term },

            // Relational -> Property gt Term
            new List<Token> { ELang.Property, ELang.Gt, ELang.Term },
            
            // Relational -> Property lt Term
            new List<Token> { ELang.Property, ELang.Lt, ELang.Term },

            // Relational -> Property gte Term
            new List<Token> { ELang.Property, ELang.Gte, ELang.Term },
            
            // Relational -> Property lte Term
            new List<Token> { ELang.Property, ELang.Lte, ELang.Term },

            // Relational -> ( Expression )
            new List<Token> { ELang.LParenthesis, ELang.Expression, ELang.RParenthesis },
        },
        [ELang.Term] = new DefinitionRules
        {
            // Term -> Number
            new List<Token> { ELang.Number },

            // Term -> Boolean
            new List<Token> { ELang.Boolean },

            // Term -> String
            new List<Token> { ELang.String },
        }
    });
```
### Semantics
* The previous syntax definition is incomplete. Com o you can see the rules of production are clear, but there is no 'logic' that shows what to do.
    - We take this production rule from another example:
    ```C#
        [ELang.Addition] = new DefinitionRules
        {
            new List<Token> { ELang.Number, ELang.Add, ELang.Number },
        }
    ```
    - It can be read as _"Addition produces Number Add Number"_. For **CSharpParserGenerator**, this translates to a single **List** where the first element ```[0]``` is the non-terminal of production rule (Addition) and the other positions are Number ```[1]```, Add ```[2]```, and Number ```[3]```. If we expect to raise the result of that sum in the tree, having clear the initial values of the List, we can infer that the operation would be ```o[0] = o[1] + o[3]```.
    - In CSharpParserGenerator, you can create a semantic operation with an instance with the ```Op``` class. For the sum production rule mentioned, it can be written as follows:
    ```C#
    new Op(o => { o[0] = o[1] + o[3]; })
    ```
    - The output of the production rule with the semantic operation in CSharpParserGenerator would be as follows:
    ```C#
        [ELang.Addition] = new DefinitionRules
        {
            new List<Token> { ELang.Number, ELang.Add, ELang.Number, new Op(o => { o[0] = o[1] + o[3]; }) },
        }
    ```
    - As you can see, just write the operation at the end of the production rule. Now let's continue with the example of the Dynamic Parser...
* In CSharpParserGenerator you can upload any object you want in each production rule. For this example we have created a class called MyExpressionTree:
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
* The idea is that for each node, the operation is stored whether it is relational or logical; in case of being relational store the property of the query and the value to compare and for logical operations the left and right nodes of the operation.
* So if we have the expression "Relational produces Property Eq Term" written as: 
```C#
    [ELang.Relational] = new DefinitionRules
    {
        new List<Token> { ELang.Property, ELang.Eq, ELang.Term }
    }
```
* The semantic operation would be:
```C#
    new Op(
        o => o[0] = new MyExpressionTree() 
        { 
            Property = o[1], 
            Operation = ELang.Eq, 
            Value = o[3] 
        }
    )
```
* The production rule with the semantic action would be as follows:
```C#
    [ELang.Relational] = new DefinitionRules
    {
        new List<Token> { 
            ELang.Property, ELang.Eq, ELang.Term, 
            new Op(o => o[0] = new MyExpressionTree() 
            { 
                Property = o[1], 
                Operation = ELang.Eq, 
                Value = o[3] 
            }) 
        }
    }
```
* By having the production rules defined it is possible to create an instance of the parser
```C#
    var parser = new ParserGenerator<ELang>(lexer, rules).CompileParser();
```
* You can see the implementation of all the rules in [/Services/MyQueryParser.cs](./Services/MyQueryParser.cs)