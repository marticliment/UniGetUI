using System.Text.Json;
using UniGetUI.Core.Data;

namespace UniGetUI.Core.SettingsEngine.Tests
{

    public sealed class SerializableTestSub
    {
        public SerializableTestSub(string s, int c) { sub = s; count = c; }
        public string sub { get; set; }
        public int count { get; set; }
    }
    public sealed class SerializableTest
    {
        public SerializableTest(string t, int c, SerializableTestSub s) { title = t; count = c; sub = s; }
        public string title { get; set; }
        public int count { get; set; }
        public SerializableTestSub sub { get; set; }
    }

    public class SettingsTest
    {
        private readonly string _testRoot;

        private readonly string _oldConfigurationDirectory;
        private readonly string _newConfigurationDirectory;

        public SettingsTest()
        {
            _testRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testRoot);

            // Configure the test environment.
            CoreData.TEST_DataDirectoryOverride = Path.Combine(_testRoot, "Data");
            _oldConfigurationDirectory = CoreData.UniGetUIDataDirectory;
            _newConfigurationDirectory = CoreData.UniGetUIUserConfigurationDirectory;

            // Ensure the new configuration directory is removed so that fresh installations are tested.
            if (Directory.Exists(_newConfigurationDirectory))
            {
                Directory.Delete(_newConfigurationDirectory, true);
            }
        }

        public void Dispose()
        {
            Directory.Delete(_testRoot, true);
        }

        private string GetNewSettingPath(string fileName) => Path.Combine(_newConfigurationDirectory, fileName);
        private string GetOldSettingsPath(string fileName) => Path.Combine(_oldConfigurationDirectory, fileName);


        [Fact]
        public void TestSettingsSaveToNewDirectory()
        {
            Settings.Set(Settings.K.FreshBoolSetting, true);
            Settings.SetValue(Settings.K.FreshValue, "test");

            Assert.True(File.Exists(GetNewSettingPath("FreshBoolSetting")));
            Assert.True(File.Exists(GetNewSettingPath("FreshValue")));
        }

        [Fact]
        public void TestExistingSettingsMigrateToNewDirectory()
        {
            string settingName = Settings.ResolveKey(Settings.K.Test7_Legacy);
            var oldPath = GetOldSettingsPath(settingName);
            File.WriteAllText(oldPath, "");

            var migratedValue = Settings.Get(Settings.K.Test7_Legacy);
            var newPath = GetNewSettingPath(settingName);
            var valueAfterMigration = Settings.Get(Settings.K.Test7_Legacy);

            Assert.True(migratedValue);
            Assert.True(valueAfterMigration);

            Assert.True(File.Exists(newPath));
            Assert.False(File.Exists(oldPath));
        }

        [Theory]
        [InlineData(Settings.K.Test1, true, false, false, true)]
        [InlineData(Settings.K.Test2, true, false, false, false)]
        [InlineData(Settings.K.Test3, true, false, true, true)]
        [InlineData(Settings.K.Test4, false, true, false, false)]
        [InlineData(Settings.K.Test5, false, false, false, false)]
        [InlineData(Settings.K.Test6, true, false, true, true)]
        public void TestBooleanSettings(Settings.K key, bool st1, bool st2, bool st3, bool st4)
        {
            string sName = Settings.ResolveKey(key);
            Settings.Set(key, st1);
            Assert.Equal(st1, Settings.Get(key));
            Assert.Equal(st1, File.Exists(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, sName)));

