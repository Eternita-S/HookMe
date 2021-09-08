using Dalamud.Game.Internal;
using Dalamud.Game.Internal.Network;
using Dalamud.Plugin;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XivCommon;

namespace HookMe
{
    unsafe class HookMe : IDalamudPlugin
    {
        private DalamudPluginInterface pi;
        bool Active = true;
        private XivCommonBase XivCommon;
        ConcurrentQueue<Action> Tasks;
        int NextMessage = 0;

        public string Name => "HookMe";

        public void Dispose()
        {
            pi.Framework.Network.OnNetworkMessage -= NetMsg;
            pi.Framework.OnUpdateEvent -= Tick;
            pi.CommandManager.RemoveHandler("/hookme");
            pi.Dispose();
        }

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            pi = pluginInterface;
            Tasks = new ConcurrentQueue<Action>();
            pi.Framework.Network.OnNetworkMessage += NetMsg;
            pi.Framework.OnUpdateEvent += Tick;
            pi.CommandManager.AddHandler("/hookme", new Dalamud.Game.Command.CommandInfo(delegate
            {
                Active = !Active;
                pi.Framework.Gui.Toast.ShowQuest("Active: " + Active);
            }));
            XivCommon = new XivCommonBase(pi);
        }

        [HandleProcessCorruptedStateExceptions]
        private void Tick(Framework framework)
        {
            if (!Active) return;
            if (Tasks.TryDequeue(out var act))
            {
                try
                {
                    act.Invoke();
                }
                catch(Exception e)
                {
                    pi.Framework.Gui.Chat.Print(e.Message + "\n" + e.StackTrace);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        private void NetMsg(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
        {
            if (!Active) return;
            if (direction == NetworkMessageDirection.ZoneDown && opCode == 363)
            {
                try
                {
                    var result = *(ushort*)(dataPtr + 12);
                    if (result == 5)
                    {
                        Task.Run(delegate
                        {
                            Thread.Sleep(new Random().Next(500, 1000));
                            Tasks.Enqueue(new Action(delegate
                            {
                                SendMessage("/ac \"Hook\"");
                            }));
                        });
                    }
                }
                catch (Exception e)
                {
                    pi.Framework.Gui.Chat.Print(e.Message + "\n" + e.StackTrace);
                }
            }
        }

        void SendMessage(string str)
        {
            if (Environment.TickCount > NextMessage)
            {
                pi.Framework.Gui.Chat.Print("Sending: " + str);
                XivCommon.Functions.Chat.SendMessage(str);
                NextMessage = Environment.TickCount + 500;
            }
        }
    }
}
