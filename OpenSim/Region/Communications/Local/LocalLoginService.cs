using System;
using libsecondlife;
using OpenSim.Framework.Communications;
using OpenSim.Framework.Data;
using OpenSim.Framework.Types;
using OpenSim.Framework.UserManagement;
using OpenSim.Framework.Utilities;

namespace OpenSim.Region.Communications.Local
{
    public class LocalLoginService : LoginService
    {
        private CommunicationsLocal m_Parent;

        private NetworkServersInfo serversInfo;
        private uint defaultHomeX;
        private uint defaultHomeY;
        private bool authUsers = false;

        public LocalLoginService(UserManagerBase userManager, string welcomeMess, CommunicationsLocal parent, NetworkServersInfo serversInfo, bool authenticate)
            : base(userManager, welcomeMess)
        {
            m_Parent = parent;
            this.serversInfo = serversInfo;
            defaultHomeX = this.serversInfo.DefaultHomeLocX;
            defaultHomeY = this.serversInfo.DefaultHomeLocY;
            this.authUsers = authenticate;
        }


        public override UserProfileData GetTheUser(string firstname, string lastname)
        {
            UserProfileData profile = this.m_userManager.getUserProfile(firstname, lastname);
            if (profile != null)
            {

                return profile;
            }

            if (!authUsers)
            {
                //no current user account so make one
                Console.WriteLine("No User account found so creating a new one ");
                this.m_userManager.AddUserProfile(firstname, lastname, "test", defaultHomeX, defaultHomeY);

                profile = this.m_userManager.getUserProfile(firstname, lastname);

                return profile;
            }
            return null;
        }

        public override bool AuthenticateUser(UserProfileData profile, string password)
        {
            if (!authUsers)
            {
                //for now we will accept any password in sandbox mode
                Console.WriteLine("authorising user");
                return true;
            }
            else
            {
                Console.WriteLine("Authenticating " + profile.username + " " + profile.surname);

                password = password.Remove(0, 3); //remove $1$

                string s = Util.Md5Hash(password + ":" + profile.passwordSalt);

                return profile.passwordHash.Equals(s.ToString(), StringComparison.InvariantCultureIgnoreCase);
            }
        }

        public override void CustomiseResponse(LoginResponse response, UserProfileData theUser)
        {
            ulong currentRegion = theUser.currentAgent.currentHandle;
            RegionInfo reg = m_Parent.GridServer.RequestNeighbourInfo(currentRegion);

            if (reg != null)
            {
                response.Home = "{'region_handle':[r" + (reg.RegionLocX * 256).ToString() + ",r" + (reg.RegionLocY * 256).ToString() + "], " +
                 "'position':[r" + theUser.homeLocation.X.ToString() + ",r" + theUser.homeLocation.Y.ToString() + ",r" + theUser.homeLocation.Z.ToString() + "], " +
                 "'look_at':[r" + theUser.homeLocation.X.ToString() + ",r" + theUser.homeLocation.Y.ToString() + ",r" + theUser.homeLocation.Z.ToString() + "]}";
                string capsPath = Util.GetRandomCapsPath();
                response.SimAddress = reg.ExternalEndPoint.Address.ToString();
                response.SimPort = (Int32)reg.ExternalEndPoint.Port;
                response.RegionX = reg.RegionLocX;
                response.RegionY = reg.RegionLocY;

                //following port needs changing as we don't want a http listener for every region (or do we?)
                response.SeedCapability = "http://" + reg.ExternalHostName + ":" + this.serversInfo.HttpListenerPort.ToString() + "/CAPS/" + capsPath + "0000/";
                theUser.currentAgent.currentRegion = reg.SimUUID;
                theUser.currentAgent.currentHandle = reg.RegionHandle;

                Login _login = new Login();
                //copy data to login object
                _login.First = response.Firstname;
                _login.Last = response.Lastname;
                _login.Agent = response.AgentID;
                _login.Session = response.SessionID;
                _login.SecureSession = response.SecureSessionID;
                _login.CircuitCode = (uint)response.CircuitCode;
                _login.CapsPath = capsPath;

                m_Parent.InformRegionOfLogin(currentRegion, _login);
            }
            else
            {
                Console.WriteLine("not found region " + currentRegion);
            }

        }
    }
}
