using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using MapObjects;
using MapParser;
using System.Linq;

public class MapBuilder : MonoBehaviour
{

    [SerializeField] GameObject player;
    [SerializeField] bool disableWalls = false;
    [SerializeField] bool disableFloors = false;
    [SerializeField] bool disableCeilings = false;

    [SerializeField] bool disableDoorWindow = false;
    [SerializeField] bool disableFurniture = false;
    [SerializeField] bool disableUtensil = false;
    [SerializeField] bool disableElectronic = false;
    [SerializeField] bool disableGoal = false;

    [FormerlySerializedAs("jsonRawFile")]
    [SerializeField] private TextAsset mapFile;

    [SerializeField] private MaterialData floorMaterialData;
    [SerializeField] private MaterialData wallMaterialData;
    [SerializeField] private MaterialData ceilingMaterialData;

    [SerializeField] private ObjectData doorWindowObjectData;
    [SerializeField] private ObjectData furnitureObjectData;
    [SerializeField] private ObjectData utensilObjectData;
    [SerializeField] private ObjectData electronicObjectData;
    [SerializeField] private ObjectData goalObjectData;
    [SerializeField] private Material defaultFloorMaterial;
    [SerializeField] private Material defaultWallMaterial;
    [SerializeField] private Material defaultCeilingMaterial;
    [SerializeField] private float ceilingHeight = 3.0f;
    [SerializeField] private float floorTextureScale = .75f;
    [SerializeField] private float wallTextureScale = 2.0f;
    [SerializeField] private float ceilingTextureScale = 1f;

    [SerializeField] private bool useGrass = true;
    [SerializeField] private string grassID = "3.1";

    [SerializeField] GameObject grassSprite;

    private Mesh floorMesh;
    private Mesh wallMesh;
    private Mesh objMesh;

    private GameObject floorParent;
    private GameObject ceilingParent;
    private GameObject wallsParent;
    private GameObject doorWindowParent;
    private GameObject furnitureParent;
    private GameObject utensilParent;
    private GameObject electronicParent;
    private GameObject goalParent;

    private void InstanceFloorTile(Floor floor)
    {
        string code = floor.type;
        int[] startArr = floor.start;
        int[] endArr = floor.end;

        Vector3 start = new Vector3(startArr[0], 0, -startArr[1]);
        Vector3 end = new Vector3(endArr[0] + 1, 0, -endArr[1] - 1);
        Vector3 center = (end - start) / 2;
        Vector3 size = new Vector3(Mathf.Abs(end.x - start.x), 1, Mathf.Abs(end.z - start.z));


        // get the material data
        Material material = null;
        bool useGlobalUV = false;
        Vector2 scale = Vector2.one;
        try
        {
            material = floorMaterialData.GetMaterial(code);
            useGlobalUV = floorMaterialData.DoesMaterialUsesGlobalUV(code);
            scale = floorMaterialData.GetMaterialScale(code);
        }
        catch (System.Exception)
        {
            Debug.LogError("Material " + code + " not found");
            material = defaultFloorMaterial;
        }

        // make a copy of the floor mesh
        Mesh mesh = new Mesh
        {
            vertices = floorMesh.vertices,
            triangles = floorMesh.triangles,
            uv = floorMesh.uv
        };
        // scale the mesh uvs
        var newUvs = new Vector2[mesh.uv.Length];
        for (var i = 0; i < newUvs.Length; i++)
        {
            newUvs[i] = new Vector2(mesh.uv[i].x * size.x, mesh.uv[i].y * size.z);
        }

        // apply the scale
        for (var i = 0; i < newUvs.Length; i++)
        {
            newUvs[i] *= scale;
        }

        // if the material uses global uv, move the uvs to the of the "world"
        if (useGlobalUV)
        {
            for (var i = 0; i < newUvs.Length; i++)
            {
                newUvs[i] += new Vector2(start.x, start.z);
            }
        }


        mesh.uv = newUvs;
        // fix the mesh normals
        mesh.RecalculateNormals();

        // create the floor tile
        var floorPiece = new GameObject("Floor:" + startArr[0] + "_" + startArr[1] + "_" + endArr[0] + "_" + endArr[1]);
        floorPiece.transform.position = start + center;
        floorPiece.transform.rotation = Quaternion.identity;
        floorPiece.transform.localScale = size;
        floorPiece.transform.parent = floorParent.transform;
        //add mesh renderer and filter
        floorPiece.AddComponent<MeshRenderer>();
        floorPiece.AddComponent<MeshFilter>();
        floorPiece.AddComponent<MeshCollider>();

        floorPiece.tag = "floor";
        floorPiece.GetComponent<MeshRenderer>().material = material;
        floorPiece.GetComponent<MeshFilter>().mesh = mesh;
        floorPiece.GetComponent<MeshCollider>().sharedMesh = mesh;

        if (code == grassID && useGrass)
        {
            float density = size.x * size.z;
            for (int i = 0; i < density; i++)
            {
                float x = Random.Range(start.x, end.x);
                float z = Random.Range(start.z, end.z);
                float y = 0.01f;
                Vector3 position = new Vector3(x, y, z);
                // create cube
                GameObject grass = Instantiate(grassSprite, position, Quaternion.identity);
                grass.transform.parent = floorPiece.transform;
            }

        }
    }

