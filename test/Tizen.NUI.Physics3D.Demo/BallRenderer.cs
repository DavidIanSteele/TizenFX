/*
 * Copyright (c) 2023 Samsung Electronics Co., Ltd.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Render;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;


class BallRenderer : IRenderer
{
    private static Mesh _mesh;
    public View View;
        
    public BallRenderer(Vec3 size, string url)
    {
        View = CreateView(size, url);
    }

    private static readonly string VERTEX_SHADER =
        "attribute mediump vec3 aPosition;  // DALi shader builtin\n" +
        "attribute mediump vec2 aTexCoord;  // DALi shader builtin\n" +
        "uniform   mediump mat4 uMvpMatrix; // DALi shader builtin\n" +
        "uniform   mediump mat4 uViewMatrix; // DALi shader builtin\n" +
        "uniform   mediump mat4 uModelView; // DALi shader builtin\n" +
        "uniform   mediump vec3 uSize;      // DALi shader builtin\n" +
        "uniform   mediump vec3 uLightPos;  // Property\n" +
        "varying mediump vec3 vIllumination;\n" +
        "varying mediump vec2 vTexCoord;\n" +
        "\n" +
        "void main()\n" +
        "{\n" +
        "  mediump vec4 vertexPosition = vec4(aPosition, 1.0);\n" +
        "  mediump vec3 normal = normalize(vertexPosition.xyz);\n" +
        "\n" +
        "  vertexPosition.xyz *= uSize;\n" +
        "  vec4 pos = uModelView * vertexPosition;\n" +
        "\n" +
        "  vec4 lightPosition = vec4(uLightPos, 1.0);\n" +
        "  vec4 mvLightPos = uViewMatrix * lightPosition;\n" +
        "  vec3 vectorToLight = normalize(mvLightPos.xyz - pos.xyz);\n" +
        "  float lightDiffuse = max(dot(vectorToLight, normal), 0.0);\n" +
        "\n" +
        "  vIllumination = vec3(lightDiffuse * 0.5 + 0.5);\n" +
        "  vTexCoord = aTexCoord;\n" +
        "  gl_Position = uMvpMatrix * vertexPosition;\n" +
        "}\n";

    private static readonly string FRAGMENT_SHADER =
        "uniform sampler2D uTexture;\n" +
        "uniform mediump float uBrightness;\n" +
        "varying mediump vec2 vTexCoord;\n" +
        "varying mediump vec3 vIllumination;\n" +
        "\n" +
        "mediump vec3 redistribute_rgb(mediump vec3 color)\n" +
        "{\n" +
        "    mediump float threshold = 0.9999999;\n" +
        "    mediump float m = max(max(color.r, color.g), color.b);\n" +
        "    if(m <= threshold)\n" +
        "    {\n" +
        "        return color;\n" +
        "    }\n" +
        "    mediump float total = color.r + color.g + color.b;\n" +
        "    if( total >= 3.0 * threshold)\n" +
        "    {\n" +
        "        return vec3(threshold);\n" +
        "    }\n" +
        "    mediump float x = (3.0 * threshold - total) / (3.0 * m - total);\n" +
        "    mediump float gray = threshold - x * m;\n" +
        "    return vec3(gray) + vec3(x)*color;\n" +
        "}\n" +
        "\n" +
        "void main()\n" +
        "{\n" +
        "  mediump vec4 texColor = texture2D( uTexture, vTexCoord );\n" +
        "\n" +
        "  //mediump vec3 pcol=vec3(vIllumination.rgb * texColor.rgb)*(1.0+uBrightness);\n" +
        "  //gl_FragColor = vec4( redistribute_rgb(pcol), 1.0);\n" +
        "  //gl_FragColor = texColor;\n" +
        "  gl_FragColor = vec4(vIllumination.rgb * texColor.rgb, texColor.a);\n" +
        "}\n";

    static Shader gBallShader;

    private void MapUVsToSphere(ref List<Render.Vertex> vertices)
    {
        // Convert world coords to long-lat
        // Assume radius=1;
        // V=(cos(long)cos(lat), sin(long)cos(lat), sin(lat))
        // => lat=arcsin(z), range (-PI/2, PI/2); => 0.5+(asin(z)/PI) range(0,1)
        // => y/x = sin(long)/cos(long) => long = atan2(y/x), range(-pi, pi)
        var vertexSpan = CollectionsMarshal.AsSpan(_mesh.Vertices);
        for (var i = 0; i < vertices.Count; ++i)
        {
            vertexSpan[i].SetTexCoord(new Vec2(1.0f - (0.5f + (MathF.Asin(vertexSpan[i].aPosition.Z) / MathF.PI)),
                1.0f + (MathF.Atan2(vertexSpan[i].aPosition.Y, vertexSpan[i].aPosition.X) / (2.0f * MathF.PI))));
        }

        _mesh.Normalize();
    }

    private Geometry CreateBallGeometry()
    {
        var phi = (1.0f + MathF.Sqrt(5.0f)) * 0.5f; // golden ratio
        var a = 1.0f;
        var b = 1.0f / phi;

        _mesh = new Mesh();
        
        // add vertices
        var vertices = new List<Render.Vertex>
        {
            new Render.Vertex(new Render.Vec3(0, b, -a)),
            new Render.Vertex(new Render.Vec3(b, a, 0)),
            new Render.Vertex(new Render.Vec3(-b, a, -a)),
            new Render.Vertex(new Render.Vec3(0, b, a)),
            new Render.Vertex(new Render.Vec3(0, -b, a)),
            new Render.Vertex(new Render.Vec3(-a, 0, b)),
            new Render.Vertex(new Render.Vec3(0, -b, -a)),
            new Render.Vertex(new Render.Vec3(a, 0, -b)),
            new Render.Vertex(new Render.Vec3(a, 0, b)),
            new Render.Vertex(new Render.Vec3(-a, 0, -b)),
            new Render.Vertex(new Render.Vec3(b, -a, 0)),
            new Render.Vertex(new Render.Vec3(-b, -a, 0))
        };

        var indices = new List<ushort>
        {
            2, 1, 0, 1, 2, 3, 5, 4, 3, 4, 8, 3, 7, 6, 0, 6, 9, 0, 11, 10, 4, 10, 11, 6, 9, 5, 2, 5, 9, 11, 8,
            7, 1, 7, 8, 10, 2, 5, 3, 8, 1, 3, 9, 2, 0, 1, 7, 0, 11, 9, 6, 7, 10, 6, 5, 11, 3, 10, 8, 4
        };
        _mesh.Vertices = vertices;
        _mesh.Indices = indices;
        _mesh.Normalize();

        // 2 subdivisions gives a reasonably nice sphere
        _mesh.SubDivide();
        _mesh.SubDivide();

        MapUVsToSphere(ref vertices);

        var format = new PropertyMap();
        format.Add("aPosition", new PropertyValue((int)PropertyType.Vector3));
        format.Add("aTexCoord", new PropertyValue((int)PropertyType.Vector2));
        var vertexBuffer = new VertexBuffer(format);
        var vertexData = vertices.ToArray();
        vertexBuffer.SetData(vertexData);
        var ballGeometry = new Geometry();
        ballGeometry.AddVertexBuffer(vertexBuffer);
        ballGeometry.SetIndexBuffer(indices.ToArray(), (uint)indices.Count);
        ballGeometry.SetType(Geometry.Type.TRIANGLES);
        return ballGeometry;
    }

    private static TextureSet CreateTexture(string url)
    {
        // Load image from file
        var pb = ImageLoader.LoadImageFromFile(url); //, new Size2D(), FittingModeType.ScaleToFill);
        var pixels = PixelBuffer.Convert(pb);
        var texture = new Texture(TextureType.TEXTURE_2D, pixels.GetPixelFormat(), pixels.GetWidth(),
            pixels.GetHeight());
        texture.Upload(pixels);

        // create TextureSet
        var textureSet = new TextureSet();
        textureSet.SetTexture(0, texture);
        return textureSet;
    }

    private static Shader CreateShader()
    {
        if (!gBallShader)
        {
            gBallShader = new Shader(VERTEX_SHADER, FRAGMENT_SHADER);
        }

        return gBallShader;
    }

    private Renderer CreateRenderer(TextureSet textures)
    {
        var geometry = CreateBallGeometry();
        var shader = CreateShader();
        var renderer = new Renderer(geometry, shader);
        renderer.SetTextures(textures);
        renderer.FaceCullingMode = (int)FaceCullingModeType.Back;
        return renderer;
    }

    public View CreateView(Vec3 size, string url)
    {
        var actor = new View();
        actor.PivotPoint = PivotPoint.Center;
        actor.ParentOrigin = ParentOrigin.Center;
        actor.Position = new Vector3(0.0f, 0.0f, 0.0f);
        actor.Size = new Size(size.X * 0.5f, size.Y * 0.5f, size.Z * 0.5f);

        var textures = CreateTexture(url);
        var renderer = CreateRenderer(textures);
        actor.AddRenderer(renderer);
        actor.RegisterProperty("uLightPos", PropertyValue.CreateFromObject(new Vector3(400.0f, -400.0f, 400.0f)));
        return actor;
    }


}

