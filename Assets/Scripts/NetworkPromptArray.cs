using UnityEngine;
using Unity.Netcode;
using System;

public struct NetworkPromptArray : INetworkSerializable, IEquatable<NetworkPromptArray>
{
    public string[] StringArray;

    public NetworkPromptArray(int size = 4)
    {
        StringArray = new string[size];
        for (int i=0; i<size; i++)
        {
            StringArray[i] = string.Empty;
        }
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        for (int i=0; i<4; i++)
        {
            if (StringArray == null)
            {
                StringArray = new string[4];
            }
            if (StringArray[i] == null)
            {
                StringArray[i] = string.Empty;
            }
            serializer.SerializeValue(ref StringArray[i]);
        }
    }

    public bool Equals(NetworkPromptArray other)
    {
        if (StringArray == null || other.StringArray == null) return false;
        if (StringArray.Length != other.StringArray.Length) return false;

        for (int i=0; i<StringArray.Length; i++)
        {
            if (StringArray[i] != other.StringArray[i]) return false;
        }
        return true;
    }
}