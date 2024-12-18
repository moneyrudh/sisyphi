using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using System;

public struct PlayerData: IEquatable<PlayerData>, INetworkSerializable
{
    public ulong clientId;
    public FixedString64Bytes playerName;
    public int hairColorId;
    public int skinColorId;
    public int pantColorId;
    public int eyesColorId;

    public bool Equals(PlayerData other)
    {
        return 
            clientId == other.clientId &&
            playerName == other.playerName &&
            hairColorId == other.hairColorId && 
            skinColorId == other.skinColorId &&
            eyesColorId == other.eyesColorId;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref hairColorId);
        serializer.SerializeValue(ref skinColorId);
        serializer.SerializeValue(ref pantColorId);
        serializer.SerializeValue(ref eyesColorId);
    }
}