    private void InstanceWallTile(Wall wall)
    {
        string code = wall.type;
        int[] startArr = wall.start;
        int[] endArr = wall.end;

        Vector3 start = new Vector3(startArr[0], 0, -startArr[1]);
        Vector3 end = new Vector3(endArr[0] + 1, 0, -endArr[1] - 1);
        Vector3 center = (end - start) / 2;
        var size = new Vector3(
            Mathf.Abs(end.x - start.x),
            ceilingHeight,
            Mathf.Abs(end.z - start.z)
        );

        // get the material data
        Material material = null;
        //bool useGlobalUV = false;
        //Vector2 scale = Vector2.one;
        try
        {
            material = wallMaterialData.GetMaterial(code);
            //useGlobalUV = floorMaterialData.DoesMaterialUsesGlobalUV(code);
            //scale = floorMaterialData.GetMaterialScale(code);
        }
        catch (System.Exception)
        {
            Debug.LogError("Material " + code + " not found");
            material = defaultWallMaterial;
        }

        // make 4 copies of the wall mesh and rotate them accordingly

        Mesh meshFront = new Mesh
        {
            vertices = wallMesh.vertices,
            triangles = wallMesh.triangles,
            uv = wallMesh.uv
        };

        Mesh meshBack = new Mesh
        {
            vertices = wallMesh.vertices,
            triangles = wallMesh.triangles,
            uv = wallMesh.uv
        };

        Mesh meshLeft = new Mesh
        {
            vertices = wallMesh.vertices,
            triangles = wallMesh.triangles,
            uv = wallMesh.uv
        };

        Mesh meshRight = new Mesh
        {
            vertices = wallMesh.vertices,
            triangles = wallMesh.triangles,
            uv = wallMesh.uv
        };

        // scale the mesh uvs
        var newUvsFront = new Vector2[meshFront.uv.Length];
        var newUvsBack = new Vector2[meshBack.uv.Length];
        var newUvsLeft = new Vector2[meshLeft.uv.Length];
        var newUvsRight = new Vector2[meshRight.uv.Length];

        // front and back
        for (var i = 0; i < newUvsFront.Length; i++)
        {
            newUvsFront[i] = new Vector2(meshFront.uv[i].x * size.z, meshFront.uv[i].y * size.y);
            newUvsBack[i] = new Vector2(meshBack.uv[i].x * size.z, meshBack.uv[i].y * size.y);

            newUvsFront[i] *= wallTextureScale;
            newUvsBack[i] *= wallTextureScale;
        }
        // left and right
        for (var i = 0; i < newUvsLeft.Length; i++)
        {
            newUvsLeft[i] = new Vector2(meshLeft.uv[i].x * size.x, meshLeft.uv[i].y * size.y);
            newUvsRight[i] = new Vector2(meshRight.uv[i].x * size.x, meshRight.uv[i].y * size.y);

            newUvsLeft[i] *= wallTextureScale;
            newUvsRight[i] *= wallTextureScale;
        }

        meshFront.uv = newUvsFront;
        meshBack.uv = newUvsBack;

        meshLeft.uv = newUvsLeft;
        meshRight.uv = newUvsRight;

        // fix the mesh normals
        meshFront.RecalculateNormals();
        meshBack.RecalculateNormals();
        meshLeft.RecalculateNormals();
        meshRight.RecalculateNormals();

        // create the object and tiles
        var wallObj = new GameObject("Wall:" + startArr[0] + "_" + startArr[1] + "_" + endArr[0] + "_" + endArr[1]);
        wallObj.tag = "wall";
        var wallFront = new GameObject("WallFront:" + startArr[0] + "_" + startArr[1] + "_" + endArr[0] + "_" + endArr[1]);
        var wallBack = new GameObject("WallBack:" + startArr[0] + "_" + startArr[1] + "_" + endArr[0] + "_" + endArr[1]);
        var wallLeft = new GameObject("WallLeft:" + startArr[0] + "_" + startArr[1] + "_" + endArr[0] + "_" + endArr[1]);
        var wallRight = new GameObject("WallRight:" + startArr[0] + "_" + startArr[1] + "_" + endArr[0] + "_" + endArr[1]);
        var wallPieces = new GameObject[] { wallFront, wallBack, wallLeft, wallRight };


        foreach (var wallPiece in wallPieces)
        {
            //wallPiece.transform.position = start + center;
            wallPiece.transform.rotation = Quaternion.identity;
            //wallPiece.transform.localScale = size;
            wallPiece.transform.parent = wallObj.transform;
            //add mesh renderer and filter

            wallPiece.AddComponent<MeshRenderer>();
            wallPiece.AddComponent<MeshFilter>();
            wallPiece.AddComponent<MeshCollider>();

        }

        // rotate the wall pieces
        wallFront.transform.Rotate(0, 0, 0);
        wallBack.transform.Rotate(0, 180, 0);
        wallLeft.transform.Rotate(0, 90, 0);
        wallRight.transform.Rotate(0, -90, 0);

        // add the meshes to the wall
        wallFront.GetComponent<MeshRenderer>().material = material;
        wallFront.GetComponent<MeshFilter>().mesh = meshFront;
        wallFront.GetComponent<MeshCollider>().sharedMesh = meshFront;

        wallBack.GetComponent<MeshRenderer>().material = material;
        wallBack.GetComponent<MeshFilter>().mesh = meshBack;
        wallBack.GetComponent<MeshCollider>().sharedMesh = meshBack;

        wallLeft.GetComponent<MeshRenderer>().material = material;
        wallLeft.GetComponent<MeshFilter>().mesh = meshLeft;
        wallLeft.GetComponent<MeshCollider>().sharedMesh = meshLeft;

        wallRight.GetComponent<MeshRenderer>().material = material;
        wallRight.GetComponent<MeshFilter>().mesh = meshRight;
        wallRight.GetComponent<MeshCollider>().sharedMesh = meshRight;

        wallObj.transform.position = start + center;
        wallObj.transform.rotation = Quaternion.identity;
        wallObj.transform.localScale = size;
        wallObj.transform.parent = wallsParent.transform;
    }

