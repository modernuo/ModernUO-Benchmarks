using System;
using Server.Collections;
using Server.Gumps;
using Server.Network;
using System.Buffers;
using System.IO;
using System.IO.Compression;

namespace Gumps;

internal static class FakeSender
{
    private static readonly byte[] _layoutBuffer = GC.AllocateUninitializedArray<byte>(0x20000);
    private static readonly byte[] _stringsBuffer = GC.AllocateUninitializedArray<byte>(0x20000);
    private static readonly OrderedHashSet<string> _stringsList = new(32);

    public static void SendOldGump(Gump gump, out int switches, out int entries)
    {
        switches = 0;
        entries = 0;

        var packed = true;

        var layoutWriter = new SpanWriter(_layoutBuffer);

        if (!gump.Draggable)
        {
            layoutWriter.Write(Gump.NoMove);
        }

        if (!gump.Closable)
        {
            layoutWriter.Write(Gump.NoClose);
        }

        if (!gump.Disposable)
        {
            layoutWriter.Write(Gump.NoDispose);
        }

        if (!gump.Resizable)
        {
            layoutWriter.Write(Gump.NoResize);
        }

        foreach (var entry in gump.Entries)
        {
            entry.AppendTo(ref layoutWriter, _stringsList, ref entries, ref switches);
        }

        var stringsWriter = new SpanWriter(_stringsBuffer);

        foreach (var str in _stringsList)
        {
            var s = str ?? "";
            stringsWriter.Write((ushort)s.Length);
            stringsWriter.WriteBigUni(s);
        }

        int maxLength;
        if (packed)
        {
            var worstLayoutLength = Zlib.MaxPackSize(layoutWriter.BytesWritten);
            var worstStringsLength = Zlib.MaxPackSize(stringsWriter.BytesWritten);
            maxLength = 40 + worstLayoutLength + worstStringsLength;
        }
        else
        {
            maxLength = 23 + layoutWriter.BytesWritten + stringsWriter.BytesWritten;
        }

        var writer = new SpanWriter(maxLength);
        writer.Write((byte)(packed ? 0xDD : 0xB0)); // Packet ID
        writer.Seek(2, SeekOrigin.Current);

        writer.Write(gump.Serial);
        writer.Write(gump.TypeID);
        writer.Write(gump.X);
        writer.Write(gump.Y);

        if (packed)
        {
            layoutWriter.Write((byte)0); // Layout text terminator
            OutgoingGumpPackets.WritePacked(layoutWriter.Span, ref writer);

            writer.Write(_stringsList.Count);
            OutgoingGumpPackets.WritePacked(stringsWriter.Span, ref writer);
        }
        else
        {
            writer.Write((ushort)layoutWriter.BytesWritten);
            writer.Write(layoutWriter.Span);

            writer.Write((ushort)_stringsList.Count);
            writer.Write(stringsWriter.Span);
        }

        writer.WritePacketLength();

        //ns.Send(writer.Span);

        layoutWriter.Dispose();  // Just in case
        stringsWriter.Dispose(); // Just in case

        if (_stringsList.Count > 0)
        {
            _stringsList.Clear();
        }
    }
}