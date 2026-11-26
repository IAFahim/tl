using System;
using System.Linq;
using Tl;

namespace KindPure
{
    public static unsafe class PureControlHost
    {
        sealed class Players : IPureRequests
        {
            public PlaybackTable Table = null!;

            public void Request(int row, RequestOp op, ushort timeline, ushort at)
            {
                switch (op)
                {
                    case RequestOp.Start: Table.Start(row, timeline, at); break;
                    case RequestOp.Stop: Table.Stop(row, timeline); break;
                    case RequestOp.StopAll: Table.StopAll(row); break;
                    case RequestOp.Pause: Table.Pause(row, timeline); break;
                    case RequestOp.Resume: Table.Resume(row, timeline); break;
                    case RequestOp.Seek: Table.Seek(row, timeline, at); break;
                    case RequestOp.Reverse: Table.Reverse(row, timeline, at != 0); break;
                    case RequestOp.Speed: Table.SetSpeed(row, timeline, at); break;
                    default: throw new ArgumentOutOfRangeException(nameof(op), op, "the control script asked for an op this host does not send");
                }
            }
        }

        public static ulong Digest(int control, byte[] first, byte[] second)
        {
            var script = (PureControl)control;
            var rows = PureControls.Rows;
            var kindNames = typeof(WalkTrack).Assembly.GetTypes().Where(t => t.Namespace == "KindPure" && t.IsDefined(typeof(TimelineKindAttribute), false)).Select(static t => t.FullName!).OrderBy(static n => n, StringComparer.Ordinal).ToArray();
            using var world = new TimelineWorld(first);
            using var table = new PlaybackTable(rows);
            var reach = new ulong[rows];
            Array.Fill(reach, (1ul << kindNames.Length) - 1);
            using var store = new KindStore(world, table, kindNames, reach);
            var players = new Players { Table = table };
            var motion = new Motion[rows];
            var stat = new WalkStat[rows];
            var power = new JumpPower[rows];
            var stamina = new Stamina[rows];
            var health = new Health[rows];
            for (var e = 0; e < rows; e++)
            {
                stat[e].Value = 1f;
                power[e].Value = 1f;
                health[e].Value = 1_000_000;
            }
            var rng = PureControls.Seed;
            var onSecond = false;
            var digest = TlStateHash.Begin();
            for (var step = 0; step < PureControls.StepsOf(script); step++)
            {
                PureControls.Requests(script, step, ref rng, players);
                if (PureControls.SecondLibrary(script, step) != onSecond)
                {
                    onSecond = !onSecond;
                    world.Reload(onSecond ? second : first);
                }
                var forward = PureControls.Forward(script, step);
                world.Tick(table, forward);
                new Movement { Motion = motion, WalkStat = stat, JumpPower = power, Stamina = stamina }.Run(world, table, forward);
                new Vitals { Health = health }.Run(world, table, forward);
                var h = TlStateHash.Begin();
                for (var e = 0; e < rows; e++)
                {
                    h.Float(motion[e].X);
                    h.Float(motion[e].Y);
                    h.Int(stamina[e].Value);
                    h.Int(health[e].Value);
                    h.Slots(table.View.RunOf(e), table.View.CountOf(e));
                }
                digest.Lane(h.Value);
            }
            return digest.Value;
        }
    }
}
