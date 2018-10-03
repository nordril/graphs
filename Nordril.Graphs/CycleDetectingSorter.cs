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
    /// A topological sorters which can try to topologically sort a directred graph and return the encountered cycles.
    /// </summary>
    /// <typeparam name="TVertex">The type of the vertices in the graph.</typeparam>
    /// <typeparam name="TEdge">The type of the edges in the graph.</typeparam>
    public class CycleDetectingSorter<TVertex, TEdge> : TopologicalSorter<TVertex, TEdge> where TEdge : class, IEdge<TVertex>
    {
        private IList<(TVertex, HashSet<TEdge>)> cycles;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="graphToCrawl">The graph to crawl.</param>
        /// <param name="directionMeansOrder">If set to true, a directed edge from A to B is interpreted as the order in which A and B should be done, i.e. first A then B. When false is passed (default) the edge from A to B is interpreted as A is depending on B and therefore B should be done before A.</param>
        public CycleDetectingSorter(GraphBase<TVertex, TEdge> graphToCrawl, bool directionMeansOrder)
        : base(graphToCrawl, directionMeansOrder)
        {
        }

        /// <summary>
        /// Adds a cyle to the internal cycle-list when a cycle has been encountered.
        /// </summary>
        /// <param name="relatedVertex">The vertex participating in the cylce.</param>
        /// <param name="edges">The edges participating in the cylce.</param>
        protected override bool CycleDetected(TVertex relatedVertex, HashSet<TEdge> edges)
        {
            cycles.Add((relatedVertex, edges));

            return base.CycleDetected(relatedVertex, edges);
        }

        /// <summary>
        /// Runs the sorter and returns those vertices with their relevant outgoing edges which participate in cycles, if there are any.
        /// </summary>
        /// <param name="sortedVertices">The list of sortted vertices, if there are no cycles. This sorting will respect edge order, meaning that the following will hold, if no cycles exist:
        /// <code>
        ///     [forall i,j]i &lt; j =&gt; there is a path sortedVertices[i]~&gt;sortedVertices[j] and there is no path sortedVertices[j]\&gt;sortedVertices[j].
        /// </code>
        /// If there are cycles, this parameter will be set to <c>default</c>.
        /// </param>
        /// <returns>The vertices which participate in cycles, with their relevant outgoing edges.</returns>
        public Maybe<IEnumerable<(TVertex, HashSet<TEdge>)>> SortWithCycleDetection(out List<TVertex> sortedVertices)
        {
            cycles = new List<(TVertex, HashSet<TEdge>)>();
            sortedVertices = default;

            try
            {
                Sort();
                sortedVertices = this.SortResults;
            }
            catch (InvalidOperationException)
            {
                if (cycles.Any())
                    return Maybe.Just((IEnumerable<(TVertex, HashSet<TEdge>)>)cycles);
            }

            return Maybe.Nothing<IEnumerable<(TVertex, HashSet<TEdge>)>>();
        }
    }
}
