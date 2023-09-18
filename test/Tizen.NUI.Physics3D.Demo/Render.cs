using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Render
{
    public struct Vec2
    {
        public float X { get; }
        public float Y { get; }

        public Vec2(float x, float y)
        {
            this.X = x;
            this.Y = y;
        }

        public static Vec2 operator -(Vec2 a, Vec2 b)
        {
            return new Vec2(a.X - b.X, a.Y - b.Y);
        }
    }

    public struct Vec3
    {
        public float X;
        public float Y;
        public float Z;

        public Vec3(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public static Vec3 operator +(Vec3 a, Vec3 b)
        {
            return new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vec3 operator -(Vec3 a, Vec3 b)
        {
            return new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vec3 operator *(Vec3 a, float scale)
        {
            return new Vec3(a.X * scale, a.Y * scale, a.Z * scale);
        }

        public float Length()
        {
            return MathF.Sqrt(X * X + Y * Y + Z * Z);
        }

        public static Vec3 Normalize(Vec3 aVec3)
        {
            var length = aVec3.Length();
            return length <= 0 ? aVec3 : new Vec3(aVec3.X / length, aVec3.Y / length, aVec3.Z / length);
        }
    }
    
    public struct Vertex
    {
        public Vec3 aPosition;
        public Vec2 aTexCoord;

        public Vertex(Vec3 position, Vec2 uv)
        {
            this.aPosition = new Vec3(position.X, position.Y, position.Z);
            this.aTexCoord = new Vec2(uv.X, uv.Y);
        }

        public Vertex(Vec3 position)
        {
            this.aPosition = new Vec3(position.X, position.Y, position.Z);
            this.aTexCoord = new Vec2(0.0f, 0.0f);
        }

        public void SetTexCoord(Vec2 uv)
        {
            this.aTexCoord = new Vec2(uv.X, uv.Y);
        }
    }

    class Mesh
    {
        private List<Vertex> vertices;
        private List<ushort> indices;

        public List<Vertex> Vertices
        {
            get => vertices;
            set => vertices = value;
        }

        public List<ushort> Indices
        {
            get => indices;
            set => indices = value;
        }
        
        public Mesh()
        {
            vertices = new List<Vertex>();
            indices = new List<ushort>();
        }

        public void SubDivide()
        {
            var triangleCount = indices.Count / 3;
            for (var i = 0; i < triangleCount; ++i)
            {
                var v1 = vertices[indices[i * 3]].aPosition;
                var v2 = vertices[indices[i * 3 + 1]].aPosition;
                var v3 = vertices[indices[i * 3 + 2]].aPosition;
                // Triangle subdivision adds pts halfway along each edge.
                var v4 = v1 + (v2 - v1) * 0.5f;
                var v5 = v2 + (v3 - v2) * 0.5f;
                var v6 = v3 + (v1 - v3) * 0.5f;
                var j = vertices.Count;
                vertices.Add(new Vertex(v4, new Vec2()));
                vertices.Add(new Vertex(v5, new Vec2()));
                vertices.Add(new Vertex(v6, new Vec2()));

                // Now, original tri breaks into 4, so replace this tri, and add 3 more
                var i1 = indices[i * 3 + 1];
                var i2 = indices[i * 3 + 2];
                indices[i * 3 + 1] = (ushort)j;
                indices[i * 3 + 2] = (ushort)(j + 2);

                var newTris = new List<ushort>
                {
                    (ushort)j, i1, (ushort)(j + 1), (ushort)j, (ushort)(j + 1), (ushort)(j + 2), (ushort)(j + 1), i2,
                    (ushort)(j + 2)
                };
                indices.AddRange(newTris);
            }
        }

        public void Normalize()
        {
            Span<Render.Vertex> vertexSpan = CollectionsMarshal.AsSpan(vertices);
            for (var i = 0; i < vertices.Count; ++i)
            {
                vertexSpan[i].aPosition = Vec3.Normalize(vertexSpan[i].aPosition);
            }
        }
    }
}