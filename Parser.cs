// to decide where everything goes in the final AST: assign every node type a "importance/level" value and then loop through all the ASTNodes and assign all the nodes from the highest level to the topAST primaryChildren?
using System.Text;

public static class Parser
{
    public static List<ASTNode> nodes = new List<ASTNode>();
    public static List<Util.Token> tokenList;

    public static Util.TokenType[] binaryExpectedTokens = { Util.TokenType.Number };
    public static ASTNode.NodeType[] binaryExpectedNodes = { ASTNode.NodeType.NumberExpression, ASTNode.NodeType.BinaryExpression };

    public static class topAST
    {
        public static List<ASTNode> primaryChildren = new List<ASTNode>();
    }


    public abstract class ASTNode
    {
        public List<ASTNode> children = new List<ASTNode>();
        public ASTNode? parent;

        public NodeType nodeType;

        public enum NodeType
        {
            NumberExpression,
            BinaryExpression,
            Prototype,
            Function
        }

        public virtual void addParent(ASTNode parent)
        {
            this.parent = parent;
            if (this.parent != null)
            {
                nodes.Remove(this);
            }
        }

        public virtual void addChild(ASTNode child)
        {
            children.Add(child);
        }
    }

    public class PrototypeAST : ASTNode
    {
        string? name;
        List<string>? arguments;

        public PrototypeAST(string? name = null, List<string>? arguments = null)
        {
            this.nodeType = NodeType.Prototype;
            this.name = name;
            this.arguments = arguments;

        }
    }

    public class FunctionAST : ASTNode
    {
        public PrototypeAST prototype;
        public List<ASTNode> body;


        public FunctionAST(PrototypeAST prototype, List<ASTNode>? body = null)
        {
            this.nodeType = NodeType.Function;
            this.prototype = prototype;
            this.body = body != null ? body : new List<ASTNode>();
        }

        public FunctionAST(PrototypeAST prototype, ASTNode body)
        {
            this.nodeType = NodeType.Function;
            this.prototype = prototype;
            this.body = new List<ASTNode>();
            this.body.Add(body);
        }

    }


    public class NumberExpression : ASTNode
    {
        public double value;

        public NumberExpression(Util.Token token, ASTNode? parent)
        {
            this.value = Double.Parse(token.value);
            this.parent = parent;

            if (parent != null)
            {
                this.parent.addChild(this);
            }
            else
            {
                nodes.Add(this);
            }
        }

    }


    public class BinaryExpression : ASTNode
    {
        public ASTNode leftHand;
        public ASTNode? rightHand;
        public OperatorType operatorType;

        public enum OperatorType
        {
            Add,
            Subtract,
            Multiply,
            Divide
        }

        public BinaryExpression(Util.Token token, ASTNode? previousNode, Util.Token nextToken, ASTNode? parent)
        {
            //TODO: implement operator precedence parsing
            this.nodeType = NodeType.BinaryExpression;
            switch (token.value)
            {
                case "+":
                    this.operatorType = OperatorType.Add;
                    break;
                case "-":
                    this.operatorType = OperatorType.Subtract;
                    break;
                case "*":
                    this.operatorType = OperatorType.Multiply;
                    break;
                case "/":
                    this.operatorType = OperatorType.Divide;
                    break;
                default:
                    throw new ArgumentException("op " + token.value + " is not a valid operator");
            }

            this.parent = parent;

            checkNode(previousNode, binaryExpectedNodes);

            this.leftHand = previousNode;

            if (this.leftHand.parent == null && this.leftHand.nodeType == ASTNode.NodeType.NumberExpression)
            {
                this.leftHand.addParent(this);
            }
            else if (parent == null)
            {
                this.parent = previousNode;
            }


            // this.rightHand = new NumberExpression(checkToken(nextToken, Util.tokenType.number), this);


            if (this.parent != null)
            {
                this.parent.addChild(this);
            }
            else
            {
                //TODO: add the creation of an anonymous function for the binary expression here
                PrototypeAST proto = new PrototypeAST();
                FunctionAST func = new FunctionAST(proto, this);
                nodes.Add(func);
            }
        }

        public override void addChild(ASTNode child)
        {
            this.children.Add(child);
            if (child.nodeType == ASTNode.NodeType.BinaryExpression)
            {
            }
            else
            {
                this.rightHand = child;
            }
        }
    }

    public static void checkNode(ASTNode? node, ASTNode.NodeType[] expectedTypes)
    {
        if (node == null)
        {
            throw new ArgumentException($"expected a node at (line and column goes here) but got null");
        }
        else if (node.nodeType == ASTNode.NodeType.Function)
        {
            FunctionAST func = (FunctionAST)node;
            node = func.body.Last();
        }
        foreach (ASTNode.NodeType expectedNodeType in expectedTypes)
        {
            if (node.nodeType != expectedNodeType && expectedNodeType == expectedTypes.Last())
            {
                throw new ArgumentException($"expected type {string.Join(", ", expectedTypes)} but got {node.nodeType}");
            }
            else if (node.nodeType == expectedNodeType)
            {
                break;
            }
        }
    }

    public static void checkToken(Util.Token? token, Util.TokenType[] expectedTypes)
    {
        if (token == null)
        {
            throw new ArgumentException($"expected a token at {token.line}:{token.column} but got null");
        }

        foreach (Util.TokenType expectedTokenType in expectedTypes)
        {
            if (token.type != expectedTokenType && expectedTokenType == expectedTypes.Last())
            {
                throw new ArgumentException($"expected type {string.Join(", ", expectedTypes)} but got {token.type} at {token.line}:{token.column}");
            }
            else if (token.type == expectedTokenType)
            {
                break;
            }
        }
    }

    public static string printAST()
    {
        StringBuilder stringBuilder = new StringBuilder();

        foreach (ASTNode node in nodes)
        {
            stringBuilder.Append(node.nodeType);
            stringBuilder.Append("\n");
        }

        return stringBuilder.ToString();
    }

    public static bool parseToken(Util.Token token, int tokenIndex, ASTNode? parent = null, Util.TokenType[]? expectedTypes = null)
    {

        ASTNode? previousNode = nodes.Count > 0 ? nodes.Last() : null;

        Console.WriteLine($"parse loop {tokenIndex}: {printAST()}");
        if (token.type == Util.TokenType.EOF)
        {
            return true;
        }

        if (expectedTypes != null)
        {
            checkToken(token, expectedTypes);
        }

        switch (token.type)
        {
            case Util.TokenType.Number:
                new NumberExpression(token, parent);
                break;
            case Util.TokenType.Operator:
                BinaryExpression binExpr = new BinaryExpression(token, previousNode, tokenList[tokenIndex + 1], parent);
                return parseToken(tokenList[tokenIndex + 1], tokenIndex + 1, binExpr, binaryExpectedTokens);

        }
        return parseToken(tokenList[tokenIndex + 1], tokenIndex + 1);

    }

    public static List<ASTNode> beginParse(List<Util.Token> _tokenList)
    {
        tokenList = _tokenList;
        parseToken(tokenList[0], 0);

        Console.WriteLine("node types below");
        Console.WriteLine(printAST());

        return nodes;
    }

}