    private void InstanceCeilingTile(Ceiling ceiling)
    {
        string code = ceiling.type;
        int[] startArr = ceiling.start;
        int[] endArr = ceiling.end;

        Vector3 start = new Vector3(startArr[0], ceilingHeight, -startArr[1]);
        Vector3 end = new Vector3(endArr[0] + 1, ceilingHeight, -endArr[1] - 1);
        Vector3 center = (end - start) / 2;
        Vector3 size = new Vector3(Mathf.Abs(end.x - start.x), 1, Mathf.Abs(end.z - start.z));

        // get the material
        Material material = null;
        // bool useGlobalUV = false;
        // Vector2 scale = Vector2.one;
        try
        {
            material = ceilingMaterialData.GetMaterial("0.0");
            // useGlobalUV = floorMaterialData.DoesMaterialUsesGlobalUV(code);
            // scale = floorMaterialData.GetMaterialScale(code);
        }
        catch (System.Exception)
        {
            Debug.LogError("Material " + code + " not found");
            material = defaultCeilingMaterial;
        }

        GameObject ceilingPiece = new GameObject("Ceiling:" + startArr[0] + "_" + startArr[1] + "_" + endArr[0] + "_" + endArr[1]);
        ceilingPiece.transform.position = start + center;
        ceilingPiece.tag = "ceiling";
        // it needs to be rotated 180 degrees since the mesh is upside down
        ceilingPiece.transform.rotation = Quaternion.Euler(180, 0, 0);
        ceilingPiece.transform.localScale = size;
        ceilingPiece.transform.parent = ceilingParent.transform;
        //add mesh renderer and filter
        ceilingPiece.AddComponent<MeshRenderer>();
        ceilingPiece.AddComponent<MeshFilter>();
        ceilingPiece.AddComponent<MeshCollider>();

        // make a copy of the floor mesh
        Mesh mesh = new Mesh
        {
            vertices = floorMesh.vertices,
            triangles = floorMesh.triangles,
            uv = floorMesh.uv
        };
        // scale the mesh uvs
        var newUvs = new Vector2[mesh.uv.Length];
        for (var i = 0; i < newUvs.Length; i++)
        {
            newUvs[i] = new Vector2(mesh.uv[i].x * size.x, mesh.uv[i].y * size.z);
            newUvs[i] *= ceilingTextureScale;
        }

        mesh.uv = newUvs;


        // fix the mesh normals
        mesh.RecalculateNormals();

        ceilingPiece.GetComponent<MeshRenderer>().material = material;
        ceilingPiece.GetComponent<MeshFilter>().mesh = mesh;
        ceilingPiece.GetComponent<MeshCollider>().sharedMesh = mesh;

    }

