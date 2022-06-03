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
        Divide,
        Equals,
    }

    public BinaryExpression(Util.Token token, ASTNode? previousNode, Util.Token nextToken, ASTNode? parent) : base(token)

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
            case "==":
                this.operatorType = OperatorType.Equals;
                break;
            default:
                throw new ParserException("op " + token.value + " is not a valid binary operator", token);
        }

        this.parent = parent;
        if (this.parent != null)
        {
            if (this.parent.nodeType == NodeType.Function)
            {
                FunctionAST prevFunc = (FunctionAST)parent;
                Parser.checkNode(prevFunc.body.Last(), Parser.binaryExpectedNodes);
                this.leftHand = prevFunc.body.Last();
            }
            else if (this.parent.nodeType == NodeType.FunctionCall)
            {
                FunctionCall prevCall = (FunctionCall)parent;
                Parser.checkNode(prevCall.args.Last(), Parser.binaryExpectedNodes);
                this.leftHand = prevCall.args.Last();
            }
            else if (this.parent.nodeType == NodeType.IfStatement)
            {
                IfStatement ifStat = (IfStatement)parent;
                this.leftHand = ifStat.children.Last();
                ifStat.children.RemoveAt(ifStat.children.Count - 1);
            }
            else
            {
                Parser.checkNode(previousNode, Parser.binaryExpectedNodes);
                this.leftHand = previousNode;
            }
        }
        if (this.leftHand.nodeType == ASTNode.NodeType.NumberExpression && this.leftHand.nodeType == NodeType.VariableExpression)
        {
            this.leftHand.addParent(this);
        }
        else if (parent == null && this.leftHand.nodeType == NodeType.BinaryExpression)
        {
            this.parent = this.leftHand;
        }


        // this.rightHand = new NumberExpression(checkToken(nextToken, Util.tokenType.number), this);

        if (this.parent == null)
        {
            //NOTE: - commented out below code is to throw in an anonymous function 
            // PrototypeAST proto = new PrototypeAST();
            // FunctionAST func = new FunctionAST(proto, this);
            Parser.nodes.Add(this);
        }
        else
        {
            this.parent.addChild(this);
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
