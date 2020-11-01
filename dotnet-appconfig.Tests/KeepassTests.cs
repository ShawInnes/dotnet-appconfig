using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using KeePassLib;
using KeePassLib.Keys;
using KeePassLib.Serialization;
using Xunit;

namespace ConfigManager.Tests
{
    public class KeepassTests
    {
        [Fact]
        public void CanReadKeepassFile()
        {
            var dbFile = "test.kdbx";
            var masterPassword = "topsecret";

            var location = typeof(KeepassTests).GetTypeInfo().Assembly.Location;
            var dirPath = Path.GetDirectoryName(location);

            var dbPath = Path.Combine(dirPath, dbFile);

            var ioConnectionInfo = new IOConnectionInfo {Path = dbPath};
            var compositeKey = new CompositeKey();
            compositeKey.AddUserKey(new KcpPassword(masterPassword));

            var db = new PwDatabase();
            db.Open(ioConnectionInfo, compositeKey, null);
            var entries = db.RootGroup.GetEntries(bIncludeSubGroupEntries: true).Where(p => p.ParentGroup.Name == "General").ToList();

            var entry = entries.Single(p => p.Strings.GetSafe("Title").ReadString() == "test-entry");
            var key = entry.Strings.GetSafe("Title").ReadString();
            var value = entry.Strings.GetSafe("Password").ReadString();

            db.Close();

            key.Should().Be("test-entry");
            value.Should().Be("test-value");
        }
    }
}
