using RDPCOMAPILib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceLibrary
{
    class ScreenShare
    {
        private bool first = true;
        private int sessionID = 0;
        private RDPSession x = new RDPSession();
        private string rdsKeyLocation = @"\\BSSFILES2\Dept\adm\lr-temp\rds-key.txt";

        private static ScreenShare screenShare = null;

        private ScreenShare()
        {

        }

        public static ScreenShare getInstance()
        {
            if (screenShare == null)
                screenShare = new ScreenShare();
            return screenShare;
        }

        public void Start(List<LabClient> clients)
        {
            this.sessionID++;
            Service.getInstance().TransferAndRun(clients);

            x.OnAttendeeConnected += Incoming;
            if (first)
                x.Open();
            else x.Resume();

            IRDPSRAPIInvitation Invitation = x.Invitations.CreateInvitation("Trial" + sessionID, "MyGroup" + sessionID, "", 50);
            String Contents = Invitation.ConnectionString.Trim();
            System.IO.StreamWriter file = new System.IO.StreamWriter(rdsKeyLocation);
            file.WriteLine(Contents);
            file.Close();

            first = false;
        }

        public void Stop(List<LabClient> clients)
        {
            x.Pause();

            foreach (LabClient client in clients)
            {
                Service.getInstance().killRemoteProcess(client.ComputerName, "scr-viewer.exe");
            }
        }

        private void Incoming(object Guest)
        {
            IRDPSRAPIAttendee MyGuest = (IRDPSRAPIAttendee)Guest;
            MyGuest.ControlLevel = CTRL_LEVEL.CTRL_LEVEL_VIEW;
        }
    }
}
