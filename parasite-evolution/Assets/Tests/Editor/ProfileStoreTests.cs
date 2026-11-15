using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace ParasiteEvolution.Tests
{
    public sealed class ProfileStoreTests
    {
        string folder,path;
        [SetUp] public void SetUp()
        {folder=Path.Combine(Path.GetTempPath(),"parasite-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);path=Path.Combine(folder,"profile.json");}
        [TearDown] public void TearDown(){if(Directory.Exists(folder))Directory.Delete(folder,true);}
        [Test]
        public void RejectedTransactionDoesNotWriteOrMutate()
        {
            var store=new ProfileStore(path);
            Assert.That(store.Apply(p=>{p.bank=10;return false;}),Is.False);
            Assert.That(store.Current.bank,Is.Zero);Assert.That(File.Exists(path),Is.False);
        }
        [Test]
        public void BrokenPrimaryRecoversThePreviousCommittedBackup()
        {
            var store=new ProfileStore(path);
            store.Apply(p=>{p.bank=90;return true;});store.Apply(p=>{p.bank=110;return true;});
            File.WriteAllText(path,"{broken");
            var recovered=new ProfileStore(path);Assert.That(recovered.Current.bank,Is.EqualTo(90));
        }
        [Test]
        public void CompleteNewerTemporaryRecordRecoversInterruptedReplacement()
        {
            var store=new ProfileStore(path);store.Apply(p=>true);
            var next=JsonUtility.FromJson<ProfileData>(JsonUtility.ToJson(store.Current));
            next.revision=2;next.bank=55;File.WriteAllText(path+".tmp",JsonUtility.ToJson(next));
            var recovered=new ProfileStore(path);Assert.That(recovered.Current.bank,Is.EqualTo(55));
            recovered.Apply(p=>{p.bank++;return true;});Assert.That(new ProfileStore(path).Current.bank,Is.EqualTo(56));
        }
        [Test]
        public void FutureSchemaIsPreservedAndNotSilentlyReset()
        {
            var profile=ProfileData.Fresh();profile.schemaVersion=2;
            var json=JsonUtility.ToJson(profile);File.WriteAllText(path,json);
            Assert.Throws<NotSupportedException>(()=>new ProfileStore(path));
            Assert.That(File.ReadAllText(path),Is.EqualTo(json));
        }
        [Test]
        public void FailedWriteDoesNotPublishTheNewInMemoryBalance()
        {
            var store=new ProfileStore(path);Directory.CreateDirectory(path+".tmp");
            Assert.That(()=>store.Apply(p=>{p.bank=100;return true;}),Throws.Exception);
            Assert.That(store.Current.bank,Is.Zero);Assert.That(store.Current.revision,Is.Zero);
        }
    }
}
