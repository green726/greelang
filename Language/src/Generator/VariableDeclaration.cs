namespace Generator;
using LLVMSharp;
using static IRGen;

public class VariableDeclaration : Base
{
    public AST.VariableDeclaration varDec;
    public LLVMTypeRef typeLLVM;

    private bool init = false;

    public VariableDeclaration(AST.Node node)
    {
        this.varDec = (AST.VariableDeclaration)node;
    }

    public override void generate()
    {
        if (varDec.generated) { return; }
        // if (varDec.type.value == "string")
        // {
        //     buildGlobalString();
        //     return;
        // }

        LLVMValueRef valRef = new LLVMValueRef();

        if (this.varDec.defaultValue.nodeType != AST.Node.NodeType.NullExpression)
        {
            this.varDec.defaultValue.generator.generate();
            valRef = valueStack.Pop();
            init = true;
        }

        DebugConsole.Write(this.varDec.type.value);
        this.varDec.type.generator.generate();
        typeLLVM = typeStack.Pop();
        DebugConsole.WriteAnsi($"[red] type stack[/]");
        DebugConsole.Write(typeLLVM);

        if (!varDec.mutable && typeLLVM.TypeKind != LLVMTypeKind.LLVMStructTypeKind)
        {
            LLVMValueRef constRef = LLVM.AddGlobal(module, typeLLVM, varDec.name);
            if (init)
            {
                LLVM.SetInitializer(constRef, valRef);
            }
            valueStack.Push(constRef);
        }
        else
        {
            if (!mainBuilt)
            {
                DebugConsole.Write("adding to main nodes to build");
                nodesToBuild.Add(varDec);
                return;
            }
            LLVM.PositionBuilderAtEnd(builder, mainEntryBlock);
            DebugConsole.Write($"building for mutable var with name of {varDec.name} and type of");
            DebugConsole.DumpType(typeLLVM);
            DebugConsole.Write();
            LLVMValueRef allocaRef = LLVM.BuildAlloca(builder, typeLLVM, varDec.name);
            valueStack.Push(allocaRef);
            DebugConsole.Write("built and pushed alloca: " + allocaRef);
            if (init)
            {
                DebugConsole.Write("store ref target: " + valRef);
                LLVMValueRef storeRef = LLVM.BuildStore(builder, valRef, allocaRef);
                valueStack.Push(storeRef);
                DebugConsole.Write("built and pushed store ref: " + storeRef);
            }

            namedMutablesLLVM.Add(varDec.name, allocaRef);
        }

        DebugConsole.Write("adding var to named globals with name of" + varDec.name);
        namedValuesAST.Add(varDec.name, varDec);
    }
}
