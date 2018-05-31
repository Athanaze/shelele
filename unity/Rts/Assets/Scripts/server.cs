using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System;
using UnityEngine.UI;

public class ServerClient
{
    public int connectionId;
    public string playerName;
    public int side;
}
public class ServerUnit
{
    public string id;
    public string side;
    public int health;
    public string type; //tank, jet or RL
    public string x;
    public string y;
    public string z;
    public string qx;
    public string qy;
    public string qz;
    public string qw;


}
public class server : MonoBehaviour {
    [SerializeField]
    private Text debugText;

    [SerializeField]
    private Text nClientsText;

    //Type of units
    private const string TANK = "tank";
    private const string JET = "jet";
    private const string RL = "rl";

    //Health
    private const int TANK_HEALTH = 100;
    private const int JET_HEALTH = 10;
    private const int RL_HEALTH = 100;


    private const int MAX_CONNECTION = 100;
    private const string S = "|";
    private int port = 5701;

    private int hostId;
    //private int webHostId;
    private int reliableChannel;
    private int unreliableChannel;

    private bool isStarted = false;
    private byte error;

    private List<ServerClient> clients = new List<ServerClient>();
    private List<ServerUnit> units = new List<ServerUnit>();

    private void Start()
    {
        //Init babe
        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();

        //How to send messages
        reliableChannel = cc.AddChannel(QosType.Reliable);
        unreliableChannel = cc.AddChannel(QosType.Unreliable);

        HostTopology topo = new HostTopology(cc, MAX_CONNECTION);

        hostId = NetworkTransport.AddHost(topo, port, null);
       
        isStarted = true;

    }
    private void Update()
    {
        if (!isStarted)
        {
            return;
        }
        int recHostId;
        int connectionId;
        int channelId;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error;
        NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
        switch (recData)
        {
            case NetworkEventType.ConnectEvent:
                Debug.Log("Player" + connectionId + " has connected");
                onClientConnection(connectionId);
                break;

            case NetworkEventType.DataEvent:
                //Decode msg
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                Debug.Log("Receiving : " + msg);
                string[] splitData = msg.Split('|');
                switch (splitData[0])
                {
                    case "addUnit":
                        //"addUnit|1|1.0f|0.0f|1.0f|1"
                        print("side : " + splitData[1] + ", x : " + splitData[2] + ", y : " + splitData[3] + ", z : " + splitData[4] + ", type : "+ splitData[5]);
                        addUnit(splitData[1], splitData[2], splitData[3], splitData[4], splitData[5], connectionId);
                        break;
                    case "updatePosOfMyUnit":
                        //id|x|y|z|qx|qy|qz|qw
                        if ( updatePosUnit(splitData[1], splitData[2], splitData[3], splitData[4], splitData[5], splitData[6], splitData[7], splitData[8]) )
                        {
                            send("updatePos|"+ splitData[1]
                                + S + splitData[2]
                                + S + splitData[3]
                                + S + splitData[4]
                                + S + splitData[5]
                                + S + splitData[6]
                                + S + splitData[7]
                                + S + splitData[8],
                                reliableChannel, clients);
                        }
                        else
                        {
                            print("There is no match for the unit " + splitData[1]);
                        }
                        break;
                }
                break;

            case NetworkEventType.DisconnectEvent:
                print("DisconnectEvent");
                Debug.Log("Player" + connectionId + " has disconnected");
                break;
        }
        
    }

    private void onClientConnection(int cnnId)
    {
        //Add him to the list
        ServerClient c = new ServerClient();
        c.connectionId = cnnId;
        c.playerName = "TEMP_NAME";
        c.side = clients.Count + 1;
        clients.Add(c);

        //Send a message to this client
        //send("POS|1.1|0.0|3.0", reliableChannel, cnnId);
        send("OK|"+c.side.ToString(), reliableChannel, cnnId);

        nClientsText.text = clients.Count.ToString() + " connected clients";
    }

    private void addUnit(string side, string x, string y, string z, string type, int cnnId)
    {
        Debug.Log("addUnit()");
        ServerUnit su = new ServerUnit();
        int c = units.Count + 1;
        su.id = c.ToString();
        print("ID SERVER UNIT : " + su.id);
        su.side = side;
        su.x = x;
        su.y = y;
        su.z = z;
        su.type = type;

        switch (type)
        {
            case TANK:
                su.health = TANK_HEALTH;
                break;
            case JET:
                su.health = JET_HEALTH;
                break;
            case RL:
                su.health = RL_HEALTH;
                break;
        }

        units.Add(su);
        //Now tell ALL the clients that a new unit has been added
        //newUnit|id|side|type|x|y|z
        string msg = "newUnit|" + su.id +S+ su.side + S + su.type + S + su.x + S + su.y + S + su.z;
        send(msg, reliableChannel, clients);

        debugText.text = units.Count.ToString() + " units in the game";
    }
    private bool updatePosUnit(string id, string x, string y, string z, string qx, string qy, string qz, string qw)
    {
        bool match = false;
        foreach(ServerUnit unit in units)
        {
            if (unit.id == id)
            {
                match = true;
                unit.x = x;
                unit.y = y;
                unit.z = z;
                unit.qx = qx;
                unit.qy = qy;
                unit.qz = qz;
                unit.qw = qw;
            }
        }
        return match;

    }
    private Vector3 makeV3(string x, string y, string z)
    {
        return new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
    }
    private void send(string message, int channelId, int cnnId)
    {

        List<ServerClient> c = new List<ServerClient>();
        c.Add(clients.Find(x => x.connectionId == cnnId));
        send(message, channelId, c);
    }
    private void send(string message, int channelId, List<ServerClient> c)
    {
        Debug.Log("Sending : " + message);
        byte[] msg = Encoding.Unicode.GetBytes(message);
        foreach(ServerClient sc in c)
        {
            NetworkTransport.Send(hostId, sc.connectionId, channelId, msg, message.Length * sizeof(char), out error);
        }
    }
}
