using Nordril.Functional;
using SD.Tools.Algorithmia.Graphs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nordril.Graphs
{
    /// <summary>
    /// An <see cref="IEqualityComparer{T}"/> which structurally compares graphs.
    /// Note: <see cref="IEqualityComparer{T}.GetHashCode(T)"/> also hashes all elements of the graph and this might be an expensive operation if the graph is large.
    /// </summary>
    /// <typeparam name="TGraph">The type of the graph.</typeparam>
    /// <typeparam name="TVertex">The type of the vertices.</typeparam>
    /// <typeparam name="TEdge">The type of the edges.</typeparam>
    public class GraphEqualityComparer<TGraph, TVertex, TEdge> : IEqualityComparer<TGraph>
        where TGraph : GraphBase<TVertex, TEdge>
        where TEdge : class, IEdge<TVertex>
    {
        private readonly IComparer<TVertex> vertexComparer;
        private readonly IComparer<TEdge> edgeComparer;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="vertexComparer">The comparer for vertices. See <see cref="GraphEqualityComparer.VertexCompare{T}"/> for a default comparer.</param>
        /// <param name="edgeComparer">The comparer for edges. See <see cref="GraphEqualityComparer.EdgeCompare{T}()"/> for a default comparer.</param>
        public GraphEqualityComparer(IComparer<TVertex> vertexComparer, IComparer<TEdge> edgeComparer)
        {
            this.vertexComparer = vertexComparer;
            this.edgeComparer = edgeComparer;
        }

        /// <inheritdoc />
        public bool Equals(TGraph x, TGraph y)
        {
            if (x.EdgeCount != y.EdgeCount || x.VertexCount != y.VertexCount)
                return false;

            var anyVertexDifferent = x.Vertices.OrderBy(e => e, vertexComparer)
                .Zip(y.Vertices.OrderBy(e => e, vertexComparer), (e, f) => vertexComparer.Compare(e, f))
                .SkipWhile(c => c == 0)
                .Any();

            if (anyVertexDifferent)
                return false;

            var anyEdgeDifferent = x.Edges.OrderBy(e => e, edgeComparer)
                .Zip(y.Edges.OrderBy(e => e, edgeComparer), (e, f) => edgeComparer.Compare(e, f))
                .SkipWhile(c => c == 0)
                .Any();

            if (anyEdgeDifferent)
                return false;

            return true;
        }

        /// <summary>
        /// Structurally hashes the vertices and edges of a graph.
        /// </summary>
        /// <param name="obj">The graph to hash.</param>
        public int GetHashCode(TGraph obj)
        {
            var vertexList = obj.Vertices.OrderBy(v => v, vertexComparer).HashElements();
            var edgeList = obj.Edges.OrderBy(e => e, edgeComparer).HashElements();

            return obj.DefaultHash(vertexList, edgeList);
        }
    }

    /// <summary>
    /// Extension methods for <see cref="GraphEqualityComparer{TGraph, TVertex, TEdge}"/>.
    /// </summary>
    public static class GraphEqualityComparer
    {
        /// <summary>
        /// Returns an <see cref="IComparer{T}"/> which uses the <see cref="IComparable{T}"/>-insance of the vertex <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the vertices.</typeparam>
        public static FuncComparer<T> VertexCompare<T>() where T : IComparable<T> => new FuncComparer<T>((c, d) => c.CompareTo(d), x => x.GetHashCode());

        /// <summary>
        /// Returns an <see cref="IComparer{T}"/> which uses the <see cref="IComparable{T}"/>-instance of the vertex <typeparamref name="T"/> and which compares the start- and edn-vertex of the edges.
        /// </summary>
        /// <typeparam name="T">The type of the vertices.</typeparam>
        public static FuncComparer<IEdge<T>> EdgeCompare<T>() where T : IComparable<T> => new FuncComparer<IEdge<T>>((e, f) => {
            var startComp = e.StartVertex.CompareTo(f.StartVertex);
            if (startComp != 0)
                return startComp;
            else
                return e.EndVertex.CompareTo(f.EndVertex);
        }, x => x.GetHashCode());

        /// <summary>
        /// Returns an <see cref="IComparer{T}"/> which uses an <see cref="IComparer{T}"/> <paramref name="comparer"/> on the vertex <typeparamref name="T"/> and which compares the start- and edn-vertex of the edges.
        /// </summary>
        /// <typeparam name="T">The type of the vertices.</typeparam>
        public static FuncComparer<IEdge<T>> EdgeCompare<T>(IComparer<T> comparer) => new FuncComparer<IEdge<T>>((e, f) => {
            var startComp = comparer.Compare(e.StartVertex, f.StartVertex);
            if (startComp != 0)
                return startComp;
            else
                return comparer.Compare(e.EndVertex, f.EndVertex);
        }, x => x.GetHashCode());
    }
}
