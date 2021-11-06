using System;
using DotsNav.Collections;
using Unity.Collections;
using Unity.Mathematics;

namespace DotsNav.PathFinding
{
    struct Funnel
    {
        readonly Deque<Node> _funnel;
        public bool IsCreated => _funnel.IsCreated;
        Node _apex;
        Deque<Node> _output;
        float _radius;

        public Funnel(int capacity, Allocator allocator) : this()
        {
            _funnel = new Deque<Node>(capacity, allocator);
        }

        public void GetPath(List<Gate> channel, float2 start, float2 goal, float radius, Deque<Node> output)
        {
            _funnel.Clear();
            _radius = radius;
            _output = output;

            if (channel.Length == 0)
            {
                _output.PushBack(new Node(goal, start, goal, NodeType.Point));
                return;
            }

            _apex = new Node(start, start, start, NodeType.Point);
            AddLeft(_apex);
            AddL(channel[0].Left);
            AddR(channel[0].Right);

            for (int i = 1; i < channel.Length; i++)
            {
                Assert.IsTrue(math.all(channel[i].Left == channel[i - 1].Left) != math.all(channel[i].Right == channel[i - 1].Right));

                var toInsert = channel[i];

                var addRight = math.all(toInsert.Left == channel[i - 1].Left);
                var p = addRight ? toInsert.Right : toInsert.Left;

                if (toInsert.IsGoalGate && math.distancesq(_apex.Vertex, goal) < math.distancesq(_apex.Vertex, p))
                    break;

                if (addRight)
                    AddR(p);
                else
                    AddL(p);
            }

            AddP(goal);
        }

        void AddL(float2 v)
        {
            while (true)
            {
                PathSegment(Left.Vertex, v, Left.Type, NodeType.Left, _radius, out var from, out var to);

                var add = math.all(Left.Vertex == Right.Vertex);

                if (!add)
                {
                    var node = math.all(Left.Vertex == _apex.Vertex) ? FromLeft(1) : Left;
                    add = Math.Angle(node.To - node.From, to - from) <= 0;
                }

                if (add)
                {
                    AddLeft(new Node(v, from, to, NodeType.Left));
                    return;
                }

                if (math.all(Left.Vertex == _apex.Vertex))
                {
                    AddToPath(_apex);

                    var f = FromLeft(1).From;
                    var t = FromLeft(1).To;
                    if (math.distancesq(from, to) < math.distancesq(f, t) && DistanceToLineSq(f, t, to) <= Math.Square(_radius))
                    {
                        PopLeft();

                        var toReplace = PopLeft();
                        PathSegment(v, toReplace.Vertex, NodeType.Left, NodeType.Right, _radius, out var from1, out var to1);
                        AddLeft(new Node(toReplace.Vertex, from1, to1, NodeType.Right));

                        AddLeft(new Node(v, from, to, NodeType.Left));
                        _apex = Left;
                        return;
                    }

                    _apex = FromLeft(1);
                }

                PopLeft();
            }
        }

        void AddR(float2 v, NodeType type = NodeType.Right)
        {
            while (true)
            {
                PathSegment(Right.Vertex, v, Right.Type, type, _radius, out var from, out var to);

                var add = math.all(Right.Vertex == Left.Vertex);

                if (!add)
                {
                    var node = math.all(Right.Vertex == _apex.Vertex) ? FromRight(1) : Right;
                    add = Math.Angle(node.To - node.From, to - from) >= 0;
                }

                if (add)
                {
                    AddRight(new Node(v, from, to, type));
                    return;
                }

                if (math.all(Right.Vertex == _apex.Vertex))
                {
                    AddToPath(_apex);

                    var f = FromRight(1).From;
                    var t = FromRight(1).To;
                    if (math.distancesq(from, to) < math.distancesq(f, t) && DistanceToLineSq(f, t, to) <= Math.Square(_radius))
                    {
                        PopRight();

                        var toReplace = PopRight();
                        PathSegment(v, toReplace.Vertex, NodeType.Right, NodeType.Left, _radius, out var from1, out var to1);
                        AddRight(new Node(toReplace.Vertex, from1, to1, NodeType.Left));

                        AddRight(new Node(v, from, to, type));
                        _apex = Right;
                        return;
                    }

                    _apex = FromRight(1);
                }

                PopRight();
            }
        }

