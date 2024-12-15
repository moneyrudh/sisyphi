using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using System;
using UnityEngine;

public enum MaterialCategory : byte
{
    Hair = 0,
    Skin = 1,
    Pant = 2,
    Eyes = 3
}

public struct NetworkedMaterialCategory : IEquatable<NetworkedMaterialCategory>, INetworkSerializable
{
    public MaterialCategory Value;

    public bool Equals(NetworkedMaterialCategory networkedMaterialCategory)
    {
        return networkedMaterialCategory.Value == Value;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Value);
    }

    // Implicit conversion operators for convenience
    public static implicit operator MaterialCategory(NetworkedMaterialCategory networkedEnum) => networkedEnum.Value;
    public static implicit operator NetworkedMaterialCategory(MaterialCategory value) => new NetworkedMaterialCategory { Value = value };
}
