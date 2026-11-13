namespace SmartX.Shared.DataStructures.Heaps;

/// <summary>
/// Represents a generic min-heap data structure that maintains the smallest element at the root. The heap is implemented using a list and supports insertion and extraction of the minimum element while preserving the heap property.
/// </summary>
/// <typeparam name="T"></typeparam>
public class MinHeap<T> where T : IComparable<T>
{
    private readonly List<T> _items = [];
    /// <summary>
    /// Gets the number of items currently in the heap.
    /// </summary>
    public int Count => _items.Count;
    /// <summary>
    /// Inserts a new item into the heap while maintaining the min-heap property.
    /// </summary>
    /// <param name="item"></param>
    public void Insert(T item)
    {
        _items.Add(item);
        SiftUp(_items.Count - 1);
    }
    /// <summary>
    /// Attempts to remove and return the smallest item from the heap. If the heap is empty, it returns false and sets the output item to its default value.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool TryExtractMin(out T item)
    {
        if (_items.Count == 0)
        {
            item = default!;
            return false;
        }

        item = _items[0];

        var lastIndex = _items.Count - 1;
        var lastItem = _items[lastIndex];

        _items.RemoveAt(lastIndex);

        if (_items.Count > 0)
        {
            _items[0] = lastItem;
            SiftDown(0);
        }

        return true;
    }
    /// <summary>
    /// Restores the min-heap property by moving an item up toward the root.
    /// </summary>
    /// <param name="index"></param>
    private void SiftUp(int index)
    {
        while (index > 0)
        {
            var parentIndex = (index - 1) / 2;

            if (_items[index].CompareTo(_items[parentIndex]) >= 0)
            {
                break;
            }

            (_items[index], _items[parentIndex]) =
                (_items[parentIndex], _items[index]);

            index = parentIndex;
        }
    }
    /// <summary>
    /// Restores the min-heap property by moving an item down toward the leaves.
    /// </summary>
    /// <param name="index"></param>
    private void SiftDown(int index)
    {
        while (true)
        {
            var leftChild = index * 2 + 1;
            var rightChild = index * 2 + 2;
            var smallest = index;

            if (leftChild < _items.Count &&
                _items[leftChild].CompareTo(_items[smallest]) < 0)
            {
                smallest = leftChild;
            }

            if (rightChild < _items.Count &&
                _items[rightChild].CompareTo(_items[smallest]) < 0)
            {
                smallest = rightChild;
            }

            if (smallest == index)
            {
                break;
            }

            (_items[index], _items[smallest]) =
                (_items[smallest], _items[index]);

            index = smallest;
        }
    }
}