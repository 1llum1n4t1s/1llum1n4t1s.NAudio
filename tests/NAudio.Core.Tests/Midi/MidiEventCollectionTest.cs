using System.Collections.Generic;
using NUnit.Framework;
using NAudio.Midi;

namespace NAudio.Core.Tests.Midi;

[TestFixture]
[Category("UnitTest")]
public class MidiEventCollectionTest
{
    [Test]
    public void TestType1()
    {
        MidiEventCollection collection = new MidiEventCollection(1, 120);
        collection.AddEvent(new TextEvent("Test", MetaEventType.TextEvent, 0), 0);
        collection.AddEvent(new NoteOnEvent(0, 1, 30, 100, 15), 1);
        collection.AddEvent(new NoteOnEvent(15, 1, 30, 100, 15), 1);
        collection.AddEvent(new NoteOnEvent(30, 1, 30, 100, 15), 1);
        collection.AddEvent(new NoteOnEvent(0, 10, 60, 100, 15), 10);
        collection.AddEvent(new NoteOnEvent(15, 10, 60, 100, 15), 10);
        collection.AddEvent(new NoteOnEvent(30, 10, 60, 100, 15), 10);
        Assert.That(collection.Tracks, Is.EqualTo(11));
        collection.PrepareForExport();
        Assert.That(collection.Tracks, Is.EqualTo(3));
        IList<MidiEvent> track0 = collection.GetTrackEvents(0);
        Assert.That(track0.Count, Is.EqualTo(2));
        Assert.That(collection.GetTrackEvents(1).Count, Is.EqualTo(4));
        Assert.That(collection.GetTrackEvents(2).Count, Is.EqualTo(4));
        Assert.That(MidiEvent.IsEndTrack(track0[track0.Count - 1]), Is.True);
    }

    [Test]
    public void TestType0()
    {
        MidiEventCollection collection = new MidiEventCollection(0, 120);
        collection.AddEvent(new TextEvent("Test", MetaEventType.TextEvent, 0), 0);
        collection.AddEvent(new NoteOnEvent(0, 1, 30, 100, 15), 1);
        collection.AddEvent(new NoteOnEvent(15, 1, 30, 100, 15), 1);
        collection.AddEvent(new NoteOnEvent(30, 1, 30, 100, 15), 1);
        collection.AddEvent(new NoteOnEvent(0, 10, 60, 100, 15), 10);
        collection.AddEvent(new NoteOnEvent(15, 10, 60, 100, 15), 10);
        collection.AddEvent(new NoteOnEvent(30, 10, 60, 100, 15), 10);
        Assert.That(collection.Tracks, Is.EqualTo(1));
        collection.PrepareForExport();
        Assert.That(collection.Tracks, Is.EqualTo(1));
        IList<MidiEvent> track0 = collection.GetTrackEvents(0);
        Assert.That(track0.Count, Is.EqualTo(8));
        Assert.That(MidiEvent.IsEndTrack(track0[track0.Count - 1]), Is.True);
    }

    [Test]
    public void TestType1ToType0()
    {
        MidiEventCollection collection = new MidiEventCollection(1, 120);
        collection.AddEvent(new TextEvent("Test", MetaEventType.TextEvent, 0), 0);
        collection.AddEvent(new NoteOnEvent(0, 1, 30, 100, 15), 1);
        collection.AddEvent(new NoteOnEvent(15, 1, 30, 100, 15), 1);
        collection.AddEvent(new NoteOnEvent(30, 1, 30, 100, 15), 1);
        collection.AddEvent(new NoteOnEvent(0, 10, 60, 100, 15), 10);
        collection.AddEvent(new NoteOnEvent(15, 10, 60, 100, 15), 10);
        collection.AddEvent(new NoteOnEvent(30, 10, 60, 100, 15), 10);
        Assert.That(collection.Tracks, Is.EqualTo(11));
        collection.MidiFileType = 0;
        collection.PrepareForExport();
        Assert.That(collection.Tracks, Is.EqualTo(1));
        IList<MidiEvent> track0 = collection.GetTrackEvents(0);
        Assert.That(track0.Count, Is.EqualTo(8));
        Assert.That(MidiEvent.IsEndTrack(track0[track0.Count - 1]), Is.True);
    }

