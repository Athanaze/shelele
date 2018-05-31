using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;
/*
 * TODO: STRUCTURER LES UNITS DANS DES OBJETS, ET PASSER LES OBJETS EN ARGUMENTS,
 * AU LIEU DE PASSER "prefab" et "type" séparement. Cela permettrai d'instancier au début
 * Unit tank = new Unit(blalblasle ,1, 6,34); et d'ensuite utiliser tank.prefab au lieu
 * de prefabTank !
 * #Refactoring
public class Unit2
{
    public int price;
    public int health;
    public GameObject prefab;
    public int type; // 0 : TANK, 1 : JET, 2 : RL
}*/

public class ClientUnit
{
    public GameObject go;
    public float lastX;
    public float lastY;
    public float lastZ;

    public string id;
}

public class client : MonoBehaviour {
    //NETWORK STUFF
    private const int MAX_CONNECTION = 100;

    private int port = 5701;

    private int hostId;
    private int reliableChannel;
    private int unreliableChannel;

    private int connectionId;
    private bool isConnected = false;
    private bool isStarted = false;
    private float connectionTime;

    private byte error;

    private string playerName;
    private string thisClientSide;
    private const string S = "|";

    //Update of the positions of the units
    private const float MIN_DELTA = 0.05f;
    private const float INTERPOLATION_PERIOD = 0.01f;

    //Type of units
    private const string TANK = "tank";
    private const string JET = "jet";
    private const string RL = "rl";

    // -- LOCAL --
    //Camera movement
    private float speed = 0.1f;
    [SerializeField]
    private Camera camera;
    //Keys
    private bool w;
    private bool a;
    private bool s;
    private bool d;

    // -- PREFABS --
    // Units
    [SerializeField]
    private GameObject prefabTank;

    [SerializeField]
    private GameObject prefabRL;

    [SerializeField]
    private GameObject prefabJet;

    [SerializeField]
    private GameObject prefabAA;

    //Cross
    [SerializeField]
    private GameObject prefabCross;
    [SerializeField]
    private float altitudeOfTheCross;

    // Effects
    [SerializeField]
    private GameObject prefabSmoke;

    [SerializeField]
    private GameObject target;

    //UI
    [SerializeField]
    public InputField nameInput;

    [SerializeField]
    public Text debugText;

    [SerializeField]
    private Text moneyText;

    // MONEY
    public int money = 100;
    public const int PRICE_TANK = 10;
    public const int PRICE_RL = 30;
    public const int PRICE_JET = 25;

    //List of this player's units
    private List<ClientUnit> myUnits;

    //List of the opponent's units
    private List<ClientUnit> opponentUnits;

    private bool destination;
    [SerializeField]
    private LayerMask clickablesLayer;

    private void Start()
    {
        destination = false;
        myUnits = new List<ClientUnit>();
        opponentUnits = new List<ClientUnit>();

    }
    private float time = 0.0f;
    
    private void Update()
    {
        cameraMove();
        if (!isConnected)
        {
            return;
        }
        buyListener();
        refreshMoneyDisplay();
        unitsSelection();
        updateNetwork();
        setDestination();

        time += Time.deltaTime;
        if (time >= INTERPOLATION_PERIOD)
        {
            time = time - INTERPOLATION_PERIOD;
            loopOnUnits();
        }
    }

