using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FusionContinent
{
    public class ChannelManager : MonoBehaviour
    {
        public static ChannelManager Instance
        {
            get
            {
                return instance;
            }
        }
        private static ChannelManager instance;
        public const string ChannelFileName = "Channel";
        public string CurrentChannel = ""; 
        List<string> ChannelList = new List<string>();
        private void Awake()
        {
            if (instance!= null)
            {
                Destroy(instance);
                instance =  null;
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            CurrentChannel = "";
            ChannelList.Clear();
            var channelContent = ConfigAotManager.Instance.ConfigData.Channel;
            CurrentChannel = channelContent;
            Debug.Log("Channel Name: " + CurrentChannel);
            ParseChannel(channelContent);
        }

        public string FirstChannel()
        {
            return ChannelList.First();
        }

        public void Initialized()
        {
            
        }

        /// <summary>
        /// 判断是不是当前渠道
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public bool IsChannelExist(string channel)
        {
            return ChannelList.Contains(channel);
        }
        
        void ParseChannel(string channel)
        {
            ChannelList = channel.Split(",").ToList();
        }
    }
}