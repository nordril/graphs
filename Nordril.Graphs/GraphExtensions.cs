using Nordril.Collections;
using Nordril.Functional;
using Nordril.Functional.Algebra;
using Nordril.Functional.Data;
using SD.Tools.Algorithmia.Graphs;
using SD.Tools.Algorithmia.Graphs.Algorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nordril.Graphs
{
    /// <summary>
    /// Extension methods for <see cref="GraphBase{TVertex, TEdge}"/>.
    /// </summary>
    public static class GraphExtensions
    {
        /// <summary>
        /// Turns a DAG into a tree, if possible. All ancestory of each node are merged, all indirect ancestor-relationships as well as multi-edges between nodes and loops are removed. The input graph will be mutated.
        /// </summary>
        /// <typeparam name="TGraph">The type of the graph.</typeparam>
        /// <typeparam name="TVertex">The type of the vertices.</typeparam>
        /// <param name="g">The graph.</param>
        /// <param name="vertexMerger">A function to merge the ancestors of a node.</param>
        /// <param name="makeEdge">A function which creates an edge between a node and one which was part of the merged ancestors Either the first or second argument of this function will be the merged ancestor, depending on whether the edge was outgoing or incoming.</param>
        /// <param name="contains">The contains-relation between vertices. <c>contains(x,y)</c> should be true iff <c>x</c> contains <c>y</c> as a smaller element.</param>
        public static Maybe<TGraph> TreeifyDag<TGraph, TVertex>(
            this TGraph g,
            Func<ISet<TVertex>, TVertex> vertexMerger,
            Func<TVertex, TVertex, DirectedEdge<TVertex>> makeEdge,
            Func<TVertex, TVertex, bool> contains)
            where TGraph : DirectedGraph<TVertex, DirectedEdge<TVertex>>
            where TVertex : IEquatable<TVertex>
        {
            var ret = g
                .MergeAncestors(vertexMerger, makeEdge, contains, true)
                .Map(g2 => g2.MergeMultiEdges<TGraph, TVertex, DirectedEdge<TVertex>>(es => makeEdge(es.First().StartVertex, es.First().EndVertex))).ToMaybe()
                .Bind(g2 => g2.RemoveIndirectParentRelationships<TGraph, TVertex, DirectedEdge<TVertex>>())
                .Map(g2 => g2.RemoveLoops<TGraph, TVertex, DirectedEdge<TVertex>>()).ToMaybe()
                .Bind(g2 => Maybe.JustIf(!g2.HasCycles(out var _), () => g2)).ToMaybe();

            return ret;
        }

        /// <summary>
        /// Turns a DAG into a tree, if possible. All ancestory of each node are merged, all indirect ancestor-relationships as well as multi-edges between nodes and loops are removed. The input graph will be mutated.
        /// </summary>
        /// <typeparam name="TGraph">The type of the graph.</typeparam>
        /// <typeparam name="TVertex">The type of the vertices.</typeparam>
        /// <param name="g">The graph.</param>
        public static Maybe<DirectedGraph<IFuncSet<TVertex>, DirectedEdge<IFuncSet<TVertex>>>> TreeifyDag<TGraph, TVertex>(
            this TGraph g)
            where TGraph : DirectedGraph<TVertex, DirectedEdge<TVertex>>
            where TVertex : IEquatable<TVertex>
        {
            var ret = g
                .MergeAncestors(true)
                .Map(g2 => g2.MergeMultiEdges<DirectedGraph<IFuncSet<TVertex>, DirectedEdge<IFuncSet<TVertex>>>, IFuncSet<TVertex>, DirectedEdge<IFuncSet<TVertex>>>(es => es.First())).ToMaybe()
                .Bind(g2 => g2.RemoveIndirectParentRelationships<DirectedGraph<IFuncSet<TVertex>, DirectedEdge<IFuncSet<TVertex>>>, IFuncSet<TVertex>, DirectedEdge<IFuncSet<TVertex>>>())
                .Map(g2 => g2.RemoveLoops<DirectedGraph<IFuncSet<TVertex>, DirectedEdge<IFuncSet<TVertex>>>, IFuncSet<TVertex>, DirectedEdge<IFuncSet<TVertex>>>()).ToMaybe()
                .Bind(g2 => Maybe.JustIf(!g2.HasCycles(out var _), () => g2)).ToMaybe();

            return ret;
        }

        /// <summary>
        /// Turns a DAG which is already a tree into a <see cref="Tree{T}"/>. If the DAG is weakly or strongly cyclic, or disconnected, <see cref="Maybe.Nothing{T}"/> is returned.
        /// </summary>
        /// <typeparam name="TVertex">The type of the vertices.</typeparam>
        /// <param name="g">The graph.</param>
        public static Maybe<Tree<TVertex>> DagToTree<TVertex>(this DirectedGraph<TVertex, DirectedEdge<TVertex>> g)
            where TVertex : IEquatable<TVertex>
        {
            var visitedVertices = new HashSet<TVertex>();
            var roots = g.Vertices.Where(v => g.GetEdgesToEndVertex(v).Where(e => e.EndVertex.Equals(v)).Empty()).ToList();

            var sorter = new CycleDetectingSorter<TVertex, DirectedEdge<TVertex>>(g, true);

            //Definition of a tree: |E| = |V|-1 && the graph is connected.
            if (roots.Count != 1 || g.EdgeCount != (g.VertexCount - 1) || !g.IsConnected())
                return Maybe.Nothing<Tree<TVertex>>();

            Tree<TVertex> go(TVertex cur)
            {
                visitedVertices.Add(cur);

                var children = g.GetEdgesFromStartVertex(cur);

                if (children.Count == 0)
                    return Tree.MakeLeaf(cur);
                else
                    return Tree.MakeInner(cur, children.Where(c => !visitedVertices.Contains(c.EndVertex)).Select(c => go(c.EndVertex)));
            };

            return Maybe.Just(go(roots[0]));
        }

        /// <summary>
        /// Returns true iff the graph has cycles.
        /// </summary>
        /// <param name="g">The graph.</param>
        /// <param name="cycles">The vertices which form part of cycles, with their outgoing edges. If there are no cycles, <c>default</c> will be returned.</param>
        public static bool HasCycles<TVertex, TEdge>(this DirectedGraph<TVertex, TEdge> g, out IDictionary<TVertex, ISet<TVertex>> cycles)
            where TEdge : DirectedEdge<TVertex>
        {
            var sorter = new CycleDetectingSorter<TVertex, TEdge>(g, true);
            cycles = default;

            var result = sorter.SortWithCycleDetection(out var _);

            if (result.HasValue)
            {
                cycles = new Dictionary<TVertex, ISet<TVertex>>();

                foreach (var cycle in result.Value())
                    cycles[cycle.Item1] = cycle.Item2.Select(c => c.EndVertex).ToHashSet();

                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Returns the list of strongly connected components in a graph. A strongly connected component is one in which
        /// <list type="number">
        ///     <item>for every pair of vertices V,W, W is reachable from V, and </item>
        ///     <item>one cannot add another node U such that the first property still holds.</item>
        /// </list>
        /// </summary>
        /// <remarks>Uses Tarjan's algorithm.</remarks>
        /// <typeparam name="TVertex">The type of the vertices.</typeparam>
        /// <typeparam name="TEdge">The type of the edges.</typeparam>
        /// <param name="g">The graph.</param>
        public static IEnumerable<DirectedGraph<TVertex, TEdge>> StronglyConnectedComponents<TVertex, TEdge>(this DirectedGraph<TVertex, TEdge> g)
            where TVertex : IEquatable<TVertex>
            where TEdge : DirectedEdge<TVertex>
        {
            //https://en.wikipedia.org/wiki/Tarjan%27s_strongly_connected_components_algorithm

            var index = 0;
            var stack = new Stack<TVertex>();
            var vertexData = new Dictionary<TVertex, (int index, int lowLink, bool onStack)>();

            g.Vertices.ForEach(v => vertexData[v] = (-1, -1, false));

            Func<int, int> min(int x) => y => Math.Min(x, y);

            IEnumerable<DirectedGraph<TVertex, TEdge>> strongConnect(TVertex v)
            {
                vertexData.Update(v, i => (index, index, true));
                index++;
                stack.Push(v);

                foreach (var e in g.GetEdgesFromStartVertex(v))
                {
                    if (vertexData[e.EndVertex].index < 0)
                    {
                        foreach (var scc in strongConnect(e.EndVertex))
                            yield return scc;
                        vertexData.Update(v, i => i.Second(min(vertexData[e.EndVertex].lowLink)));
                    }
                    else if (vertexData[e.EndVertex].onStack)
                        vertexData.Update(v, i => i.Second(min(vertexData[e.EndVertex].index)));
                }

                var vData = vertexData[v];

                if (vData.lowLink == vData.index)
                {
                    var result = new DirectedGraph<TVertex, TEdge>();
                    TVertex w;

                    do
                    {
                        w = stack.Pop();
                        vertexData.Update(w, i => i.Third(_ => false));
                        result.Add(w);
                    } while (!w.Equals(v));

                    foreach (var v1 in result.Vertices)
                        foreach (var v2 in result.Vertices)
                            g.GetEdges(v1, v2).ForEach(result.Add);

                    yield return result;
                }
            }

            foreach (var v in g.Vertices)
            {
                if (vertexData[v].index < 0)
                {
                    foreach (var scc in strongConnect(v))
                        yield return scc;
                }
            }
        }

        /*public static IEnumerable<IMutableVertexAndEdgeListGraph<TVertex, TEdge>> StronglyConnectedComponents<TVertex, TEdge>(this IVertexListGraph<TVertex, TEdge> g, Func<IMutableVertexAndEdgeListGraph<TVertex, TEdge>> componentMaker)
            where TEdge : QuickGraph.IEdge<TVertex>
        {
            g.StronglyConnectedComponents(out var scc);

            return scc.GroupBy(kv => kv.Value).Select(group =>
            {
                var c = componentMaker();

                group.ForEach(kv => c.AddVertex(kv.Key));
                
                foreach (var v1 in c.Vertices)
                    foreach (var v2 in c.Vertices)
                    {
                        if (g.TryGetEdges(v1, v2, out var edges))
                            edges.ForEach(e => c.AddEdge(e));
                    }
            });
        }*/



        /// <summary>
        /// Returns the list of weakly connected components in a graph. A weakly connected component is one in which
        /// <list type="number">
        ///     <item>for every pair of vertices V,W, W is reachable from V, ignoring edge-direction, and </item>
        ///     <item>one cannot add another node U such that the first property still holds.</item>
        /// </list>
        /// </summary>
        /// <typeparam name="TGraph">The type of the produced components.</typeparam>
        /// <typeparam name="TVertex">The type of the vertices.</typeparam>
        /// <typeparam name="TEdge">The type of the edges.</typeparam>
        /// <param name="g">The graph.</param>
        /// <param name="componentMaker">A producer-function for empty empty components.</param>
        public static IList<TGraph> WeaklyConnectedComponents<TGraph, TVertex, TEdge>(this GraphBase<TVertex, TEdge> g, Func<TGraph> componentMaker)
            where TEdge : class, IEdge<TVertex>
            where TGraph : GraphBase<TVertex, TEdge>
        {
            var undirected = new NonDirectedGraph<TVertex, NonDirectedEdge<TVertex>>();

            g.Vertices.ForEach(undirected.Add);
            g.Edges.ForEach(e => undirected.Add(new NonDirectedEdge<TVertex>(e.StartVertex, e.EndVertex)));

            var subgraphs = new List<TGraph>();

            //Find the subgraphs (connected components that are candidates for being turned into trees).
            var dcGraphFinder = new DisconnectedGraphsFinder<TVertex, NonDirectedEdge<TVertex>>(
                () => new SubGraphView<TVertex, NonDirectedEdge<TVertex>>(undirected), undirected);

            dcGraphFinder.FindDisconnectedGraphs();

            foreach (var component in dcGraphFinder.FoundDisconnectedGraphs)
            {
                var subG = componentMaker();
                component.Vertices.ForEach(subG.Add);

                foreach (var v1 in component.Vertices)
                    foreach (var v2 in component.Vertices)
                        g.GetEdges(v1, v2).ForEach(subG.Add);

                subgraphs.Add(subG);
            }

            return subgraphs;
        }

        /// <summary>
        /// Removes all loops from a graph. The input graph will be mutated.
        /// </summary>
        /// <typeparam name="TGraph">The type of the graph.</typeparam>
        /// <typeparam name="TVertex">The type of the vertices.</typeparam>
        /// <typeparam name="TEdge">The type of the edges.</typeparam>
        /// <param name="g">The graph.</param>
        public static TGraph RemoveLoops<TGraph, TVertex, TEdge>(this TGraph g)
            where TGraph : GraphBase<TVertex, TEdge>
            where TVertex : IEquatable<TVertex>
            where TEdge : class, IEdge<TVertex>
            => g.Set(g2 => g2.Edges
                    .Where(e => e.StartVertex.Equals(e.EndVertex))
                    .ToList()
                    .ForEach(g.Remove));

        /// <summary>
        /// Merges the ancestors of all nodes (recursively) if any node has multiple ancestors (i.e. multiple incoming edges). The input graph will not be mutated.
        /// If any nodes which are to be merged have edges between them, the merging will create loops, unless <paramref name="eliminateLoopsFromMergedNodes"/> is true.
        /// </summary>
        /// <typeparam name="TVertex">The type of the vertices.</typeparam>
        /// <param name="g">The graph.</param>
        /// <param name="eliminateLoopsFromMergedNodes">If true, any loop that would be created for a merged node due to the nodes to be merged having edges among them is removed.</param>
        /// <returns>The mutated input graph if there were no cycles in the original graph, and <see cref="Maybe.Nothing{T}"/> otherwise.</returns>
        public static Maybe<DirectedGraph<IFuncSet<TVertex>, DirectedEdge<IFuncSet<TVertex>>>> MergeAncestors<TVertex>(
            this DirectedGraph<TVertex, DirectedEdge<TVertex>> g,
            bool eliminateLoopsFromMergedNodes = false)
            where TVertex : IEquatable<TVertex>
        {
            var copy = new DirectedGraph<IFuncSet<TVertex>, DirectedEdge<IFuncSet<TVertex>>>();

            g.Vertices.ForEach(v => copy.Add(new FuncSet<TVertex> { v }));
            g.Edges.ForEach(e => copy.Add(new DirectedEdge<IFuncSet<TVertex>>(new FuncSet<TVertex> { e.StartVertex }, new FuncSet<TVertex> { e.EndVertex })));

            var ret = copy.MergeAncestors<DirectedGraph<IFuncSet<TVertex>, DirectedEdge<IFuncSet<TVertex>>>, IFuncSet<TVertex>>(
                vs => new FuncSet<TVertex>(vs.Concat()),
                (s, e) => new DirectedEdge<IFuncSet<TVertex>>(s, e),
                (xs,ys) => xs.IsSupersetOf(ys), eliminateLoopsFromMergedNodes);

            return ret;
        }

        /// <summary>
        /// Merges the ancestors of all nodes (recursively) if any node has multiple ancestors (i.e. multiple incoming edges). The input graph will be mutated, but if the graph has cycles, the operation will fail before any mutation is done.
        /// If any nodes which are to be merged have edges between them, the merging will create loops, unless <paramref name="eliminateLoopsFromMergedNodes"/> is true.
        /// </summary>
        /// <typeparam name="TGraph">The type of the graph.</typeparam>
        /// <typeparam name="TVertex">The type of the vertices.</typeparam>
        /// <param name="g">The graph.</param>
        /// <param name="vertexMerger">A function to merge the ancestors of a node.</param>
        /// <param name="makeEdge">A function which creates an edge between a node and one which was part of the merged ancestors Either the first or second argument of this function will be the merged ancestor, depending on whether the edge was outgoing or incoming.</param>
        /// <param name="contains">The contains-relation between vertices. <c>contains(x,y)</c> should be true iff <c>x</c> contains <c>y</c> as a smaller element.</param>
        /// <param name="eliminateLoopsFromMergedNodes">If true, any loop that would be created for a merged node due to the nodes to be merged having edges among them is removed.</param>
        /// <returns>The mutated input graph if there were no cycles in the original graph, and <see cref="Maybe.Nothing{T}"/> otherwise.</returns>
        public static Maybe<TGraph> MergeAncestors<TGraph, TVertex>(
            this TGraph g,
            Func<ISet<TVertex>, TVertex> vertexMerger,
            Func<TVertex, TVertex, DirectedEdge<TVertex>> makeEdge,
            Func<TVertex, TVertex, bool> contains,
            bool eliminateLoopsFromMergedNodes = false)
            where TGraph : DirectedGraph<TVertex, DirectedEdge<TVertex>>
            where TVertex : IEquatable<TVertex>
        {
            //First, we check for cycles and error out if we find one, otherwise we'd get stuck in an infinite loop.
            if (g.HasCycles(out var _))
                return Maybe.Nothing<TGraph>();

            var nodeQueue = new Queue<TVertex>(g.VertexCount);

            //Enqueue all leaves in the DAG.
            g.Vertices.Where(v => g.GetEdgesFromStartVertex(v).Count == 0).ForEach(nodeQueue.Enqueue);

            //While we have any node in the queue, dequeue it as cur, merge all ancestors of cur, and queue the merged ancestor.
            while (nodeQueue.Count > 0)
            {
                var cur = nodeQueue.Dequeue();
                var a1 = g.GetEdgesToEndVertex(cur);
                var a2 = a1.Where(e => contains(cur, e.EndVertex) && !e.StartVertex.Equals(e.EndVertex));
                var a3 = a2.Select(e => e.StartVertex);

                var ancestors = a3.ToHashSet();

                if (ancestors.Count > 0)
                {
                    var mergedAncestor = vertexMerger(ancestors);
                    g.MergeNodes(ancestors, mergedAncestor, makeEdge, contains);

                    //Eliminate any loops from/to the merged ancestor if the flag to do so is set.
                    if (eliminateLoopsFromMergedNodes)
                        g.GetEdgesFromStartVertex(mergedAncestor).Where(e => e.EndVertex.Equals(mergedAncestor)).ForEach(g.Remove);

                    nodeQueue.Enqueue(mergedAncestor);
                }
            }

            return Maybe.Just(g);
        }

        /// <summary>
        /// Merges a set of nodes in a graph, including the edges which touch any of those nodes. The input graph is mutated.
        /// </summary>
        /// <typeparam name="TGraph">The type of the graph.</typeparam>
        /// <typeparam name="TVertex">The type of the vertices.</typeparam>
        /// <param name="g">The graph.</param>
        /// <param name="nodesToMerge">The set of nodes to merge.</param>
        /// <param name="mergedNode">The merged node to insert in stead of the nodes in <paramref name="nodesToMerge"/>.</param>
        /// <param name="makeEdge">A function which creates an edge between a node and one which was part of <paramref name="nodesToMerge"/>. Either the first or second argument of this function will be <paramref name="mergedNode"/>, depending on whether the edge was outgoing or incoming.</param>
        /// <param name="contains">The contains-relation between vertices. <c>contains(x,y)</c> should be true iff <c>x</c> contains <c>y</c> as a smaller element.</param>
        public static TGraph MergeNodes<TGraph, TVertex>(this TGraph g, ISet<TVertex> nodesToMerge, TVertex mergedNode, Func<TVertex, TVertex, DirectedEdge<TVertex>> makeEdge, Func<TVertex, TVertex, bool> contains)
            where TGraph : DirectedGraph<TVertex, DirectedEdge<TVertex>>
        {
            /*DirectedEdge<TVertex> replacePartWithWhole(DirectedEdge<TVertex> v)
                => new DirectedEdge<TVertex>(
                    contains(mergedNode, v.StartVertex) ? mergedNode : v.StartVertex,
                    contains(mergedNode, v.EndVertex) ? mergedNode : v.EndVertex);*/

            //Collect all ingoing edges to the merge-set
            var incoming = nodesToMerge.SelectMany(g.GetEdgesToEndVertex).ToHashSet();
            incoming.RemoveWhere(e => nodesToMerge.Contains(e.StartVertex));

            //Collect all outgoing edges from the merge-set
            var outgoing = nodesToMerge.SelectMany(g.GetEdgesFromStartVertex).ToHashSet();
            outgoing.RemoveWhere(e => nodesToMerge.Contains(e.EndVertex));

            var numLoops = nodesToMerge.SelectMany(g.GetEdgesFromStartVertex).Where(e => nodesToMerge.Contains(e.EndVertex)).Count();

            //Delete all old edges to the nodes of the merge-set and the nodes of the merge-set
            incoming.ForEach(g.Remove);
            outgoing.ForEach(g.Remove);
            nodesToMerge.ForEach(g.Remove);

            //Add the new, merged node and re-create a single incoming/outgoing edge for each edge which touched the merge-set.
            g.Add(mergedNode);

            incoming.GroupBy(e => e.StartVertex).ForEach(group => g.Add(makeEdge(group.Key, mergedNode)));
            outgoing.GroupBy(e => e.EndVertex).ForEach(group => g.Add(makeEdge(mergedNode, group.Key)));
            for (int i = 0; i < numLoops; i++)
                g.Add(new DirectedEdge<TVertex>(mergedNode, mergedNode));

            return g;
        }

        /// <summary>
        /// In a directed, acyclic graph, removes all edges between non-direct ancestors. A node A is a non-direct ancestor of a node B if there is a path from A to B with more than one edge. The input graph is mutated.
        /// If the graph contains cycles, <see cref="Maybe.Nothing{T}"/> is returned.
        /// </summary>
        /// <typeparam name="TVertex">The type of the vertices.</typeparam>
        /// <typeparam name="TGraph">The type of the graph.</typeparam>
        /// <typeparam name="TEdge">The type of the edges.</typeparam>
        /// <param name="g">The graph.</param>
        public static Maybe<TGraph> RemoveIndirectParentRelationships<TGraph, TVertex, TEdge>(this TGraph g)
            where TGraph : DirectedGraph<TVertex, TEdge>
            where TEdge : DirectedEdge<TVertex>
        {
            //1. Compute all longest paths (with constant edge-cost 1)
            return g.GetLongestPaths(_ => 1)
            //2. If, for any pair of nodes n, m, cost(n,m) > 1, sever n and m, since there is an indirect path between them.
                .Map(longestPaths =>
                {
                    var edgesToRemove = new HashSet<TEdge>();

                    foreach (var v1 in g.Vertices)
                        foreach (var v2 in g.Vertices)
                        {
                            var dist = longestPaths[(v1, v2)];
                            if (dist.ValueOr(0D) > 1D)
                                foreach (var e in g.GetEdges(v1, v2))
                                    edgesToRemove.Add(e);
                        }

                    edgesToRemove.ForEach(g.Remove);

                    return g;
                })
                .ToMaybe();
        }

        /// <summary>
        /// Merges the set of all edges going between any par of nodes n, m into a single edge, using the function <paramref name="merger"/>.
        /// The original grpah <paramref name="g"/> is mutated.
        /// </summary>
        /// <typeparam name="TGraph">The type of the graph.</typeparam>
        /// <typeparam name="TVertex">The type of the vertices.</typeparam>
        /// <typeparam name="TEdge">The type of the edges.</typeparam>
        /// <param name="g">The graph.</param>
        /// <param name="merger">The edge-merge function, called at most once for all pairs of nodes (never when there's no edge between two nodes).</param>
        /// <returns>The mutated version of the input graph.</returns>
        public static TGraph MergeMultiEdges<TGraph, TVertex, TEdge>(this TGraph g, Func<ISet<TEdge>, TEdge> merger)
            where TGraph : GraphBase<TVertex, TEdge>
            where TEdge : Edge<TVertex>
        {
            var newEdges = new Dictionary<(TVertex, TVertex), (ISet<TEdge>, TEdge)>();

            foreach (var v in g.Vertices)
                foreach (var edgeGroup in g.GetEdgesFromStartVertex(v).GroupBy(e => e.EndVertex))
                {
                    var edgeSet = edgeGroup.ToHashSet();
                    newEdges[(v, edgeGroup.Key)] = (edgeSet, merger(edgeSet));
                }

            foreach (var ((vin, vout), (edges, edge)) in newEdges)
            {
                edges.ForEach(g.Remove);
                g.Add(edge);
            }

            return g;
        }

        /// <summary>
        /// Computes the shortest paths between all pairs of nodes in a graph using the Floyd-Warshall algorithm.
        /// The cost-function <paramref name="costFunction"/> may be positive or negative, but the graph <paramref name="g"/> may contain no negative-cost cycles. In such a case, the algorithm will fail.
        /// </summary>
        /// <typeparam name="TVertex">The type of the vertices.</typeparam>
        /// <typeparam name="TEdge">The type of the edges.</typeparam>
        /// <param name="g">The graph.</param>
        /// <param name="costFunction">The cost-function to apply to each edge. Costs may be negative, but if a negative-cost cycle exists, <see cref="Maybe.Nothing{T}"/> is returned.</param>
        /// <returns>A list of edge-pairs, with the cost of the shortest path between them. If there is no path between two nodes, the corresponding cost will be <see cref="Maybe.Nothing{T}"/>.</returns>
        public static Maybe<Dictionary<(TVertex, TVertex), Maybe<double>>> GetShortestPaths<TVertex, TEdge>(this DirectedGraph<TVertex, TEdge> g, Func<TEdge, double> costFunction)
            where TEdge : DirectedEdge<TVertex>
        {
            var order = TotalOrder.FromComparable<double>().LiftTotalOrderWithInfinity();
            var monoid = new Monoid<double>(0D, (x, y) => x + y).LiftMonoidWithInfinity();

            var dist = new Dictionary<(TVertex, TVertex), Maybe<double>>();

            //Initialize all distances to infinity.
            foreach (var v1 in g.Vertices)
                foreach (var v2 in g.Vertices)
                    dist[(v1, v2)] = Maybe.Nothing<double>();


            //For each edge e = (n,m), set the distance between n and m to costFunction(e)
            foreach (var e in g.Edges)
                dist[(e.StartVertex, e.EndVertex)] = Maybe.Just(costFunction(e));

            //For each vertex n, set the distance from n to n to 0 (monoid.Neutral)
            foreach (var v in g.Vertices)
                dist[(v, v)] = Maybe.Just(0D);

            foreach (var k in g.Vertices)
                foreach (var i in g.Vertices)
                    foreach (var j in g.Vertices)
                    {
                        var ij = dist[(i, j)];
                        var ik = dist[(i, k)];
                        var kj = dist[(k, j)];

                        var indirectPath = monoid.Op(ik, kj);

                        if (order.Ge(ij, indirectPath))
                            dist[(i, j)] = indirectPath;
                    }

            //Negative cycle-check: if, for any i, dist[(i,i)] < 0, we have a negative cycle and we quit.
            if (g.Vertices.Any(v => dist[(v, v)].ValueOr(0D) < 0D))
                return Maybe.Nothing<Dictionary<(TVertex, TVertex), Maybe<double>>>();

            return Maybe.Just(dist);
        }

        /// <summary>
        /// Computes the longest paths between all pairs of nodes in a graph using the Floyd-Warshall algorithm.
        /// The cost-function <paramref name="costFunction"/> may be positive or negative, but the graph <paramref name="g"/> may contain no positive-cost cycles. In such a case, the algorithm will fail.
        /// </summary>
        /// <typeparam name="TVertex">The type of the vertices.</typeparam>
        /// <typeparam name="TEdge">The type of the edges.</typeparam>
        /// <param name="g">The graph.</param>
        /// <param name="costFunction">The cost-function to apply to each edge. Costs may be positive, but if a positive-cost cycle exists, <see cref="Maybe.Nothing{T}"/> is returned.</param>
        /// <returns>A list of edge-pairs, with the cost of the longest path between them. If there is no path between two nodes, the corresponding cost will be <see cref="Maybe.Nothing{T}"/>.</returns>
        public static Maybe<Dictionary<(TVertex, TVertex), Maybe<double>>> GetLongestPaths<TVertex, TEdge>(this DirectedGraph<TVertex, TEdge> g, Func<TEdge, double> costFunction)
            where TEdge : DirectedEdge<TVertex>
            => g.GetShortestPaths(e => costFunction(e) * (-1));

        /// <summary>
        /// Adds a vertex that has precedence-wishes. Predences are represented as two sets of edges <paramref name="antecedents"/>, which specify which other vertices should come before this vertex, and <paramref name="successors"/>, which specifies which vertices should come after this vertex. If any of the vertices in <paramref name="antecedents"/> or <paramref name="successors"/> do not exist in the graph, <paramref name="addMissingVertices"/> determines what is to be done.
        /// </summary>
        /// <typeparam name="TVertex">The type of the graph's vertices.</typeparam>
        /// <param name="g">The graph to which to add the vertices and edges.</param>
        /// <param name="vertex">The vertex to add.</param>
        /// <param name="antecedents">The antecedents of <paramref name="vertex"/>.</param>
        /// <param name="successors">The successors of <paramref name="vertex"/>.</param>
        /// <param name="addMissingVertices">If true, members of <paramref name="antecedents"/> and <paramref name="successors"/> which are not vertices in <paramref name="g"/> will be added to it, if false, they will be ignored.</param>
        public static void AddToPrecendeGraph<TVertex>(
            this DirectedGraph<TVertex, DirectedEdge<TVertex>> g,
            TVertex vertex,
            ISet<TVertex> antecedents,
            ISet<TVertex> successors,
            bool addMissingVertices = false)
        {
            g.Add(vertex);

            foreach (var x in antecedents)
                if (addMissingVertices || g.Contains(x))
                    g.Add(new DirectedEdge<TVertex>(x, vertex));

            foreach (var x in successors)
                if (addMissingVertices || g.Contains(x))
                    g.Add(new DirectedEdge<TVertex>(vertex, x));

        }
    }
}