    //Networking
    public void Connect()
    {
        if (nameInput.text == "")
        {
            Debug.Log("YOU MUST ENTER A NAME !");
            return;
        }
        playerName = nameInput.text;

        //Init babe
        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();

        //How to send messages
        reliableChannel = cc.AddChannel(QosType.Reliable);
        unreliableChannel = cc.AddChannel(QosType.Unreliable);

        HostTopology topo = new HostTopology(cc, MAX_CONNECTION);

        hostId = NetworkTransport.AddHost(topo, 0);
        connectionId = NetworkTransport.Connect(hostId, "127.0.0.1", port, 0, out error);
        connectionTime = Time.time;
        isConnected = true;

    }
    private void updateNetwork()
    {
        int recHostId;
        int connectionId;
        int channelId;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error;

        //Received data
        NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
        switch (recData)
        {
            case NetworkEventType.DataEvent:
                //Decode msg
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                Debug.Log("Receiving : " + msg);
                string[] splitData = msg.Split('|');
                switch (splitData[0])
                {
                    case "POS":
                        print("x : " + splitData[1] + ", y : " + splitData[2] + ", z : " + splitData[3]);
                        break;
                    case "OK":
                        print("Client bonde connected");
                        thisClientSide = splitData[1];
                        break;
                    //newUnit|id|side|type|x|y|z
                    case "newUnit":
                        addClientUnits(splitData[1], splitData[2], splitData[3], splitData[4], splitData[5], splitData[6]);
                        break;
                    //updatePos|id|x|y|z|qx|qy|qz|qw
                    case "updatePos":
                        //TODO: set unit position. Get the unit by id.
                        updatePositionUnit(splitData[1], splitData[2], splitData[3], splitData[4], splitData[5], splitData[6], splitData[7], splitData[8]);
                        break;
                }
                break;
        }
    }
    private void send(string message, int channelId)
    {
        //Debug.Log("Sending to server: " + message);
        byte[] msg = Encoding.Unicode.GetBytes(message);
        NetworkTransport.Send(hostId, connectionId, channelId, msg, message.Length * sizeof(char), out error);
    }

    //Buy stuff
    private void buyListener()
    {
        //TANK [T]
        if (Input.GetKeyDown(KeyCode.T))
        {
            buyTank();
        }
        /* //Jet [R]
         if (Input.GetKeyDown(KeyCode.R))
         {
             buyJet();
         }

         //RL [E]
         if (Input.GetKeyDown(KeyCode.E))
         {
             buyRL();
         }*/

    }
    private void buyTank()
    {
        //Can the player buy it ?
        if (buy(PRICE_TANK))
        {
            //If yes, ask the server to add a tank to the game, at a certain position
            // Now it is just a random position
            Vector3 pos = getRandomPos();
            //"addUnit|1|1.0f|0.0f|1.0f|1"
            send("addUnit" + S + thisClientSide + S + pos.x.ToString() + S + pos.y.ToString() + S + pos.z.ToString() + S + TANK, reliableChannel);
        }
    }

    private bool buy(int price)
    {
        if ((money - price) >= 0)
        {
            money = money - price;
            refreshMoneyDisplay();
            return true;
        }
        else
        {
            return false;
        }

    }
    private void refreshMoneyDisplay()
    {
        moneyText.text = money.ToString() + " $";
    }

