using UnityEngine;

namespace CastleWars.Core
{
    /// <summary>
    /// 网络玩家占位类 - 保持兼容性（纯本地模式）
    /// 注意：推荐使用 PlayerController 作为主要玩家控制器
    /// </summary>
    public class NetworkPlayer : MonoBehaviour
    {
        [Header("玩家信息")]
        public int PlayerId { get; private set; }
        public string PlayerName { get; private set; }

        /// <summary>
        /// 初始化玩家
        /// </summary>
        public void Initialize(int playerId, string playerName = "")
        {
            PlayerId = playerId;
            PlayerName = string.IsNullOrEmpty(playerName) ? $"Player {playerId}" : playerName;

            Debug.Log($"[NetworkPlayer] Player initialized: {PlayerName} (ID: {playerId})");
        }
    }
}
