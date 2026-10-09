using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FightAgainstIsrael
{
    /// <summary>A bounded, mesh-batched coastal world. Tick moves and recycles four already-built chunks.</summary>
    public sealed class ScrollingWorld : MonoBehaviour
    {
        const float ChunkLength=36f;
        readonly Transform[] chunks=new Transform[4];
        Material ocean, sand, desert, cliff, foam, road, roadPaint, walls, whiteWalls, roofs, blueRoof, metal, timber, green, cloud;
        Mesh box, cylinder, sphere, palmCrown, boatHull;
        float scroll;
        bool initialized;

        public void Initialize(Camera camera)
        {
            if(initialized)return;
            initialized=true;
            box=MakeBox(); cylinder=MakeCylinder(12); sphere=MakeSphere(8,12); palmCrown=MakePalmCrown(); boatHull=MakeBoatHull();
            sand=MakeMaterial("Warm ivory beach sand",new Color(.81f,.69f,.43f),0,.24f);
            desert=MakeMaterial("Golden desert plateau",new Color(.67f,.51f,.28f),0,.28f);
            cliff=MakeMaterial("Stratified coastal rock",new Color(.34f,.28f,.20f),0,.25f);
            foam=MakeMaterial("Shallow turquoise surf",new Color(.18f,.64f,.61f),.06f,.55f);
            road=MakeMaterial("Weathered tarmac",new Color(.18f,.23f,.23f),0,.25f);
            roadPaint=MakeMaterial("Warm road markings",new Color(.80f,.76f,.58f),0,.15f);
            walls=MakeMaterial("Sandstone buildings",new Color(.72f,.63f,.45f),0,.20f);
            whiteWalls=MakeMaterial("Whitewashed buildings",new Color(.88f,.86f,.72f),0,.24f);
            roofs=MakeMaterial("Terracotta roofs",new Color(.45f,.24f,.14f),0,.26f);
            blueRoof=MakeMaterial("Harbor painted roof",new Color(.14f,.29f,.34f),.2f,.35f);
            metal=MakeMaterial("Dock equipment steel",new Color(.36f,.41f,.40f),.55f,.42f);
            timber=MakeMaterial("Dock wood and tree trunks",new Color(.25f,.19f,.12f),0,.20f);
            green=MakeMaterial("Date palm foliage",new Color(.17f,.31f,.20f),0,.25f);
            cloud=MakeCloudMaterial();
            Shader oceanShader=Shader.Find("FightAgainstIsrael/CoastalOcean");
            ocean=new Material(oceanShader!=null?oceanShader:Shader.Find("Standard")){name="Animated coastal ocean"};
            ocean.color=new Color(.025f,.28f,.34f);
            if(oceanShader==null){ocean.SetFloat("_Metallic",.18f);ocean.SetFloat("_Glossiness",.76f);}
            var water=new GameObject("Ocean, animated ripples and currents");water.transform.SetParent(transform,false);
            water.AddComponent<MeshFilter>().sharedMesh=MakeOceanGrid(160,200,64,80);
            var waterRenderer=water.AddComponent<MeshRenderer>();waterRenderer.sharedMaterial=ocean;waterRenderer.shadowCastingMode=ShadowCastingMode.Off;
            for(int i=0;i<chunks.Length;i++)
            {
                var chunk=new GameObject("Coastal sector "+(i+1));chunk.transform.SetParent(transform,false);chunk.transform.localPosition=new Vector3(0,0,(i-1.5f)*ChunkLength);chunks[i]=chunk.transform;
                BuildChunk(chunk.transform,i);
            }
            // The sea extends well beyond both sides of every supported aspect ratio.
            if(camera!=null)water.transform.localPosition=new Vector3(camera.transform.position.x,0,0);
        }

        public void Tick(float dt,float speed)
        {
            if(!initialized)return;
            float distance=Mathf.Max(0,dt)*Mathf.Max(0,speed);
            scroll+=distance;
            if(scroll>10000f)scroll-=10000f;
            ocean.SetFloat("_Travel",scroll);
            for(int i=0;i<chunks.Length;i++)
            {
                Vector3 position=chunks[i].localPosition;position.z-=distance;
                while(position.z<-ChunkLength*2f)position.z+=ChunkLength*chunks.Length;
                chunks[i].localPosition=position;
            }
        }

        void BuildChunk(Transform parent,int seed)
        {
            var batch=new MeshBatch();var random=new System.Random(8139+seed*171);
            float side=seed%2==0?1f:-1f;
            Vector3 center=new Vector3(side*(18f+seed%2*2f),0,seed%2==0?1f:-2f);
            float rx=11f+seed%2*1.5f,rz=14.5f;
            AddIsland(batch,center,rx,rz,seed);
            AddSettlement(batch,center,side,random,seed);
            AddHarbor(batch,center,side,seed);
            // Smaller rocky islets and a boat add depth across the open central flight corridor.
            Vector3 small=new Vector3(-side*(12f+seed*1.3f),0,-9+seed*3f);
            AddIsland(batch,small,2.1f,3.5f,seed+12,false);
            for(int rock=0;rock<5;rock++)
            {
                Vector3 p=small+new Vector3(Range(random,-.7f,.7f),.75f,Range(random,-1.3f,1.3f));
                batch.Add(sphere,cliff,p,new Vector3(Range(random,.3f,.65f),Range(random,.22f,.48f),Range(random,.3f,.7f)),Quaternion.Euler(0,Range(random,0,360),14));
            }
            AddShip(batch,new Vector3(side*6.8f,.05f,-5f+seed*2f),seed%2==0?24f:-18f,seed%2==0);
            for(int i=0;i<4;i++)
            {
                float z=-13f+i*7.4f;float x=center.x-side*(rx*.80f);
                batch.Add(box,foam,new Vector3(x,.018f,z+center.z),new Vector3(.22f,.018f,1.5f),Quaternion.Euler(0,side*Range(random,-11,11),0));
            }
            AddClouds(batch,random,seed);
            batch.Flush(parent);
        }

        void AddIsland(MeshBatch batch,Vector3 center,float rx,float rz,int seed,bool plateau=true)
        {
            const int sides=36;float[] shape=new float[sides];
            for(int i=0;i<sides;i++)
            {float angle=i*2*Mathf.PI/sides;shape[i]=1f+.10f*Mathf.Sin(angle*3+seed*1.7f)+.055f*Mathf.Cos(angle*5-seed*.8f)+.03f*Mathf.Sin(angle*9+seed);}
            batch.Add(RingMesh(shape,rx,rz,1.03f,-.075f,.93f,.035f,seed),foam,center,Vector3.one,Quaternion.identity);
            batch.Add(RingMesh(shape,rx,rz,.93f,.035f,.78f,.17f,seed),sand,center,Vector3.one,Quaternion.identity);
            batch.Add(RingMesh(shape,rx,rz,.78f,.17f,.63f,plateau?1.03f:.64f,seed),cliff,center,Vector3.one,Quaternion.identity);
            batch.Add(RingMesh(shape,rx,rz,.63f,plateau?1.03f:.64f,0,plateau?1.06f:.68f,seed),plateau?desert:cliff,center,Vector3.one,Quaternion.identity);
            // Two thin bands make the exposed rock read as a weathered cliff rather than a cone.
            batch.Add(RingMesh(shape,rx,rz,.725f,.48f,.712f,.53f,seed),desert,center,Vector3.one,Quaternion.identity);
            batch.Add(RingMesh(shape,rx,rz,.675f,.78f,.667f,.825f,seed),sand,center,Vector3.one,Quaternion.identity);
        }

        void AddSettlement(MeshBatch batch,Vector3 center,float side,System.Random random,int seed)
        {
            const float ground=1.1f;
            // A coastal road, side streets, roof terraces, tanks, and palms make a small inhabited island.
            batch.Add(box,road,center+new Vector3(-side*1.5f,ground,0),new Vector3(1.05f,.035f,13f),Quaternion.identity);
            for(int i=0;i<9;i++)batch.Add(box,roadPaint,center+new Vector3(-side*1.5f,ground+.023f,-5.7f+i*1.45f),new Vector3(.06f,.006f,.64f),Quaternion.identity);
            for(int street=0;street<3;street++)
            {
                float z=-4.3f+street*4.3f;
                batch.Add(box,road,center+new Vector3(side*1.0f,ground,z),new Vector3(5.8f,.031f,.65f),Quaternion.identity);
                for(int row=0;row<2;row++)for(int column=0;column<3;column++)
                {
                    float x=side*(-.15f+column*1.6f);float localZ=z+(row==0?-.98f:1.04f);
                    float width=Range(random,.83f,1.19f),depth=Range(random,.75f,1.16f),height=Range(random,.3f,.75f);
                    Vector3 p=center+new Vector3(x,ground+height*.5f,localZ);
                    Material wall=(column+street+seed)%3==0?whiteWalls:walls;
                    batch.Add(box,wall,p,new Vector3(width,height,depth),Quaternion.identity);
                    batch.Add(box,(column+row)%3==0?blueRoof:roofs,p+Vector3.up*(height*.5f+.027f),new Vector3(width+.035f,.045f,depth+.035f),Quaternion.identity);
                    // Roof parapets and a small white water tank survive well at the overhead camera scale.
                    if((column+row+street)%2==0)
                    {
                        batch.Add(box,whiteWalls,p+new Vector3(-width*.46f,height*.5f+.065f,0),new Vector3(.065f,.09f,depth),Quaternion.identity);
                        batch.Add(box,whiteWalls,p+new Vector3(width*.46f,height*.5f+.065f,0),new Vector3(.065f,.09f,depth),Quaternion.identity);
                        batch.Add(cylinder,whiteWalls,p+new Vector3(width*.2f,height*.5f+.115f,depth*.18f),new Vector3(.17f,.16f,.17f),Quaternion.identity);
                    }
                    else batch.Add(box,metal,p+new Vector3(-width*.19f,height*.5f+.06f,0),new Vector3(width*.33f,.045f,depth*.62f),Quaternion.Euler(0,0,6));
                    // Narrow dark window bands create visible architectural detail without extra objects.
                    batch.Add(box,blueRoof,p+new Vector3(0,.01f,depth*.501f),new Vector3(width*.66f,.085f,.012f),Quaternion.identity);
                }
            }
            for(int i=0;i<10;i++)
            {
                Vector3 p=center+new Vector3(-side*2.35f,ground,-6f+i*1.27f);
                AddPalm(batch,p,.8f+Range(random,0,.25f),Range(random,0,360));
            }
            // Small marked circular landing pad beside the settlement.
            Vector3 pad=center+new Vector3(side*4.0f,ground+.035f,-6.6f);
            batch.Add(cylinder,road,pad,new Vector3(1.52f,.028f,1.52f),Quaternion.identity);
            batch.Add(cylinder,roadPaint,pad+Vector3.up*.022f,new Vector3(1.28f,.015f,1.28f),Quaternion.identity);
            batch.Add(cylinder,road,pad+Vector3.up*.034f,new Vector3(1.21f,.012f,1.21f),Quaternion.identity);
            batch.Add(box,roadPaint,pad+new Vector3(-.22f,.05f,0),new Vector3(.10f,.009f,.63f),Quaternion.identity);
            batch.Add(box,roadPaint,pad+new Vector3(.22f,.05f,0),new Vector3(.10f,.009f,.63f),Quaternion.identity);
            batch.Add(box,roadPaint,pad+new Vector3(0,.05f,0),new Vector3(.48f,.009f,.10f),Quaternion.identity);
        }

        void AddHarbor(MeshBatch batch,Vector3 center,float side,int seed)
        {
            Vector3 shore=center+new Vector3(-side*8.8f,.09f,2.7f);
            batch.Add(box,timber,shore+new Vector3(-side*.95f,0,0),new Vector3(3.4f,.17f,.83f),Quaternion.identity);
            batch.Add(box,timber,shore+new Vector3(-side*2.25f,0,1.15f),new Vector3(.73f,.17f,3.1f),Quaternion.identity);
            for(int i=0;i<5;i++)
            {
                Vector3 p=shore+new Vector3(-side*(.1f+i*.7f),-.07f,.29f);
                batch.Add(cylinder,timber,p,new Vector3(.13f,.55f,.13f),Quaternion.identity);
                batch.Add(box,metal,shore+new Vector3(-side*(.1f+i*.7f),.115f,0),new Vector3(.06f,.03f,.83f),Quaternion.identity);
            }
            AddShip(batch,shore+new Vector3(-side*2.95f,-.04f,.4f),0,false);
            // Colored freight crates near the dock read as harbor activity.
            for(int i=0;i<3;i++)batch.Add(box,i%2==0?blueRoof:roofs,shore+new Vector3(side*.3f,.23f,-.65f+i*.34f),new Vector3(.7f,.32f,.27f),Quaternion.identity);
        }

        void AddShip(MeshBatch batch,Vector3 position,float yaw,bool freighter)
        {
            Quaternion rotation=Quaternion.Euler(0,yaw,0);
            float scale=freighter?1.20f:.70f;
            batch.Add(boatHull,blueRoof,position,new Vector3(.8f,.22f,2.45f)*scale,rotation);
            AddShipPart(batch,box,whiteWalls,position,rotation,new Vector3(0,.20f,-.52f)*scale,new Vector3(.53f,.30f,.64f)*scale);
            AddShipPart(batch,box,metal,position,rotation,new Vector3(0,.39f,-.56f)*scale,new Vector3(.57f,.05f,.66f)*scale);
            AddShipPart(batch,cylinder,metal,position,rotation,new Vector3(0,.63f,-.51f)*scale,new Vector3(.045f,.52f,.045f)*scale);
            if(freighter)
            {
                for(int i=0;i<3;i++)AddShipPart(batch,box,i%2==0?roofs:walls,position,rotation,new Vector3(0,.20f,-.01f+i*.31f)*scale,new Vector3(.54f,.20f,.25f)*scale);
                // Curved looking wake composed from a pair of tapered surface wedges.
                AddShipPart(batch,boatHull,foam,position,rotation,new Vector3(-.31f,-.036f,-1.52f)*scale,new Vector3(.20f,.014f,1.34f)*scale);
                AddShipPart(batch,boatHull,foam,position,rotation,new Vector3(.31f,-.036f,-1.52f)*scale,new Vector3(.20f,.014f,1.34f)*scale);
            }
        }
        void AddShipPart(MeshBatch batch,Mesh mesh,Material material,Vector3 origin,Quaternion rotation,Vector3 offset,Vector3 scale)
        {batch.Add(mesh,material,origin+rotation*offset,scale,rotation);}
        void AddPalm(MeshBatch batch,Vector3 position,float height,float yaw)
        {
            batch.Add(cylinder,timber,position+Vector3.up*(height*.5f),new Vector3(.09f,height,.09f),Quaternion.Euler(0,0,5));
            batch.Add(palmCrown,green,position+Vector3.up*height,new Vector3(.64f,.32f,.64f),Quaternion.Euler(0,yaw,0));
        }
        void AddClouds(MeshBatch batch,System.Random random,int seed)
        {
            for(int i=0;i<2;i++)
            {
                Vector3 center=new Vector3(Range(random,-8f,8f),1.8f,-13f+i*19f+seed);
                for(int lobe=0;lobe<4;lobe++)batch.Add(sphere,cloud,center+new Vector3(lobe*1.0f-1.5f,Range(random,0,.06f),Range(random,-.5f,.5f)),new Vector3(Range(random,1.1f,1.7f),.20f,Range(random,.8f,1.35f)),Quaternion.identity);
            }
        }

        static float Range(System.Random random,float min,float max)=>min+(float)random.NextDouble()*(max-min);
        static Material MakeMaterial(string name,Color color,float metal,float smoothness)
        {var material=new Material(Shader.Find("Standard")){name=name,color=color};material.SetFloat("_Metallic",metal);material.SetFloat("_Glossiness",smoothness);return material;}
        static Material MakeCloudMaterial()
        {
            var material=MakeMaterial("Soft translucent low cloud",new Color(.88f,.94f,.90f,.09f),0,0);
            material.SetFloat("_Mode",3);material.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);material.SetInt("_ZWrite",0);
            material.DisableKeyword("_ALPHATEST_ON");material.EnableKeyword("_ALPHABLEND_ON");material.DisableKeyword("_ALPHAPREMULTIPLY_ON");material.renderQueue=(int)RenderQueue.Transparent;return material;
        }

        static Mesh RingMesh(float[] profile,float rx,float rz,float outer,float outerHeight,float inner,float innerHeight,int seed)
        {
            int count=profile.Length;var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int ring=0;ring<2;ring++)for(int i=0;i<count;i++)
            {
                float angle=i*2*Mathf.PI/count,ratio=ring==0?outer:inner,height=ring==0?outerHeight:innerHeight;
                float ripple=ratio==0?0:.028f*Mathf.Sin(angle*7+seed);
                vertices.Add(new Vector3(Mathf.Cos(angle)*rx*profile[i]*ratio,height+ripple,Mathf.Sin(angle)*rz*profile[i]*ratio));
            }
            for(int i=0;i<count;i++){int j=(i+1)%count;triangles.AddRange(new[]{i,count+i,j,j,count+i,count+j});}
            var mesh=CreateMesh("Coastal contour surface",vertices,triangles);
            return mesh;
        }
        static Mesh MakeOceanGrid(float width,float depth,int columns,int rows)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int z=0;z<=rows;z++)for(int x=0;x<=columns;x++){vertices.Add(new Vector3(((float)x/columns-.5f)*width,-.035f,((float)z/rows-.5f)*depth));uv.Add(new Vector2((float)x/columns,(float)z/rows));}
            for(int z=0;z<rows;z++)for(int x=0;x<columns;x++){int a=z*(columns+1)+x,b=a+columns+1;triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            var mesh=CreateMesh("Tessellated ocean surface",vertices,triangles);mesh.SetUVs(0,uv);return mesh;
        }
        static Mesh MakeBox()
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            Vector3[] normals={Vector3.up,Vector3.down,Vector3.right,Vector3.left,Vector3.forward,Vector3.back};
            foreach(Vector3 normal in normals)
            {
                Vector3 tangent=Mathf.Abs(normal.y)>.5f?Vector3.right:Vector3.up;Vector3 bitangent=Vector3.Cross(normal,tangent);
                int offset=vertices.Count;vertices.Add(normal*.5f-tangent*.5f-bitangent*.5f);vertices.Add(normal*.5f+tangent*.5f-bitangent*.5f);vertices.Add(normal*.5f+tangent*.5f+bitangent*.5f);vertices.Add(normal*.5f-tangent*.5f+bitangent*.5f);
                triangles.AddRange(new[]{offset,offset+1,offset+2,offset,offset+2,offset+3});
            }
            return CreateMesh("Batched architectural box",vertices,triangles);
        }
        static Mesh MakeCylinder(int sides)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int y=0;y<2;y++)for(int i=0;i<sides;i++){float angle=i*2*Mathf.PI/sides;vertices.Add(new Vector3(Mathf.Cos(angle)*.5f,y-.5f,Mathf.Sin(angle)*.5f));}
            for(int i=0;i<sides;i++){int j=(i+1)%sides;triangles.AddRange(new[]{i,sides+i,j,j,sides+i,sides+j});}
            for(int y=0;y<2;y++)
            {
                int center=vertices.Count;vertices.Add(new Vector3(0,y-.5f,0));int start=vertices.Count;
                for(int i=0;i<sides;i++){float angle=i*2*Mathf.PI/sides;vertices.Add(new Vector3(Mathf.Cos(angle)*.5f,y-.5f,Mathf.Sin(angle)*.5f));}
                for(int i=0;i<sides;i++){int j=(i+1)%sides;if(y==0)triangles.AddRange(new[]{center,start+i,start+j});else triangles.AddRange(new[]{center,start+j,start+i});}
            }
            return CreateMesh("Batched cylinder",vertices,triangles);
        }
        static Mesh MakeSphere(int rings,int sides)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int y=0;y<=rings;y++)for(int i=0;i<=sides;i++){float a=Mathf.PI*y/rings,b=2*Mathf.PI*i/sides;vertices.Add(new Vector3(Mathf.Sin(a)*Mathf.Cos(b),Mathf.Cos(a),Mathf.Sin(a)*Mathf.Sin(b)));}
            for(int y=0;y<rings;y++)for(int i=0;i<sides;i++){int a=y*(sides+1)+i,b=a+sides+1;triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            return CreateMesh("Batched rounded rock or cloud",vertices,triangles);
        }
        static Mesh MakePalmCrown()
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<7;i++)
            {
                float angle=i*2*Mathf.PI/7;Vector3 direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));Vector3 side=Vector3.Cross(Vector3.up,direction)*.17f;
                int o=vertices.Count;vertices.Add(new Vector3(0,.10f,0));vertices.Add(direction*.58f+side+Vector3.up*.17f);vertices.Add(direction);vertices.Add(direction*.58f-side+Vector3.up*.17f);
                triangles.AddRange(new[]{o,o+2,o+1,o,o+3,o+2});
            }
            return CreateMesh("Date palm silhouette",vertices,triangles);
        }
        static Mesh MakeBoatHull()
        {
            Vector3[] polygon={new Vector3(-.42f,0,-.5f),new Vector3(.42f,0,-.5f),new Vector3(.48f,0,.18f),new Vector3(0,0,.5f),new Vector3(-.48f,0,.18f)};
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<polygon.Length;i++)vertices.Add(polygon[i]+Vector3.up*.5f);
            for(int i=1;i<polygon.Length-1;i++)triangles.AddRange(new[]{0,i+1,i});
            for(int i=0;i<polygon.Length;i++)
            {
                int j=(i+1)%polygon.Length,o=vertices.Count;
                vertices.Add(polygon[i]+Vector3.up*.5f);vertices.Add(polygon[j]+Vector3.up*.5f);vertices.Add(polygon[j]*.7f-Vector3.up*.5f);vertices.Add(polygon[i]*.7f-Vector3.up*.5f);
                triangles.AddRange(new[]{o,o+1,o+2,o,o+2,o+3});
            }
            return CreateMesh("Original pointed boat hull",vertices,triangles);
        }
        static Mesh CreateMesh(string name,List<Vector3> vertices,List<int> triangles)
        {var mesh=new Mesh{name=name,indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;}

        sealed class MeshBatch
        {
            readonly Dictionary<Material,List<CombineInstance>> entries=new Dictionary<Material,List<CombineInstance>>();
            public void Add(Mesh mesh,Material material,Vector3 position,Vector3 scale,Quaternion rotation)
            {
                List<CombineInstance> list;if(!entries.TryGetValue(material,out list)){list=new List<CombineInstance>();entries.Add(material,list);}
                list.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(position,rotation,scale)});
            }
            public void Flush(Transform parent)
            {
                foreach(var entry in entries)
                {
                    var mesh=new Mesh{name="Combined "+entry.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(entry.Value.ToArray(),true,true);
                    var part=new GameObject(entry.Key.name);part.transform.SetParent(parent,false);part.AddComponent<MeshFilter>().sharedMesh=mesh;
                    var renderer=part.AddComponent<MeshRenderer>();renderer.sharedMaterial=entry.Key;
                    if(entry.Key.renderQueue>2500){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
                }
            }
        }
    }
}
