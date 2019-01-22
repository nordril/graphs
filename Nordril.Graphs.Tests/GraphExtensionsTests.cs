using Nordril.Functional;
using Nordril.Functional.Data;
using SD.Tools.Algorithmia.Graphs;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Nordril.Graphs.Tests
{
    using CharGraph = DirectedGraph<char, DirectedEdge<char>>;
    using CharSetGraph = DirectedGraph<IFuncSet<char>, DirectedEdge<IFuncSet<char>>>;

    public class GraphExtensionsTests
    {
        public static IEnumerable<object[]> ShortestPathData()
        {
            //Empty graph
            var g = new CharGraph();
            var paths = new Dictionary<(char, char), Maybe<double>>();

            yield return new object[] { g, paths };

            //One-node graph with no edges
            g = new CharGraph();
            paths = new Dictionary<(char, char), Maybe<double>>();

            g.Add('a');

            paths.Add(('a', 'a'), Maybe.Just(0D));

            yield return new object[] { g, paths };

            //One-node graph with a loop
            g = new CharGraph();
            paths = new Dictionary<(char, char), Maybe<double>>();

            g.Add('a');
            g.Add(new DirectedEdge<char>('a', 'a'));

            paths.Add(('a', 'a'), Maybe.Just(0D));

            yield return new object[] { g, paths };

            //Two-node graph, no cycles
            g = new CharGraph();
            paths = new Dictionary<(char, char), Maybe<double>>();

            g.Add('a');
            g.Add('b');
            g.Add(new DirectedEdge<char>('a', 'b'));

            paths.Add(('a', 'a'), Maybe.Just(0D));
            paths.Add(('b', 'b'), Maybe.Just(0D));
            paths.Add(('a', 'b'), Maybe.Just(2D));
            paths.Add(('b', 'a'), Maybe.Nothing<double>());

            yield return new object[] { g, paths };

            //Five-node graph, no cycles
            g = new CharGraph();
            paths = new Dictionary<(char, char), Maybe<double>>();

            g.Add('a');
            g.Add('b');
            g.Add('c');
            g.Add('d');
            g.Add('e');
            g.Add(new DirectedEdge<char>('a', 'b'));
            g.Add(new DirectedEdge<char>('a', 'c'));
            g.Add(new DirectedEdge<char>('a', 'd'));
            g.Add(new DirectedEdge<char>('b', 'c'));
            g.Add(new DirectedEdge<char>('c', 'e'));

            paths.Add(('a', 'a'), Maybe.Just(0D));
            paths.Add(('b', 'b'), Maybe.Just(0D));
            paths.Add(('c', 'c'), Maybe.Just(0D));
            paths.Add(('d', 'd'), Maybe.Just(0D));
            paths.Add(('e', 'e'), Maybe.Just(0D));

            paths.Add(('a', 'b'), Maybe.Just(2D));
            paths.Add(('a', 'c'), Maybe.Just(2D));
            paths.Add(('a', 'd'), Maybe.Just(2D));
            paths.Add(('a', 'e'), Maybe.Just(4D));

            paths.Add(('b', 'a'), Maybe.Nothing<double>());
            paths.Add(('b', 'c'), Maybe.Just(2D));
            paths.Add(('b', 'd'), Maybe.Nothing<double>());
            paths.Add(('b', 'e'), Maybe.Just(4D));

            paths.Add(('c', 'a'), Maybe.Nothing<double>());
            paths.Add(('c', 'b'), Maybe.Nothing<double>());
            paths.Add(('c', 'd'), Maybe.Nothing<double>());
            paths.Add(('c', 'e'), Maybe.Just(2D));

            paths.Add(('d', 'a'), Maybe.Nothing<double>());
            paths.Add(('d', 'b'), Maybe.Nothing<double>());
            paths.Add(('d', 'c'), Maybe.Nothing<double>());
            paths.Add(('d', 'e'), Maybe.Nothing<double>());

            paths.Add(('e', 'a'), Maybe.Nothing<double>());
            paths.Add(('e', 'b'), Maybe.Nothing<double>());
            paths.Add(('e', 'c'), Maybe.Nothing<double>());
            paths.Add(('e', 'd'), Maybe.Nothing<double>());

            yield return new object[] { g, paths };

            //Two-component graph, 2 + 3 nodes, one cycle
            g = new CharGraph();
            paths = new Dictionary<(char, char), Maybe<double>>();

            g.Add('a');
            g.Add('b');
            g.Add('c');
            g.Add('d');
            g.Add('e');
            g.Add(new DirectedEdge<char>('a', 'b'));
            g.Add(new DirectedEdge<char>('b', 'a'));
            g.Add(new DirectedEdge<char>('c', 'd'));
            g.Add(new DirectedEdge<char>('d', 'e'));

            paths.Add(('a', 'a'), Maybe.Just(0D));
            paths.Add(('b', 'b'), Maybe.Just(0D));
            paths.Add(('c', 'c'), Maybe.Just(0D));
            paths.Add(('d', 'd'), Maybe.Just(0D));
            paths.Add(('e', 'e'), Maybe.Just(0D));

            paths.Add(('a', 'b'), Maybe.Just(2D));
            paths.Add(('a', 'c'), Maybe.Nothing<double>());
            paths.Add(('a', 'd'), Maybe.Nothing<double>());
            paths.Add(('a', 'e'), Maybe.Nothing<double>());

            paths.Add(('b', 'a'), Maybe.Just(2D));
            paths.Add(('b', 'c'), Maybe.Nothing<double>());
            paths.Add(('b', 'd'), Maybe.Nothing<double>());
            paths.Add(('b', 'e'), Maybe.Nothing<double>());

            paths.Add(('c', 'a'), Maybe.Nothing<double>());
            paths.Add(('c', 'b'), Maybe.Nothing<double>());
            paths.Add(('c', 'd'), Maybe.Just(2D));
            paths.Add(('c', 'e'), Maybe.Just(4D));

            paths.Add(('d', 'a'), Maybe.Nothing<double>());
            paths.Add(('d', 'b'), Maybe.Nothing<double>());
            paths.Add(('d', 'c'), Maybe.Nothing<double>());
            paths.Add(('d', 'e'), Maybe.Just(2D));

            paths.Add(('e', 'a'), Maybe.Nothing<double>());
            paths.Add(('e', 'b'), Maybe.Nothing<double>());
            paths.Add(('e', 'c'), Maybe.Nothing<double>());
            paths.Add(('e', 'd'), Maybe.Nothing<double>());

            yield return new object[] { g, paths };
        }

        public static IEnumerable<object[]> MergeMultiEdgesData()
        {
            var g = new CharGraph();
            var g2 = new CharGraph();
            g.Add('a');
            g.Add('b');
            g.Add('c');
            g.Add('d');
            g.Add('e');
            g.Add(new DirectedEdge<char>('a', 'b'));
            g.Add(new DirectedEdge<char>('b', 'a'));
            g.Add(new DirectedEdge<char>('c', 'd'));
            g.Add(new DirectedEdge<char>('d', 'e'));

            g2.Add('a');
            g2.Add('b');
            g2.Add('c');
            g2.Add('d');
            g2.Add('e');
            g2.Add(new DirectedEdge<char>('a', 'b'));
            g2.Add(new DirectedEdge<char>('b', 'a'));
            g2.Add(new DirectedEdge<char>('c', 'd'));
            g2.Add(new DirectedEdge<char>('d', 'e'));
            yield return new object[] { g, g2 };

            g = new CharGraph();
            g2 = new CharGraph();
            g.Add('a');

            g2.Add('a');
            yield return new object[] { g, g2 };

            g = new CharGraph();
            g2 = new CharGraph();
            g.Add('a');
            g.Add('b');

            g2.Add('a');
            g2.Add('b');
            yield return new object[] { g, g2 };

            g = new CharGraph();
            g2 = new CharGraph();
            g.Add('a');
            g.Add(new DirectedEdge<char>('a', 'a'));

            g2.Add('a');
            g2.Add(new DirectedEdge<char>('a', 'a'));
            yield return new object[] { g, g2 };

            g = new CharGraph();
            g2 = new CharGraph();
            g.Add('a');
            g.Add(new DirectedEdge<char>('a', 'a'));
            g.Add(new DirectedEdge<char>('a', 'a'));

            g2.Add('a');
            g2.Add(new DirectedEdge<char>('a', 'a'));
            yield return new object[] { g, g2 };

            g = new CharGraph();
            g2 = new CharGraph();
            g.Add('a');
            g.Add(new DirectedEdge<char>('a', 'a'));
            g.Add(new DirectedEdge<char>('a', 'a'));
            g.Add(new DirectedEdge<char>('a', 'a'));
            g.Add(new DirectedEdge<char>('a', 'a'));

            g2.Add('a');
            g2.Add(new DirectedEdge<char>('a', 'a'));
            yield return new object[] { g, g2 };

            g = new CharGraph();
            g2 = new CharGraph();
            g.Add('a');
            g.Add('b');
            g.Add(new DirectedEdge<char>('a', 'a'));
            g.Add(new DirectedEdge<char>('b', 'a'));

            g2.Add('a');
            g2.Add('b');
            g2.Add(new DirectedEdge<char>('a', 'a'));
            g2.Add(new DirectedEdge<char>('b', 'a'));
            yield return new object[] { g, g2 };

            g = new CharGraph();
            g2 = new CharGraph();
            g.Add('a');
            g.Add('b');
            g.Add(new DirectedEdge<char>('a', 'a'));
            g.Add(new DirectedEdge<char>('b', 'a'));
            g.Add(new DirectedEdge<char>('b', 'a'));

            g2.Add('a');
            g2.Add('b');
            g2.Add(new DirectedEdge<char>('a', 'a'));
            g2.Add(new DirectedEdge<char>('b', 'a'));
            yield return new object[] { g, g2 };

            g = new CharGraph();
            g2 = new CharGraph();
            g.Add('a');
            g.Add('b');
            g.Add('c');
            g.Add('d');
            g.Add('e');
            g.Add(new DirectedEdge<char>('a', 'a'));
            g.Add(new DirectedEdge<char>('b', 'a'));
            g.Add(new DirectedEdge<char>('b', 'c'));
            g.Add(new DirectedEdge<char>('b', 'c'));

            g2.Add('a');
            g2.Add('b');
            g2.Add('c');
            g2.Add('d');
            g2.Add('e');
            g2.Add(new DirectedEdge<char>('a', 'a'));
            g2.Add(new DirectedEdge<char>('b', 'a'));
            g2.Add(new DirectedEdge<char>('b', 'c'));
            yield return new object[] { g, g2 };
        }

        public static IEnumerable<object[]> GraphEqualData()
        {
            CharGraph makeCopy(CharGraph graph)
            {
                var ret = new CharGraph();

                graph.Vertices.ForEach(ret.Add);
                graph.Edges.ForEach(e => ret.Add(new DirectedEdge<char>(e.StartVertex, e.EndVertex)));

                return ret;
            }

            //true-cases
            var g = new CharGraph();
            yield return new object[] { g, makeCopy(g), true };

            g = new CharGraph();
            g.Add('a');
            yield return new object[] { g, makeCopy(g), true };

            g = new CharGraph();
            g.Add('a');
            g.Add('b');
            yield return new object[] { g, makeCopy(g), true };

            g = new CharGraph();
            g.Add('a');
            g.Add('b');
            g.Add(new DirectedEdge<char>('a', 'b'));
            yield return new object[] { g, makeCopy(g), true };

            g = new CharGraph();
            g.Add('a');
            g.Add('b');
            g.Add(new DirectedEdge<char>('a', 'b'));
            g.Add(new DirectedEdge<char>('b', 'a'));
            yield return new object[] { g, makeCopy(g), true };

            g = new CharGraph();
            g.Add('a');
            g.Add('b');
            g.Add('c');
            g.Add('d');
            g.Add('e');
            g.Add(new DirectedEdge<char>('a', 'b'));
            g.Add(new DirectedEdge<char>('a', 'c'));
            g.Add(new DirectedEdge<char>('a', 'd'));
            g.Add(new DirectedEdge<char>('b', 'c'));
            g.Add(new DirectedEdge<char>('c', 'e'));
            yield return new object[] { g, makeCopy(g), true };

            g = new CharGraph();
            g.Add('a');
            g.Add('b');
            g.Add('c');
            g.Add('d');
            g.Add('e');
            g.Add(new DirectedEdge<char>('a', 'b'));
            g.Add(new DirectedEdge<char>('b', 'a'));
            g.Add(new DirectedEdge<char>('c', 'd'));
            g.Add(new DirectedEdge<char>('d', 'e'));
            yield return new object[] { g, makeCopy(g), true };

            //false-cases
            g = new CharGraph();
            var g2 = new CharGraph();
            g.Add('a');
            g.Add('b');
            g.Add('c');
            g.Add('d');
            g.Add('e');
            g.Add(new DirectedEdge<char>('a', 'b'));
            g.Add(new DirectedEdge<char>('b', 'a'));
            g.Add(new DirectedEdge<char>('c', 'd'));
            g.Add(new DirectedEdge<char>('d', 'e'));

            g2.Add('a');
            g2.Add('b');
            g2.Add('c');
            g2.Add('d');
            g2.Add('e');
            g2.Add(new DirectedEdge<char>('a', 'b'));
            g2.Add(new DirectedEdge<char>('b', 'a'));
            g2.Add(new DirectedEdge<char>('c', 'd'));
            yield return new object[] { g, g2, false };

            g = new CharGraph();
            g2 = new CharGraph();
            g.Add('a');
            g.Add('b');
            g.Add('c');
            g.Add('d');
            g.Add('e');
            g.Add(new DirectedEdge<char>('a', 'b'));
            g.Add(new DirectedEdge<char>('b', 'a'));
            g.Add(new DirectedEdge<char>('c', 'd'));
            g.Add(new DirectedEdge<char>('d', 'e'));

            g2.Add('a');
            g2.Add('b');
            g2.Add('c');
            g2.Add('d');
            g2.Add('e');
            g2.Add('f');
            g2.Add(new DirectedEdge<char>('a', 'b'));
            g2.Add(new DirectedEdge<char>('b', 'a'));
            g2.Add(new DirectedEdge<char>('c', 'd'));
            yield return new object[] { g, g2, false };
        }

        public static IEnumerable<object[]> MergeAncestorsData()
        {
            DirectedEdge<IFuncSet<char>> mkEdge(IEnumerable<char> from, IEnumerable<char> to)
                => new DirectedEdge<IFuncSet<char>>(new FuncSet<char>(from), new FuncSet<char>(to));

            //trees
            //these should remain unchanged
            var g = new CharGraph();
            var g2 = new CharSetGraph();
            yield return new object[] { g, g2 };

            g = new CharGraph();
            g.Add('a');
            g2 = new CharSetGraph();
            g2.Add(new FuncSet<char> { 'a' });
            yield return new object[] { g, g2 };

            g = new CharGraph();
            g.Add('a');
            g.Add('b');
            g2 = new CharSetGraph();
            g2.Add(new FuncSet<char> { 'a' });
            g2.Add(new FuncSet<char> { 'b' });
            yield return new object[] { g, g2 };

            g = new CharGraph();
            g.Add('a');
            g.Add('b');
            g.Add(new DirectedEdge<char>('a', 'b'));
            g2 = new CharSetGraph();
            g2.Add(new FuncSet<char> { 'a' });
            g2.Add(new FuncSet<char> { 'b' });
            g2.Add(mkEdge("a", "b"));
            yield return new object[] { g, g2 };

            g = new CharGraph();
            g.Add('a');
            g.Add('b');
            g.Add('c');
            g.Add('d');
            g.Add('e');
            g.Add('f');
            g.Add('g');
            g.Add(new DirectedEdge<char>('a', 'b'));
            g.Add(new DirectedEdge<char>('a', 'c'));
            g.Add(new DirectedEdge<char>('b', 'd'));
            g.Add(new DirectedEdge<char>('b', 'e'));
            g.Add(new DirectedEdge<char>('c', 'f'));
            g.Add(new DirectedEdge<char>('c', 'g'));
            g2 = new CharSetGraph();
            g2.Add(new FuncSet<char> { 'a' });
            g2.Add(new FuncSet<char>{'b'});
            g2.Add(new FuncSet<char>{'c'});
            g2.Add(new FuncSet<char>{'d'});
            g2.Add(new FuncSet<char>{'e'});
            g2.Add(new FuncSet<char>{'f'});
            g2.Add(new FuncSet<char> { 'g' });
            g2.Add(mkEdge("a", "b"));
            g2.Add(mkEdge("a", "c"));
            g2.Add(mkEdge("b", "d"));
            g2.Add(mkEdge("b", "e"));
            g2.Add(mkEdge("c", "f"));
            g2.Add(mkEdge("c", "g"));
            yield return new object[] { g, g2 };

            //weakly but not strongly cyclic DAGs
            //the algorithm succeeds on these but changes the graph.
            g = new CharGraph();
            g.Add('a');
            g.Add('b');
            g.Add('c');
            g.Add(new DirectedEdge<char>('a', 'b'));
            g.Add(new DirectedEdge<char>('a', 'c'));
            g.Add(new DirectedEdge<char>('b', 'c'));
            g2 = new CharSetGraph();
            g2.Add(new FuncSet<char> { 'a', 'b' });
            g2.Add(new FuncSet<char> { 'c' });
            g2.Add(mkEdge("ab", "c"));
            yield return new object[] { g, g2 };

            g = new CharGraph();
            g.Add('a');
            g.Add('b');
            g.Add('c');
            g.Add('d');
            g.Add('e');
            g.Add(new DirectedEdge<char>('a', 'b'));
            g.Add(new DirectedEdge<char>('a', 'c'));
            g.Add(new DirectedEdge<char>('b', 'd'));
            g.Add(new DirectedEdge<char>('b', 'e'));
            g.Add(new DirectedEdge<char>('c', 'e'));
            g2 = new CharSetGraph();
            g2.Add(new FuncSet<char> { 'a' });
            g2.Add(new FuncSet<char> { 'b', 'c' });
            g2.Add(new FuncSet<char> { 'd' });
            g2.Add(new FuncSet<char> { 'e' });
            g2.Add(mkEdge("a", "bc"));
            g2.Add(mkEdge("bc", "d"));
            g2.Add(mkEdge("bc", "e"));
            yield return new object[] { g, g2 };

            g = new CharGraph();
            g.Add('a');
            g.Add('b');
            g.Add('c');
            g.Add('d');
            g.Add('e');
            g.Add('f');
            g.Add(new DirectedEdge<char>('a', 'b'));
            g.Add(new DirectedEdge<char>('a', 'c'));
            g.Add(new DirectedEdge<char>('b', 'd'));
            g.Add(new DirectedEdge<char>('b', 'e'));
            g.Add(new DirectedEdge<char>('b', 'f'));
            g.Add(new DirectedEdge<char>('c', 'd'));
            g.Add(new DirectedEdge<char>('c', 'e'));
            g.Add(new DirectedEdge<char>('c', 'f'));
            g2 = new CharSetGraph();
            g2.Add(new FuncSet<char> { 'a' });
            g2.Add(new FuncSet<char> { 'b', 'c' });
            g2.Add(new FuncSet<char> { 'd' });
            g2.Add(new FuncSet<char> { 'e' });
            g2.Add(new FuncSet<char> { 'f' });
            g2.Add(mkEdge("a", "bc"));
            g2.Add(mkEdge("bc", "d"));
            g2.Add(mkEdge("bc", "e"));
            g2.Add(mkEdge("bc", "f"));
            yield return new object[] { g, g2 };

            g = new CharGraph();
            g.Add('a');
            g.Add('b');
            g.Add('c');
            g.Add('d');
            g.Add('e');
            g.Add('f');
            g.Add('g');
            g.Add('h');
            g.Add('i');
            g.Add(new DirectedEdge<char>('a', 'b'));
            g.Add(new DirectedEdge<char>('a', 'c'));
            g.Add(new DirectedEdge<char>('b', 'd'));
            g.Add(new DirectedEdge<char>('c', 'd'));
            g.Add(new DirectedEdge<char>('d', 'e'));
            g.Add(new DirectedEdge<char>('e', 'f'));
            g.Add(new DirectedEdge<char>('e', 'g'));
            g.Add(new DirectedEdge<char>('f', 'h'));
            g.Add(new DirectedEdge<char>('g', 'h'));
            g.Add(new DirectedEdge<char>('h', 'i'));
            g2 = new CharSetGraph();
            g2.Add(new FuncSet<char> { 'a' });
            g2.Add(new FuncSet<char> { 'b', 'c' });
            g2.Add(new FuncSet<char> { 'd' });
            g2.Add(new FuncSet<char> { 'e' });
            g2.Add(new FuncSet<char> { 'f', 'g' });
            g2.Add(new FuncSet<char> { 'h' });
            g2.Add(new FuncSet<char> { 'i' });
            g2.Add(mkEdge("a", "bc"));
            g2.Add(mkEdge("bc", "d"));
            g2.Add(mkEdge("d", "e"));
            g2.Add(mkEdge("e", "fg"));
            g2.Add(mkEdge("fg", "h"));
            g2.Add(mkEdge("h", "i"));
            yield return new object[] { g, g2 };
        }

        public static IEnumerable<object[]> MergeAncestorsCyclesData()
        {
            var g = new CharGraph();
            g.Add('a');
            g.Add('b');
            g.Add(new DirectedEdge<char>('a', 'b'));
            g.Add(new DirectedEdge<char>('b', 'a'));
            yield return new object[] { g};

            g = new CharGraph();
            g.Add('a');
            g.Add('b');
            g.Add('c');
            g.Add(new DirectedEdge<char>('a', 'b'));
            g.Add(new DirectedEdge<char>('b', 'c'));
            g.Add(new DirectedEdge<char>('c', 'a'));
            yield return new object[] { g };

            g = new CharGraph();
            g.Add('a');
            g.Add('b');
            g.Add('c');
            g.Add('d');
            g.Add('e');
            g.Add(new DirectedEdge<char>('a', 'b'));
            g.Add(new DirectedEdge<char>('b', 'c'));
            g.Add(new DirectedEdge<char>('c', 'a'));

            g.Add(new DirectedEdge<char>('c', 'd'));
            g.Add(new DirectedEdge<char>('d', 'e'));
            g.Add(new DirectedEdge<char>('e', 'c'));
            yield return new object[] { g };

            g = new CharGraph();
            g.Add('a');
            g.Add('b');
            g.Add('c');
            g.Add('d');
            g.Add('e');
            g.Add('f');
            g.Add(new DirectedEdge<char>('a', 'b'));
            g.Add(new DirectedEdge<char>('b', 'c'));
            g.Add(new DirectedEdge<char>('c', 'a'));

            g.Add(new DirectedEdge<char>('c', 'd'));
            g.Add(new DirectedEdge<char>('d', 'e'));
            g.Add(new DirectedEdge<char>('e', 'c'));
            g.Add(new DirectedEdge<char>('c','f'));
            yield return new object[] { g };

            g = new CharGraph();
            g.Add('a');
            g.Add('b');
            g.Add('c');
            g.Add('d');
            g.Add('e');
            g.Add('f');
            g.Add(new DirectedEdge<char>('a', 'b'));
            g.Add(new DirectedEdge<char>('b', 'c'));
            g.Add(new DirectedEdge<char>('c', 'a'));

            g.Add(new DirectedEdge<char>('c', 'd'));
            g.Add(new DirectedEdge<char>('d', 'e'));
            g.Add(new DirectedEdge<char>('e', 'c'));
            g.Add(new DirectedEdge<char>('f', 'c'));
            yield return new object[] { g };
        }

        public static IEnumerable<object[]> DagToTreeData()
        {
            IFuncSet<char> mkfs(IEnumerable<char> cs) => new FuncSet<char>(cs);

            var g = new CharSetGraph();
            yield return new object[] { g, Maybe.Nothing<Tree<IFuncSet<char>>>() };

            g = new CharSetGraph();
            g.Add(mkfs("a"));
            yield return new object[] { g, Maybe.Just(Tree.MakeLeaf(mkfs("a"))) };

            g = new CharSetGraph();
            g.Add(mkfs("a"));
            g.Add(mkfs("b"));
            g.Add(new DirectedEdge<IFuncSet<char>>(mkfs("a"), mkfs("b")));
            yield return new object[] { g, Maybe.Just(Tree.MakeInner(mkfs("a"), new[] { Tree.MakeLeaf(mkfs("b")) })) };

            g = new CharSetGraph();
            g.Add(mkfs("a"));
            g.Add(mkfs("b"));
            g.Add(mkfs("c"));
            g.Add(mkfs("d"));
            g.Add(mkfs("e"));
            g.Add(mkfs("f"));
            g.Add(mkfs("g"));
            g.Add(new DirectedEdge<IFuncSet<char>>(mkfs("a"), mkfs("b")));
            g.Add(new DirectedEdge<IFuncSet<char>>(mkfs("a"), mkfs("e")));

            g.Add(new DirectedEdge<IFuncSet<char>>(mkfs("b"), mkfs("c")));
            g.Add(new DirectedEdge<IFuncSet<char>>(mkfs("b"), mkfs("d")));

            g.Add(new DirectedEdge<IFuncSet<char>>(mkfs("e"), mkfs("f")));
            g.Add(new DirectedEdge<IFuncSet<char>>(mkfs("e"), mkfs("g")));
            yield return new object[] { g, Maybe.Just(
                Tree.MakeInner(mkfs("a"), new []{
                        Tree.MakeInner(mkfs("b"), new [] {
                            Tree.MakeLeaf(mkfs("c")), Tree.MakeLeaf(mkfs("d"))}),
                        Tree.MakeInner(mkfs("e"), new [] {
                            Tree.MakeLeaf(mkfs("f")), Tree.MakeLeaf(mkfs("g"))})}))};

            g = new CharSetGraph();
            g.Add(mkfs("a"));
            g.Add(mkfs("b"));
            g.Add(mkfs("c"));
            g.Add(mkfs("d"));
            g.Add(mkfs("e"));
            g.Add(mkfs("f"));
            g.Add(mkfs("g"));
            g.Add(new DirectedEdge<IFuncSet<char>>(mkfs("a"), mkfs("b")));
            g.Add(new DirectedEdge<IFuncSet<char>>(mkfs("a"), mkfs("e")));

            g.Add(new DirectedEdge<IFuncSet<char>>(mkfs("b"), mkfs("c")));
            g.Add(new DirectedEdge<IFuncSet<char>>(mkfs("b"), mkfs("d")));

            g.Add(new DirectedEdge<IFuncSet<char>>(mkfs("e"), mkfs("f")));
            g.Add(new DirectedEdge<IFuncSet<char>>(mkfs("e"), mkfs("g")));

            g.Add(new DirectedEdge<IFuncSet<char>>(mkfs("f"), mkfs("g")));
            yield return new object[] { g, Maybe.Nothing<Tree<IFuncSet<char>>>() };

            g = new CharSetGraph();
            g.Add(mkfs("a"));
            g.Add(mkfs("b"));
            g.Add(mkfs("c"));
            g.Add(mkfs("d"));
            g.Add(new DirectedEdge<IFuncSet<char>>(mkfs("a"), mkfs("b")));
            g.Add(new DirectedEdge<IFuncSet<char>>(mkfs("c"), mkfs("d")));
            yield return new object[] { g, Maybe.Nothing<Tree<IFuncSet<char>>>() };
        }

        [Theory]
        [MemberData(nameof(ShortestPathData))]
        public static void ShortestPathTest(CharGraph g, IDictionary<(char, char), Maybe<double>> expected)
        {
            var actual = g.GetShortestPaths(_ => 2D);
            Assert.True(actual.HasValue);

            var actualValue = actual.Value();

            Assert.Equal(expected.Count, actualValue.Count);
            
            foreach (var (k,v) in actualValue)
            {
                Assert.True(expected.ContainsKey(k));
                Assert.Equal(expected[k].HasValue, v.HasValue);
                if (v.HasValue)
                    Assert.Equal(expected[k].Value(), v.Value());
            }
        }

        [Theory]
        [MemberData(nameof(MergeMultiEdgesData))]
        public static void MergeMultiEdgesTest(CharGraph g, CharGraph expected)
        {
            g.MergeMultiEdges<CharGraph, char, DirectedEdge<char>>(es => new DirectedEdge<char>(es.First().StartVertex, es.First().EndVertex));

            var comparer = new GraphEqualityComparer<CharGraph, char, DirectedEdge<char>>(
                GraphEqualityComparer.VertexCompare<char>(),
                GraphEqualityComparer.EdgeCompare<char>());

            Assert.Equal(expected, g, comparer);
        }

        [Theory]
        [MemberData(nameof(GraphEqualData))]
        public static void GraphEqualTest(CharGraph g, CharGraph g2, bool expected)
        {
            var comparer = new GraphEqualityComparer<CharGraph, char, DirectedEdge<char>>(
                GraphEqualityComparer.VertexCompare<char>(),
                GraphEqualityComparer.EdgeCompare<char>());

            if (expected)
                Assert.Equal(g, g2, comparer);
            else
                Assert.NotEqual(g, g2, comparer);
        }

        [Theory]
        [MemberData(nameof(MergeAncestorsData))]
        public static void MergeAncestorsTest(CharGraph g, CharSetGraph expected)
        {
            var fs = new FuncSet<char>();

            var comparer = new GraphEqualityComparer<CharSetGraph, IFuncSet<char>, DirectedEdge<IFuncSet<char>>>(
                fs.GetComparer<char>(),
                GraphEqualityComparer.EdgeCompare<IFuncSet<char>>(fs.GetComparer<char>()));

            var copy = g.MergeAncestors(true);

            Assert.True(copy.HasValue);
            Assert.Equal(expected, copy.Value(), comparer);
        }

        [Theory]
        [MemberData(nameof(MergeAncestorsCyclesData))]
        public static void MergeAncestorsCyclesTest(CharGraph g)
        {
            var fs = new FuncSet<char>();

            var comparer = new GraphEqualityComparer<CharSetGraph, IFuncSet<char>, DirectedEdge<IFuncSet<char>>>(
                fs.GetComparer<char>(),
                GraphEqualityComparer.EdgeCompare<IFuncSet<char>>(fs.GetComparer<char>()));

            var copy = g.MergeAncestors(true);

            Assert.True(copy.IsNothing);
        }

        [Theory]
        [MemberData(nameof(DagToTreeData))]
        public static void DagToTreeTest(CharSetGraph g, Maybe<Tree<IFuncSet<char>>> expected)
        {
            var actual = g.DagToTree();

            Assert.Equal(actual.HasValue, expected.HasValue);
            if (expected.HasValue)
                Assert.Equal(actual.Value(), expected.Value());
        }
    }
}
