namespace Generator;

using LLVMSharp;
using static IRGen;

public class FunctionCall : Base
{
    AST.FunctionCall funcCall;

    public FunctionCall(AST.Node node)
    {
        this.funcCall = (AST.FunctionCall)node;
    }

    public override void generate()
    {
        if (funcCall.builtIn)
        {
            generateBuiltinCall();
            return;
        }
        LLVMValueRef funcRef = LLVM.GetNamedFunction(module, funcCall.functionName);

        if (funcRef.Pointer == IntPtr.Zero)
        {
            if (Config.options.function.declaration.reorder && Parser.declaredFunctionDict.ContainsKey(funcCall.functionName))
            {
                LLVMBasicBlockRef currentBlock = LLVM.GetInsertBlock(builder);
                AST.Function calledFunc = Parser.declaredFunctionDict[funcCall.functionName];
                calledFunc.generator.generate();
                calledFunc.generated = true;
                LLVM.PositionBuilderAtEnd(builder, currentBlock);
                funcRef = LLVM.GetNamedFunction(module, funcCall.functionName);
            }
            else
            {
                throw new GenException($"Unknown function ({funcCall.functionName}) referenced", funcCall);
            }
        }

        if (LLVM.CountParams(funcRef) != funcCall.args.Count)
        {
            throw new GenException($"Incorrect # arguments passed ({funcCall.args.Count} passed but {LLVM.CountParams(funcRef)} required)", funcCall);
        }

        int argumentCount = funcCall.args.Count;
        var argsRef = new LLVMValueRef[argumentCount];
        for (int i = 0; i < argumentCount; ++i)
        {
            funcCall.args[i].generator.generate();
            argsRef[i] = valueStack.Pop();
        }

        valueStack.Push(LLVM.BuildCall(builder, funcRef, argsRef, "calltmp"));

    }

    public void generateBuiltinCall()
    {
        AST.StringExpression printFormat;
        switch (funcCall.functionName)
        {
            case "print":
                funcCall.functionName = "printf";

                printFormat = evaluatePrintFormat();
                // Console.WriteLine("successfully evaluated print format");

                funcCall.addChildAtStart(printFormat);
                // Console.WriteLine("appended child to start of print call");
                break;
            case "println":
                funcCall.functionName = "printf";

                printFormat = evaluatePrintFormat();
                Console.WriteLine("successfully evaluated print format");

                funcCall.addChildAtStart(printFormat);

                AST.FunctionCall printNLCall = new AST.FunctionCall(new Util.Token(Util.TokenType.Keyword, "print!", funcCall.line, funcCall.column), new List<AST.Node>() { new AST.VariableExpression(new Util.Token(Util.TokenType.Keyword, "nl", funcCall.line, funcCall.column), parentRequired: false) }, true, funcCall.parent, false);
                break;
        }


        LLVMValueRef funcRef = LLVM.GetNamedFunction(module, funcCall.functionName);


        if (funcRef.Pointer == IntPtr.Zero)
        {
            throw new GenException($"Unknown function ({funcCall.functionName}) referenced", funcCall);
        }



        if (LLVM.CountParams(funcRef) != funcCall.args.Count)
        {
            throw new GenException($"Incorrect # arguments passed ({funcCall.args.Count} passed but {LLVM.CountParams(funcRef)} required)", funcCall);
        }

        int argumentCount = funcCall.args.Count;
        var argsRef = new LLVMValueRef[argumentCount];


        for (int i = 0; i < argumentCount; i++)
        {
            // Console.WriteLine("builtin with arg of: " + Parser.printASTRet(new List<ASTNode>() { builtIn.args[i] }));
            funcCall.args[i].generator.generate();
            argsRef[i] = valueStack.Pop();
            // Console.WriteLine(argsRef[i]);
            // Console.WriteLine($"evaluated builtin arg of {builtIn.args[i]}");
        }

        valueStack.Push(LLVM.BuildCall(builder, funcRef, argsRef, "calltmp"));
        // Console.WriteLine("successfully evaluated builtin call");

    }

    public AST.StringExpression evaluatePrintFormat()
    {
        switch (funcCall.args[0].nodeType)
        {
            case AST.Node.NodeType.NumberExpression:
                AST.NumberExpression numExpr = (AST.NumberExpression)funcCall.args[0];
                if (numExpr.type.value == "int")
                {
                    return new AST.StringExpression(new Util.Token(Util.TokenType.String, "\"%d\"", 0, 0), funcCall, true);
                }
                else if (numExpr.type.value == "double")
                {
                    return new AST.StringExpression(new Util.Token(Util.TokenType.String, "\"%f\"", 0, 0), funcCall, true);
                }
                return new AST.StringExpression(new Util.Token(Util.TokenType.String, "\"%f\"", 0, 0), funcCall, true);
            case AST.Node.NodeType.StringExpression:
                return new AST.StringExpression(new Util.Token(Util.TokenType.String, "\"%s\"", 0, 0), funcCall, true);
            case AST.Node.NodeType.VariableExpression:
                AST.VariableExpression varExpr = (AST.VariableExpression)funcCall.args[0];
                if (namedGlobalsAST.ContainsKey(varExpr.varName))
                {
                    return evaluatePrintFormat(namedGlobalsAST[varExpr.varName].type);
                }
                else if (namedValuesLLVM.ContainsKey(varExpr.varName))
                {
                    AST.Type printType = LLVMTypeToASTType(namedValuesLLVM[varExpr.varName].TypeOf(), funcCall);
                    return evaluatePrintFormat(printType);
                }
                throw GenException.FactoryMethod("An unknown variable was printed", "Likely a typo", varExpr, true, varExpr.varName);


        }

        return new AST.StringExpression(new Util.Token(Util.TokenType.String, "\"%f\"", 0, 0), funcCall, true);
    }

    public AST.StringExpression evaluatePrintFormat(AST.Type type)
    {
        switch (type.value)
        {
            case "double":
                return new AST.StringExpression(new Util.Token(Util.TokenType.String, "\"%f\"", 0, 0), funcCall, true);
            case "int":
                return new AST.StringExpression(new Util.Token(Util.TokenType.String, "\"%d\"", 0, 0), funcCall, true);
            case "string":
                return new AST.StringExpression(new Util.Token(Util.TokenType.String, "\"%s\"", 0, 0), funcCall, true);
            default:
                throw new GenException($"attempting to print obj of illegal or unknown type | obj: {funcCall.args[0]} type: {type.value}", funcCall);
        }

        return new AST.StringExpression(new Util.Token(Util.TokenType.String, "\"%f\"", 0, 0), funcCall, true);
    }

}
