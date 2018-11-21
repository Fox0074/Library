#define USE_COMPRESSION

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Runtime.Serialization;


public interface IEvents
{
    Action OnStartConnect { get; set; }
    Action OnConnected { get; set; }
    Action OnDisconnect { get; set; }
    Action OnPing { get; set; }
    Action<Exception> OnError { get; set; }
    Action<Unit,User> OnGetMessage { get; set; }
    Action<int> OnBark { get; set; }
}

public enum UserType
{
    UnAuthorized,
    User,
    Admin,
    System,
}

public class ServerNet : IEvents
{
    public static ServerNet current;
    public IEvents Events
    {
        get { return this; }
    }


    #region События

    Action IEvents.OnStartConnect { get; set; }
    Action IEvents.OnConnected { get; set; }
    Action IEvents.OnDisconnect { get; set; }
    Action IEvents.OnPing { get; set; }
    Action<Exception> IEvents.OnError { get; set; }
    Action<Unit, User> IEvents.OnGetMessage { get; set; }
    Action<int> IEvents.OnBark { get; set; }
    #endregion


    public System.Net.Sockets.TcpListener SERV;
    public static readonly List<User> ConnectedUsers = new List<User>();


    public ServerNet(int Port)
    {
        SERV = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, Port);
    }

    public void Start()
    {       
        SERV.Start();
        SERV.BeginAcceptTcpClient(OnAcceptClient, null);
        if (current == null) current = this;
    }

    public void Stop()
    {
        SERV.Stop();
    }

    private void OnAcceptClient(IAsyncResult asyncResult)
    {
        var client = SERV.EndAcceptTcpClient(asyncResult);
        SERV.BeginAcceptTcpClient(OnAcceptClient, null);

        User up = new User(client);
        ConnectedUsers.Add(up);
        Events.OnConnected.Invoke();

        try
        {
            up.nStream.BeginRead(up.HeaderLength, OnDataReadCallback, up);
        }
        catch (IOException ex)
        {
            ConnectedUsers.Remove(up);
            Events.OnDisconnect.Invoke();
        }
    }
    
    private void OnDataReadCallback(IAsyncResult asyncResult)
    {
        User user = (User)asyncResult.AsyncState;
        byte[] data;           
        try
        {
            user.nStream.EndRead(asyncResult);
            int dataLength = BitConverter.ToInt32(user.HeaderLength, 0);
            data = new byte[dataLength];
            WaitData(user._socket,dataLength);
            user.nStream.Read(data);

            Unit unit = MessageFromBinary<Unit>(data);
            if (unit.Command == "OnPing")
            {
                // отражаем пинг
                SendMessage(user.nStream, unit);
                if (Events.OnPing != null) Events.OnPing.BeginInvoke(null, null);
            }
            else
            {
                Events.OnGetMessage.Invoke(unit,user);
                ProcessMessages.GuideMessage(unit,user);
            }

            user.nStream.BeginRead(user.HeaderLength, OnDataReadCallback, user);
        }
        catch (Exception ex)
        {
            ConnectedUsers.Remove(user);
            Events.OnDisconnect.Invoke();
            GC.Collect(2, GCCollectionMode.Optimized);                
            return;
        }
    }

    private void WaitData(TcpClient stream,int dataLength)
    {
        int x = 0;
        while (x < dataLength)
        {               
            x = stream.Available;
            Thread.Sleep(5);
        }
    }
#region Send/Receive

sealed class DeserializeBinder : SerializationBinder
{
    public override Type BindToType(string assemblyName, string typeName)
    {
        return Type.GetType(typeName);
    }
}

private T MessageFromBinary<T>(byte[] BinaryData) where T : class
    {
#if USE_COMPRESSION
        using (MemoryStream memory = new MemoryStream(BinaryData))
        {
            using (var gZipStream = new GZipStream(memory, CompressionMode.Decompress, false))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Binder = new DeserializeBinder();
                return (T)binaryFormatter.Deserialize(gZipStream);
            }
        }
#else
        using (MemoryStream memory = new MemoryStream(BinaryData))
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            return (T)binaryFormatter.Deserialize(memory);
        }
#endif
    }

    public static void SendMessage(ConqurentNetworkStream nStream, Unit msg)
    {
#if USE_COMPRESSION

        using (MemoryStream memory = new MemoryStream())
        {
            using (var gZipStream = new GZipStream(memory, CompressionMode.Compress, false))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
                binaryFormatter.Serialize(gZipStream, msg);
            }

            byte[] BinaryData = memory.ToArray();
            byte[] DataLength = BitConverter.GetBytes(BinaryData.Length);
            byte[] DataWithHeader = DataLength.Concat(BinaryData).ToArray();

            nStream.Add(DataWithHeader);
        }

#else
        using (MemoryStream memory = new MemoryStream())
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(memory, msg);
            nStream.Add(memory.ToArray());
        }
#endif
    }
    #endregion
}
