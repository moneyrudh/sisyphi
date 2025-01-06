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
    public FixedString64Bytes playerId;
    public int hairColorId;
    public int skinColorId;
    public int pantColorId;
    public int eyesColorId;
    public int boulderMaterialId;

    public bool Equals(PlayerData other)
    {
        return 
            clientId == other.clientId &&
            playerName == other.playerName &&
            playerId == other.playerId &&
            hairColorId == other.hairColorId && 
            skinColorId == other.skinColorId &&
            eyesColorId == other.eyesColorId &&
            boulderMaterialId == other.boulderMaterialId;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref playerId);
        serializer.SerializeValue(ref hairColorId);
        serializer.SerializeValue(ref skinColorId);
        serializer.SerializeValue(ref pantColorId);
        serializer.SerializeValue(ref eyesColorId);
        serializer.SerializeValue(ref boulderMaterialId);
    }
}
