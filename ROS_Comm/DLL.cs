// File: DLL.cs
// Project: ROS_C-Sharp
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 04/28/2015
// Updated: 02/10/2016

#region USINGZ

using System;

#endregion

//Generic double-linked list
//Eric McCann 2014

namespace Ros_CSharp
{
    public delegate bool DLLInsertionPoint<T>(T newNode, T toInsertAfter);

    public delegate DLLNode<T> DLLClosestNode<T>(DLLNode<T> candidateOne, DLLNode<T> candidateTwo);

    public class DLL<T>
    {
        private DLLNode<T> _first, _last;
        private UInt64 count;

        public DLL()
        {
            _first = new DLLNode<T>();
            _last = new DLLNode<T>();
            _first.next = _last;
            _last.previous = _first;
        }

        public UInt64 Count
        {
            get { return count; }
        }

        public T this[UInt64 key]
        {
            get
            {
                if (key >= count || key < 0)
                    throw new Exception("WTF?!");
                bool forwards = (key - (count/2) > 0);
                UInt64 c = forwards ? 0 : count - 1;
                lock (this)
                    for (DLLNode<T> curr = forwards ? _first.next : _last.previous; forwards ? curr != _last : curr != _first; curr = walk(forwards, curr))
                    {
                        if (c == key)
                        {
                            return curr.element;
                        }
                        if (forwards)
                            c++;
                        else
                            c--;
                    }
                return default(T);
            }
            set
            {
                if (key >= count || key < 0)
                    throw new Exception("WTF?!");
                bool forwards = (key - (count/2) < 0);
                UInt64 c = forwards ? 0 : count - 1;
                DLLNode<T> newnode;
                lock (this)
                {
                    for (DLLNode<T> curr = forwards ? _first.next : _last.previous; forwards ? curr != _last : curr != _first; curr = walk(forwards, curr))
                    {
                        if (c == key)
                        {
                            newnode = new DLLNode<T>(value);
                            curr.next.previous = newnode;
                            curr.next = newnode;
                            newnode.next = curr.next;
                            newnode.previous = curr;
                            count++;
                            return;
                        }
                        if (forwards)
                            c++;
                        else
                            c--;
                    }
                    newnode = new DLLNode<T>(value);
                    _last.previous.next = newnode;
                    _last.previous = newnode;
                    newnode.previous = _last.previous;
                    newnode.next = _last;
                    count++;
                }
            }
        }

        public T Front
        {
            get { lock (this) return _first.next.element; }
        }

        public T Back
        {
            get { lock (this) return _last.previous.element; }
        }

        private DLLNode<T> walk(bool forwards, DLLNode<T> node)
        {
            return forwards ? node.next : node.previous;
        }

        public void pushFront(T t)
        {
            lock (this)
            {
                DLLNode<T> newnode = new DLLNode<T>(t);
                _first.next.previous = newnode;
                newnode.next = _first.next;
                newnode.previous = _first;
                _first.next = newnode;
                count++;
            }
        }

        public void pushBack(T t)
        {
            lock (this)
            {
                DLLNode<T> newnode = new DLLNode<T>(t);
                newnode.next = _last;
                newnode.previous = _last.previous;
                _last.previous.next = newnode;
                _last.previous = newnode;
                count++;
            }
        }

        public void insert(T t, DLLInsertionPoint<T> comp)
        {
            lock (this)
            {
                for (DLLNode<T> curr = _first.next; curr != _last; curr = curr.next)
                {
                    if (comp(t, curr.element))
                    {
                        DLLNode<T> newnode = new DLLNode<T>(t);
                        curr.next.previous = newnode;
                        newnode.next = curr.next;
                        newnode.previous = curr;
                        curr.next = newnode;
                        count++;
                        return;
                    }
                }
                pushBack(t);
                count++;
            }
        }

        private T _popFront()
        {
            if (_first.next == _last) return default(T);
            DLLNode<T> deadnode = _first.next;
            _first.next = deadnode.next;
            deadnode.next.previous = _first;
            count--;
            return deadnode.element;
        }

        public T popFront()
        {
            lock (this)
                return _popFront();
        }

        private T _popBack()
        {
            if (_last.previous == _first) return default(T);
            DLLNode<T> deadnode = _last.previous;
            _last.previous = deadnode.previous;
            deadnode.previous.next = _last;
            count--;
            return deadnode.element;
        }

        public T popBack()
        {
            lock (this)
                return _popBack();
        }

        public T spliceOut(T spliceout)
        {
            lock (this)
            {
                for (DLLNode<T> curr = _first.next; curr != _last; curr = curr.next)
                {
                    if (curr.element.Equals(spliceout))
                    {
                        DLLNode<T> deadnode = curr;
                        deadnode.previous.next = deadnode.next;
                        deadnode.next.previous = deadnode.previous;
                        count--;
                        return deadnode.element;
                    }
                }
            }
            return default(T);
        }

        public T spliceOut(DLLClosestNode<T> comp)
        {
            lock (this)
            {
                for (DLLNode<T> curr = _first.next; curr != _last; curr = curr.next)
                {
                    DLLNode<T> deadnode = null;
                    if ((deadnode = comp(curr, curr.next)) != null)
                    {
                        deadnode.previous.next = deadnode.next;
                        deadnode.next.previous = deadnode.previous;
                        count--;
                        return deadnode.element;
                    }
                }
            }
            return default(T);
        }

        public void Clear()
        {
            lock (this)
                while (_popFront() != null)
                {
                }
            count = 0;
        }
    }

    public class DLLNode<T>
    {
        public T element = default(T);
        public DLLNode<T> next;
        public DLLNode<T> previous;

        public DLLNode()
        {
        }

        public DLLNode(T t)
        {
            element = t;
        }
    }
}