<p align="center">
  <img src="https://github.com/bassmit/images/blob/master/DotsNav/title.png?raw=true">
</p>
<p align="center">
  <a href="https://vimeo.com/505612775">Video</a>&nbsp;&nbsp;&nbsp;&nbsp;<a href="https://drive.google.com/uc?export=download&id=16oqRmsAZEoRKiqKRB47jXDkUoU7JM4sF">Demo</a>&nbsp;&nbsp;&nbsp;&nbsp;<a href="https://discord.gg/3kq4bhwY7w">Discord</a>&nbsp;&nbsp;&nbsp;&nbsp;<a href="https://github.com/sponsors/bassmit">Sponsor</a>
</p>
<p align="center">

## Table of Contents
- [Introduction](https://github.com/dotsnav/dotsnav/blob/master/README.md#introduction)
- [Getting Started](https://github.com/dotsnav/dotsnav/blob/master/README.md#getting-started)
- [Getting Started with DOTS](https://github.com/dotsnav/dotsnav/blob/master/README.md#getting-started-with-dots)
- [Technical Information](https://github.com/dotsnav/dotsnav/blob/master/README.md#technical-information)
- [Roadmap](https://github.com/dotsnav/dotsnav/blob/master/README.md#roadmap)
- [Bibliography](https://github.com/dotsnav/dotsnav/blob/master/README.md#bibliography)
- [Acknowledgements](https://github.com/dotsnav/dotsnav/blob/master/README.md#acknowledgements)

## Introduction
DotsNav is a fully dynamic and robust planar navmesh Unity package built on DOTS. It is fast enough to add and remove many obstacles each frame, supports agents of any size, and can be used through monobehaviours without prior knowledge of DOTS.

DotsNav is in an early stage of development. It passes demanding robustness tests as can be seen in the video and demo, but has not been used beyond developing the demo. DotsNav is known to run as Windows standalone, UWP app and on Android. Apple devices have not been tested.

To support further development consider becoming a sponsor and get access to the beta and development repositories which are a few months ahead of this repository, access tot the private discord channel and votes on the [roadmap](https://github.com/dotsnav/dotsnav/blob/master/README.md#roadmap). The first improvement will be to implement local avoidance by porting [rvo2-cs](https://github.com/snape/RVO2-CS).

## Getting Started
### Installing the package

While the package manager supports adding packages through a GitHub url, it is not able to notify you of updates to these packages. The recommended way of installing DotsNav is therefore through the [OpenUPM](https://openupm.com/packages/com.bassmit.dotsnav/) unitypackage installer (top right). After downloading the installer add the unitypackage to your project and DotsNav will be installed, including setting up the scoped registry required.

The package manager ui then needs to be augmented to show and install updates by installing [UpmGitExtension](https://openupm.com/packages/com.coffee.upm-git-extension/).

Alternatively, open the package manager and choose Add package from git URL.

![](https://github.com/bassmit/images/blob/master/DotsNav/image16.png?raw=true)

And enter the url.

    https://github.com/dotsnav/dotsnav.git


Samples can be imported through the package manager.

![](https://github.com/bassmit/images/blob/master/DotsNav/image18.png?raw=true)

### Navmesh
To create a navmesh attach a DotsNav Navmesh behaviour to a gameobject. The navmesh dimensions will be drawn in the scene view. A top down orthogonal perspective is usually the easiest way to view navmeshes and edit obstacles. Currently only one navmesh is allowed which will be centered around the origin. The value of Expected Verts determines the size of initial allocations.

![](https://github.com/bassmit/images/blob/master/DotsNav/image1.png?raw=true)

### Conversion to DOTS
Add a Convert to Entity component to the Navmesh so it is converted to DOTS. When using monobehaviours to develop a project choose “Convert and Inject”. Obstacles can then be removed from the navmesh by destroying the associated gameobject. The navmesh can be disposed of similarly.

![](https://github.com/bassmit/images/blob/master/DotsNav/image2.png?raw=true)

### Obstacles
To create an obstacle add a DotsNav Obstacle behaviour to a different gameobject and make sure it is converted.

![](https://github.com/bassmit/images/blob/master/DotsNav/image3.png?raw=true)
 
Add a few vertices and move them around using the position handle. The edit mode colors can be set in the DotsNav tab in Preferences.

![](https://github.com/bassmit/images/blob/master/DotsNav/image4.png?raw=true)

Alternatively an obstacle's Vertices array can be populated through script. Obstacle gameobjects can be scaled, rotated around their y axis, and used as prefabs.

### Pathfinder
To enable pathfinding agents first add a DotsNav Pathfinder behaviour and make sure it is converted. The navmesh gameobject is a good place to add the pathfinder, but this is not required.

![](https://github.com/bassmit/images/blob/master/DotsNav/image5.png?raw=true)

### Agents
Add a DotsNav Agent behaviour to a different gameobject and make sure it is converted.

![](https://github.com/bassmit/images/blob/master/DotsNav/image6.png?raw=true)

### Renderer
To draw the navmesh while playing, add a DotsNav Renderer component to a gameobject. If the renderer is attached to the camera it can draw in the game view as well as the scene view.

![](https://github.com/bassmit/images/blob/master/DotsNav/image7.png?raw=true)

### Path Queries
Path queries can be enqueued using DotsNavAgent.FindPath.

![](https://github.com/bassmit/images/blob/master/DotsNav/image8.png?raw=true)

Next navmesh update a path will be calculated.

![](https://github.com/bassmit/images/blob/master/DotsNav/image9.png?raw=true)

Each navmesh update the direction required to follow the path is calculated using the agent's position.

![](https://github.com/bassmit/images/blob/master/DotsNav/image10.png?raw=true)

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
The easiest way to get started with DOTS is to do all of the above, but instead use “Convert and Destroy”.

![](https://github.com/bassmit/images/blob/master/DotsNav/image14.png?raw=true)

This creates appropriate entities and components. Alternatively you can create entities and components manually, which is described below.

### Navmesh
Navmeshes are created by adding a NavmeshComponent to an entity. This allows you to supply the parameters used when the navmesh is created. Destroy the entity to dispose of its resources. There should only be one navmesh at any time.

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

Entities with static archetypes are destroyed after insertion. To remove dynamic entities from the navmesh destroy their associated entity.

### PathFinder
Add a PathFinderComponent to an entity. Use the constructor so it is initialized properly. Destroy the entity to dispose of its resources. There should be only one PathFinderComponent at any time.

### Agents
Create an entity with the following archetype:

- AgentComponent
- DynamicBuffer&lt;PathSegmentElement&gt;
- DynamicBuffer&lt;TriangleElement&gt;
- AgentDirectionComponent (optional)
- AgentDrawComponent (optional)

To trigger path queries set AgentComponent.State to Pending.

![](https://github.com/bassmit/images/blob/master/DotsNav/image15.png?raw=true)

### Accessing the triangulation
The Navmesh component provides access to the triangulation through the following methods, allowing for traversal of the triangulation and development of additional algorithms:

- Edge* FindTriangleContainingPoint
- Vertex* FindClosestVertex

## Technical Information
DotsNav uses a Local Clearance Triangulation which reduces path finding using an arbitrary agent radius to a single floating point comparison per expanded edge. It uses a quadedge to represent the triangulation, and a bucketed quadtree for point location.

DotsNav's insertion and removal algorithms are guaranteed to succeed and guarantee closed polygons remain closed, no matter how many intersecting obstacles are inserted or removed. In short, intersections use an existing point chosen to converge on the destination in the rare case no valid point can be created due to the density of the navmesh. When points chosen in this way are not connected directly, A* is used to determine which edges need to be constrained.

Due to the nature of the algorithms involved exact geometric predicates are required for a robust implementation. DotsNav relies on adaptive predicates, only when regular floating point calculations do not provide sufficient precision is the costly exact calculation performed. The predicates are [available separately](https://github.com/bassmit/robustgeometricpredicates).

DotsNav provides locally optimal search. First, a channel of connected triangles with enough clearance is found using A*. The optimal path given this channel is then found using the funnel algorithm. While channels found are often optimal they are not guaranteed to be. An algorithm to find the optimal channel exists, but can easily take 100 times longer to execute and is not currently implemented. As there are valid use cases for globally optimal search, if only to benchmark cost functions, it is included on the roadmap.

## Roadmap
The roadmap will be updated based on feedback.

- Collision avoidance, port rvo2c#
- Preferred radius to use where clearance allows
- Custom cost functions, so agents can prefer to avoid certain conditions
- Deterministic path finding budget and agent priorities
- Multiple and overlapping navmeshes and offmesh links
- Serialization for faster loading of large amounts of obstacles
- Queries to determine shapes are outside any obstacle
- Steering behaviours
- Globally optimal search, very slow but needed to benchmark cost functions
- Allow for loading or generating and unloading neighbouring chunks of navmesh
- Hierarchical path finding

## Bibliography
- Dynamic and Robust Local Clearance Triangulations, [Kallmann 2014](http://graphics.ucmerced.edu/papers/14-tog-lct.pdf)
- Shortest Paths with Arbitrary Clearance from Navigation Meshes, [Kallmann 2010](http://graphics.ucmerced.edu/papers/10-sca-tripath.pdf)
- Fully Dynamic Constrained Delaunay Triangulations, [Kallmann 2003](https://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.14.6477&rep=rep1&type=pdf)
- An Improved Incremental algorithm for constructing restricted Delaunay triangulations, [Vigo 1997](http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.61.3862&rep=rep1&type=pdf)
- Incremental Delaunay Triangulation, [Lischinski 1994](http://karlchenofhell.org/cppswp/lischinski.pdf)
- Primitives for the Manipulation of General Subdivisions and the Computation of Voronoi Diagrams, [Guibas and Stolfi 1985](http://sccg.sk/~samuelcik/dgs/quad_edge.pdf)
- Adaptive Precision Floating-Point Arithmetic and Fast Robust Predicates for Computational Geometry, [Shewchuk 1996](https://www.cs.cmu.edu/~quake/robust.html)

## Acknowledgements
I would like to thank Marcello Kallmann for describing the local clearance triangulation including robust and dynamic insertion and removal algorithms, Jonathan Shewchuk for placing his geometric primitives in the public domain, and Govert van Drimmelen for porting them to C#.
