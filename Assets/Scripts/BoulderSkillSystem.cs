using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class BoulderSkillSystem : NetworkBehaviour
{
    [Header("Input Actions")]
    public InputActionReference shrinkAction;
    public InputActionReference growAction;

    [Header("Cooldown Settings")]
    public float skillCooldownDuration = 60f;
    public float abnormalStateDuration = 30f;

    [Header("References")]
    public GameObject boulder;
    private BoulderController boulderController;
    private NetworkVariable<NetworkObjectReference> boulderRef = new NetworkVariable<NetworkObjectReference>();
    private NetworkVariable<BoulderSize> currentSize = new NetworkVariable<BoulderSize>(BoulderSize.Medium);
    private NetworkVariable<ulong> lastModifiedClientId = new NetworkVariable<ulong>();

    private NetworkVariable<float> skillCooldownEndTime = new NetworkVariable<float>(0f);
    private NetworkVariable<float> stateResetTime = new NetworkVariable<float>(0f);
    private NetworkVariable<bool> skillEnabled = new NetworkVariable<bool>(true);
    private Coroutine stateResetCoroutine;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            currentSize.OnValueChanged += OnSizeChanged;
        }
        // boulderController = boulder.GetComponent<BoulderController>();
        // if (IsOwner)
        // {
        //     EnableInputs();
        // }
    }

    private void OnSizeChanged(BoulderSize previousValue, BoulderSize newValue)
    {
        if (IsOwner)
        {
            if (previousValue == BoulderSize.Small && newValue == BoulderSize.Medium)
            {
                PlaySound("BoulderLarge");
            }
            if (previousValue == BoulderSize.Medium && newValue == BoulderSize.Large)
            {
                PlaySound("BoulderLarge");
            }
            if (previousValue == BoulderSize.Large && newValue == BoulderSize.Medium)
            {
                PlaySound("BoulderSmall");
            }
            if (previousValue == BoulderSize.Medium && newValue == BoulderSize.Small)
            {
                PlaySound("BoulderSmall");
            }
        }

        if (!IsServer) return;

        if (stateResetCoroutine != null)
        {
            StopCoroutine(stateResetCoroutine);
        }

        if (newValue != BoulderSize.Medium)
        {
            stateResetTime.Value = Time.time + abnormalStateDuration;
            stateResetCoroutine = StartCoroutine(StateResetTimer());
        }
    }

    private IEnumerator StateResetTimer()
    {
        yield return new WaitForSeconds(abnormalStateDuration);

        if (currentSize.Value != BoulderSize.Medium)
        {
            // RequestSizeChangeServerRpc(BoulderSize.Medium);
            ResetSizeServerRpc();
        }
    }

    private void Start()
    {
        SisyphiGameManager.Instance.GameFinishedEvent += BoulderSkillSystem_OnGameFinished;
    }

    private void Update()
    {
        if (IsOwner)
        {
            if (!skillEnabled.Value && Time.time >= skillCooldownEndTime.Value)
            {
                SetSkillEnabledServerRpc(true);
                PlayerHUD.Instance.HandleUsedSkill(false);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetSkillEnabledServerRpc(bool enabled)
    {
        skillEnabled.Value = enabled;
    }

    [ServerRpc(RequireOwnership = false)]
    private void ResetSizeServerRpc()
    {
        currentSize.Value = BoulderSize.Medium;
        if (boulderRef.Value.TryGet(out NetworkObject boulderNetObj))
        {
            BoulderController boulderController = boulderNetObj.GetComponent<BoulderController>();
            if (boulderController != null)
            {
                boulderController.SetBoulderProperties(BoulderSize.Medium);
            }
        }
    }

    private void BoulderSkillSystem_OnGameFinished(object sender, System.EventArgs e)
    {
        DisableInputs();
    }

    [ServerRpc(RequireOwnership = false)]
    public void InitializeBoulderSkillSystemServerRpc(ServerRpcParams serverRpcParams = default)
    {
        int index = SisyphiGameMultiplayer.Instance.GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);
        boulder = GameObject.Find("Boulder_" + index);

        if (IsServer && boulder != null)
        {
            Debug.Log("Setting up skill system for player " + index);
            if (boulder != null)
            {
                NetworkObject boulderNetObj = boulder.GetComponent<NetworkObject>();
                if (boulderNetObj != null)
                {
                    Debug.Log("Setting new boulder object reference for player " + index);
                    boulderRef.Value = new NetworkObjectReference(boulderNetObj);
                }
            }
        }
    }

    private void OnEnable()
    {
        EnableInputs();
        if (IsOwner)
        {
        }
    }

    public override void OnDestroy()
    {
        DisableInputs();
        if (IsOwner)
        {
            currentSize.OnValueChanged -= OnSizeChanged;
        }
    }

    private void EnableInputs()
    {
        if (shrinkAction != null)
        {
            shrinkAction.action.Enable();
            shrinkAction.action.started += OnShrinkPerformed;
        }

        if (growAction != null)
        {
            growAction.action.Enable();
            growAction.action.started += OnGrowPerformed;
        }
    }

    private void DisableInputs()
    {
        if (shrinkAction != null)
        {
            shrinkAction.action.Disable();
            shrinkAction.action.started -= OnShrinkPerformed;
        }

        if (growAction != null)
        {
            growAction.action.Disable();
            growAction.action.started -= OnGrowPerformed;
        }
    }

    private void StartSkillCooldown()
    {
        if (!IsOwner) return;

        SetSkillCooldownServerRpc(Time.time + skillCooldownDuration);
        SetSkillEnabledServerRpc(false);
        PlayerHUD.Instance.HandleUsedSkill(true);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetSkillCooldownServerRpc(float endTime)
    {
        skillCooldownEndTime.Value = endTime;
    }

    private void OnShrinkPerformed(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (!skillEnabled.Value) return;

        StartSkillCooldown();
        if (lastModifiedClientId.Value != OwnerClientId && currentSize.Value == BoulderSize.Large)
        {
            RequestSizeChangeServerRpc(BoulderSize.Medium);
            return;
        }

        if (currentSize.Value == BoulderSize.Medium)
        {
            RequestSizeChangeServerRpc(BoulderSize.Small);
        }
    }

    private void OnGrowPerformed(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (!skillEnabled.Value) return;

        StartSkillCooldown();
        PlaySound("Boulder-E");
        int index = SisyphiGameMultiplayer.Instance.GetPlayerDataIndexFromClientId(OwnerClientId);
        index = (index + 1) % SisyphiGameMultiplayer.PLAYER_COUNT;

        GameObject oppPlayer = GameObject.Find("Player_" + index);
        // BoulderSkillSystem oppSkillSystem = oppBoulder.GetComponent<BoulderSkillSystem>();
        if (oppPlayer != null && oppPlayer.TryGetComponent<NetworkObject>(out NetworkObject netObj))
        {
            Debug.Log($"Player {OwnerClientId} performed GROW against player {index}");
            RequestGrowOtherBoulderServerRpc(netObj.NetworkObjectId, OwnerClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSizeChangeServerRpc(BoulderSize size, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log($"[BoulderSkillSystem] RequestSizeChangeServerRpc called, size: {size}, sender: {serverRpcParams.Receive.SenderClientId}");
        currentSize.Value = size;
        lastModifiedClientId.Value = serverRpcParams.Receive.SenderClientId;

        if (boulderRef.Value.TryGet(out NetworkObject boulderNetObj))
        {
            Debug.Log($"[BoulderSkillSystem] Found boulder: {boulderNetObj.name}");
            BoulderController boulderController = boulderNetObj.GetComponent<BoulderController>();
            if (boulderController != null)
            {
                Debug.Log($"[BoulderSkillSystem] Calling SetBoulderProperties on {boulderNetObj.name}");
                boulderController.SetBoulderProperties(size);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestGrowOtherBoulderServerRpc(ulong targetPlayerNetId, ulong senderId)
    {
        Debug.Log($"Inside RequestGrowOtherBoulderServerRpc for player " + senderId);
        NetworkObject targetPlayer = NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetPlayerNetId];
        if (targetPlayer != null)
        {
            Debug.Log($"Target player acquired.");
            BoulderSkillSystem targetSkillSystem = targetPlayer.GetComponent<BoulderSkillSystem>();
            if (targetSkillSystem != null)
            {
                Debug.Log("Target Skill System acquired.");
                if (targetSkillSystem.currentSize.Value == BoulderSize.Small)
                {
                    Debug.Log($"Player {senderId} making Player {targetPlayer.OwnerClientId}'s small boulder medium");
                    targetSkillSystem.RequestSizeChangeServerRpc(BoulderSize.Medium);
                }
                else if (targetSkillSystem.currentSize.Value == BoulderSize.Medium)
                {
                    Debug.Log($"Player {senderId} making Player {targetPlayer.OwnerClientId}'s medium boulder large");
                    targetSkillSystem.RequestSizeChangeServerRpc(BoulderSize.Large);
                }
                targetSkillSystem.lastModifiedClientId.Value = senderId;
            }
        }
    }

    public BoulderSize GetCurrentSize()
    {
        return currentSize.Value;
    }

    private void PlaySound(string name)
    {
        if (IsOwner) SoundManager.Instance.PlayOneShot(name);
    }
}