    //Tools
    private Vector3 getRandomPos()
    {
        return new Vector3(Random.Range(-2.0f, 2.0f), 1.0f, Random.Range(-2.0f, 2.0f));
    }
    private Vector3 getErrorPos()
    {
        return new Vector3(-99.9f, -99.9f, -99.99f);
    }
    private Vector3 makeV3(string x, string y, string z)
    {
        return new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
    }
    private GameObject getPrefab(string type)
    {
        GameObject prefab;
        switch (type)
        {
            case TANK:
                prefab = prefabTank;
                break;

            case JET:
                prefab = prefabJet;
                break;

            case RL:
                prefab = prefabRL;
                break;

            default:
                prefab = prefabTank;
                break;

        }
        return prefab;
    }

    
    //Move units
    private void setDestination()
    {
        if (Input.GetMouseButton(1))
        {
            //Check if there is a selected unit
            if (aUnitIsSelected())
            {
                if (!destination)
                {
                    destination = true;
                    RaycastHit hit;
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out hit))
                    {
                        Vector3 coord = new Vector3(hit.point.x, altitudeOfTheCross, hit.point.z);
                        GameObject crossy = Instantiate(prefabCross, coord, Quaternion.identity);
                        moveMyUnits(crossy.transform);
                        selectAllUnits(false);

                    }
                }
            }

        }
    }
    public void moveMyUnits(Transform target)
    {
        foreach (ClientUnit unit in myUnits)
        {
            if (unit.go.GetComponent<Unit>().currentlySelected)
            {
                unit.go.GetComponent<TanksMove>().GoToTarget(target);
            }
        }
    }

    //Units selection
    private void unitsSelection()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            selectAllUnits(false);
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            selectAllUnits(true);
        }
        //Selects Units
        if (Input.GetMouseButton(0))
        {
            destination = false;
            RaycastHit rayHit;

            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out rayHit, Mathf.Infinity, clickablesLayer))
            {
                rayHit.collider.GetComponent<Unit>().select();

            }
        }
    }
    public void selectAllUnits(bool b)
    {
        if (b)
        {
            foreach (ClientUnit unit in myUnits)
            {
                unit.go.GetComponent<Unit>().select();
            }
        }
        else
        {
            foreach (ClientUnit unit in myUnits)
            {
                unit.go.GetComponent<Unit>().deselect();
            }
        }
    }
    public bool aUnitIsSelected()
    {
        bool response = false;
        foreach (ClientUnit unit in myUnits)
        {
            if (unit.go.GetComponent<Unit>().currentlySelected)
            {
                response = true;
            }
        }
        return response;
    }

    //Called in Update()
    private void loopOnUnits()
    {
        foreach (ClientUnit unit in myUnits)
        {
            float x = unit.go.transform.position.x;
            float y = unit.go.transform.position.y;
            float z = unit.go.transform.position.z;
            bool update = false;
            
            if(Mathf.Abs(x - unit.lastX) > MIN_DELTA)
            {
                update = true;
            }
            if (Mathf.Abs(z - unit.lastZ) > MIN_DELTA)
            {
                update = true;
            }
            if (update)
            { 
                string sx = x.ToString();
                string sy = y.ToString();
                string sz = z.ToString();

                string qx = unit.go.transform.rotation.x.ToString();
                string qy = unit.go.transform.rotation.y.ToString();
                string qz = unit.go.transform.rotation.z.ToString();
                string qw = unit.go.transform.rotation.w.ToString();

                string pos = sx + S + sy + S + sz;
                string rot = qx + S + qy + S + qz + S + qw;
                debugText.text = unit.id;
                //If the id has been assigned, send a message
                if (unit.id != "")
                {
                    //Tell the server to update his data about our units
                    //updatePosOfMyUnit|unitId|position
                    send("updatePosOfMyUnit|" + unit.id + S + pos + S + rot, reliableChannel);
                    unit.lastX = x;
                    unit.lastY = y;
                    unit.lastZ = z;
                }
            }
        }
    }

    //Local actions
    private void cameraMove()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            w = true;
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            s = true;
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            a = true;
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            d = true;
        }

        //Release
        if (Input.GetKeyUp(KeyCode.W))
        {
            w = false;
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
            s = false;
        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            a = false;
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
            d = false;
        }

        Vector3 ctp = camera.transform.position;
        if (w)
        {
            camera.transform.position = new Vector3(ctp.x, ctp.y, ctp.z + speed);
        }
        if (s)
        {
            camera.transform.position = new Vector3(ctp.x, ctp.y, ctp.z - speed);
        }
        if (a)
        {
            camera.transform.position = new Vector3(ctp.x - speed, ctp.y, ctp.z);
        }
        if (d)
        {
            camera.transform.position = new Vector3(ctp.x + speed, ctp.y, ctp.z);
        }
    }
    private void addClientUnits(string id, string side, string type, string x, string y, string z)
    {
        ClientUnit unit = new ClientUnit();
        unit.id = id;
        unit.lastX = float.Parse(x);
        unit.lastY = float.Parse(y);
        unit.lastZ = float.Parse(z);

        Vector3 v3 = makeV3(x, y, z);
        unit.go = Instantiate(getPrefab(type), v3, Quaternion.identity) as GameObject;

        if(side == thisClientSide)
        {
            myUnits.Add(unit);
        }
        else
        {
            opponentUnits.Add(unit);
        }
        
    }
    private void updatePositionUnit(string id, string x,  string y, string z, string qx, string qy, string qz, string qw)
    {
        foreach (ClientUnit unit in opponentUnits)
        {
            if (unit.id == id)
            {
                unit.go.transform.position = new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
                unit.go.transform.rotation = new Quaternion(float.Parse(qx), float.Parse(qy), float.Parse(qz), float.Parse(qw));

            }
        }
    }
}
