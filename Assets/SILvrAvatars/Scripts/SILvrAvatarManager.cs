using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SILvr.Avatars {
    public class SILvrAvatarManager : MonoBehaviour
    {
        //singleton instance
        private static SILvrAvatarManager _avatarManager;
        public static SILvrAvatarManager Instance
        {
            get
            {
                if (!_avatarManager)
                {
                    _avatarManager = FindObjectOfType(typeof(SILvrAvatarManager)) as SILvrAvatarManager;

                    if (!_avatarManager)
                    {
                        Debug.LogError("There needs to be one active SILvrAvatarManager script on a GameObject in your scene.");
                    }
                }
                return _avatarManager;
            }
        }

        // The ID of the local player
        [HideInInspector] public int localID;

        // Data structures storing avatars and their ids
        private List<SILvrAvatar> avatars = new List<SILvrAvatar>();
        private List<int> ids = new List<int>();
        private Dictionary<int, SILvrAvatar> avatarsByID = new Dictionary<int, SILvrAvatar>();
        private Dictionary<SILvrAvatar, int> idsByAvatar = new Dictionary<SILvrAvatar, int>();

        // Avatar events
        public delegate void AvatarManagerEvent(SILvrAvatar avatar, int id);
        public AvatarManagerEvent OnAvatarAdded;
        public AvatarManagerEvent OnAvatarRemoved;

        // Adds an avatar to the list of avatars tracked by this manager
        public void AddAvatar(SILvrAvatar avatar, int id)
        {
            if (!avatarsByID.ContainsKey(id)) //add only player controlled avatars (first avatar) to dictionary - for replay stuff
            {
                // Add avatar info to relevant data structures
                avatars.Add(avatar);
                ids.Add(id);
                avatarsByID.Add(id, avatar);
                idsByAvatar.Add(avatar, id);

                // Let other scripts know an avatar has been added
                OnAvatarAdded?.Invoke(avatar, id);
            }
        }

        // Removes an avatar from the list of avatars tracked by this manager
        public void RemoveAvatar(SILvrAvatar avatar)
        {
            if (avatarsByID.ContainsValue(avatar)) //remove only the player controlled avatars from the dictionary
            {
                // Get the id for this avatar
                int id = idsByAvatar[avatar];

                // Remove the avatar info from relevant data structures
                avatars.Remove(avatar);
                ids.Remove(id);
                avatarsByID.Remove(id);
                idsByAvatar.Remove(avatar);

                // Let other scripts know an avatar has been removed
                OnAvatarRemoved?.Invoke(avatar, id);
            }
        }

        // Returns a list of all avatars
        public List<SILvrAvatar> GetAllAvatars()
        {
            return avatars;
        }

        // Returns the avatar associated with the given ID
        public SILvrAvatar GetAvatarByID(int id)
        {
            return avatarsByID[id];
        }

        // Returns a list of all player IDs
        public List<int> GetAllIDs()
        {
            return ids;
        }

        // Returns the player ID associated with the given avatar
        public int GetIDByAvatar(SILvrAvatar avatar)
        {
            return idsByAvatar[avatar];
        }
    }
}


