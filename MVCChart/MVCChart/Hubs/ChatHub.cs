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

        /// <summary>
        /// 用户登录注册信息
        /// </summary>
        /// <param name="id"></param>
        public void Register(string uid, string nickName)
        {
            var UserInfo = UserList.Where(p => p.UserId == uid).FirstOrDefault();
            if (UserInfo != null)
            {
                UserInfo.UserNickName = nickName;
                UserInfo.ConnectionId = Context.ConnectionId;
                UserInfo.UserStates = "Yes";
            }
            else
            {
                OnlineUserInfo newUser = new OnlineUserInfo() { UserId = uid, ConnectionId = Context.ConnectionId, UserNickName = nickName, UserStates = "Yes" };
                UserList.Add(newUser);
                //通知用户上线
                Clients.Others.addNewMessageToPage("系统消息",nickName + " 上线了!");
            }

            //刷新用户列表
            Clients.All.CurUserList(Common.Common.JsonConverter.Serialize(UserList));
        }

        /// <summary>
        /// 用户登录注册信息(不可用)
        /// </summary>
        /// <param name="id"></param>
        public void ChkLogout()
        {
            string logoutUsers = string.Join(",", UserList.Where(x => x.UserStates == "No").Select(x=>x.UserNickName));
            UserList.RemoveAll(x => x.UserStates == "No");

            if (!string.IsNullOrEmpty(logoutUsers))
            {
                Clients.All.addNewMessageToPage("系统消息", logoutUsers + "下线了");
                //刷新用户列表
                Clients.All.CurUserList(Common.Common.JsonConverter.Serialize(UserList));
            }
        }

        /// <summary>
        /// 全体群发
        /// </summary>
        /// <param name="name"></param>
        /// <param name="message"></param>
        public void SendToAll(string name, string message)
        {
            Clients.All.addNewMessageToPage(name, message);
        }

        /// <summary>
        /// 注册群组 注册用户信息
        /// </summary>
        /// <param name="groupid">群组ID</param>
        /// <param name="usernickname">用户昵称</param>
        /// <param name="userfaceimg">用户头像</param>
        /// <param name="userid">用户在网站中的唯一标识ID</param>
        public void Regest(string groupid, string usernickname, string userfaceimg, string userid)
        {
            //添加用户到群组 Groups.Add（用户连接ID，群组）
            Groups.Add(Context.ConnectionId, groupid);

            //如果说是一个简单的聊天室 下面这段代码是没有什么作用的 因为Context.ConnectionId是唯一的用户于服务器之间的连接
            //这里我传递进来了 用户的昵称和头像 还有网站中用户的ID 所以我要把用户的信息添加到我们上面建立的那个列表类中

            //如果用户不存在在线列表中
            if (UserList.Where(p => p.UserId == userid).FirstOrDefault() == null)
            {
                //我们在列表中 添加这个用户 并且标记用户在线 UserStates = "True"
                UserList.Add(new OnlineUserInfo() { UserId = userid, ConnectionId = Context.ConnectionId, UserNickName = usernickname, UserFaceImg = userfaceimg, UserStates = "True" });
            }
            //如果用户已经存在于在线列表中
            else
            {
                //我们更新用户列表中用户的信息 （这里更新的信息主要是用户的连接ID  ConnectionId = Context.ConnectionId）
                var UserInfo = UserList.Where(p => p.UserId == userid).FirstOrDefault();
                UserList.Remove(UserInfo);
                UserList.Add(new OnlineUserInfo() { UserId = userid, ConnectionId = Context.ConnectionId, UserNickName = usernickname, UserFaceImg = userfaceimg, UserStates = "True" });
            }

            //这个方法是调用客户端LoginUser方法 并且传递当前用户列表 客户端会刷新当前用户列表 调用的是全部的已连接的用户 Clients.All
            Clients.All.LoginUser(Common.Common.JsonConverter.Serialize(UserList));
            //这个方法是调用客户端的 addNewMessageToPage方法 目的是实现 当一个用户上线是 提醒所有的用户 某个用户上线了 提醒的是所有的已连接用户 所以也是Clients.All
            Clients.All.addNewMessageToPage("系统消息：" + DateTime.Now.ToString("HH:mm:ss") + "&nbsp;" + usernickname + "&nbsp;上线了");
        }


        /// <summary>
        /// 发送消息 自定义判断是发送给全部用户还是某一个组（类似于群聊啦）
        /// </summary>
        /// <param name="groupid">接收的组</param>
        /// <param name="userfaceimg">发送用户的头像</param>
        /// <param name="usernickname">发送用户的昵称</param>
        /// <param name="message">发送的消息</param>
        public void Send(string groupid, string userfaceimg, string usernickname, string message)
        {
            if (groupid == "All")//全部用户（广播）
            {
                //调用所有客户端的addNewMessageToPage方法 推送一条消息
                Clients.All.addNewMessageToPage("<dl class=\"clearfix\"><dt><img src=\"" + userfaceimg + "\" /></dt><dd><i></i><div class=\"J_Users\">" + usernickname + "</div><div class=\"J_Content\">" + message + "</div></dd></dl>");
            }
            else//指定组(组播)
            {
                //调用指定客户端的addNewMessageToPage方法 推送一条消息（所有属于组groupid的已连接用户）
                Clients.Group(groupid).addNewMessageToPage("<dl class=\"clearfix\"><dt><img src=\"" + userfaceimg + "\" /></dt><dd><i></i><div class=\"J_Users\">" + usernickname + "</div><div class=\"J_Content\">" + message + "</div></dd></dl>");
            }
        }

        /// <summary>
        /// 发送给指定用户（单播）
        /// </summary>
        /// <param name="clientId">接收用户的连接ID</param>
        /// <param name="userfaceimg">发送用户的头像</param>
        /// <param name="usernickname">发送用户的昵称</param>
        /// <param name="message">发送的消息</param>
        public void SendSingle(string clientId, string tonick, string mynick, string message)
        {
            //首先我们获取一下接收用户的信息
            var UserInfo = UserList.Where(p => p.ConnectionId == clientId).FirstOrDefault();
            //如果用户不存在或用户的在线状态为False 那么提醒一下 发送用户 对方不在线
            if (UserInfo == null)
            {
                Clients.Client(Context.ConnectionId).addNewMessageToPage("系统消息","当前用户不在线");
            }
            else
            {
                //如果用户存在并且在线呢 就把消息推送给接收的用户 并且加上当前用户信息 以及添加一个onclick事件 让接收的用户 可以直接点击消息的用户 回复 私聊信息 （不然还要在用户列表中找到谁给我发的消息 点击回复 这不科学...）
                Clients.Client(clientId).addNewMessageToPerPage(Context.ConnectionId, mynick, message);
                //这句是发送给发送用户的 总不能我发送个私聊 对方收到了信息 我这里什么都不显示是吧 我也显示我发送的私聊信息 因为发送发就是我自己 所以不加onclick事件了 不允许自己跟自己聊天哦
                Clients.Client(Context.ConnectionId).addNewMessageToPerPage(clientId, "我", message);
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
                UserInfo.UserStates = "No";
            }
            string usernickname = UserInfo.UserNickName;
            UserList.Remove(UserInfo);

            Clients.All.LoginUser(Common.Common.JsonConverter.Serialize(UserList));
            Clients.All.addNewMessageToPage("系统消息", usernickname + "离线了");
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
                UserInfo.UserStates = "Yes";
            }

            Clients.Others.addNewMessageToPage("系统消息", UserInfo.UserNickName + "重连了");
            //刷新用户列表
            Clients.All.CurUserList(Common.Common.JsonConverter.Serialize(UserList));

            return base.OnReconnected();
        }
    }
}