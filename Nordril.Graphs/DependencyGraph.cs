using Castle.Core;
using Castle.Windsor;
using Nordril.Functional.Data;
using SD.Tools.Algorithmia.Graphs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Nordril.Graphs
{
    /// <summary>
    /// A dependency graph for <see cref="IWindsorContainer"/> which can be used to visualize dependencies and detect dependency cycles.
    /// </summary>
    public class DependencyGraph
    {
        private readonly IWindsorContainer container;

        private DirectedGraph<Type, DirectedEdge<Type>> cachedGraph = null;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="container">The IOC container for which to create the dependency graph.</param>
        public DependencyGraph(IWindsorContainer container)
        {
            this.container = container;
        }

        /// <summary>
        /// Writes the container's dependency graph to a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="forceRecompute">Whether to force the recomputation of the dependency graph. If false, the cached graph will be used, if it has been previously computed.</param>
        /// <typeparam name="T">The concrete type of <see cref="TextWriter"/> to use.</typeparam>
        public State<T, Unit> WriteGraph<T>(bool forceRecompute = false)
            where T : TextWriter
        { 
            return new State<T, Unit>(tw =>
            {
                var g = CreateGraph(forceRecompute);

                foreach (var v in g.Vertices)
                {
                    tw.WriteLine($"{v} ->");

                    foreach (var v2 in g.GetAdjacencyListForVertex(v).Keys)
                    {
                        tw.Write("   ");
                        tw.WriteLine(v2.FullName);
                    }
                }

                return (new Unit(), tw);
            });
        }

        /// <summary>
        /// Creates and returns the dependency graph. The result will be cached.
        /// </summary>
        /// <param name="forceRecompute">Whether to force the recomputation of the dependency graph. If false, the cached graph will be used, if it has been previously computed.</param>
        public DirectedGraph<Type, DirectedEdge<Type>> CreateGraph(bool forceRecompute = false)
        {
            if (!forceRecompute && cachedGraph != null)
                return cachedGraph;

            var graphNodes = container.Kernel.GraphNodes;
            var graph = new DirectedGraph<Type, DirectedEdge<Type>>();

            //Build the nodes
            foreach (var graphNode in graphNodes)
            {
                if (graphNode is ComponentModel componentModel)
                {
                    foreach (var service in componentModel.Services)
                        if (!graph.Contains(service))
                            graph.Add(service);

                    if (!graph.Contains(componentModel.Implementation))
                        graph.Add(componentModel.Implementation);
                }
            }

            //Build the edges
            foreach (var graphNode in graphNodes)
            {
                if (graphNode is ComponentModel componentModel)
                {
                    foreach (var dep in componentModel.Dependencies)
                    {
                        foreach (var service in componentModel.Services)
                            graph.Add(new DirectedEdge<Type>(service, dep.TargetItemType));

                        graph.Add(new DirectedEdge<Type>(componentModel.Implementation, dep.TargetItemType));
                    }
                }
            }

            cachedGraph = graph;
            return graph;
        }

        /// <summary>
        /// Returns true iff the graph has cycles. The dependency from <see cref="IWindsorContainer"/> to <see cref="IWindsorContainer"/> will be excluded, as that is the one permissible cycle according to Castle Windsor.
        /// </summary>
        /// <param name="cycles">The vertices which form part of cycles, with their outgoing edges. The outgoing edges indicates dependencies of the vertex. If there are no cycles, <c>default</c> will be returned.</param>
        public bool HasCycles(out IDictionary<Type, ISet<Type>> cycles)
        {
            var g = CreateGraph(false);

            var sorter = new CycleDetectingSorter<Type, DirectedEdge<Type>>(g, true);
            cycles = default;

            var result = sorter.SortWithCycleDetection(out var _);

            if (result.HasValue)
            {
                cycles = new Dictionary<Type, ISet<Type>>();

                foreach (var cycle in result.Value())
                    cycles[cycle.Item1] = cycle.Item2.Select(c => c.EndVertex).ToHashSet();

                //Special case: we exclude the cycle {IWindsorContainer -> IWindsorContainer}
                if (HasOnlyWindsorCycle(cycles))
                {
                    cycles = default;
                    return false;
                }

                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Returns true iff <paramref name="cycles"/> consists of only one cycle which goes from <see cref="IWindsorContainer"/> to <see cref="IWindsorContainer"/> and nothing else.
        /// </summary>
        /// <param name="cycles">The set of cycles to check.</param>
        private bool HasOnlyWindsorCycle(IDictionary<Type, ISet<Type>> cycles)
            => cycles.Count == 1
            && cycles.TryGetValue(typeof(IWindsorContainer), out var deps)
            && deps.Count == 1
            && deps.First() == typeof(IWindsorContainer);
    }
}
