namespace SmartX.Shared.DataStructures.Trees;

/// <summary>
/// Represents a self-balancing binary search tree (AVL tree) that maintains sorted key/value pairs. The tree automatically balances itself after insertions to ensure that the height difference between the left and right subtrees of any node is at most one, providing efficient search, insertion, and deletion operations.
/// 
/// https://www.geeksforgeeks.org/dsa/introduction-to-avl-tree/
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public class AvlTree<TKey, TValue> where TKey : IComparable<TKey>
{
    private sealed class Node
    {
        public TKey Key { get; }
        public TValue Value { get; set; }
        public Node? Left { get; set; }
        public Node? Right { get; set; }
        public int Height { get; set; } = 1;

        public Node(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }

    private Node? _root;

    /// <summary>
    /// Gets the number of key/value pairs currently stored in the tree.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Inserts a key/value pair into the tree. If the key already exists, its value is updated. The tree is automatically balanced after the insertion to maintain the AVL property.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void Insert(TKey key, TValue value)
    {
        _root = Insert(_root, key, value);
    }
    /// <summary>
    /// Recursively inserts a key/value pair into the subtree rooted at the specified node. If the node is null, a new node is created. If the key already exists, its value is updated. After insertion, the subtree is balanced to maintain the AVL property.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    private Node Insert(Node? node, TKey key, TValue value)
    {
        if (node is null)
        {
            Count++;
            return new Node(key, value);
        }

        var comparison = key.CompareTo(node.Key);

        if (comparison < 0)
        {
            node.Left = Insert(node.Left, key, value);
        }
        else if (comparison > 0)
        {
            node.Right = Insert(node.Right, key, value);
        }
        else
        {
            node.Value = value;
            return node;
        }

        UpdateHeight(node);

        return Balance(node);
    }
    /// <summary>
    /// Attempts to retrieve the value associated with the specified key. If the key exists in the tree, the method returns true and outputs the corresponding value; otherwise, it returns false and outputs the default value for TValue.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool TryGetValue(TKey key, out TValue value)
    {
        var current = _root;

        while (current is not null)
        {
            var comparison = key.CompareTo(current.Key);

            if (comparison == 0)
            {
                value = current.Value;
                return true;
            }

            current = comparison < 0
                ? current.Left
                : current.Right;
        }

        value = default!;
        return false;
    }
    /// <summary>
    /// Returns all key/value pairs within the specified range of keys, inclusive. The method performs an in-order traversal of the tree and collects entries whose keys fall between the specified 'from' and 'to' values. If 'from' is greater than 'to', an empty list is returned.
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    public IReadOnlyList<KeyValuePair<TKey, TValue>> Range(
        TKey from,
        TKey to)
    {
        var results = new List<KeyValuePair<TKey, TValue>>();

        if (from.CompareTo(to) > 0)
        {
            return results;
        }

        CollectRange(_root, from, to, results);

        return results;
    }
    /// <summary>
    /// Recursively collects all key/value pairs within the specified range of keys from the subtree rooted at the given node. The method performs an in-order traversal and adds entries to the results list if their keys fall between 'from' and 'to', inclusive.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="results"></param>
    private static void CollectRange(
        Node? node,
        TKey from,
        TKey to,
        List<KeyValuePair<TKey, TValue>> results)
    {
        if (node is null)
        {
            return;
        }

        if (node.Key.CompareTo(from) > 0)
        {
            CollectRange(node.Left, from, to, results);
        }

        if (node.Key.CompareTo(from) >= 0 &&
            node.Key.CompareTo(to) <= 0)
        {
            results.Add(
                new KeyValuePair<TKey, TValue>(
                    node.Key,
                    node.Value));
        }

        if (node.Key.CompareTo(to) < 0)
        {
            CollectRange(node.Right, from, to, results);
        }
    }
    /// <summary>
    /// Returns all key/value pairs in the tree in sorted order based on their keys. The method performs an in-order traversal of the tree and collects entries into a list, which is then returned as a read-only collection.
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<KeyValuePair<TKey, TValue>> InOrder()
    {
        var results = new List<KeyValuePair<TKey, TValue>>();
        TraverseInOrder(_root, results);
        return results;
    }
    /// <summary>
    /// Recursively traverses the subtree rooted at the specified node in in-order fashion and collects all key/value pairs into the provided results list. The method visits the left subtree, then the current node, and finally the right subtree, ensuring that entries are added to the list in sorted order based on their keys.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="results"></param>
    private static void TraverseInOrder(
        Node? node,
        List<KeyValuePair<TKey, TValue>> results)
    {
        if (node is null)
        {
            return;
        }

        TraverseInOrder(node.Left, results);

        results.Add(
            new KeyValuePair<TKey, TValue>(
                node.Key,
                node.Value));

        TraverseInOrder(node.Right, results);
    }
    /// <summary>
    /// Attempts to retrieve the key/value pair with the maximum key in the tree. If the tree is not empty, the method returns true and outputs the entry with the largest key; otherwise, it returns false and outputs the default value for KeyValuePair<TKey, TValue>.
    /// </summary>
    /// <param name="entry"></param>
    /// <returns></returns>
    public bool TryGetMaximum(
        out KeyValuePair<TKey, TValue> entry)
    {
        if (_root is null)
        {
            entry = default;
            return false;
        }

        var current = _root;

        while (current.Right is not null)
        {
            current = current.Right;
        }

        entry = new KeyValuePair<TKey, TValue>(current.Key, current.Value);

        return true;
    }
    /// <summary>
    /// Clears all key/value pairs from the tree, effectively resetting it to an empty state. The root node is set to null, and the count of entries is reset to zero.
    /// </summary>
    public void Clear()
    {
        _root = null;
        Count = 0;
    }
    /// <summary>
    /// Returns the height of the specified node. If the node is null, the method returns 0; otherwise, it returns the height property of the node. The height is used to determine the balance factor of nodes during insertion and balancing operations.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    private static int GetHeight(Node? node)
    {
        return node?.Height ?? 0;
    }
    /// <summary>
    /// Updates the height of the specified node based on the heights of its left and right children. The height is calculated as one plus the maximum height of the left and right subtrees. This method is called after insertions and rotations to ensure that the height property of each node remains accurate for balancing purposes.
    /// </summary>
    /// <param name="node"></param>
    private static void UpdateHeight(Node node)
    {
        node.Height =
            1 + Math.Max(
                GetHeight(node.Left),
                GetHeight(node.Right));
    }

    private static int GetBalanceFactor(Node node)
    {
        return GetHeight(node.Left) -
               GetHeight(node.Right);
    }
    /// <summary>
    /// Balances the subtree rooted at the specified node to maintain the AVL property. The method calculates the balance factor of the node and performs appropriate rotations (single or double) to ensure that the height difference between the left and right subtrees is at most one. The balanced node is then returned as the new root of the subtree.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    private static Node Balance(Node node)
    {
        var balanceFactor = GetBalanceFactor(node);

        // Left-heavy tree.
        if (balanceFactor > 1)
        {
            if (node.Left is not null &&
                GetBalanceFactor(node.Left) < 0)
            {
                node.Left = RotateLeft(node.Left);
            }

            return RotateRight(node);
        }

        // Right-heavy tree.
        if (balanceFactor < -1)
        {
            if (node.Right is not null &&
                GetBalanceFactor(node.Right) > 0)
            {
                node.Right = RotateRight(node.Right);
            }

            return RotateLeft(node);
        }

        return node;
    }
    /// <summary>
    /// Performs a right rotation on the specified node to balance the subtree. The method promotes the left child of the node to be the new root of the subtree, and the original node becomes the right child of the new root. The heights of the affected nodes are updated accordingly. The new root of the subtree is returned after the rotation.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    private static Node RotateRight(Node node)
    {
        var newRoot = node.Left!;

        node.Left = newRoot.Right;
        newRoot.Right = node;

        UpdateHeight(node);
        UpdateHeight(newRoot);

        return newRoot;
    }
    /// <summary>
    /// Performs a left rotation on the specified node to balance the subtree. The method promotes the right child of the node to be the new root of the subtree, and the original node becomes the left child of the new root. The heights of the affected nodes are updated accordingly. The new root of the subtree is returned after the rotation.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    private static Node RotateLeft(Node node)
    {
        var newRoot = node.Right!;

        node.Right = newRoot.Left;
        newRoot.Left = node;

        UpdateHeight(node);
        UpdateHeight(newRoot);

        return newRoot;
    }
}