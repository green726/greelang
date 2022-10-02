namespace AST;

using System.Collections.Generic;
using System.Text;


public class Prototype : AST.Node
{
    public string name = "";
    public Dictionary<string, Type> arguments = new Dictionary<string, Type>();
    private bool typePredicted = true;
    private Type prevType;

    public Type returnType;

    public bool external = false;

    public bool variableArgument = false;

    public Prototype(Util.Token token, AST.Node parent = null, bool startWithRet = false, bool external = false) : base(token)
    {
        this.nodeType = NodeType.Prototype;
        this.generator = new Generator.Prototype(this);

        this.external = external;

        if (startWithRet == false)
        {
            if (!Config.settings.function.declaration.marker.word)
            {
                DebugConsole.WriteAnsi("[yellow]string name was substringed[/]");
                this.name = token.value.Substring(1);
                DebugConsole.Write("post substring val: " + this.name);
            }
            else
            {
                this.name = "";
            }
            this.returnType = new Type("null", this);
        }
        else
        {
            this.returnType = new Type(token);
        }


        this.arguments = new Dictionary<string, Type>();

        //TODO: replace this node type with external section
        if (external/*  || parent.nodeType == NodeType.BinaryExpression */)
        {
            // Parser.nodes.Add(this);
        }
        if (parent?.nodeType != AST.Node.NodeType.ExternStatement && parent != null)
        {
            throw ParserException.FactoryMethod("A prototype may not have a non-extern parent", "Make the prototype top level", this);
        }
        parent?.addChild(this);
        this.parent = parent;
    }

    //NOTE: addArgs are just extended from the add child - just to seperate handling of other tokens added (like names)
    public void addArg(AST.Node arg)
    {
        if (typePredicted)
        {
            prevType = (AST.Type)arg;
            typePredicted = !typePredicted;
        }
        else
        {
            // throw ParserException.FactoryMethod();
        }
    }

    public void addArg(Util.Token token)
    {
        if (typePredicted)
        {
            prevType = new Type(token);
            typePredicted = !typePredicted;
        }
        else
        {
            // throw ParserException.FactoryMethod();
            arguments.Add(token.value, prevType);
        }
    }

    public string getArgTypes()
    {
        StringBuilder stringBuilder = new StringBuilder();
        foreach (KeyValuePair<string, Type> arg in this.arguments)
        {
            stringBuilder.Append("_" + arg.Value.value);
        }

        return stringBuilder.ToString();
    }

    public void handleOverload()
    {
        if (this.parent?.nodeType != AST.Node.NodeType.ExternStatement)
        {
            string altName = this.name + getArgTypes();
            DebugConsole.WriteAnsi("[red]alt name below[/]");
            DebugConsole.Write(altName);

            Parser.declaredFuncs.Add(altName, this);
            this.name = altName;
        }
        else
        {
            Parser.declaredFuncs.Add(this.name, this);
        }
    }

    public override void addChild(AST.Node child)
    {
        // throw ParserException.FactoryMethod();
        base.addChild(child);
    }

    public override void addChild(Util.Token child)
    {
        if (this.variableArgument == true && child.value != ")")
        {
            throw ParserException.FactoryMethod("Illegal additions to prototype after declaring variable arguments", "Remove the illegal additions or the variable argument declaration", child, this);
        }

        // DebugConsole.WriteAnsi($"[green]adding child to proto with name {name} (type predicted is: " + typePredicted + ") with value: " + child.value + " [/]");
        if (this.name == "")
        {
            if (!Config.settings.function.declaration.marker.word)
            {
                this.name = child.value.Substring(1);
            }
            else
            {
                this.name = child.value;
            }
            return;
        }

        //NOTE: uses commas to handle arg seperation
        if (child.value == ",")
        {
            typePredicted = true;
            prevType = null;
        }
        else if (child.value == "#")
        {
            if (typePredicted == true)
            {
                if (this?.parent?.nodeType != NodeType.ExternStatement)
                {
                    throw ParserException.FactoryMethod("A variable argument prototype was declared outside of an extern statement (variable argument functions are not yet supported - only prototypes for external functions are currently supported)", "Remove the variable argument declaration or place the prototype in an extern statement", child, this);
                }
                this.variableArgument = true;
            }
            else
            {
                throw ParserException.FactoryMethod("Illegal variable argument prototype declaration (\"#\")", "Put the variable argument symbol (...) in a legal location (ie \"printf(int i, #)\") or remove it", child, this);
            }
        }
        else if (child.value == "[" || child.value == "]")
        {
            DebugConsole.Write("array param detected");
            prevType.addChild(child);
            return;
        }
        //TODO: replace this with config delims
        else if (child.value != "(" && child.value != ")")
        {
            addArg(child);
        }
    }
}
