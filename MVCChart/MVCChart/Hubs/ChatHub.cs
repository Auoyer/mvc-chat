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

                Clients.Others.addAllMessageToPage("","系统消息", nickName + "重连了", GetTime());
            }
            else
            {
                // 用户不存在，则添加新用户
                OnlineUserInfo newUser = new OnlineUserInfo() { UserId = uid, ConnectionId = Context.ConnectionId, UserNickName = nickName };
                UserList.Add(newUser);
                UserInfo = newUser;
                //通知用户上线 
                Clients.Others.addAllMessageToPage("","系统消息", nickName + "上线了!", GetTime());
            }

            // 更新组成员
            for (int i = 0; i < GroupList.Count; i++)
            {
                var arrItems = GroupList[i].GropuItems.Split(',').ToList();

                // 更新connectionId
                if (arrItems.Contains(UserInfo.UserId))
                {
                    Groups.Add(UserInfo.ConnectionId, GroupList[i].GroupId);
                }

                // 清除空组
                if (UserList.FindAll(x => arrItems.Contains(x.UserId)).Count < 1)
                {
                    GroupList.RemoveAt(i);
                    i--;
                }
                
            }

            // 获取用户所在组
            Clients.Client(Context.ConnectionId).CurUserGroup(Common.Common.JsonConverter.Serialize(GroupList.FindAll(x => x.GropuItems.Split(',').Contains(UserInfo.UserId)).ToList()));
            // 刷新用户列表
            Clients.All.CurUserList(Common.Common.JsonConverter.Serialize(UserList));

            Loadhistory();
        }

        /// <summary>
        /// 设置组信息
        /// </summary>
        /// <param name="id"></param>
        public void SetGroup(string gid, string items,string gname)
        {
            var UserInfo = UserList.Where(p => p.ConnectionId == Context.ConnectionId).FirstOrDefault();
            var GroupInfo = GroupList.Where(p => p.GroupId == gid).FirstOrDefault();

            string groupId = Guid.NewGuid().ToString(); // 创建唯一组号
            var arrItems = items.Split(',');
            List<OnlineUserInfo> GItems = UserList.Where(p => arrItems.Contains(p.UserId)).ToList(); // 新的组员信息
            string groupName = string.IsNullOrWhiteSpace(gname) ? string.Join(",", GItems.Select(x => x.UserNickName)) : gname; // 新的组名

            if (GroupInfo != null)
            {
                var oldItems = GroupInfo.GropuItems.Split(',');
                var delItems = oldItems.Except(arrItems);
                var addItems = arrItems.Except(oldItems);
                
                // 组装hub组信息
                foreach (var conn in UserList.Where(p => delItems.Contains(p.UserId)).Select(x => x.ConnectionId).ToArray())
                {
                    Groups.Remove(conn, gid);
                }
                // 组装hub组信息
                foreach (var conn in UserList.Where(p => arrItems.Contains(p.UserId)).Select(x => x.ConnectionId).ToArray())
                {
                    Groups.Add(conn, gid);
                }

                GroupInfo.GropuItems = items;
                GroupInfo.GroupName = groupName;
                // 刷新组列表
                Clients.Group(gid).UpdateUserGroup(Common.Common.JsonConverter.Serialize(GroupInfo));
                // 把消息推送组内（对话框标识，对话框名称，发送者标识，发送者昵称，消息，发送时间）
                Clients.Group(gid).addGroupMessageToPage(GroupInfo.GroupId, GroupInfo.GroupName, "", "系统提示", UserInfo.UserNickName + "修改了组信息。", GetTime());

            }
            else
            {
                // 组装hub组信息
                foreach (var conn in GItems.Select(x => x.ConnectionId).ToArray())
                {
                    Groups.Add(conn, groupId); 
                }
                
                // 组不存在，则添加新组
                Group newGroup = new Group() { GroupId = groupId, GroupName = groupName, GropuItems = items};
                GroupList.Add(newGroup);
                // 刷新组列表
                Clients.Group(groupId).UpdateUserGroup(Common.Common.JsonConverter.Serialize(newGroup));
            }

            
        }

        /// <summary>
        /// 获取组成员
        /// </summary>
        /// <param name="gid"></param>
        public void getCurGroupItems(string gid)
        {
            var GroupInfo = GroupList.Where(p => p.GroupId == gid).FirstOrDefault();
            Clients.Client(Context.ConnectionId).setCurGroupItems(Common.Common.JsonConverter.Serialize(UserList), GroupInfo==null?"":GroupInfo.GropuItems);
        }

        /// <summary>
        /// 用户改名
        /// </summary>
        /// <param name="id"></param>
        public void ResetName(string nickName)
        {
            var UserInfo = UserList.Where(p => p.ConnectionId == Context.ConnectionId).FirstOrDefault();
            if (UserInfo != null)
            {
                // 用户已存在则直接刷新该用户信息
                Clients.Others.addAllMessageToPage("","系统消息", UserInfo.UserNickName + "改名为" + nickName, GetTime());
                UserInfo.UserNickName = nickName;
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
            var CurUserInfo = UserList.Where(p => p.ConnectionId == Context.ConnectionId).FirstOrDefault();
            // 遍历公共消息及私信
            foreach (var msg in MsgList)
            {
                if (string.IsNullOrEmpty(msg.ReceiveId))
                {
                    Clients.Client(Context.ConnectionId).addAllMessageToPage(msg.SendId, msg.SendNick, msg.Content, msg.StrTime);
                }
                else
                {
                    // 消息接收者的Id和当前Id一致，则发送该消息
                    if (msg.ReceiveId == CurUserInfo.UserId)
                    {
                        // 把消息推送当前用户（对话框标识，对话框名称，发送者标识，发送者昵称，消息，发送时间）
                        Clients.Client(Context.ConnectionId).addPerMessageToPage(msg.ChatId, msg.ChatName, msg.SendId, msg.SendNick, msg.Content, msg.StrTime);
                    }
                }
            }

            // 遍历组消息
            foreach (var g in GroupList)
            {
                if (g.GropuItems.Split(',').ToList().Contains(CurUserInfo.UserId))
                {
                    foreach (var msg in g.GroupMsgs)
                    {
                        // 把消息推送当前用户（对话框标识，对话框名称，发送者标识，发送者昵称，消息，发送时间）
                        Clients.Client(Context.ConnectionId).addGroupMessageToPage(msg.ChatId, msg.ChatName, msg.SendId, msg.SendNick, msg.Content, msg.StrTime);
                    }
                }
            }
        }

        /// <summary>
        /// 全体群发
        /// </summary>
        /// <param name="name"></param>
        /// <param name="message"></param>
        public void SendToAll(string message)
        {
            // 获取发送用户的信息
            var SendUserInfo = UserList.Where(p => p.ConnectionId == Context.ConnectionId).FirstOrDefault();
            string curTime = GetTime();
            MsgList.Add(new Message() { SendId = SendUserInfo.UserId, SendNick = SendUserInfo.UserNickName, Content = message, StrTime = curTime });
            Clients.All.addAllMessageToPage(SendUserInfo.UserId, SendUserInfo.UserNickName, message, curTime);
        }

        /// <summary>
        /// 发送给指定用户（单播）
        /// </summary>
        /// <param name="chatId">接收用户的连接ID</param>
        /// <param name="userfaceimg">接收用户的昵称</param>
        /// <param name="usernickname">发送用户的昵称</param>
        /// <param name="message">发送的消息</param>
        public void SendSingle(string chatId, string message)
        {
            // 获取接收用户的信息
            var ReceiveUserInfo = UserList.Where(p => p.UserId == chatId).FirstOrDefault();
            // 获取发送用户的信息
            var SendUserInfo = UserList.Where(p => p.ConnectionId == Context.ConnectionId).FirstOrDefault();
            //如果用户不存在或用户的在线状态为False 那么提醒一下 发送用户 对方不在线
            if (ReceiveUserInfo == null || SendUserInfo == null)
            {
                Clients.Client(Context.ConnectionId).addTipToPage("系统消息", "当前用户不在线");
            }
            else
            {
                string curTime = GetTime();
                MsgList.Add(new Message() { ReceiveId = ReceiveUserInfo.UserId, ChatId = SendUserInfo.UserId, ChatName = SendUserInfo.UserNickName, SendId = SendUserInfo.UserId, SendNick = SendUserInfo.UserNickName, Content = message, StrTime = curTime });
                MsgList.Add(new Message() { ReceiveId = SendUserInfo.UserId, ChatId = ReceiveUserInfo.UserId, ChatName = ReceiveUserInfo.UserNickName, SendId = SendUserInfo.UserId, SendNick = SendUserInfo.UserNickName, Content = message, StrTime = curTime });

                // 把消息推送对方（对话框标识，对话框名称，发送者标识，发送者昵称，消息，发送时间）
                Clients.Client(ReceiveUserInfo.ConnectionId).addPerMessageToPage(SendUserInfo.UserId, SendUserInfo.UserNickName, SendUserInfo.UserId, SendUserInfo.UserNickName, message, curTime);
                // 把消息推送自己（对话框标识，对话框名称，发送者标识，发送者昵称，消息，发送时间）
                Clients.Client(Context.ConnectionId).addPerMessageToPage(ReceiveUserInfo.UserId, ReceiveUserInfo.UserNickName, SendUserInfo.UserId, SendUserInfo.UserNickName, message, curTime);
            }
        }

        /// <summary>
        /// 发送给指定组
        /// </summary>
        /// <param name="chatId">接收用户的连接ID</param>
        /// <param name="userfaceimg">接收用户的昵称</param>
        /// <param name="usernickname">发送用户的昵称</param>
        /// <param name="message">发送的消息</param>
        public void SendGroup(string chatId, string message)
        {
            // 获取接收用户的信息
            var GroupInfo = GroupList.Where(p => p.GroupId == chatId).FirstOrDefault();
            // 获取发送用户的信息
            var SendUserInfo = UserList.Where(p => p.ConnectionId == Context.ConnectionId).FirstOrDefault();
            //如果用户不存在或用户的在线状态为False 那么提醒一下 发送用户 对方不在线
            if (GroupInfo == null)
            {
                Clients.Client(Context.ConnectionId).addTipToPage("系统消息", "当前组不存在");
            }
            else
            {
                var arrItems = GroupInfo.GropuItems.Split(',');
                List<OnlineUserInfo> GItems = UserList.Where(p => arrItems.Contains(p.UserId)).ToList();
                string curTime = GetTime();
                GroupInfo.GroupMsgs.Add(new Message() { ReceiveId = GroupInfo.GroupId, ChatId = GroupInfo.GroupId, ChatName = GroupInfo.GroupName, SendId = SendUserInfo.UserId, SendNick = SendUserInfo.UserNickName, Content = message, StrTime = curTime });
                // 把消息推送给组成员（对话框标识，对话框名称，发送者标识，发送者昵称，消息，发送时间）
                Clients.Group(GroupInfo.GroupId).addGroupMessageToPage(GroupInfo.GroupId, GroupInfo.GroupName, SendUserInfo.UserId, SendUserInfo.UserNickName, message, curTime);
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
            string usernickname = UserInfo.UserNickName;

            if (UserInfo != null)
            {
                //// 从组中删除
                //List<Group> delGroups = new List<Group>();
                //for(int i=0;i<GroupList.Count;i++)
                //{
                //    var arrItems = GroupList[i].GropuItems.Split(',').ToList();

                //    if (arrItems.Contains(UserInfo.UserId))
                //    {
                //        arrItems.Remove(UserInfo.UserId);
                //        string curItems = string.Join(",", arrItems);
                //        GroupList[i].GropuItems = curItems;
                //        Groups.Remove(UserInfo.ConnectionId, GroupList[i].GroupId);
                //    }

                //    // 删除组员不足一人的组
                //    if (arrItems.Count < 1)
                //    {
                //        GroupList.RemoveAt(i);
                //        i--;
                //    }
                //}

                // 从用户列表中删除
                UserList.Remove(UserInfo);

                Clients.All.addAllMessageToPage("","系统消息", usernickname + "离线了", GetTime());
                //刷新用户列表
                Clients.All.CurUserList(Common.Common.JsonConverter.Serialize(UserList));
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
                Clients.Others.addAllMessageToPage("","系统消息", UserInfo.UserNickName + "重连了", GetTime());
                //刷新用户列表
                Clients.All.CurUserList(Common.Common.JsonConverter.Serialize(UserList));
            }

            return base.OnReconnected();
        }

        /// <summary>
        /// 获取当前时间
        /// </summary>
        /// <returns></returns>
        public string GetTime()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}