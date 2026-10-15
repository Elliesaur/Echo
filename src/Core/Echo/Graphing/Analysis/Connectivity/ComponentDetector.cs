using System;
using System.Collections.Generic;

namespace Echo.Graphing.Analysis.Connectivity
{
    /// <summary>
    /// Provides utility members for finding connected components within a graph.
    /// </summary>
    public static class ComponentDetector
    {
        /// <summary>
        /// Finds all strongly connected components in the provided graph.
        /// </summary>
        /// <param name="graph">The graph to get the components from.</param>
        /// <returns>A collection of sets representing the strongly connected components.</returns>
        public static ICollection<ISet<INode>> FindStronglyConnectedComponents(this IGraph graph)
        {
            return FindStronglyConnectedComponents(graph.GetNodes(), null);
        }

        /// <summary>
        /// Finds all strongly connected components among the nodes of the provided graph that pass a filter.
        /// </summary>
        /// <param name="graph">The graph to get the components from.</param>
        /// <param name="filter">Determines whether a node is included. Edges to nodes that are not included are ignored.</param>
        /// <returns>A collection of sets representing the strongly connected components.</returns>
        public static ICollection<ISet<INode>> FindStronglyConnectedComponents(this IGraph graph, Func<INode, bool> filter)
        {
            return FindStronglyConnectedComponents(graph.GetNodes(), filter);
        }

        private static ICollection<ISet<INode>> FindStronglyConnectedComponents(
            IEnumerable<INode> nodes,
            Func<INode, bool>? filter)
        {
            // This is an implementation of the Kosaraju’s algorithm, but altered to work on disconnected graphs
            // and the transposition of the graph is replaced with looking at the predecessors rather than the
            // successors of the current node in the depth first search. 
            
            var visited = new HashSet<INode>();
            var result = new List<ISet<INode>>();

            var stack = GetFillOrder(nodes, filter);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (!visited.Contains(current))
                    result.Add(ReverseDepthFirstSearch(visited, current, filter));
            }

            return result;
        }

        private static Stack<INode> GetFillOrder(IEnumerable<INode> nodes, Func<INode, bool>? filter)
        {
            // This used to be a recursive function, but was recoded to be iterative to avoid stack overflows.
            
            var result = new Stack<INode>();
            
            var visited = new HashSet<INode>();
            
            var agenda = new Stack<FillOrderState>();
            foreach (var node in nodes)
            {
                if (filter == null || filter(node))
                    agenda.Push(new FillOrderState(node, false));
            }

            while (agenda.Count > 0)
            {
                var currentState = agenda.Pop();
                if (!currentState.HasTraversedSuccessors)
                {
                    if (!visited.Add(currentState.Node))
                        continue;

                    // Schedule current node first to get it post-processed after the successors.
                    currentState.HasTraversedSuccessors = true;
                    agenda.Push(currentState);

                    // Schedule successors for initial processing.
                    foreach (var successor in currentState.Node.GetSuccessors())
                    {
                        if (!visited.Contains(successor) && (filter == null || filter(successor)))
                            agenda.Push(new FillOrderState(successor, false));
                    }
                }
                else
                {
                    // current node's successors were traversed, add to the resulting stack.
                    result.Push(currentState.Node);
                }
            }

            return result;
        }

        private struct FillOrderState
        {
            public FillOrderState(INode node, bool hasTraversedSuccessors)
            {
                Node = node;
                HasTraversedSuccessors = hasTraversedSuccessors;
            }
            
            public readonly INode Node;
            public bool HasTraversedSuccessors;
        }

        private static ISet<INode> ReverseDepthFirstSearch(ISet<INode> visited, INode start, Func<INode, bool>? filter)
        {
            var result = new HashSet<INode>();
            
            var agenda = new Stack<INode>();
            agenda.Push(start);

            while (agenda.Count > 0)
            {
                var current = agenda.Pop();
                result.Add(current);
                visited.Add(current);

                foreach (var predecessor in current.GetPredecessors())
                {
                    if (!visited.Contains(predecessor) && (filter == null || filter(predecessor)))
                        agenda.Push(predecessor);
                }
            }

            return result;
        }
    }
}