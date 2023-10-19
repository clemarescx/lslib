namespace LSLibLite.LS;

public class Node
{
    #region Members

    public readonly Dictionary<string, List<Node>> Children = new();
    public readonly Dictionary<string, NodeAttribute> Attributes = new();
    public Node? Parent;
    public string? Name;

    #endregion

    #region Properties

    public int ChildCount => Children.Select(c => c.Value.Count).Sum();

    #endregion

    public void AppendChild(Node? child)
    {
        if (!Children.TryGetValue(child.Name, out var children))
        {
            children = new List<Node>();
            Children.Add(child.Name, children);
        }

        children.Add(child);
    }
}