# ExpressoSharp
Easy to use compiler to evaluate string expressions for .NET

Easily compile an expression, like "x * 2", into a Func<..> type like so:
```csharp
var calc = ExpressoCompiler.CompileExpression<Func<int, int>>("x * 2", "x");
```

And then be able to use the result as a simple function, like so:
```csharp
var fortyTwo = calc(21);
```

You can find the package on Nuget using this link: https://www.nuget.org/packages/ExpressoSharp/
