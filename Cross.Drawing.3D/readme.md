## Coordinate System ##
- This library uses the right handed rectangular coordinate system and has the same X, Y coordinates as GDI+;

## Quaternion ##

In mathematics, quaternions are a non-commutative number system that extend the complex numbers.
They provide a convenient mathematical notation for representing orientations and rotations of objects in three
 dimensions. Quaternions have 4 dimensions (each quaternion consists of 4 scalar numbers), one real dimension w 
and 3 imaginary dimensions xi + yj + zk that can describe an axis of rotation and an angle.
Quaternions are often used in 3D engines to rotate points in space quickly.

**q** = w + xi + yj + zk = w + (x, y, z) = cos(a/2) + **u**sin(a/2)

where **u** is a unit vector and a is rotated angle around the u axis.

Let also **v** be an ordinary vector of the 3 dimensional space, considered as a quaternion with a real 
coordinate equal(w) to zero. Then the quaternion product:

qvq^-1

yields the vector **v** rotated by an angle a around the **u** axis. The rotation is clockwise if our line
 of sight points in the direction pointed by **u**. This operation is known as conjugation by **q**. 
The quaternion multiplication is composition of rotations, for if **p** and **q** are quaternions representing
 rotations, then rotation (conjugation) by **pq** is:

pqv(pq)^-1 = pqvp^-1p^-1 = p(qvq^-1)p^-1

## Projection ##
To project the 3D point to 2D, the first step is to translate the origin to the camera's Location and use camera's
 Quaternion to rotate the 3D objects in relation to camera's rotation. The second step is projection.