using SD.Tools.Algorithmia.Graphs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nordril.Graphs
{
    /// <summary>
    /// Extension methods for <see cref="DirectedGraph{TVertex, TEdge}"/>
    /// </summary>
    public static class DirectedGraphExtensions
    {
        /// <summary>
        /// Adds a vertex that has precedence-wishes. Predences are represented as two sets of edges <paramref name="antecedents"/>, which specify which other vertices should come before this vertex, and <paramref name="successors"/>, which specifies which vertices should come after this vertex. If any of the vertices in <paramref name="antecedents"/> or <paramref name="successors"/> do not exist in the graph, <paramref name="addMissingVertices"/> determines what is to be done.
        /// </summary>
        /// <typeparam name="TVertex">The type of the graph's vertices.</typeparam>
        /// <typeparam name="TEdge">The type of the graph's edges.</typeparam>
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
