namespace AST;

using System.Collections.Generic;

public abstract class Node
{
    public Generator.Base generator;

    public Parser parser;

    private Parser parentParser;

    public List<Node> children = new List<Node>();
    public Node? parent = null;

    public int line = 0;
    public int column = 0;
    public int charNum = 0;

    public bool isExpression = false;

    public string codeExcerpt = "";

    public NodeType nodeType = NodeType.Unknown;

    public bool newLineReset = false;

    protected Node(Util.Token token)
    {
        this.charNum = token.charNum;
        this.codeExcerpt = token.value;
        this.line = token.line;
        this.column = token.column;
        parser = Parser.getInstance();
        parentParser = parser.parentParser;
    }

    protected Node(Node node)
    {
        this.line = node.line;
        this.column = node.column;
        this.charNum = node.charNum;
        this.codeExcerpt = node.codeExcerpt;
        parser = Parser.getInstance();
        parentParser = parser.parentParser;
    }

    public enum NodeType
    {
        Unknown,
        Struct,
        IndexReference,
        VariableExpression,
        NumberExpression,
        BinaryExpression,
        VariableAssignment,
        VariableDeclaration,
        Prototype,
        Function,
        FunctionCall,
        BuiltinCall,
        StringExpression,
        Type,
        IfStatement,
        IfStatementDeclaration,
        ElseStatement,
        ForLoop,
        WhileLoop,
        PhiVariable,
        ImportStatement,
        Return,
        ArrayExpression,
        NullExpression,
        ExternStatement,
        Reference,
        Dereference,
    }

    public virtual void addSpace(Util.Token space)
    {
        this.codeExcerpt += space.value;
    }

    public virtual void addParent(Node parent)
    {
        if (this.parent != null)
        {
            this.parent.removeChild(this);
        }
        if (parent != null)
        {
            parser.nodes.Remove(this);
            this.parent = parent;
        }
    }

    public virtual void addCode(Util.Token code)
    {
        this.codeExcerpt += code.value;
    }

    public virtual void addCode(string code)
    {
        this.codeExcerpt += code;
    }

    public virtual void addNL()
    {
        this.codeExcerpt += "\n";
    }

    public virtual void addChild(Node child)
    {
        // this.codeExcerpt += child.codeExcerpt;
        children.Add(child);
    }

    public virtual void addChild(Util.Token child)
    {
        // this.codeExcerpt += child.value;
    }

    public virtual void removeChild(Node child)
    {
        this.codeExcerpt.Replace(child.codeExcerpt, "");
        children.Remove(child);
    }

    public virtual void checkExport()
    {
        //TODO: add public and private here
        if (parent == null)
        {
            this.parentParser.nodes.Add(this);
        }
    }
}
