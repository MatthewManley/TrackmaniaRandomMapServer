using GbxRemoteNet;
using GbxRemoteNet.XmlRpc.ExtraTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackmaniaRandomMapServer
{
    internal class TmMultiCall : MultiCall
    {
        public TmMultiCall()
        {

        }

        /// <summary>
        /// See <see cref="GbxRemoteClient.ChatSendServerMessageAsync"/>
        /// </summary>
        public TmMultiCall ChatSendServerMessageAsync(string message)
        {
            Add("ChatSendServerMessage", message);
            return this;
        }

        /// <summary>
        /// See <see cref="GbxRemoteClient.SendHideManialinkPageAsync"/>
        /// </summary>
        public TmMultiCall SendHideManialinkPageAsync()
        {
            Add("SendHideManialinkPage");
            return this;
        }

        /// <summary>
        /// See <see cref="GbxRemoteClient.SendHideManialinkPageToLoginAsync"/>
        /// </summary>
        public TmMultiCall SendHideManialinkPageToLoginAsync(string playerLogin)
        {
            Add("SendHideManialinkPageToLogin", playerLogin);
            return this;
        }

        /// <summary>
        /// See <see cref="GbxRemoteClient.SendDisplayManialinkPageAsync"/>
        /// </summary>
        public TmMultiCall SendDisplayManialinkPageAsync(string xml, int timeout, bool autohide)
        {
            Add("SendDisplayManialinkPage", xml, timeout, autohide);
            return this;
        }

        /// <summary>
        /// See <see cref="GbxRemoteClient.SendDisplayManialinkPageToLoginAsync"/>
        /// </summary>
        public TmMultiCall SendDisplayManialinkPageToLoginAsync(string playerLogin, string xml, int timeout, bool autohide)
        {
            Add("SendDisplayManialinkPageToLogin", playerLogin, xml, timeout, autohide);
            return this;
        }

        /// <summary>
        /// See <see cref="GbxRemoteClient.TriggerModeScriptEventArrayAsync"/>
        /// </summary>
        public TmMultiCall TriggerModeScriptEventArrayAsync(string method, params string[] parameters)
        {
            Add("TriggerModeScriptEventArray", method, parameters);
            return this;
        }

        /// <summary>
        /// See <see cref="GbxRemoteClient.SetModeScriptSettingsAsync"/>
        /// </summary>
        public TmMultiCall SetModeScriptSettingsAsync(GbxDynamicObject modescriptSettings)
        {
            Add("SetModeScriptSettings", modescriptSettings);
            return this;
        }

        /// <summary>
        /// See <see cref="GbxRemoteClient.InsertMapAsync"/>
        /// </summary>
        public TmMultiCall InsertMapAsync(string filename)
        {
            Add("InsertMap", filename);
            return this;
        }

        /// <summary>
        /// See <see cref="GbxRemoteClient.RemoveMapListAsync"/>
        /// </summary>
        public TmMultiCall RemoveMapListAsync(Array filenames)
        {
            Add("RemoveMapList", filenames);
            return this;
        }

        /// <summary>
        /// See <see cref="GbxRemoteClient.NextMapAsync"/>
        /// </summary>
        public TmMultiCall NextMapAsync()
        {
            Add("NextMap");
            return this;
        }

        /// <summary>
        /// See <see cref="GbxRemoteClient.RestartMapAsync"/>
        /// </summary>
        public TmMultiCall RestartMapAsync()
        {
            Add("RestartMap");
            return this;
        }

        /// <summary>
        /// See <see cref="GbxRemoteClient.ChatSendServerMessageToLoginAsync"/>
        /// </summary>
        public TmMultiCall ChatSendServerMessageToLoginAsync(string message, string playerLogins)
        {
            Add("ChatSendServerMessageToLogin", message, playerLogins);
            return this;
        }
    }
}