        static float DistanceToLineSq(double2 a, double2 b, double2 p)
        {
            var ap = p - a;
            var ab = b - a;
            var f = math.dot(ap, ab) / math.lengthsq(ab);
            return (float) math.distancesq(p, a + ab * f);
        }

        void AddP(float2 v)
        {
            AddR(v, NodeType.Point);

            for (var i = 0; i < _funnel.Count; i++)
            {
                if (math.all(FromLeft(i).Vertex == _apex.Vertex))
                {
                    for (; i < _funnel.Count; i++)
                        AddToPath(FromLeft(i));
                    break;
                }
            }

            _output.PopFront();
        }

        void AddToPath(Node node)
        {
            while (_output.Count > 1 && (_output.Back.Type == NodeType.Left) != Math.Ccw(_output.Back.From, _output.Back.To, node.To))
            {
                _output.PopBack();
                PathSegment(_output.Back.Vertex, node.Vertex, _output.Back.Type, node.Type, _radius, out var from, out var to);
                node.To = to;
                node.From = from;
            }

            _output.PushBack(node);
        }

        static void PathSegment(float2 v0, float2 v1, NodeType t0, NodeType t1, float r, out float2 from, out float2 to)
        {
            switch (t0)
            {
                case NodeType.Point:
                    from = v0;
                    switch (t1)
                    {
                        case NodeType.Point:
                            to = v1;
                            return;
                        case NodeType.Left:
                            to = Math.GetTangentRight(v0, v1, r);
                            return;
                        case NodeType.Right:
                            to = Math.GetTangentLeft(v0, v1, r);
                            return;
                    }

                    break;
                case NodeType.Left:
                    switch (t1)
                    {
                        case NodeType.Point:
                            to = v1;
                            from = Math.GetTangentLeft(v1, v0, r);
                            return;
                        case NodeType.Left:
                            Math.GetOuterTangentRight(v0, v1, r, out from, out to);
                            return;
                        case NodeType.Right:
                            Math.GetInnerTangentRight(v0, v1, r, out from, out to);
                            return;
                    }

                    break;
                case NodeType.Right:
                    switch (t1)
                    {
                        case NodeType.Point:
                            to = v1;
                            from = Math.GetTangentRight(v1, v0, r);
                            return;
                        case NodeType.Left:
                            Math.GetInnerTangentLeft(v0, v1, r, out from, out to);
                            return;
                        case NodeType.Right:
                            Math.GetOuterTangentLeft(v0, v1, r, out from, out to);
                            return;
                    }

                    break;
            }

            throw new ArgumentOutOfRangeException();
        }

        void AddLeft(Node v) => _funnel.PushFront(v);
        void AddRight(Node v) => _funnel.PushBack(v);
        Node PopLeft() => _funnel.PopFront();
        Node PopRight() => _funnel.PopBack();
        Node FromLeft(int index) => _funnel.FromFront(index);
        Node FromRight(int index) => _funnel.FromBack(index);
        Node Left => _funnel.Front;
        Node Right => _funnel.Back;

        public void Clear()
        {
            _funnel.Clear();
        }
    
        public void Dispose()
        {
            _funnel.Dispose();
        }

        public struct Node
        {
            public readonly float2 Vertex;
            public float2 From;
            public float2 To;
            public readonly NodeType Type;

            public float Length => math.length(To - From);
        
            public Node(float2 vertex, float2 from, float2 to, NodeType type)
            {
                Vertex = vertex;
                From = from;
                To = to;
                Type = type;
            }

            public override string ToString() => $"{Type} node at {Vertex}";
        }

        public enum NodeType
        {
            Point,
            Left,
            Right
        }
    }
}