    [Test]
    public void TestType0ToType1()
    {
        MidiEventCollection collection = new MidiEventCollection(0, 120);
        collection.AddEvent(new TextEvent("Test", MetaEventType.TextEvent, 0), 0);
        collection.AddEvent(new NoteOnEvent(0, 1, 30, 100, 15), 1);
        collection.AddEvent(new NoteOnEvent(15, 1, 30, 100, 15), 1);
        collection.AddEvent(new NoteOnEvent(30, 1, 30, 100, 15), 1);
        collection.AddEvent(new NoteOnEvent(0, 10, 60, 100, 15), 10);
        collection.AddEvent(new NoteOnEvent(15, 10, 60, 100, 15), 10);
        collection.AddEvent(new NoteOnEvent(30, 10, 60, 100, 15), 10);
        Assert.That(collection.Tracks, Is.EqualTo(1));
        collection.MidiFileType = 1;
        collection.PrepareForExport();
        Assert.That(collection.Tracks, Is.EqualTo(3), "Wrong number of tracks");
        IList<MidiEvent> track0 = collection.GetTrackEvents(0);
        Assert.That(track0.Count, Is.EqualTo(2));
        Assert.That(collection.GetTrackEvents(1).Count, Is.EqualTo(4));
        Assert.That(collection.GetTrackEvents(2).Count, Is.EqualTo(4));
        Assert.That(MidiEvent.IsEndTrack(track0[track0.Count - 1]), Is.True);
    }

    [Test]
    public void PrepareForExportSortsByTimeAndKeepsSameTimeEventsStable()
    {
        // Pins the stable ordering of the track sort: events added in reverse time order end
        // up ascending, and at equal times meta events (except EndTrack) come before note
        // events while each group keeps its insertion order.
        MidiEventCollection collection = new MidiEventCollection(0, 120);
        var expectedLate = new List<MidiEvent>();
        for (int time = 50; time >= 10; time -= 10)
        {
            var note = new NoteOnEvent(time, 1, 40 + time / 10, 100, 0);
            collection.AddEvent(note, 0);
            expectedLate.Insert(0, note);
        }

        var noteA = new NoteOnEvent(0, 1, 70, 100, 0);
        var textA = new TextEvent("a", MetaEventType.TextEvent, 0);
        var noteB = new NoteOnEvent(0, 1, 71, 100, 0);
        var textB = new TextEvent("b", MetaEventType.TextEvent, 0);
        collection.AddEvent(noteA, 0);
        collection.AddEvent(textA, 0);
        collection.AddEvent(noteB, 0);
        collection.AddEvent(textB, 0);

        collection.PrepareForExport();

        IList<MidiEvent> track = collection.GetTrackEvents(0);
        var expected = new List<MidiEvent> { textA, textB, noteA, noteB };
        expected.AddRange(expectedLate);
        Assert.That(track.Count, Is.EqualTo(expected.Count + 1), "events plus the appended EndTrack");
        for (int n = 0; n < expected.Count; n++)
        {
            Assert.That(track[n], Is.SameAs(expected[n]), $"Event {n}");
        }
        Assert.That(MidiEvent.IsEndTrack(track[track.Count - 1]), Is.True);
    }

    [Test]
    public void SortingAnAlreadySortedTrackDoesNotWriteToIt()
    {
        // The stable sort leaves an already-sorted track untouched, so an enumerator taken
        // before PrepareForExport (or MidiFile.Export) stays valid.
        MidiEventCollection collection = new MidiEventCollection(0, 120);
        collection.AddEvent(new TextEvent("a", MetaEventType.TextEvent, 0), 0);
        for (int time = 0; time < 40; time += 10)
        {
            collection.AddEvent(new NoteOnEvent(time, 1, 60, 100, 0), 0);
        }
        collection.PrepareForExport();

        using var enumerator = collection.GetTrackEvents(0).GetEnumerator();
        Assert.That(enumerator.MoveNext(), Is.True);
        collection.PrepareForExport();
        Assert.That(() => enumerator.MoveNext(), Throws.Nothing);
    }
}
