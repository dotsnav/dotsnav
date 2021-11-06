<p align="center">
  <img src="https://github.com/bassmit/images/blob/master/DotsNav/title.png?raw=true">
</p>
<p align="center">
  <a href="https://vimeo.com/505612775">Video (v0.1)</a>&nbsp;&nbsp;&nbsp;&nbsp;<a href="https://vimeo.com/608336630">Video (avoidance)</a>&nbsp;&nbsp;&nbsp;&nbsp;<a href="https://drive.google.com/uc?export=download&id=1Z7bYa7unqLYBCRxgVJnd1EG94Xhdvb4L">Demo</a>&nbsp;&nbsp;&nbsp;&nbsp;<a href="https://discord.gg/3kq4bhwY7w">Discord</a>&nbsp;&nbsp;&nbsp;&nbsp;<a href="https://github.com/sponsors/bassmit">Sponsor</a>
</p>
<p align="center">

## Table of Contents
- [Introduction](https://github.com/dotsnav/dotsnav/blob/master/README.md#introduction)
- [Getting Started](https://github.com/dotsnav/dotsnav/blob/master/README.md#getting-started)
- [Getting Started with DOTS](https://github.com/dotsnav/dotsnav/blob/master/README.md#getting-started-with-dots)
- [Technical Information](https://github.com/dotsnav/dotsnav/blob/master/README.md#technical-information)
- [Known Issues](https://github.com/dotsnav/dotsnav/blob/master/README.md#known-issues)
- [Bibliography](https://github.com/dotsnav/dotsnav/blob/master/README.md#bibliography)
- [Acknowledgements](https://github.com/dotsnav/dotsnav/blob/master/README.md#acknowledgements)

## Introduction
DotsNav is a fully dynamic and robust planar navmesh Unity package built on DOTS. It is fast enough to add and remove many obstacles each frame, supports agents of any size, and can be used through monobehaviours without prior knowledge of DOTS.

To support further development consider becoming a [sponsor](https://github.com/sponsors/bassmit).

## Getting Started
### Installation
If [OpenUpm](https://openupm.com/packages/com.dotsnav.dotsnav/) is not installed, ensure [Node.js](https://nodejs.org/en/download/) is installed so you have access to npm and run:
    
    npm install -g openupm-cli
    
To add DotsNav to a Unity project, open a prompt in the project root and run:

    openupm add com.bassmit.dotsnav

### Planes
Attach a DotsNavPlane behaviour to a gameobject. Its border will be drawn in the scene view.

![](https://github.com/bassmit/images/blob/master/DotsNav/image21.png?raw=true)

To create a navmesh attach a DotsNavNavmesh behaviour. The value of ExpectedVerts determines the size of initial allocations.

![](https://github.com/bassmit/images/blob/master/DotsNav/image28.png?raw=true)

To enable local avoidance attach a DotsNavLocalAvoidance behaviour. This behaviour does not require a navmesh.
![](https://github.com/bassmit/images/blob/master/DotsNav/image22.png?raw=true)
    
### Pathfinder
To enable path finding attach a DotsNavPathfinder behaviour to a gameobject. The pathfinder manages the resources required to search for paths on any number of threads, and only one pathfinder is allowed at any time.

![](https://github.com/bassmit/images/blob/master/DotsNav/image23.png?raw=true) 

### Obstacles
To create an obstacle add a DotsNavObstacle behaviour to a gameobject and assign the Plane field. When spawning obstacle prefabs make sure to assign the Plane immediately after instantiation.

![](https://github.com/bassmit/images/blob/master/DotsNav/image24.png?raw=true)

Add DotsNavNavmeshObstacle and DotsNavLocalAvoidanceObstacle behaviours as appropriate.

![](https://github.com/bassmit/images/blob/master/DotsNav/image20.png?raw=true)
  
Add a few vertices and move them around using the position handle. The edit mode colors can be set in the DotsNav tab in Preferences.

![](https://github.com/bassmit/images/blob/master/DotsNav/image4.png?raw=true)
  
Alternatively an obstacle's Vertices array can be populated through script. Obstacle gameobjects can be scaled, rotated and used as prefabs. Obstacles are projected on to their respective planes when inserted.

### Agents
To create an agent attach a DotsNavAgent behaviour to a gameobject and assign the Plane field. When spawning agent prefabs make sure to assign the Plane immediately after instantiation.

![](https://github.com/bassmit/images/blob/master/DotsNav/image25.png?raw=true)

Add DotsNavPathfindingAgent and DotsNavLocalAvoidanceAgent behaviours as appropriate.

![](https://github.com/bassmit/images/blob/master/DotsNav/image26.png?raw=true)

Note that DotsNavPathfindingAgent and DotsNavLocalAvoidanceAgent have Direction and Velocity fields respectively. Currently no steering behaviours are provided, but a basic example can be seen in the avoidance sample scene.
  
### Conversion to DOTS
Attach a Convert to Entity component all to planes, agents, obstacles and the pathfinder. When using monobehaviours to develop a project choose “Convert and Inject”. Obstacles and agents can then be removed by destroying the associated gameobject. Planes can be disposed of similarly.

![](https://github.com/bassmit/images/blob/master/DotsNav/image27.png?raw=true)

### Renderer
To draw the navmesh while playing, add a DotsNavRenderer component to a gameobject. If the renderer is attached to the camera it can draw in the game view as well as the scene view.

![](https://github.com/bassmit/images/blob/master/DotsNav/image7.png?raw=true)

### Path Queries
Path queries can be enqueued using DotsNavAgent.FindPath.

![](https://github.com/bassmit/images/blob/master/DotsNav/image8.png?raw=true)

Next navmesh update a path will be calculated.

![](https://github.com/bassmit/images/blob/master/DotsNav/image9.png?raw=true)

Each navmesh update the direction required to follow the path is calculated using the agent's position.

![](https://github.com/bassmit/images/blob/master/DotsNav/image28.png?raw=true)

Using the default settings, invalidated paths are recalculated automatically.

![](https://github.com/bassmit/images/blob/master/DotsNav/image11.png?raw=true)

For multiple agents, path finding is performed in parallel.

![](https://github.com/bassmit/images/blob/master/DotsNav/image19.png?raw=true)

### Creating obstacles from code
Obstacle prefabs are automatically enqueued for insertion when spawned. Alternatively obstacles can be inserted by calling InsertObstacle and providing a list of vertices.

![](https://github.com/bassmit/images/blob/master/DotsNav/image12.png?raw=true)

Obstacles can be removed by destroying a previously spawned gameobject, or by calling RemoveObstacle providing an id returned by InsertObstacle.

![](https://github.com/bassmit/images/blob/master/DotsNav/image13.png?raw=true)

## Getting Started with DOTS
### Conversion
When using monobehaviour conversion use “Convert and Destroy”.

![](https://github.com/bassmit/images/blob/master/DotsNav/image14.png?raw=true)

Note that you can use the DOTS Editor to inspect these and entities in the sample scenes to get a better understanding of the components involved.

### API
All public APIs expose read-only operations. Write operations are triggered by creating entities with appropriate archetypes, or updating component data.

### Planes
Navmeshes are created by adding a NavmeshComponent to an entity. Local avoidance requires an ObstacleTreeComponent and a DynamicTreeComponent. Destroy the entity to dispose of its resources.

### PathFinder
When adding a PathFinderComponent to an entity, use the constructor so it is initialized properly. Destroy the entity to dispose of its resources. There should be only one PathFinderComponent at any time.

### Obstacles
There are two types of obstacles:

- Dynamic, these obstacles are associated with an entity through which they can be identified and removed
- Static, these obstacles can not be removed or identified beyond being static, but can be inserted in bulk

The following archetypes trigger obstacle insertion:

- Dynamic
  - ObstacleComponent, DynamicBuffer&lt;VertexElement&gt;
  - ObstacleComponent, VertexBlobComponent
- Static
  - DynamicBuffer&lt;VertexElement&gt;, DynamicBuffer&lt;VertexAmountElement&gt;
  - ObstacleBlobComponent

Note that any obstacle requires a NavmeshObstacleComponent and or a ObstacleTreeElementComponent. Obstacles with static archetypes are destroyed after insertion. To destroy dynamic obstacles destroy their associated entity.

### Agents
General components:
- RadiusComponent

Pathfinding related components:
- NavmeshAgentComponent
- PathQueryComponent
- DynamicBuffer&lt;PathSegmentElement&gt;
- DynamicBuffer&lt;TriangleElement&gt;
- DirectionComponent (optional)
- AgentDrawComponent (optional)

Local avoidance related components:
- DynamicTreeElementComponent
- ObstacleTreeAgentComponent
- VelocityObstacleComponent
- RVOSettingsComponent
- MaxSpeedComponent
- PreferredVelocityComponent
- VelocityComponent

To trigger a path query set PathQuery.State to Pending.

### Accessing the triangulation
The NavmeshComponent provides access to the triangulation through the following methods, allowing for traversal of the triangulation and development of additional algorithms:

- Edge* FindTriangleContainingPoint
- Vertex* FindClosestVertex

## Technical Information
DotsNav uses a Local Clearance Triangulation which reduces path finding using an arbitrary agent radius to a single floating point comparison per expanded edge. It uses a quadedge to represent the triangulation, and a bucketed quadtree for point location.

DotsNav's insertion and removal algorithms are guaranteed to succeed and guarantee closed polygons remain closed, no matter how many intersecting obstacles are inserted or removed. In short, intersections use an existing point chosen to converge on the destination in the rare case no valid point can be created due to the density of the navmesh. When points chosen in this way are not connected directly, A* is used to determine which edges need to be constrained.

Due to the nature of the algorithms involved exact geometric predicates are required for a robust implementation. DotsNav relies on adaptive predicates, only when regular floating point calculations do not provide sufficient precision is the costly exact calculation performed. The predicates are [available separately](https://github.com/bassmit/robustgeometricpredicates).

DotsNav provides locally optimal search. First, a channel of connected triangles with enough clearance is found using A*. The optimal path given this channel is then found using the funnel algorithm. While channels found are often optimal they are not guaranteed to be. An algorithm to find the optimal channel exists, but can easily require several orders of magnitude more time to execute and is not currently implemented.

## Known Issues
- Performance appears to be significantly worse and less consistent from frame to frame in a build as compared to in the editor.
- As globally optimal path search is not implemented the quality of the cost function can not easily be determined.
- Local avoidance agent settings are fragile and require trial and error to find configurations yielding acceptable results.
- Synchronizing gameobjects and entities can almost certainly be improved, as no research has gone in to the intended mechanisms for doing so.
- Summary comments have not been thoroughly checked, please raise an issue or create a pull request to correct any errors you may find.

## Bibliography
- Reciprocal n-body Collision Avoidance, [Jur van den Berg, Stephen J. Guy, Ming Lin, Dinesh Manocha 2011](https://gamma.cs.unc.edu/ORCA/publications/ORCA.pdf)
- Reciprocal Velocity Obstacles for Real-Time Multi-Agent Navigation, [Jur van den Berg, Ming C. Lin, Dinesh Manocha 2008](https://gamma.cs.unc.edu/RVO/icra2008.pdf)
- Dynamic and Robust Local Clearance Triangulations, [Kallmann 2014](http://graphics.ucmerced.edu/papers/14-tog-lct.pdf)
- Shortest Paths with Arbitrary Clearance from Navigation Meshes, [Kallmann 2010](http://graphics.ucmerced.edu/papers/10-sca-tripath.pdf)
- Fully Dynamic Constrained Delaunay Triangulations, [Kallmann 2003](https://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.14.6477&rep=rep1&type=pdf)
- An Improved Incremental algorithm for constructing restricted Delaunay triangulations, [Vigo 1997](http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.61.3862&rep=rep1&type=pdf)
- Incremental Delaunay Triangulation, [Lischinski 1994](http://karlchenofhell.org/cppswp/lischinski.pdf)
- Primitives for the Manipulation of General Subdivisions and the Computation of Voronoi Diagrams, [Guibas and Stolfi 1985](http://sccg.sk/~samuelcik/dgs/quad_edge.pdf)
- Adaptive Precision Floating-Point Arithmetic and Fast Robust Predicates for Computational Geometry, [Shewchuk 1996](https://www.cs.cmu.edu/~quake/robust.html)

## Acknowledgements
I would like to thank Marcello Kallmann for describing the local clearance triangulation including robust and dynamic insertion and removal algorithms, Jonathan Shewchuk for placing his geometric primitives in the public domain, Govert van Drimmelen for porting them to C#, and Jamie Snape for making available a C# implementation of RVO2.
