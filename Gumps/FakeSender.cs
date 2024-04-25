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

        var layoutWriter = new SpanWriter(_layoutBuffer);

        if (!gump.Draggable)
        {
            layoutWriter.Write("{ nomove }"u8);
        }

        if (!gump.Closable)
        {
            layoutWriter.Write("{ noclose }"u8);
        }

        if (!gump.Disposable)
        {
            layoutWriter.Write("{ nodispose }"u8);
        }

        if (!gump.Resizable)
        {
            layoutWriter.Write("{ noresize }"u8);
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

        var worstLayoutLength = Zlib.MaxPackSize(layoutWriter.BytesWritten);
        var worstStringsLength = Zlib.MaxPackSize(stringsWriter.BytesWritten);
        var maxLength = 40 + worstLayoutLength + worstStringsLength;

        var writer = new SpanWriter(maxLength);
        writer.Write((byte)0xDD); // Packet ID
        writer.Seek(2, SeekOrigin.Current);

        writer.Write(gump.Serial);
        writer.Write(gump.TypeID);
        writer.Write(gump.X);
        writer.Write(gump.Y);

        layoutWriter.Write((byte)0); // Layout text terminator
        OutgoingGumpPackets.WritePacked(layoutWriter.Span, ref writer);

        writer.Write(_stringsList.Count);
        OutgoingGumpPackets.WritePacked(stringsWriter.Span, ref writer);

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