    private void InstanceProp(MapProp prop, ObjectData objectData, GameObject parent = null)
    {
        string type = prop.getType();
        int[] pos = prop.getPos();
        ObjectPrefab propData = null;
        GameObject prefab = null;
        try
        {
            propData = objectData.GetObject(type);

        }
        catch (System.Exception)
        {
            Debug.LogError("Prefab not found for type " + type);
        }

        prefab = propData.prefab;
        string name = prefab.name + ":" + type + "_" + pos[0] + "_" + pos[1];

        // Position
        float posX = pos[0] + propData.offsetX;
        float posY = -pos[1] + propData.offsetY;
        Vector3 vecpos = new Vector3(posX, 0, posY);

        // Rotation
        Quaternion rot = Quaternion.Euler(0, propData.rotation, 0);

        // Create the object
        GameObject obj = Instantiate(prefab, vecpos, rot);
        Debug.LogWarning($"nome: {obj.name} quantidade: {obj.GetComponents<MeshCollider>().Count()}");
        Debug.LogWarning($"nome: {obj.name} quantidade: {prefab.GetComponents<MeshCollider>().Count()}");

        obj.name = name;
        if (parent != null)
        {
            obj.transform.parent = parent.transform;
        }

        var meshFilters = obj.GetComponents<MeshFilter>();

        if(meshFilters.Count() > 1)
        {
            for (int i = 1; i < meshFilters.Count(); i++)
            {
                Destroy(meshFilters[i]);
            }
        }
        if(!meshFilters.Any())
        {
            obj.AddComponent<MeshFilter>();
        }

        var meshColliders = obj.GetComponents<MeshCollider>();

        if(meshColliders.Count() > 1)
        {
            for (int i = 1; i < meshColliders.Count(); i++)
            {
                Destroy(meshColliders[i]);
            }
        }
        if(!meshColliders.Any())
        {
            obj.AddComponent<MeshCollider>();
        }

        var meshRenderers = obj.GetComponents<MeshRenderer>();

        if(meshRenderers.Count() > 1)
        {
            for (int i = 1; i < meshRenderers.Count(); i++)
            {
                Destroy(meshRenderers[i]);
            }
        }
        if(!meshRenderers.Any())
        {
            obj.AddComponent<MeshRenderer>();
        }

        
        var meshFilter = obj.GetComponent<MeshFilter>();

        if(meshFilter != null)
        {

            Mesh mesh = meshFilter.mesh;

            var sharedMesh = meshFilter.sharedMesh;

            Vector3[] meshVertices = mesh.vertices; 

            var meshTriangles = sharedMesh.triangles; 

                
            Mesh newMesh = new Mesh
            {
                vertices = meshVertices,
                triangles = meshTriangles
            };
            obj.GetComponent<MeshCollider>().sharedMesh = newMesh;
        }
    }

