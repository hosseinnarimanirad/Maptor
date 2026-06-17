# O(n) Simplification Algorithm

A research project implementing a linear-time O(n) geometry simplification algorithm.

The algorithm traverses the point sequence once, selecting representative vertices while preserving the overall shape — making it significantly faster than Ramer–Douglas–Peucker (O(n log n)) for large datasets at the cost of some accuracy.