            Settings.Set(key, st2);
            Assert.Equal(st2, Settings.Get(key));
            Assert.Equal(st2, File.Exists(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, sName)));

            Settings.Set(key, st3);
            Assert.Equal(st3, Settings.Get(key));
            Assert.Equal(st3, File.Exists(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, sName)));

            Settings.Set(key, st4);
            Assert.Equal(st4, Settings.Get(key));
            Assert.Equal(st4, File.Exists(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, sName)));

            Settings.Set(key, false); // Cleanup
            Assert.False(Settings.Get(key));
            Assert.False(File.Exists(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, sName)));
        }

        [Theory]
        [InlineData(Settings.K.Test1, "RandomFirstValue", "RandomSecondValue", "", "RandomThirdValue")]
        [InlineData(Settings.K.Test2, "", "", "", "RandomThirdValue")]
        [InlineData(Settings.K.Test3, "RandomFirstValue", "    ", "\t", "RandomThirdValue")]
        [InlineData(Settings.K.Test4, "RandomFirstValue", "", "", "")]
        [InlineData(Settings.K.Test5, "\x00\x01\x02\u0003", "", "", "RandomThirdValue")]
        public void TestValueSettings(Settings.K key, string st1, string st2, string st3, string st4)
        {
            string sName = Settings.ResolveKey(key);
            Settings.SetValue(key, st1);
            Assert.Equal(st1, Settings.GetValue(key));
            Assert.Equal(st1 != "", File.Exists(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, sName)));

            Settings.SetValue(key, st2);
            Assert.Equal(st2, Settings.GetValue(key));
            Assert.Equal(st2 != "", File.Exists(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, sName)));

            Settings.SetValue(key, st3);
            Assert.Equal(st3, Settings.GetValue(key));
            Assert.Equal(st3 != "", File.Exists(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, sName)));

            Settings.SetValue(key, st4);
            Assert.Equal(st4, Settings.GetValue(key));
            Assert.Equal(st4 != "", File.Exists(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, sName)));

            Settings.Set(key, false); // Cleanup
            Assert.False(Settings.Get(key));
            Assert.Equal("", Settings.GetValue(key));
            Assert.False(File.Exists(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, sName)));
        }

        [Theory]
        [InlineData("lsTestSetting1", new[] { "UpdatedFirstValue", "RandomString1", "RandomTestValue", "AnotherRandomValue" }, new[] { 9, 15, 21, 1001, 4567 }, new[] { "itemA", "itemB", "itemC" })]
        [InlineData("lsktjgshdfsd", new[] { "newValue1", "updatedString", "emptyString", "randomSymbols@123" }, new[] { 42, 23, 17, 98765, 3482 }, new[] { "itemX", "itemY", "itemZ" })]
        [InlineData("lsª", new[] { "UniqueVal1", "NewVal2", "AnotherVal3", "TestVal4" }, new[] { 123, 456, 789, 321, 654 }, new[] { "item1", "item2", "item3" })]
        [InlineData("lsTestSettingEntry With A  Space", new[] { "ChangedFirstValue", "AlteredSecondVal", "TestedValue", "FinalVal" }, new[] { 23, 98, 456, 753, 951 }, new[] { "testA", "testB", "testC" })]
        [InlineData("lsVeryVeryLongTestSettingEntrySoTheClassCanReallyBeStressedOut", new[] { "newCharacterSet\x99\x01\x02", "UpdatedRandomValue", "TestEmptyString", "FinalTestValue" }, new[] { 0b11001100, 1234, 5678, 1000000 }, new[] { "finalTest1", "finalTest2", "finalTest3" })]
        public void TestListSettings(string SettingName, string[] ls1Array, int[] ls2Array, string[] ls3Array)
        {
            // Convert arrays to Lists manually
            List<string> ls1 = ls1Array.ToList();
            List<int> ls2 = ls2Array.ToList();
            List<SerializableTest> ls3 = [];
            foreach (var item in ls3Array.ToList())
            {
                ls3.Add(new SerializableTest(item, new Random().Next(), new SerializableTestSub(item + new Random().Next(), new Random().Next())));
            }

            Settings.ClearList(SettingName);
            Assert.Empty(Settings.GetList<object>(SettingName) ?? ["this shouldn't be null; something's wrong"]);
            Settings.SetList(SettingName, ls1);
            Assert.NotEmpty(Settings.GetList<string>(SettingName) ?? []);
            Assert.Equal(ls1[0], Settings.GetListItem<string>(SettingName, 0));
            Assert.Equal(ls1[2], Settings.GetListItem<string>(SettingName, 2));
            Assert.True(Settings.ListContains(SettingName, ls1[0]));
            Assert.False(Settings.ListContains(SettingName, "this is not a test case"));
            Assert.True(Settings.RemoveFromList(SettingName, ls1[0]));
            Assert.False(Settings.ListContains(SettingName, ls1[0]));
            Assert.False(Settings.RemoveFromList(SettingName, ls1[0]));
            Assert.False(Settings.ListContains(SettingName, ls1[0]));
            Assert.Equal(ls1[2], Settings.GetListItem<string>(SettingName, 1));
            Settings.AddToList(SettingName, "this is now a test case");
            Assert.Equal("this is now a test case", Settings.GetListItem<string>(SettingName, 3));
            Assert.Null(Settings.GetListItem<string>(SettingName, 4));

            Assert.Equal(Settings.GetListItem<string>(SettingName, 0), JsonSerializer.Deserialize<List<string>>(File.ReadAllText(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, $"{SettingName}.json")), Settings.SerializationOptions)[0]);
            Settings.ClearList(SettingName);
            Assert.Empty(Settings.GetList<object>(SettingName) ?? ["this shouldn't be null; something's wrong"]);

            Settings.SetList(SettingName, ls2);
            Assert.NotEmpty(Settings.GetList<int>(SettingName) ?? []);
            Assert.Equal(ls2[0], Settings.GetListItem<int>(SettingName, 0));
            Assert.False(Settings.ListContains(SettingName, -12000));
            Assert.True(Settings.ListContains(SettingName, ls2[3]));
            Assert.True(Settings.RemoveFromList(SettingName, ls2[0]));
            Assert.False(Settings.ListContains(SettingName, ls2[0]));
            Assert.False(Settings.RemoveFromList(SettingName, ls2[0]));
            Assert.False(Settings.ListContains(SettingName, ls2[0]));

            Settings.SetList(SettingName, ls3);
            Assert.Equal(ls3.Count, Settings.GetList<SerializableTest>(SettingName)?.Count);
            Assert.Equal(ls3[1].sub.sub, Settings.GetListItem<SerializableTest>(SettingName, 1)?.sub.sub);
            Assert.True(Settings.RemoveFromList(SettingName, ls3[0]));
            Assert.False(Settings.RemoveFromList(SettingName, ls3[0]));
            Assert.Equal(ls3[1].sub.sub, Settings.GetListItem<SerializableTest>(SettingName, 0)?.sub.sub);
            Settings.ClearList(SettingName); // Cleanup
            Assert.Empty(Settings.GetList<object>(SettingName) ?? ["this shouldn't be null; something's wrong"]);
            Assert.False(File.Exists(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, $"{SettingName}.json")));
        }

        [Theory]
        [InlineData(Settings.K.Test2, new[] { "UpdatedFirstValue", "RandomString1", "RandomTestValue", "AnotherRandomValue" }, new[] { 9, 15, 21, 1001, 4567 }, new[] { "itemA", "itemB", "itemC" })]
        [InlineData(Settings.K.Test3, new[] { "newValue1", "updatedString", "emptyString", "randomSymbols@123" }, new[] { 42, 23, 17, 98765, 3482 }, new[] { "itemX", "itemY", "itemZ" })]
        [InlineData(Settings.K.Test4, new[] { "UniqueVal1", "NewVal2", "AnotherVal3", "TestVal4" }, new[] { 123, 456, 789, 321, 654 }, new[] { "item1", "item2", "item3" })]
        [InlineData(Settings.K.Test5, new[] { "ChangedFirstValue", "AlteredSecondVal", "TestedValue", "FinalVal" }, new[] { 23, 98, 456, 753, 951 }, new[] { "testA", "testB", "testC" })]
        [InlineData(Settings.K.Test6, new[] { "newCharacterSet\x99\x01\x02", "UpdatedRandomValue", "TestEmptyString", "FinalTestValue" }, new[] { 0b11001100, 1234, 5678, 1000000 }, new[] { "finalTest1", "finalTest2", "finalTest3" })]
        public void TestDictionarySettings(Settings.K SettingName, string[] keyArray, int[] intArray, string[] strArray)
        {
            Dictionary<string, SerializableTest?> test = [];
            Dictionary<string, SerializableTest?> nonEmptyDictionary = [];
            nonEmptyDictionary["this should not be null; something's wrong"] = null;

            for (int idx = 0; idx < keyArray.Length; idx++)
            {
                test[keyArray[idx]] = new SerializableTest(
                    strArray[idx % strArray.Length],
                    intArray[idx % intArray.Length],
                    new SerializableTestSub(
                        strArray[(idx + 1) % strArray.Length],
                        intArray[(idx + 1) % intArray.Length]
                    )
                );
            }

            Settings.SetDictionaryItem(SettingName, "key", 12);
            Assert.Equal(12, Settings.GetDictionaryItem<string, int>(SettingName, "key"));
            Settings.SetDictionary(SettingName, test);
            Assert.Equal(JsonSerializer.Serialize(test, Settings.SerializationOptions), File.ReadAllText(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, $"{Settings.ResolveKey(SettingName)}.json")));
            Assert.Equal(test[keyArray[0]]?.sub.count, Settings.GetDictionary<string, SerializableTest?>(SettingName)?[keyArray[0]]?.sub.count);
            Assert.Equal(test[keyArray[1]]?.sub.count, Settings.GetDictionaryItem<string, SerializableTest?>(SettingName, keyArray[1])?.sub.count);
            Settings.SetDictionaryItem(SettingName, keyArray[0], test[keyArray[1]]);
            Assert.Equal(test[keyArray[1]]?.sub.count, Settings.GetDictionaryItem<string, SerializableTest?>(SettingName, keyArray[0])?.sub.count);
            Assert.NotEqual(test[keyArray[0]]?.sub.count, Settings.GetDictionaryItem<string, SerializableTest?>(SettingName, keyArray[0])?.sub.count);
            Assert.Equal(test[keyArray[1]]?.sub.count, Settings.GetDictionaryItem<string, SerializableTest?>(SettingName, keyArray[1])?.sub.count);
            Assert.Equal(test[keyArray[1]]?.count, Settings.SetDictionaryItem(
                SettingName,
                keyArray[0],
                new SerializableTest(
                    "this is not test data",
                    -12000,
                    new SerializableTestSub("this sub is not test data", -13000)
                )
            )?.count);
            Assert.Equal(-12000, Settings.GetDictionaryItem<string, SerializableTest?>(SettingName, keyArray[0])?.count);
            Assert.Equal("this is not test data", Settings.GetDictionaryItem<string, SerializableTest?>(SettingName, keyArray[0])?.title);
            Assert.Equal(-13000, Settings.GetDictionaryItem<string, SerializableTest?>(SettingName, keyArray[0])?.sub.count);
            Settings.SetDictionaryItem(SettingName, "this is not a test data key", test[keyArray[0]]);
            Assert.Equal(test[keyArray[0]]?.title, Settings.GetDictionaryItem<string, SerializableTest?>(SettingName, "this is not a test data key")?.title);
            Assert.Equal(test[keyArray[0]]?.sub.count, Settings.GetDictionaryItem<string, SerializableTest?>(SettingName, "this is not a test data key")?.sub.count);
            Assert.True(Settings.DictionaryContainsKey<string, SerializableTest?>(SettingName, "this is not a test data key"));
            Assert.True(Settings.DictionaryContainsValue<string, SerializableTest?>(SettingName, test[keyArray[0]]));
            Assert.NotNull(Settings.RemoveDictionaryKey<string, SerializableTest?>(SettingName, "this is not a test data key"));
            Assert.Null(Settings.RemoveDictionaryKey<string, SerializableTest?>(SettingName, "this is not a test data key"));
            Assert.False(Settings.DictionaryContainsKey<string, SerializableTest?>(SettingName, "this is not a test data key"));
            Assert.False(Settings.DictionaryContainsValue<string, SerializableTest?>(SettingName, test[keyArray[0]]));
            Assert.True(Settings.DictionaryContainsValue<string, SerializableTest?>(SettingName, test[keyArray[2]]));

            Assert.Equal(
                JsonSerializer.Serialize(Settings.GetDictionary<string, SerializableTest>(SettingName), Settings.SerializationOptions),
                File.ReadAllText(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, $"{Settings.ResolveKey(SettingName)}.json"))
            );

            Settings.ClearDictionary(SettingName); // Cleanup
            Assert.Empty(Settings.GetDictionary<string, SerializableTest?>(SettingName) ?? nonEmptyDictionary);
            Assert.False(File.Exists(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, $"{Settings.ResolveKey(SettingName)}.json")));
        }

        [Fact]
        public static void EnsureAllKeysResolve()
        {
            foreach (Settings.K key in Enum.GetValues(typeof(Settings.K)))
            {
                if(key is Settings.K.Unset) continue;
                Assert.NotEmpty(Settings.ResolveKey(key));
            }
        }
    }
}
