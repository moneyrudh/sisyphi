using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System;

public struct PlayerData: IEquatable<PlayerData>, INetworkSerializable
{
    public ulong clientId;
    public int hairColorId;
    public int skinColorId;
    public int pantColorId;
    public int eyesColorId;

    public bool Equals(PlayerData other)
    {
        return clientId == other.clientId;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref hairColorId);
        serializer.SerializeValue(ref skinColorId);
        serializer.SerializeValue(ref pantColorId);
        serializer.SerializeValue(ref eyesColorId);
    }
}
