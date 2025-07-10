using System;
using System.ComponentModel;
using Avalonia;

namespace PixelEditor;

public static class PointExtensions
{
    public static Point SwapCoordinates(this  Point point) => new(point.Y, point.X);
    public static double SquaredDistanceTo(this Point point, Point other)
    {
        double dx = point.X - other.X;
        double dy = point.Y - other.Y;
        return dx * dx + dy * dy;
    }
    public static double DistanceTo(this Point point, Point other) => Math.Sqrt(point.SquaredDistanceTo(other));
    public static Point WithOffset(this Point point, double offsetX, double offsetY) => new(point.X + offsetX, point.Y + offsetY);

    record vec2(double x, double y)
    {
        public vec2 yx => new vec2(y, x);
        public static vec2 operator -(vec2 v) => new vec2(-v.x, -v.y);
        public static vec2 operator +(vec2 a, vec2 b) => new vec2(a.x + b.x, a.y + b.y);
        public static vec2 operator -(vec2 a, vec2 b) => new vec2(a.x - b.x, a.y - b.y);
        public static vec2 operator *(vec2 v, double s) => new vec2(v.x * s, v.y * s);
        public static vec2 operator *(double s, vec2 v) => new vec2(v.x * s, v.y * s);
        public static vec2 operator /(vec2 v, double s) => new vec2(v.x / s, v.y / s);
        public static vec2 operator *(vec2 a, vec2 b) => new vec2(a.x * b.x, a.y * b.y);
        public static vec2 operator /(vec2 a, vec2 b) => new vec2(a.x / b.x, a.y / b.y);
        
    };
    
    public static double DistanceToEllipse(this Point point, double a, double b)
    {
        static double acos(double x) => (double)Math.Acos(x);
        static double sqrt(double x) => (double)Math.Sqrt(x);
        static double sign(double x) => x < 0 ? -1 : 1;
        static double abs(double x) => x < 0 ? -x : x;
        static vec2 absv(vec2 v) => new vec2(abs(v.x), abs(v.y));
        static double length(vec2 v) => sqrt(v.x * v.x + v.y * v.y);
        static double pow(double x, double y) => (double)Math.Pow(x, y);
        static double cos(double x) => (double)Math.Cos(x);
        static double sin(double x) => (double)Math.Sin(x);
        static double cbrt(double x) => (double)Math.Cbrt(x);
        static vec2 vec2(double x, double y) => new vec2(x, y);
        static vec2 normalize(vec2 v) => v / length(v);
        static double dot(vec2 a, vec2 b) => a.x * b.x + a.y * b.y;
        static double sdEllipse2( vec2 p, vec2 ab )
        {
            // symmetry
            p = absv( p );
    
            // initial value
            vec2 q = ab*(p-ab);
            vec2 cs = normalize( (q.x<q.y) ? vec2(0.01,1) : vec2(1,0.01) );
    
            // find root with Newton solver
            for( int i=0; i<5; i++ )
            {
                vec2 u = ab*vec2( cs.x,cs.y);
                vec2 v = ab*vec2(-cs.y,cs.x);
                double a = dot(p-u,v);
                double c = dot(p-u,u) + dot(v,v);
                double b = sqrt(c*c-a*a);
                cs = vec2( cs.x*b-cs.y*a, cs.y*b+cs.x*a )/c;
            }
    
            // compute final point and distance
            double d = length(p-ab*cs);
    
            // return signed distance
            return (dot(p/ab,p/ab)>1.0) ? d : -d;
        }

        static double sdEllipse( vec2 p, vec2 ab )
        {
            p = absv( p );
            if( p.x>p.y ){ p=p.yx; ab=ab.yx; }
	
            double l = ab.y*ab.y - ab.x*ab.x;
            var lab = length(ab);
            if (l == 0) return Math.Pow(length(p) - length(ab), 2);
            
            double m = ab.x*p.x/l; double m2 = m*m;
            double n = ab.y*p.y/l; double n2 = n*n;
            double c = (m2+n2-1)/3.0; double c2 = c*c; double c3 = c2*c;
            double d = c3 + m2*n2;
            double q = d  + m2*n2;
            double g = m  + m *n2;

            double co;
            if( d<0.0 )
            {
                double h = acos(q/c3)/3;
                double s = cos(h) + 2;
                double t = sin(h)*sqrt(3);
                double rx = sqrt( m2-c*(s+t) );
                double ry = sqrt( m2-c*(s-t) );
                co = ry + sign(l)*rx + abs(g)/(rx*ry);
            }
            else
            {
                double h = 2*m*n*sqrt( d );
                double s = cbrt(q + h);
                double t = c2/s;
                double rx = -(s+t) - c*4 + 2*m2;
                double ry =  (s-t)*sqrt(3);
                double rm = sqrt( rx*rx + ry*ry );
                co = ry/sqrt(rm-rx) + 2*g/rm;
            }
            co = (co-m)/2;

            double si = sqrt( 1 - co*co );
            vec2 r = new vec2( ab.x*co, ab.y*si );
            return length(p-r) * sign(p.y-r.y);
        }
        return sdEllipse2(new((double)point.X, (double)point.Y), new((double)a, (double)b));
    }
    
    
}