    void BuildMap(Map map)
    {
        List<Wall> walls = map.layers.walls;
        List<Floor> floors = map.layers.floors;
        List<Ceiling> ceilings = map.layers.ceilings;
        List<DoorAndWindow> door_and_windows = map.layers.door_and_windows;
        List<Furniture> furniture = map.layers.furniture;
        List<Utensil> utensils = map.layers.utensils;
        List<Electronic> eletronics = map.layers.eletronics;
        List<Goal> goals = map.layers.goals;
        List<Person> persons = map.layers.persons;

        // Build the floors
        if (!disableFloors)
        {
            foreach (Floor floor in floors)
            {
                InstanceFloorTile(floor);
            }
        }

        // Build the walls
        if (!disableWalls)
        {
            foreach (Wall wall in walls)
            {
                InstanceWallTile(wall);
            }
        }

        // Build the ceilings
        if (!disableCeilings)
        {
            foreach (Ceiling ceiling in ceilings)
            {
                InstanceCeilingTile(ceiling);
            }
        }

        // Build the doors and windows
        if (!disableDoorWindow)
        {
            foreach (DoorAndWindow obj in door_and_windows)
            {
                InstanceProp(obj, doorWindowObjectData, doorWindowParent);
            }
        }

        // Build the furniture
        if (!disableFurniture)
        {
            foreach (Furniture obj in furniture)
            {
                InstanceProp(obj, furnitureObjectData, furnitureParent);
            }
        }

        // Build the utensils
        if (!disableUtensil)
        {
            foreach (Utensil obj in utensils)
            {
                InstanceProp(obj, utensilObjectData, utensilParent);
            }
        }

        // Build the electronics
        if (!disableElectronic)
        {
            foreach (Electronic obj in eletronics)
            {
                InstanceProp(obj, electronicObjectData, electronicParent);
            }
        }

        // Build the goals
        if (!disableGoal)
        {
            foreach (Goal obj in goals)
            {
                InstanceProp(obj, goalObjectData, goalParent);
            }
        }


        // move player to the map start position
        float x = (float)persons[0].pos[0];
        float y = (float)persons[0].pos[1];
        var startPos = new Vector3(x, 0f, -y);

        // if there is no player in the scene move the camera to the start position
        if (player != null)
        {
            player.transform.position = startPos;
        }

        // Set Map object as parent of all layers
        Transform mapTransform = GetComponent<Transform>();
        floorParent.transform.parent = mapTransform;
        ceilingParent.transform.parent = mapTransform;
        wallsParent.transform.parent = mapTransform;
        doorWindowParent.transform.parent = mapTransform;
        furnitureParent.transform.parent = mapTransform;
        utensilParent.transform.parent = mapTransform;
        electronicParent.transform.parent = mapTransform;
        goalParent.transform.parent = mapTransform;
    }

    bool isJson(string data)
    {
        string stripped = data.Trim();
        return stripped[0] == '{';
    }

    bool IsXML(string data)
    {
        string stripped = data.Trim();
        return stripped[0] == '<';
    }


    // Awake is called when the script instance is being loaded.
    void Awake()
    {
        // Floor mesh
        floorMesh = new Mesh
        {
            vertices = new Vector3[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(0.5f, 0, -0.5f) },
            triangles = new int[] { 0, 1, 2, 0, 2, 3 },
            uv = new Vector2[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) }
        };
        // Wall mesh, its the same as the but vertical
        wallMesh = new Mesh
        {
            vertices = new Vector3[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(-0.5f, 0, 0.5f), new Vector3(-0.5f, 1, -0.5f), new Vector3(-0.5f, 1, 0.5f) },
            triangles = new int[] { 0, 1, 2, 3, 2, 1 },
            //this.wallMesh.uv = new Vector2[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0), new Vector2(1, 1) };

            uv = new Vector2[] { new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 0) }
        };

        
        // Create the parents
        floorParent = new GameObject("Floor");
        ceilingParent = new GameObject("Ceiling");
        wallsParent = new GameObject("Walls");
        doorWindowParent = new GameObject("DoorWindow");
        furnitureParent = new GameObject("Furniture");
        utensilParent = new GameObject("Utensils");
        electronicParent = new GameObject("Electronics");
        goalParent = new GameObject("Goals");

        if (mapFile == null)
        {
            Debug.LogError("No map file found");
            return;
        }

        IMapParser mapParser = null;

        // Check if the file is JSON or XML
        if (isJson(mapFile.text))
        {
            Debug.Log("JSON file detected");
            mapParser = new MapParserJSON(mapFile.text);
        }
        else if (IsXML(mapFile.text))
        {
            Debug.Log("XML file detected");
            mapParser = new MapParserXML(mapFile.text);
        }
        else
        {
            Debug.LogError("Invalid file format");
            return;
        }
        Map map = mapParser.ParseMap();
        // Build the map
        BuildMap(map);
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
