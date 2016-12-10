using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVCChart.Models
{
    public class OnlineUserInfo
    {
        //用户ID
        public string UserId { get; set; }
        //用户连接ID
        public string ConnectionId { get; set; }
        //用户昵称
        public string UserNickName { get; set; }
        //用户头像
        public string UserFaceImg { get; set; }
        //用户状态
        public string UserStates { get; set; }
    }

    public class Message
    {
        // 消息接收者ID
        public string ReceiveId { get; set; }

        // 消息发送者昵称
        public string SendNick { get; set; }

        // 聊天对象ID
        public string ObjId { get; set; }
        // 聊天对象昵称
        public string ObjNick { get; set; }
        // 消息
        public string Content { get; set; }
        // 时间
        public DateTime Time { get; set; }
        // 时间
        public string StrTime { get; set; }
    }

    public class Group
    {
        //组ID
        public string GroupId { get; set; }

        //组名
        public string GroupName { get; set; }

        //组员
        public List<OnlineUserInfo> GroupUsers { get; set; }

        //组消息
        public List<Message> GroupMsgs { get; set; }
    }
}