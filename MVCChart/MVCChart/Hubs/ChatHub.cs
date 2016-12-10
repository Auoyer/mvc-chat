using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using MVCChart.Models;
using System.Web.Script.Serialization;
using System.Threading.Tasks;

namespace MVCChart.Hubs
{
    public class ChatHub : Hub
    {
        static List<OnlineUserInfo> UserList = new List<OnlineUserInfo>();
        static List<Message> MsgList = new List<Message>();
        static List<Group> GroupList = new List<Group>();

        /// <summary>
        /// 用户登录注册信息
        /// </summary>
        /// <param name="id"></param>
        public void Register(string uid, string nickName)
        {
            var UserInfo = UserList.Where(p => p.UserId == uid).FirstOrDefault();
            if (UserInfo != null)
            {
                // 用户已存在则直接刷新该用户信息
                UserInfo.UserNickName = nickName;
                UserInfo.ConnectionId = Context.ConnectionId;
                UserInfo.UserStates = "Yes";

                Clients.Others.addNewMessageToPage("系统消息", nickName + "重连了");
            }
            else
            {
                // 用户不存在，则添加新用户
                OnlineUserInfo newUser = new OnlineUserInfo() { UserId = uid, ConnectionId = Context.ConnectionId, UserNickName = nickName, UserStates = "Yes" };
                UserList.Add(newUser);
                //通知用户上线
                Clients.Others.addNewMessageToPage("系统消息",nickName + " 上线了!");
            }

            // 刷新用户列表
            Clients.All.CurUserList(Common.Common.JsonConverter.Serialize(UserList));
        }

        /// <summary>
        /// 用户刷新页面后，显示历史消息
        /// </summary>
        /// <param name="id"></param>
        public void Loadhistory()
        {
            foreach (var msg in MsgList)
            {
                if (string.IsNullOrEmpty(msg.ObjId))
                {
                    Clients.Client(Context.ConnectionId).addNewMessageToPage(msg.SendNick, msg.Content, msg.StrTime);
                }
                else
                {
                    var ReceiveUserInfo = UserList.Where(p => p.UserId == msg.ReceiveId).FirstOrDefault();
                    var ObjUserInfo = UserList.Where(p => p.UserId == msg.ObjId).FirstOrDefault();

                    // 消息接收者的Id和当前Id一致，则发送该消息
                    if (ReceiveUserInfo != null || ObjUserInfo != null || ReceiveUserInfo.ConnectionId == Context.ConnectionId)
                    {
                        //如果用户存在并且在线呢 就把消息推送给接收的用户，并且加上对方连接号，发送方昵称，消息，对方昵称
                        Clients.Client(Context.ConnectionId).addNewMessageToPerPage(ObjUserInfo.ConnectionId, msg.SendNick, msg.Content, ObjUserInfo.UserNickName, msg.StrTime);
                    }
                }
            }
        }

        /// <summary>
        /// 全体群发
        /// </summary>
        /// <param name="name"></param>
        /// <param name="message"></param>
        public void SendToAll(string name, string message)
        {
            string curTime = GetTime();
            MsgList.Add(new Message() { SendNick = name, Content = message, StrTime = curTime });
            Clients.All.addNewMessageToPage(name, message, curTime);
        }

        /// <summary>
        /// 发送给指定用户（单播）
        /// </summary>
        /// <param name="clientId">接收用户的连接ID</param>
        /// <param name="userfaceimg">接收用户的昵称</param>
        /// <param name="usernickname">发送用户的昵称</param>
        /// <param name="message">发送的消息</param>
        public void SendSingle(string clientId, string tonick, string mynick, string message)
        {
            // 获取一下接收用户的信息
            var ReceiveUserInfo = UserList.Where(p => p.ConnectionId == clientId).FirstOrDefault();
            //首先我们获取一下接收用户的信息
            var SendUserInfo = UserList.Where(p => p.ConnectionId == Context.ConnectionId).FirstOrDefault();
            //如果用户不存在或用户的在线状态为False 那么提醒一下 发送用户 对方不在线
            if (ReceiveUserInfo == null || SendUserInfo == null)
            {
                Clients.Client(Context.ConnectionId).addNewMessageToPage("系统消息","当前用户不在线");
            }
            else
            {
                string curTime = GetTime();
                MsgList.Add(new Message() { ReceiveId = ReceiveUserInfo.UserId, SendNick = SendUserInfo.UserNickName, Content = message, ObjId = SendUserInfo.UserId, ObjNick = SendUserInfo.UserNickName, StrTime = curTime });
                MsgList.Add(new Message() { ReceiveId = SendUserInfo.UserId, SendNick = SendUserInfo.UserNickName, Content = message, ObjId = ReceiveUserInfo.UserId, ObjNick = ReceiveUserInfo.UserNickName, StrTime = curTime });

                //如果用户存在并且在线呢 就把消息推送给接收的用户，并且加上对方连接号，发送方昵称，消息，对方昵称
                Clients.Client(clientId).addNewMessageToPerPage(Context.ConnectionId, mynick, message, mynick, curTime);
                //这句是发送给发送用户的 总不能我发送个私聊 对方收到了信息 我这里什么都不显示是吧 我也显示我发送的私聊信息
                Clients.Client(Context.ConnectionId).addNewMessageToPerPage(clientId, mynick, message, tonick, curTime);
            }
        }

        /// <summary>
        /// 使用者离线
        /// </summary>
        /// <param name="stopCalled"></param>
        /// <returns></returns>
        public override Task OnDisconnected(bool stopCalled)
        {
            var UserInfo = UserList.Where(p => p.ConnectionId == Context.ConnectionId).FirstOrDefault();
            if (UserInfo != null)
            {
                string usernickname = UserInfo.UserNickName;
                UserList.Remove(UserInfo);

                Clients.All.LoginUser(Common.Common.JsonConverter.Serialize(UserList));
                Clients.All.addNewMessageToPage("系统消息", usernickname + "离线了");
            }

            return base.OnDisconnected(true);
        }

        /// <summary>
        /// 使用者重新连接
        /// </summary>
        /// <returns></returns>
        public override Task OnReconnected()
        {
            var UserInfo = UserList.Where(p => p.ConnectionId == Context.ConnectionId).FirstOrDefault();
            if (UserInfo != null)
            {
                Clients.Others.addNewMessageToPage("系统消息", UserInfo.UserNickName + "重连了");
                //刷新用户列表
                Clients.All.CurUserList(Common.Common.JsonConverter.Serialize(UserList));
            }

            return base.OnReconnected();
        }

        public string GetTime()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}