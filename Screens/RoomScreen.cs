using ComputerPlusPlus.Tools;
using GorillaNetworking;
using HarmonyLib;
using Photon.Pun;
using System;

namespace ComputerPlusPlus.Screens
{
    public class RoomScreen : IScreen
    {
        public string Title => "Room";

        public string Description =>
            "Press [Option 1] to exit the current room.\n" +
            "Press [Enter] to join a room code.";

        public string Template =
            "    Current room code: {0}\n" +
            "\n" +
            "    Join Code: {2}\n" +
            "\n" +
            "    Players: {1}\n";

        public string GetContent()
        {
            try
            {
                string code = PhotonNetwork.CurrentRoom?.Name ?? "N/A";
                string players = (PhotonNetwork.CurrentRoom != null) ? PhotonNetwork.CurrentRoom.PlayerCount.ToString() : "0";
                string roomToJoin = GorillaComputer.instance?.roomToJoin ?? "";

                return string.Format(Template, code, players, roomToJoin);
            }
            catch (Exception e)
            {
                Logging.Exception(e);
                return "Error loading room info.";
            }
        }


        public void OnKeyPressed(GorillaKeyboardButton button)
        {
            try
            {
                if (!button.IsFunctionKey() && GorillaComputer.instance?.roomToJoin != null &&
                    GorillaComputer.instance.roomToJoin.Length < 10)
                {
                    GorillaComputer.instance.roomToJoin += button.characterString;
                }

                switch (button.characterString)
                {
                    case "delete":
                        if (!string.IsNullOrEmpty(GorillaComputer.instance?.roomToJoin))
                        {
                            string code = GorillaComputer.instance.roomToJoin;
                            GorillaComputer.instance.roomToJoin =
                                code.Substring(0, Math.Max(0, code.Length - 1));
                        }
                        break;

                    case "option1":
                        NetworkSystem.Instance?.ReturnToSinglePlayer();
                        break;

                    case "enter":
                        if (GorillaComputer.instance != null)
                        {
                            Traverse.Create(GorillaComputer.instance)
                                .Method("ProcessRoomState", button)?.GetValue();
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }

        public void Start() { }
    }
}
