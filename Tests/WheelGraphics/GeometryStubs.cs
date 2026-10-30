// Geometry-only substitutes. These compile the actual mesh generators but cannot
// emulate Unity component lifecycle, CanvasRenderer, shaders, or device rendering.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class,Inherited=true)]
    public sealed class RequireComponent : Attribute
    {
        public readonly Type Type;
        public RequireComponent(Type type) { Type=type; }
    }
    public sealed class CanvasRenderer { }
    public struct Vector2
    {
        public float x,y;
        public Vector2(float x,float y) { this.x=x; this.y=y; }
        public static Vector2 zero { get { return new Vector2(0,0); } }
        public static Vector2 operator +(Vector2 a,Vector2 b) { return new Vector2(a.x+b.x,a.y+b.y); }
        public static Vector2 operator -(Vector2 a,Vector2 b) { return new Vector2(a.x-b.x,a.y-b.y); }
        public static Vector2 operator *(Vector2 a,float n) { return new Vector2(a.x*n,a.y*n); }
    }
    public struct Color
    {
        public float r,g,b,a;
        public Color(float r,float g,float b,float a=1) { this.r=r; this.g=g; this.b=b; this.a=a; }
    }
    public struct Rect
    {
        public float width,height;
        public Rect(float width,float height) { this.width=width; this.height=height; }
        public Vector2 center { get { return Vector2.zero; } }
    }
    public sealed class RectTransform { public Rect rect=new Rect(400,400); }
    public static class Mathf
    {
        public const float PI=(float)Math.PI;
        public static float Sin(float a) { return (float)Math.Sin(a); }
        public static float Cos(float a) { return (float)Math.Cos(a); }
        public static float Min(float a,float b) { return Math.Min(a,b); }
    }
}
namespace UnityEngine.UI
{
    using UnityEngine;
    public abstract class Graphic
    {
        public readonly RectTransform rectTransform=new RectTransform();
        public int Dirtied { get; private set; }
        public void SetVerticesDirty() { Dirtied++; }
        protected abstract void OnPopulateMesh(VertexHelper vh);
        public VertexHelper Generate(float width,float height)
        { rectTransform.rect=new Rect(width,height); var vh=new VertexHelper(); OnPopulateMesh(vh); return vh; }
    }
    public sealed class VertexHelper
    {
        public readonly List<(Vector2 position,Color color)> Vertices=new();
        public readonly List<(int a,int b,int c)> Triangles=new();
        public int currentVertCount { get { return Vertices.Count; } }
        public void Clear() { Vertices.Clear(); Triangles.Clear(); }
        public void AddVert(Vector2 point,Color color,Vector2 uv) { Vertices.Add((point,color)); }
        public void AddTriangle(int a,int b,int c) { Triangles.Add((a,b,c)); }
    }
}
namespace OneMoreFloor
{
    using UnityEngine;
    public static class GameUI
    {
        public static Color Hex(string value)
        { int n=Convert.ToInt32(value,16); return new Color(((n>>16)&255)/255f,((n>>8)&255)/255f,(n&255)/255f); }
    }
}
