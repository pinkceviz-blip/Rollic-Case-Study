using UnityEngine;
using UnityEngine.UI;
namespace Rollic.BusJam
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MenuIconGraphic : MaskableGraphic
    {
        public enum Symbol { Profile,Bus,Gear,Home,Levels,Palette,Help,Star,Lock,Sound,Trophy,Clock }
        public Symbol symbol;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            switch(symbol)
            {
                case Symbol.Clock:
                    Circle(vh,0,0,.41f);HoleCircle(vh,0,0,.32f);
                    Line(vh,Vector2.zero,new Vector2(0,.22f),.055f);Line(vh,Vector2.zero,new Vector2(.18f,-.09f),.055f);Circle(vh,0,0,.055f);break;
                case Symbol.Trophy:
                    for(int side=-1;side<=1;side+=2)for(int i=0;i<10;i++)
                    {
                        float a=i*Mathf.PI/10,b=(i+1)*Mathf.PI/10;
                        Line(vh,new Vector2(side*(.22f+Mathf.Sin(a)*.16f),.16f+Mathf.Cos(a)*.16f),new Vector2(side*(.22f+Mathf.Sin(b)*.16f),.16f+Mathf.Cos(b)*.16f),.065f);
                    }
                    Round(vh,-.24f,-.06f,.48f,.43f,.10f);Box(vh,-.045f,-.28f,.09f,.25f);Round(vh,-.22f,-.38f,.44f,.12f,.04f);break;
                case Symbol.Profile:
                    Circle(vh,0,.19f,.20f);Round(vh,-.34f,-.37f,.68f,.38f,.16f);break;
                case Symbol.Bus:
                    Round(vh,-.36f,-.29f,.72f,.65f,.12f);Box(vh,-.40f,-.39f,.15f,.18f);Box(vh,.25f,-.39f,.15f,.18f);
                    Box(vh,-.43f,.02f,.07f,.18f);Box(vh,.36f,.02f,.07f,.18f);
                    HoleBox(vh,-.27f,.06f,.54f,.20f);HoleCircle(vh,-.23f,-.15f,.055f);HoleCircle(vh,.23f,-.15f,.055f);break;
                case Symbol.Gear:
                    for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Line(vh,new Vector2(Mathf.Sin(a),Mathf.Cos(a))*.24f,new Vector2(Mathf.Sin(a),Mathf.Cos(a))*.42f,.13f);}Circle(vh,0,0,.29f);HoleCircle(vh,0,0,.13f);break;
                case Symbol.Home:
                    Triangle(vh,new Vector2(-.44f,.02f),new Vector2(0,.43f),new Vector2(.44f,.02f));Round(vh,-.31f,-.37f,.62f,.46f,.04f);HoleBox(vh,-.09f,-.37f,.18f,.28f);break;
                case Symbol.Levels:
                    for(int x=0;x<2;x++)for(int y=0;y<2;y++)Round(vh,-.35f+x*.41f,-.35f+y*.41f,.29f,.29f,.05f);break;
                case Symbol.Palette:
                    Circle(vh,0,0,.37f);HoleCircle(vh,.17f,-.18f,.12f);HoleCircle(vh,-.18f,.12f,.06f);HoleCircle(vh,.02f,.22f,.06f);HoleCircle(vh,.21f,.10f,.06f);break;
                case Symbol.Help:
                    Circle(vh,0,0,.37f);HoleCircle(vh,0,0,.29f);Box(vh,-.04f,-.10f,.08f,.30f);Circle(vh,0,-.24f,.045f);break;
                case Symbol.Star:
                    for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;float b=(i+.5f)*Mathf.PI*2/5;float c=(i+1)*Mathf.PI*2/5;Triangle(vh,Vector2.zero,new Vector2(Mathf.Sin(a),Mathf.Cos(a))*.43f,new Vector2(Mathf.Sin(b),Mathf.Cos(b))*.19f);Triangle(vh,Vector2.zero,new Vector2(Mathf.Sin(b),Mathf.Cos(b))*.19f,new Vector2(Mathf.Sin(c),Mathf.Cos(c))*.43f);}break;
                case Symbol.Lock:
                    Round(vh,-.30f,-.35f,.60f,.44f,.06f);for(int i=0;i<12;i++){float a=Mathf.PI*i/12,b=Mathf.PI*(i+1)/12;Line(vh,new Vector2(Mathf.Cos(a)*.20f,.09f+Mathf.Sin(a)*.24f),new Vector2(Mathf.Cos(b)*.20f,.09f+Mathf.Sin(b)*.24f),.08f);}HoleCircle(vh,0,-.10f,.05f);break;
                case Symbol.Sound:
                    Box(vh,-.32f,-.11f,.17f,.22f);Triangle(vh,new Vector2(-.18f,.12f),new Vector2(.09f,.33f),new Vector2(.09f,-.33f));Triangle(vh,new Vector2(-.18f,.12f),new Vector2(.09f,-.33f),new Vector2(-.18f,-.12f));Line(vh,new Vector2(.21f,-.22f),new Vector2(.34f,0),.055f);Line(vh,new Vector2(.34f,0),new Vector2(.21f,.22f),.055f);break;
            }
        }
        readonly Color hole=new Color(.035f,.27f,.50f,1);
        Vector2 Point(Vector2 p)=>new Vector2(rectTransform.rect.center.x+p.x*rectTransform.rect.width,rectTransform.rect.center.y+p.y*rectTransform.rect.height);
        void Triangle(VertexHelper v,Vector2 a,Vector2 b,Vector2 c,Color? tint=null){int n=v.currentVertCount;var co=tint??color;v.AddVert(Point(a),co,Vector2.zero);v.AddVert(Point(b),co,Vector2.zero);v.AddVert(Point(c),co,Vector2.zero);v.AddTriangle(n,n+1,n+2);}
        void Box(VertexHelper v,float x,float y,float w,float h,Color? tint=null){Triangle(v,new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),tint);Triangle(v,new Vector2(x,y),new Vector2(x+w,y+h),new Vector2(x,y+h),tint);}
        void HoleBox(VertexHelper v,float x,float y,float w,float h)=>Box(v,x,y,w,h,hole);
        void Circle(VertexHelper v,float x,float y,float r,Color? tint=null){for(int i=0;i<32;i++){float a=2*Mathf.PI*i/32,b=2*Mathf.PI*(i+1)/32;Triangle(v,new Vector2(x,y),new Vector2(x+Mathf.Cos(a)*r,y+Mathf.Sin(a)*r),new Vector2(x+Mathf.Cos(b)*r,y+Mathf.Sin(b)*r),tint);}}
        void HoleCircle(VertexHelper v,float x,float y,float r)=>Circle(v,x,y,r,hole);
        void Line(VertexHelper v,Vector2 a,Vector2 b,float thickness){var d=(b-a).normalized;var p=new Vector2(-d.y,d.x)*thickness*.5f;Triangle(v,a+p,b+p,b-p);Triangle(v,a+p,b-p,a-p);}
        void Round(VertexHelper v,float x,float y,float w,float h,float r){Box(v,x+r,y,w-r*2,h);Box(v,x,y+r,w,h-r*2);Circle(v,x+r,y+r,r);Circle(v,x+w-r,y+r,r);Circle(v,x+r,y+h-r,r);Circle(v,x+w-r,y+h-r,r);}
    }
}
