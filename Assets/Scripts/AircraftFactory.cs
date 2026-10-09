using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FightAgainstIsrael
{
    /// <summary>Original, reusable low-poly aircraft built in metres; every aircraft faces local +Z.</summary>
    public static class AircraftFactory
    {
        static GameObject playerTemplate, enemyTemplate;
        static Material playerSkin, playerPanel, enemySkin, enemyPanel, darkMetal, canopy, trim;
        static Material cyanGlow, amberGlow, redLight, greenLight, egypt, palestine, israel, usa;
        static Mesh sphereMesh;

        public static GameObject CreatePlayer(Transform parent) { EnsureTemplates(); return Clone(playerTemplate, parent, "MiG-29 inspired player"); }
        public static GameObject CreateEnemy(Transform parent) { EnsureTemplates(); return Clone(enemyTemplate, parent, "F-35 inspired enemy"); }

        static GameObject Clone(GameObject source, Transform parent, string name)
        {
            var instance = UnityEngine.Object.Instantiate(source, parent, false);
            instance.name = name;
            instance.hideFlags = HideFlags.None;
            instance.SetActive(true);
            return instance;
        }

        static void EnsureTemplates()
        {
            if (playerTemplate != null && enemyTemplate != null) return;
            playerSkin = Material("MiG satin aluminium", new Color(.64f, .73f, .77f), .65f, .53f);
            playerPanel = Material("MiG pale wing panels", new Color(.77f, .83f, .83f), .52f, .42f);
            enemySkin = Material("F-35 graphite composite", new Color(.25f, .31f, .35f), .55f, .42f);
            enemyPanel = Material("F-35 panel surfaces", new Color(.36f, .42f, .44f), .45f, .38f);
            darkMetal = Material("Intakes and exhaust metal", new Color(.045f, .06f, .075f), .7f, .7f);
            canopy = Material("Blue tinted cockpit glass", new Color(.045f, .38f, .55f), .76f, .96f);
            trim = Material("Panel gaps", new Color(.12f, .17f, .19f), .3f, .4f);
            cyanGlow = Emissive("MiG ion blue exhaust", new Color(.15f, .68f, 1f), 2.8f);
            amberGlow = Emissive("Enemy amber exhaust", new Color(1f, .36f, .09f), 2.8f);
            redLight = Emissive("Port red navigation light", new Color(1f, .045f, .025f), 3f);
            greenLight = Emissive("Starboard green navigation light", new Color(.1f, 1f, .45f), 3f);
            egypt = FlagMaterial("Egypt wing flag", MakeFlag(FlagKind.Egypt));
            palestine = FlagMaterial("Palestine wing flag", MakeFlag(FlagKind.Palestine));
            israel = FlagMaterial("Israel wing flag", MakeFlag(FlagKind.Israel));
            usa = FlagMaterial("United States wing flag", MakeFlag(FlagKind.USA));
            sphereMesh = Ellipsoid();
            playerTemplate = BuildMiG();
            enemyTemplate = BuildF35();
            BatchAircraftBody(playerTemplate);
            BatchAircraftBody(enemyTemplate);
            PreserveTemplate(playerTemplate);
            PreserveTemplate(enemyTemplate);
        }

        static void PreserveTemplate(GameObject template)
        {
            template.SetActive(false);
            template.hideFlags = HideFlags.HideAndDontSave;
            if (Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(template);
        }

        static void BatchAircraftBody(GameObject root)
        {
            var groups=new Dictionary<Material,List<CombineInstance>>();
            var retiredParts=new List<GameObject>();
            Matrix4x4 toRoot=root.transform.worldToLocalMatrix;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                // Engine flames must remain independent because AircraftEngineVisual scales them.
                if(filter.gameObject.name=="Engine flame")continue;
                var renderer=filter.GetComponent<MeshRenderer>();
                if(renderer==null||filter.sharedMesh==null||renderer.sharedMaterial==null)continue;
                List<CombineInstance> group;
                if(!groups.TryGetValue(renderer.sharedMaterial,out group))
                {group=new List<CombineInstance>();groups.Add(renderer.sharedMaterial,group);}
                group.Add(new CombineInstance {mesh=filter.sharedMesh,transform=toRoot*filter.transform.localToWorldMatrix});
                retiredParts.Add(filter.gameObject);
            }
            foreach(var group in groups)
            {
                var mesh=new Mesh {name=root.name+" / "+group.Key.name,indexFormat=IndexFormat.UInt32};
                mesh.CombineMeshes(group.Value.ToArray(),true,true);
                mesh.RecalculateBounds();
                Part(root.transform,"Batched body / "+group.Key.name,mesh,group.Key);
            }
            foreach(var part in retiredParts)
            {
                part.SetActive(false);
                // Detach before Destroy: the first aircraft may be cloned before this frame ends.
                part.transform.SetParent(null,false);
                if(Application.isPlaying)UnityEngine.Object.Destroy(part);else UnityEngine.Object.DestroyImmediate(part);
            }
        }

        static GameObject BuildMiG()
        {
            var root = new GameObject("MiG-29 mesh template");
            Part(root.transform, "Long radome and fuselage", Loft(new[] {
                S(1.62f,.012f,.017f,0), S(1.34f,.10f,.075f,0), S(.95f,.18f,.12f,0),
                S(.48f,.27f,.16f,0), S(-.12f,.29f,.17f,0), S(-.72f,.24f,.13f,0), S(-1.27f,.13f,.09f,0)
            }, 12), playerSkin);
            Part(root.transform, "Dark nose cone", Loft(new[] { S(1.63f,.009f,.01f,0),S(1.48f,.053f,.044f,0), S(1.35f,.092f,.07f,0) },12), enemyPanel);
            Part(root.transform, "Cockpit surround", Loft(new[] {S(.99f,.02f,.015f,.125f),S(.78f,.13f,.045f,.155f),S(.36f,.155f,.045f,.17f),S(.10f,.065f,.018f,.15f)},12),darkMetal);
            Part(root.transform, "Blue bubble canopy", Loft(new[] {S(.96f,.012f,.015f,.145f),S(.73f,.114f,.105f,.184f),S(.39f,.134f,.11f,.194f),S(.13f,.045f,.027f,.165f)},14),canopy);
            Line(root.transform,"Canopy center reflection",new Vector3(0,.298f,.72f),new Vector3(0,.306f,.37f),.008f,playerPanel);
            var flames = new List<Transform>();
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * .29f;
                Part(root.transform,"Twin engine nacelle", Loft(new[] {S(.66f,.11f,.085f,-.10f),S(.43f,.195f,.15f,-.07f),S(-.55f,.19f,.15f,-.07f),S(-1.32f,.139f,.105f,-.04f)},12),playerSkin,new Vector3(x,0,0));
                Part(root.transform,"Intake lip",Loft(new[] {S(.67f,.13f,.095f,-.1f),S(.64f,.11f,.075f,-.1f)},10),enemyPanel,new Vector3(x,0,0));
                Part(root.transform,"Deep twin intake",Disc(.105f,.069f,10),darkMetal,new Vector3(x,-.10f,.672f));
                Vector3[] wing = Wing(side, new[] {new Vector2(.23f,.46f),new Vector2(1.21f,-.33f),new Vector2(1.28f,-.68f),new Vector2(.47f,-.78f),new Vector2(.23f,-.99f)},.01f);
                Part(root.transform,"Swept main wing",Extrude(wing,Vector3.up*.036f),playerPanel);
                Part(root.transform,"Wing root extension",Extrude(Wing(side,new[] {new Vector2(.18f,.91f),new Vector2(.40f,.16f),new Vector2(.32f,-.53f),new Vector2(.18f,-.56f)},.06f),Vector3.up*.025f),playerSkin);
                Part(root.transform,"Swept tailplane",Extrude(Wing(side,new[] {new Vector2(.23f,-.89f),new Vector2(.83f,-1.18f),new Vector2(.91f,-1.46f),new Vector2(.29f,-1.30f)},.006f),Vector3.up*.028f),playerPanel);
                Part(root.transform,"Twin canted tail fin",Extrude(new[] {new Vector3(side*.27f,.08f,-1.32f),new Vector3(side*.43f,.60f,-1.24f),new Vector3(side*.43f,.63f,-.99f),new Vector3(side*.27f,.10f,-.83f)},Vector3.right*.019f),playerSkin);
                Line(root.transform,"Wing leading edge",new Vector3(side*.33f,.05f,.36f),new Vector3(side*1.19f,.05f,-.35f),.013f,enemyPanel);
                Line(root.transform,"Aileron seam",new Vector3(side*.55f,.05f,-.68f),new Vector3(side*1.19f,.05f,-.60f),.012f,trim);
                Line(root.transform,"Tail elevator seam",new Vector3(side*.35f,.043f,-1.27f),new Vector3(side*.82f,.043f,-1.38f),.010f,trim);
                WingFlag(root.transform,side,side < 0 ? egypt : palestine,false);
                EllipsoidPart(root.transform,"Navigation light",new Vector3(side*1.24f,.065f,-.54f),new Vector3(.044f,.026f,.053f),side < 0 ? redLight : greenLight);
                Part(root.transform,"Exhaust nozzle",Loft(new[] {S(-1.23f,.146f,.11f,-.04f),S(-1.40f,.12f,.09f,-.04f)},12),darkMetal,new Vector3(x,0,0));
                Part(root.transform,"Glowing engine core",Disc(.092f,.072f,12),cyanGlow,new Vector3(x,-.04f,-1.405f));
                flames.Add(Part(root.transform,"Engine flame",Loft(new[] {S(0,.10f,.065f,0),S(-.20f,.068f,.045f,0),S(-.45f,.004f,.006f,0)},10),cyanGlow,new Vector3(x,-.04f,-1.405f)).transform);
            }
            for (int side=-1;side<=1;side+=2)
                Line(root.transform,"Fuselage access panel",new Vector3(side*.17f,.154f,-.21f),new Vector3(side*.14f,.14f,-.71f),.008f,trim);
            var pulse = root.AddComponent<AircraftEngineVisual>(); pulse.SetFlames(flames.ToArray());
            return root;
        }

        static GameObject BuildF35()
        {
            var root = new GameObject("F-35 mesh template");
            Part(root.transform,"Angular blended fuselage",Loft(new[] {
                S(1.54f,.015f,.018f,0),S(1.25f,.15f,.10f,0),S(.77f,.29f,.16f,0),S(.22f,.38f,.19f,0),
                S(-.35f,.38f,.18f,0),S(-.88f,.26f,.145f,0),S(-1.24f,.16f,.12f,0)
            },8),enemySkin);
            Part(root.transform,"Faceted radome",Loft(new[] {S(1.56f,.006f,.008f,0),S(1.27f,.136f,.09f,0)},8),enemyPanel);
            Part(root.transform,"Cockpit frame",Loft(new[] {S(1.03f,.035f,.018f,.13f),S(.77f,.145f,.06f,.17f),S(.35f,.16f,.06f,.185f),S(.17f,.06f,.021f,.18f)},10),darkMetal);
            Part(root.transform,"Gold blue cockpit canopy",Loft(new[] {S(1.0f,.016f,.014f,.15f),S(.77f,.122f,.105f,.196f),S(.39f,.137f,.10f,.21f),S(.19f,.04f,.025f,.19f)},12),canopy);
            for (int side=-1;side<=1;side+=2)
            {
                Part(root.transform,"Trapezoid wing",Extrude(Wing(side,new[] {new Vector2(.27f,.50f),new Vector2(1.30f,-.29f),new Vector2(1.25f,-.80f),new Vector2(.34f,-.68f)},.035f),Vector3.up*.035f),enemyPanel);
                Part(root.transform,"Blended intake shoulder",Extrude(Wing(side,new[] {new Vector2(.18f,.90f),new Vector2(.39f,.58f),new Vector2(.49f,.20f),new Vector2(.30f,-.43f)},.09f),Vector3.up*.068f),enemySkin);
                Part(root.transform,"Angular side air intake",Extrude(new[] {new Vector3(side*.26f,.105f,.68f),new Vector3(side*.39f,.11f,.49f),new Vector3(side*.43f,.032f,.34f),new Vector3(side*.27f,.024f,.43f)},Vector3.up*.007f),darkMetal);
                Part(root.transform,"Trapezoid rear stabilizer",Extrude(Wing(side,new[] {new Vector2(.18f,-.87f),new Vector2(.90f,-1.10f),new Vector2(.85f,-1.49f),new Vector2(.22f,-1.28f)},.005f),Vector3.up*.028f),enemyPanel);
                Part(root.transform,"Canted twin vertical stabilizer",Extrude(new[] {new Vector3(side*.24f,.09f,-1.23f),new Vector3(side*.48f,.58f,-1.15f),new Vector3(side*.49f,.58f,-.84f),new Vector3(side*.29f,.12f,-.67f)},Vector3.right*.026f),enemySkin);
                Line(root.transform,"Wing sawtooth panel",new Vector3(side*.54f,.081f,-.53f),new Vector3(side*1.23f,.081f,-.65f),.012f,trim);
                Line(root.transform,"Leading edge light metal trim",new Vector3(side*.39f,.081f,.39f),new Vector3(side*1.26f,.081f,-.29f),.014f,playerSkin);
                Line(root.transform,"Tail trim",new Vector3(side*.35f,.043f,-1.25f),new Vector3(side*.79f,.043f,-1.40f),.010f,trim);
                WingFlag(root.transform,side,side<0?israel:usa,true);
                EllipsoidPart(root.transform,"Navigation light",new Vector3(side*1.26f,.095f,-.52f),new Vector3(.035f,.023f,.045f),side<0?redLight:greenLight);
            }
            Line(root.transform,"Upper engine hatch left",new Vector3(-.13f,.202f,-.10f),new Vector3(-.12f,.16f,-.83f),.012f,enemyPanel);
            Line(root.transform,"Upper engine hatch right",new Vector3(.13f,.202f,-.10f),new Vector3(.12f,.16f,-.83f),.012f,enemyPanel);
            Part(root.transform,"Single engine nozzle",Loft(new[] {S(-1.17f,.174f,.123f,0),S(-1.36f,.145f,.10f,0)},12),darkMetal);
            Part(root.transform,"Single engine core",Disc(.115f,.08f,12),amberGlow,new Vector3(0,0,-1.367f));
            var flame=Part(root.transform,"Engine flame",Loft(new[] {S(0,.126f,.076f,0),S(-.18f,.078f,.05f,0),S(-.40f,.004f,.004f,0)},12),amberGlow,new Vector3(0,0,-1.37f));
            root.AddComponent<AircraftEngineVisual>().SetFlames(new[] {flame.transform});
            return root;
        }

        static void WingFlag(Transform parent,int side,Material material,bool f35)
        {
            float y=f35?.084f:.059f;
            // These sit above the wing skin and remain legible from the game's overhead camera.
            float xa=.55f,xb=1.03f,za=-.31f,zb=-.61f;
            Vector3[] points={new Vector3(side*xa,y,za),new Vector3(side*xb,y,za-.15f),new Vector3(side*xb,y,zb-.04f),new Vector3(side*xa,y,zb+.025f)};
            var mesh=new Mesh {name="Original flag decal quad"};
            mesh.vertices=points;
            mesh.uv=new[] {new Vector2(side<0?1:0,1),new Vector2(side<0?0:1,1),new Vector2(side<0?0:1,0),new Vector2(side<0?1:0,0)};
            mesh.triangles=side<0?new[] {0,2,1,0,3,2}:new[] {0,1,2,0,2,3};
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            Part(parent,"National wing marking",mesh,material);
        }

        struct Section {public float z,x,y,cy; public Section(float z,float x,float y,float cy){this.z=z;this.x=x;this.y=y;this.cy=cy;}}
        static Section S(float z,float x,float y,float cy)=>new Section(z,x,y,cy);
        static Mesh Loft(Section[] sections,int sides)
        {
            var vertices=new List<Vector3>(); var triangles=new List<int>();
            for(int j=0;j<sections.Length;j++) for(int k=0;k<sides;k++)
            {float a=2*Mathf.PI*k/sides; Section s=sections[j];vertices.Add(new Vector3(Mathf.Sin(a)*s.x,s.cy+Mathf.Cos(a)*s.y,s.z));}
            for(int j=0;j<sections.Length-1;j++) for(int k=0;k<sides;k++)
            {int a=j*sides+k,b=j*sides+(k+1)%sides,c=(j+1)*sides+k,d=(j+1)*sides+(k+1)%sides;triangles.AddRange(new[]{a,c,b,b,c,d});}
            int front=vertices.Count;vertices.Add(new Vector3(0,sections[0].cy,sections[0].z));
            int back=vertices.Count;vertices.Add(new Vector3(0,sections[sections.Length-1].cy,sections[sections.Length-1].z));
            for(int k=0;k<sides;k++){triangles.AddRange(new[]{front,k,(k+1)%sides});int a=(sections.Length-1)*sides+k,b=(sections.Length-1)*sides+(k+1)%sides;triangles.AddRange(new[]{back,b,a});}
            ReverseWinding(triangles);
            return MakeMesh("Aircraft loft",vertices,triangles);
        }
        static Mesh Disc(float width,float height,int sides)
        {
            var v=new List<Vector3>{Vector3.zero};var t=new List<int>();
            for(int i=0;i<sides;i++){float a=i*2*Mathf.PI/sides;v.Add(new Vector3(Mathf.Sin(a)*width,Mathf.Cos(a)*height,0));}
            for(int i=0;i<sides;i++)t.AddRange(new[]{0,1+(i+1)%sides,1+i});
            int offset=v.Count;v.AddRange(v.ToArray());
            for(int i=0;i<sides;i++)t.AddRange(new[]{offset,offset+1+i,offset+1+(i+1)%sides});
            return MakeMesh("Engine disc",v,t);
        }
        static Vector3[] Wing(int side,Vector2[] points,float y)
        {var result=new Vector3[points.Length];for(int i=0;i<points.Length;i++)result[i]=new Vector3(points[i].x*side,y,points[i].y);return result;}
        static Mesh Extrude(Vector3[] polygon,Vector3 thickness)
        {
            var v=new List<Vector3>(); var t=new List<int>();int n=polygon.Length;
            // Separate vertices keep the slab's thin edge crisp instead of rounding the wings.
            Vector3 normal=Vector3.Cross(polygon[1]-polygon[0],polygon[2]-polygon[0]);
            if(Vector3.Dot(normal,thickness)<0)Array.Reverse(polygon);
            for(int i=0;i<n;i++)v.Add(polygon[i]+thickness*.5f);
            for(int i=0;i<n;i++)v.Add(polygon[i]-thickness*.5f);
            for(int i=1;i<n-1;i++){t.AddRange(new[]{0,i,i+1});t.AddRange(new[]{n,n+i+1,n+i});}
            for(int i=0;i<n;i++){int j=(i+1)%n;int start=v.Count;v.AddRange(new[]{polygon[i]+thickness*.5f,polygon[i]-thickness*.5f,polygon[j]-thickness*.5f,polygon[j]+thickness*.5f});t.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});}
            return MakeMesh("Aircraft wing or stabilizer",v,t);
        }
        static Mesh MakeMesh(string name,List<Vector3> vertices,List<int> triangles)
        {var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;}
        static GameObject Part(Transform parent,string name,Mesh mesh,Material material,Vector3 position=default(Vector3))
        {var part=new GameObject(name);part.transform.SetParent(parent,false);part.transform.localPosition=position;part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterial=material;return part;}
        static void Line(Transform parent,string name,Vector3 a,Vector3 b,float width,Material material)
        {
            Vector3 perpendicular=Vector3.Cross(Vector3.up,b-a).normalized*width*.5f;
            Part(parent,name,Extrude(new[]{a-perpendicular,a+perpendicular,b+perpendicular,b-perpendicular},Vector3.up*.002f),material);
        }
        static void EllipsoidPart(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        {var part=Part(parent,name,sphereMesh,material,position);part.transform.localScale=scale;}
        static Mesh Ellipsoid()
        {
            var v=new List<Vector3>();var t=new List<int>();const int rings=8,sides=12;
            for(int y=0;y<=rings;y++){float a=Mathf.PI*y/rings;for(int k=0;k<=sides;k++){float b=2*Mathf.PI*k/sides;v.Add(new Vector3(Mathf.Sin(a)*Mathf.Cos(b),Mathf.Cos(a),Mathf.Sin(a)*Mathf.Sin(b)));}}
            for(int y=0;y<rings;y++)for(int k=0;k<sides;k++){int a=y*(sides+1)+k,b=a+sides+1;t.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            ReverseWinding(t);
            return MakeMesh("Navigation light sphere",v,t);
        }
        static Material Material(string name,Color color,float metal,float gloss)
        {var material=new Material(Shader.Find("Standard")){name=name,color=color};material.SetFloat("_Metallic",metal);material.SetFloat("_Glossiness",gloss);return material;}
        static Material Emissive(string name,Color color,float strength)
        {var material=Material(name,color,.3f,.6f);material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*strength);return material;}
        static Material FlagMaterial(string name,Texture2D texture)
        {var material=Material(name,Color.white,.05f,.25f);material.mainTexture=texture;return material;}
        enum FlagKind {Egypt,Palestine,Israel,USA}
        static Texture2D MakeFlag(FlagKind kind)
        {
            const int width=384,height=240;var pixels=new Color[width*height];
            Color red=new Color(.84f,.025f,.09f),blue=new Color(.04f,.21f,.61f),gold=new Color(.81f,.60f,.15f);
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                float u=(float)x/(width-1),v=(float)y/(height-1);Color color=Color.white;
                if(kind==FlagKind.Egypt)
                {
                    color=v>2f/3f?red:v<1f/3f?new Color(.035f,.035f,.04f):Color.white;
                    float dx=Mathf.Abs(u-.5f),dy=Mathf.Abs(v-.5f);
                    if((dx<.026f&&dy<.091f)||(dx<.069f&&dy<.045f&&dy>.013f)||(dx<.047f&&v>.398f&&v<.419f))color=gold;
                }
                else if(kind==FlagKind.Palestine)
                {color=v>2f/3f?new Color(.025f,.025f,.03f):v<1f/3f?new Color(.035f,.46f,.23f):Color.white;if(u<.38f*(1-2*Mathf.Abs(v-.5f)))color=red;}
                else if(kind==FlagKind.Israel)
                {
                    if((v>.13f&&v<.25f)||(v>.75f&&v<.87f))color=blue;
                    Vector2 p=new Vector2((u-.5f)*1.6f,v-.5f);
                    Vector2 a=new Vector2(0,.205f),b=new Vector2(-.178f,-.1025f),c=new Vector2(.178f,-.1025f);
                    if(DistanceSegment(p,a,b)<.015f||DistanceSegment(p,b,c)<.015f||DistanceSegment(p,c,a)<.015f||DistanceSegment(p,-a,-b)<.015f||DistanceSegment(p,-b,-c)<.015f||DistanceSegment(p,-c,-a)<.015f)color=blue;
                }
                else
                {
                    int stripe=Mathf.Min(12,(int)((1-v)*13));color=stripe%2==0?red:Color.white;
                    if(u<.43f&&v>.46f)
                    {
                        color=new Color(.035f,.11f,.33f);
                        for(int row=0;row<9;row++)for(int col=0;col<(row%2==0?6:5);col++)
                        {
                            float sx=.037f+col*.071f+(row%2)*.0355f,sy=.955f-row*.057f;
                            float dx=u-sx,dy=(v-sy)/1.6f;
                            float angle=Mathf.Atan2(dy,dx),radius=Mathf.Sqrt(dx*dx+dy*dy);
                            if(radius<.011f+.004f*Mathf.Cos(5*angle))color=Color.white;
                        }
                    }
                }
                pixels[y*width+x]=color;
            }
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,true){name=kind+" original procedural flag",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};texture.SetPixels(pixels);texture.Apply(true,true);return texture;
        }
        static float DistanceSegment(Vector2 point,Vector2 a,Vector2 b)
        {Vector2 ab=b-a;float t=Mathf.Clamp01(Vector2.Dot(point-a,ab)/ab.sqrMagnitude);return Vector2.Distance(point,a+ab*t);}
        static void ReverseWinding(List<int> triangles)
        {for(int i=0;i<triangles.Count;i+=3){int index=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=index;}}
    }

    public sealed class AircraftEngineVisual : MonoBehaviour
    {
        [SerializeField] Transform[] flames=Array.Empty<Transform>();
        float phase;
        public void SetFlames(Transform[] values){flames=values;phase=UnityEngine.Random.value*10f;}
        void LateUpdate()
        {float pulse=.90f+.08f*Mathf.Sin(Time.time*39f+phase)+.04f*Mathf.Sin(Time.time*73f+phase);for(int i=0;i<flames.Length;i++)if(flames[i]!=null)flames[i].localScale=new Vector3(1,1,pulse);}
    }
}
