The goal of this project is to create a library which allows the creation of block based vehicles/physics objects in Vintage Story. I'm attempting to build off of the existing minidimension systems that exist in the game code already.
The main problem is that the existing system does not allow for players/other entities to collide with the object. To address this, I have created a type of cuboid called a PsuedoCuboid (what a creative name) that tracks a central position, length, width, height, and rotation in Quaternion form.
Another big barrier is that it seems like the existing testship function in the game does not render in the latest version of the game. I am at this point unsure if that is an issue for just me or if this is a broader issue. There are specific blocks that didn't render in 1.19.8, like doors and gears.
The third barrier is not being able to interact with blocks like chests or beds.

A lot of the code I use is either based on or blatantly ripped from the game files, which I think is fine?

I'd love help with this project! Reach out to me on discord (comrade_toothpick) if you are interested in providing your expertise in any way shape or form.

Major things that need to be worked on/fixed:
Blocks in MiniDimjensions do not render
PsuedoCuboids do not yet render an outline
Don't have an easy way to put PsuedoCuboids into the game world for testing purposes.

Lower priority issues to address:
Low familiarity with creating/editing entity behaviors, need to better understand to implement
No idea if my implementation of Quaternions works correctly (testable without being able to see it by logging points while it rotates/moves)
