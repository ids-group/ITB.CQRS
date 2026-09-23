using System.Linq.Expressions;

namespace ITB.Specification;

internal class SwapVisitor(Expression from, Expression to) : ExpressionVisitor
{
    private readonly Expression _from = from, _to = to;

    public override Expression Visit(Expression node)
    {
        return node == _from ? _to : base.Visit(node);
    }
}
