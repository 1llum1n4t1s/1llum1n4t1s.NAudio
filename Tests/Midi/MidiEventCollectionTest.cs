using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using NAudio.Midi;

namespace NAudioTests.Midi
{
    [TestFixture]
    [Category("UnitTest")]
    public class MidiEventCollectionTest
    {
        [Test]
        public void TestType1()
        {
            MidiEventCollection collection = new MidiEventCollection(1,120);
            collection.AddEvent(new TextEvent("Test",MetaEventType.TextEvent,0),0);
            collection.AddEvent(new NoteOnEvent(0, 1, 30, 100, 15), 1);
            collection.AddEvent(new NoteOnEvent(15, 1, 30, 100, 15), 1);
            collection.AddEvent(new NoteOnEvent(30, 1, 30, 100, 15), 1);
            collection.AddEvent(new NoteOnEvent(0, 10, 60, 100, 15), 10);
            collection.AddEvent(new NoteOnEvent(15, 10, 60, 100, 15), 10);
            collection.AddEvent(new NoteOnEvent(30, 10, 60, 100, 15), 10);
            ClassicAssert.AreEqual(collection.Tracks, 11);
            collection.PrepareForExport();
            ClassicAssert.AreEqual(collection.Tracks, 3);
            IList<MidiEvent> track0 = collection.GetTrackEvents(0);
            ClassicAssert.AreEqual(track0.Count, 2);
            ClassicAssert.AreEqual(collection.GetTrackEvents(1).Count, 4);
            ClassicAssert.AreEqual(collection.GetTrackEvents(2).Count, 4);
            ClassicAssert.IsTrue(MidiEvent.IsEndTrack(track0[track0.Count - 1]));
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
            ClassicAssert.AreEqual(collection.Tracks, 1);
            collection.PrepareForExport();
            ClassicAssert.AreEqual(collection.Tracks, 1);
            IList<MidiEvent> track0 = collection.GetTrackEvents(0);
            ClassicAssert.AreEqual(track0.Count, 8);
            ClassicAssert.IsTrue(MidiEvent.IsEndTrack(track0[track0.Count - 1]));
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
            ClassicAssert.AreEqual(collection.Tracks, 11);
            collection.MidiFileType = 0;
            collection.PrepareForExport();
            ClassicAssert.AreEqual(collection.Tracks, 1);
            IList<MidiEvent> track0 = collection.GetTrackEvents(0);
            ClassicAssert.AreEqual(track0.Count, 8);
            ClassicAssert.IsTrue(MidiEvent.IsEndTrack(track0[track0.Count - 1]));
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
            ClassicAssert.AreEqual(collection.Tracks, 1);
            collection.MidiFileType = 1;
            collection.PrepareForExport();
            ClassicAssert.AreEqual(3, collection.Tracks, "Wrong number of tracks");
            IList<MidiEvent> track0 = collection.GetTrackEvents(0);
            ClassicAssert.AreEqual(track0.Count, 2);
            ClassicAssert.AreEqual(collection.GetTrackEvents(1).Count, 4);
            ClassicAssert.AreEqual(collection.GetTrackEvents(2).Count, 4);
            ClassicAssert.IsTrue(MidiEvent.IsEndTrack(track0[track0.Count - 1]));
        }
    }
}
